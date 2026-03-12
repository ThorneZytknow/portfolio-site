using BrawlerShared.Enums;
using BrawlerShared.Packets;

using UnityEngine;

using System.Collections;

/// <summary>
/// Especial da Nyxara: Abismo Devorador
/// Cria um buraco negro que suga (puxa fisicamente) inimigos para o centro
/// e após 2 segundos explode causando Knockback e dano massivos.
/// </summary>
public class AbismoDevoradorAbility : MonoBehaviour
{
    public float suctionRadius = 4f;
    public float suctionForce = 3f;
    public float lifetime = 2f;

    // Configurados via inicialização
    private float explosionDamage = 18f;
    private float explosionKnockback = 15f;
    private int ownerId;

    public void Initialize(float dmg, float kb, int id)
    {
        explosionDamage = dmg;
        explosionKnockback = kb;
        ownerId = id;

        StartCoroutine(BlackHoleRoutine());
    }

    private void Update()
    {
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, suctionRadius, LayerMask.GetMask("Player"));
        foreach (var target in targets)
        {
            PlayerController targetController = target.GetComponent<PlayerController>();
            if (targetController != null && targetController.actorNumber != ownerId)
            {
                Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
                if (targetRb != null)
                {
                    // Puxa em direção ao centro do abismo
                    Vector2 pullDirection = (transform.position - target.transform.position).normalized;
                    targetRb.AddForce(pullDirection * suctionForce * Time.deltaTime, ForceMode2D.Force);
                }
            }
        }
    }

    private IEnumerator BlackHoleRoutine()
    {
        yield return new WaitForSeconds(lifetime);

        // Explosão final
        Debug.Log("[Nyxara] Abismo Devorador explodiu!");
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, suctionRadius, LayerMask.GetMask("Player"));

        foreach (var enemy in hitEnemies)
        {
            CombatSystem enemyCombat = enemy.GetComponent<CombatSystem>();
            if (enemyCombat != null)
            {
                PlayerController targetController = enemyCombat.GetComponent<PlayerController>();
                if (targetController != null && targetController.actorNumber != ownerId)
                {
                    Vector2 hitDirection = (enemy.transform.position - transform.position).normalized;
                    hitDirection.y += 0.5f; // Joga pra cima

                    if (LocalServerClient.Instance != null)
                    {
                        var dmgPacket = new CombatDealDamage
                        {
                            TargetActorNumber = targetController.actorNumber,
                            DamageAmount = explosionDamage,
                            BaseKnockback = explosionKnockback,
                            DirX = hitDirection.x,
                            DirY = hitDirection.y,
                            HitlagFrames = 8,
                            HitstunDuration = 0.5f
                        };
                        LocalServerClient.Instance.SendPacket(PacketType.Combat_DealDamage, dmgPacket);
                    }
                }
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, suctionRadius);
    }
}
