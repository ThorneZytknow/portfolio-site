using BrawlerShared.Enums;
using BrawlerShared.Packets;

using UnityEngine;

using System.Collections;

/// <summary>
/// Habilidade Customizada do Veloci: Teleport Dash
/// Dá um dash rápido que ignora colisão e ao finalizar invoca uma lâmina fantasma na direção oposta (atrás do inimigo ultrapassado).
/// </summary>
public class IlusaoPassoAbility : MonoBehaviour
{
    private AbilityData sourceAbility;
    private int ownerActorNumber;
    private float slashDelay = 0.2f;

    public void Initialize(AbilityData data, int ownerId)
    {
        sourceAbility = data;
        ownerActorNumber = ownerId;
        StartCoroutine(PhantomSlashRoutine());
    }

    private IEnumerator PhantomSlashRoutine()
    {
        yield return new WaitForSeconds(slashDelay);

        Debug.Log("[Veloci] O rastro se materializa e ataca!");

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, sourceAbility.hitboxRange, LayerMask.GetMask("Player"));

        foreach (Collider2D enemy in hitEnemies)
        {
            PlayerController targetController = enemy.GetComponent<PlayerController>();
            if (targetController != null && targetController.actorNumber != ownerActorNumber)
            {
                Vector2 hitDirection = (transform.position - enemy.transform.position).normalized;
                hitDirection.y += 0.5f;

                if (LocalServerClient.Instance != null)
                {
                    var dmgPacket = new CombatDealDamage
                    {
                        TargetActorNumber = targetController.actorNumber,
                        DamageAmount = sourceAbility.damage,
                        BaseKnockback = sourceAbility.baseKnockback,
                        DirX = hitDirection.x,
                        DirY = hitDirection.y,
                        HitlagFrames = sourceAbility.hitlagFrames,
                        HitstunDuration = sourceAbility.hitstunDuration
                    };
                    LocalServerClient.Instance.SendPacket(PacketType.Combat_DealDamage, dmgPacket);
                }

                if (MatchResultsManager.Instance != null)
                {
                    MatchResultsManager.Instance.AddDamage(sourceAbility.damage);
                }
            }
        }

        Destroy(gameObject);
    }
}
