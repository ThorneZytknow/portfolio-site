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


    public void GrappleThrowRPC()
    {
        if (!controller.isLocalPlayer) return;
        Debug.Log("[Torrak] Tentando Agarrão e Lançamento! (Up-Special)");

        Collider2D hit = Physics2D.OverlapCircle(transform.position, 1.5f, LayerMask.GetMask("Player"));
        if (hit != null && hit.gameObject != this.gameObject)
        {
            PlayerController targetController = hit.GetComponent<PlayerController>();
            if (targetController != null && LocalServerClient.Instance != null)
            {
                var dmgPacket = new BrawlerShared.Packets.CombatDealDamage
                {
                    TargetActorNumber = targetController.actorNumber,
                    DamageAmount = 28f,
                    BaseKnockback = 30f,
                    DirX = 0f,
                    DirY = 1f,
                    HitlagFrames = 6,
                    HitstunDuration = 1f
                };
                LocalServerClient.Instance.SendPacket(BrawlerShared.Enums.PacketType.Combat_DealDamage, dmgPacket);
            }
        }
    }


    public void CorrentesCaosRPC()
    {
        if (!controller.isLocalPlayer) return;
        Debug.Log("[Torrak] Lançando Correntes do Caos!");

        if (abilitySystem.specialAbility.vfxPrefabReference != null)
        {
            GameObject correntesController = Instantiate(abilitySystem.specialAbility.vfxPrefabReference, transform.position, Quaternion.identity);

            CorrentesDoCaosAbility logic = correntesController.GetComponent<CorrentesDoCaosAbility>();
            if (logic != null)
            {
                logic.Initialize(controller.actorNumber);
            }
        }
    }
}
