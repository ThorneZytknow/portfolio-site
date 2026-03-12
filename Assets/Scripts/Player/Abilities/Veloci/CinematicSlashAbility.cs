using BrawlerShared.Enums;
using BrawlerShared.Packets;

using UnityEngine;

using System.Collections;

/// <summary>
/// Especial do Veloci: Especial das Mil Lâminas (Cinematic Slash)
/// Dá um dash invisível gigantesco. Se acertar o adversário no caminho, trava-o num stun
/// de 1.2s executando 12 cortes cinematográficos finalizados num único Hitlag massivo.
/// </summary>
public class CinematicSlashAbility : MonoBehaviour
{
    private float dashDistance = 8f;
    private int ownerId;

    private float slashDamage = 3f;
    private int slashCount = 12;
    private float finalKnockback = 25f;

    public void Initialize(int id)
    {
        ownerId = id;
        StartCoroutine(EngageDashRoutine());
    }

    private IEnumerator EngageDashRoutine()
    {
        Debug.Log("[Veloci] Dash das Mil Lâminas Iniciado!");

        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, new Vector2(1f, 2f), 0f, dir, dashDistance, LayerMask.GetMask("Player"));

        if (hit.collider != null)
        {
            CombatSystem targetCombat = hit.collider.GetComponent<CombatSystem>();
            if (targetCombat != null)
            {
                PlayerController targetController = hit.collider.GetComponent<PlayerController>();
                if (targetController != null && targetController.actorNumber != ownerId)
                {
                    Debug.Log($"[Veloci] Mil Cortes Conectou no alvo!");

                    transform.position = hit.collider.transform.position + new Vector3(dir.x * -1f, 0, 0);

                    for (int i = 0; i < slashCount - 1; i++) // Os 11 primeiros
                    {
                        if (LocalServerClient.Instance != null)
                        {
                            var dmgPacket = new CombatDealDamage
                            {
                                TargetActorNumber = targetController.actorNumber,
                                DamageAmount = slashDamage,
                                BaseKnockback = 0f,
                                DirX = 0f,
                                DirY = 0f,
                                HitlagFrames = 2,
                                HitstunDuration = 0.15f
                            };
                            LocalServerClient.Instance.SendPacket(PacketType.Combat_DealDamage, dmgPacket);
                        }

                        yield return new WaitForSeconds(0.1f);
                    }

                    // Último corte
                    Debug.Log("[Veloci] FINALIZADOR!");
                    Vector2 finalDir = dir + new Vector2(0, 0.5f);
                    if (LocalServerClient.Instance != null)
                    {
                        var dmgPacket = new CombatDealDamage
                        {
                            TargetActorNumber = targetController.actorNumber,
                            DamageAmount = slashDamage,
                            BaseKnockback = finalKnockback,
                            DirX = finalDir.x,
                            DirY = finalDir.y,
                            HitlagFrames = 15,
                            HitstunDuration = 1f
                        };
                        LocalServerClient.Instance.SendPacket(PacketType.Combat_DealDamage, dmgPacket);
                    }
                }
            }
        }
        else
        {
            Debug.Log("[Veloci] Dash falhou, ninguém no caminho.");
        }

        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }
}
