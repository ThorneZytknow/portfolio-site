using UnityEngine;
using Photon.Pun;
using System.Collections;

[RequireComponent(typeof(PhotonView))]
public class StockSystem : MonoBehaviourPunCallbacks
{
    [Header("Configurações de Partida")]
    public int startingStocks = 3;
    private int currentStocks;

    [Header("Referências")]
    private CombatSystem combatSystem;
    private Rigidbody2D rb;

    [Header("Respawn")]
    public float respawnDelay = 2f;
    public float invincibilityDuration = 3f;
    private bool isDead = false;

    private void Awake()
    {
        combatSystem = GetComponent<CombatSystem>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        currentStocks = startingStocks;

        // Atualiza a HUD inicial se existir
        if (HUDManager.Instance != null && photonView.IsMine)
        {
            HUDManager.Instance.UpdateStocks(photonView.OwnerActorNr, currentStocks);
        }
    }

    /// <summary>
    /// Chamado por triggers (Blast Zones / Kill Volumes) na borda da tela
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Só processa a eliminação no cliente que é dono do personagem
        if (!photonView.IsMine || isDead) return;

        if (collision.CompareTag("BlastZone"))
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        currentStocks--;

        Debug.Log($"[StockSystem] Jogador caiu! Vidas restantes: {currentStocks}");

        // Desativa física para não continuar caindo
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;

        // Oculta visualmente em todos os clientes
        photonView.RPC("SetPlayerVisibilityRPC", RpcTarget.All, false);

        if (currentStocks > 0)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            Debug.Log("[StockSystem] GAME OVER para este jogador.");

            // Avisa pela rede que este jogador perdeu (Game Over para ele)
            photonView.RPC("DeclareDefeatRPC", RpcTarget.All);
        }
    }

    [PunRPC]
    public void DeclareDefeatRPC()
    {
        // Remove a câmera de acompanhar um fantasma
        CameraController camController = FindObjectOfType<CameraController>();
        if (camController != null) camController.RemoveTarget(transform);

        if (MatchResultsManager.Instance != null)
        {
            // Se eu sou o jogador que perdeu as vidas
            if (photonView.IsMine)
            {
                MatchResultsManager.Instance.EndMatch(false); // Derrota
            }
            // Se eu NÃO sou o jogador (ou seja, foi meu oponente quem zerou as vidas)
            else
            {
                MatchResultsManager.Instance.EndMatch(true); // Vitória!
            }
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        // Zera o dano
        combatSystem.ResetDamage();

        // Move para o centro (Ponto de Spawn)
        transform.position = new Vector3(0, 5, 0); // Exemplo, o ideal é pegar do GameManager

        // Reativa visibilidade e física
        photonView.RPC("SetPlayerVisibilityRPC", RpcTarget.All, true);
        rb.isKinematic = false;
        isDead = false;

        // Inicia frames de invencibilidade (piscar)
        StartCoroutine(InvincibilityRoutine());
    }

    private IEnumerator InvincibilityRoutine()
    {
        // Neste período, ignora danos. O CombatSystem ou Collider pode checar essa variável.
        // Aqui simulamos uma proteção (para um jogo real, desativar layers ou checar flag no TakeDamageRPC)
        // No contexto desse código simplificado:
        combatSystem.isShielding = true; // Força um "escudo indestrutível"
        combatSystem.currentShieldHealth = 9999f;

        Debug.Log("[StockSystem] Invencibilidade de Respawn Ativa.");

        yield return new WaitForSeconds(invincibilityDuration);

        combatSystem.isShielding = false;
        combatSystem.currentShieldHealth = combatSystem.maxShieldHealth; // Reseta escudo normal
        Debug.Log("[StockSystem] Invencibilidade Acabou.");
    }

    [PunRPC]
    public void SetPlayerVisibilityRPC(bool isVisible)
    {
        // Ativa/desativa os renderers visuais, mas mantém o objeto ativo para escutar RPCs
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            rend.enabled = isVisible;
        }

        // Desativa colliders para não receber hit invisivelmente
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = isVisible;
        }
    }
}
