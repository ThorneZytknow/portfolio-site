using UnityEngine;

using System.Collections;


public class StockSystem : MonoBehaviour
{
    [Header("Configurações de Partida")]
    public int startingStocks = 3;
    private int currentStocks;

    [Header("Referências")]
    private CombatSystem combatSystem;
    private PlayerController playerController;
    private Rigidbody2D rb;

    [Header("Respawn")]
    public float respawnDelay = 2f;
    public float invincibilityDuration = 3f;
    private bool isDead = false;

    private void Awake()
    {
        combatSystem = GetComponent<CombatSystem>();
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        currentStocks = startingStocks;

        if (HUDManager.Instance != null && playerController.isLocalPlayer)
        {
            HUDManager.Instance.UpdateStocks(playerController.actorNumber, currentStocks);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!playerController.isLocalPlayer || isDead) return;

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

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateStocks(playerController.actorNumber, currentStocks);
        }

        if (MatchResultsManager.Instance != null) MatchResultsManager.Instance.AddDeath();

        rb.velocity = Vector2.zero;
        rb.isKinematic = true;

        SetPlayerVisibilityLocal(false);

        if (currentStocks > 0)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            Debug.Log("[StockSystem] GAME OVER para este jogador.");
            DeclareDefeatLocal();
        }
    }

    public void DeclareDefeatLocal()
    {
        CameraController camController = FindObjectOfType<CameraController>();
        if (camController != null) camController.RemoveTarget(transform);

        if (MatchResultsManager.Instance != null)
        {
            if (playerController.isLocalPlayer)
            {
                MatchResultsManager.Instance.EndMatch(false); // Derrota
            }
            else
            {
                MatchResultsManager.Instance.EndMatch(true); // Vitória!
            }
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        combatSystem.ResetDamage();
        transform.position = new Vector3(0, 5, 0);

        SetPlayerVisibilityLocal(true);
        rb.isKinematic = false;
        isDead = false;

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


    public void SetPlayerVisibilityLocal(bool isVisible)
    {
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
