using BrawlerShared.Enums;
using BrawlerShared.Packets;

using UnityEngine;


/// <summary>
/// Projétil comum utilizado por Habilidades que lançam magias, flechas, pedras, etc.
/// Ele carrega os dados da AbilityData para aplicar o dano quando colidir via OnTriggerEnter2D.
/// </summary>
public class AbilityProjectile : MonoBehaviour
{
    private AbilityData sourceAbility;
    private int ownerActorNumber;
    private Rigidbody2D rb;

    public void Initialize(AbilityData data, Vector2 direction, int ownerId)
    {
        sourceAbility = data;
        ownerActorNumber = ownerId;

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = direction * sourceAbility.projectileSpeed;
        }

        // Destrói o projétil após o tempo de vida máximo caso não acerte nada
        Destroy(gameObject, sourceAbility.hitboxDuration > 0 ? sourceAbility.hitboxDuration : 5f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ignora a si mesmo ou aliados
        CombatSystem enemyCombat = collision.GetComponent<CombatSystem>();
        if (enemyCombat != null)
        {
            PlayerController targetController = enemyCombat.GetComponent<PlayerController>();
            if (targetController != null && targetController.actorNumber == ownerActorNumber)
                return; // Fogo amigo não

            Vector2 hitDirection = (collision.transform.position - transform.position).normalized;
            hitDirection.y += 0.3f;

            if (LocalServerClient.Instance != null && targetController != null)
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

            if (sourceAbility.vfxPrefabReference != null)
            {
                Instantiate(sourceAbility.vfxPrefabReference, transform.position, Quaternion.identity);
            }

            if (MatchResultsManager.Instance != null)
            {
                MatchResultsManager.Instance.AddDamage(sourceAbility.damage);
            }

            Destroy(gameObject);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
