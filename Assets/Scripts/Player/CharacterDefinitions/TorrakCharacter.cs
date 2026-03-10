using UnityEngine;

/// <summary>
/// Torrak - O Bruto Selvagem (Heavy / Grappler).
/// Guerreiro com correntes e força absurda. Muito pesado e resistente ao impacto, mas sofre na velocidade.
/// </summary>
public class TorrakCharacter : BaseCharacter
{
    [Header("Efeitos Passivos (Super Armadura)")]
    // Como Torrak é pesado, ele ignora stun (Hitstun) quando estiver usando algumas habilidades pesadas.
    // Isso é refletido nas AbilityData marcadas como "hasArmor = true", mas ele pode ter
    // um redutor de dano base.
    public float baseDamageResistance = 0.85f; // Toma 15% a menos de dano padrão

    protected override void ApplyCharacterStats()
    {
        base.ApplyCharacterStats();

        // Passiva do Torrak: Resistente no chão, cai feito pedra no ar
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale *= 1.2f; // Cai rápido (fast faller)
        }
    }

    public override void OnCharacterSpawned()
    {
        base.OnCharacterSpawned();

        // Spawn Effect: Impacto no chão (Rachadura)
        if (characterData.spawnEffectPrefab != null)
        {
            Instantiate(characterData.spawnEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    [PunRPC]
    public void GrappleThrowRPC()
    {
        if (!photonView.IsMine) return;
        Debug.Log("[Torrak] Tentando Agarrão e Lançamento! (Up-Special)");

        // Lógica de comando para pegar inimigo adjacente:
        Collider2D hit = Physics2D.OverlapCircle(transform.position, 1.5f, LayerMask.GetMask("Player"));
        if (hit != null && hit.gameObject != this.gameObject)
        {
            // Lança o jogador para cima aplicando dano pesado
            PhotonView enemyView = hit.GetComponent<PhotonView>();
            if (enemyView != null)
            {
                // Multiplicador de gravidade no lançamento (Spike para Cima)
                enemyView.RPC("TakeAdvancedDamageRPC", RpcTarget.All, 28f, 30f, new Vector2(0, 1), 6, 1f);
            }
        }
    }

    [PunRPC]
    public void CorrentesCaosRPC()
    {
        if (!photonView.IsMine) return;
        Debug.Log("[Torrak] Lançando Correntes do Caos!");

        // Instancia duas correntes simultâneas (uma para cada lado)
        // A lógica principal que fará os Raycasts pros dois lados fica em apenas 1 objeto mestre invisível
        GameObject correntesController = PhotonNetwork.Instantiate("TorrakChainControllerPrefab", transform.position, Quaternion.identity);

        CorrentesDoCaosAbility logic = correntesController.GetComponent<CorrentesDoCaosAbility>();
        if (logic != null)
        {
            logic.Initialize(photonView.OwnerActorNr);
        }
    }
}
