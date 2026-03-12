using BrawlerShared.Enums;
using BrawlerShared.Packets;

using UnityEngine;

using System.Collections;

/// <summary>
/// Especial do Torrak: Correntes do Caos (Side Special simultâneo)
/// Lança dois projéteis de correntes em lados opostos. Se ambas acertarem alvos diferentes,
/// puxa ambos para o centro causando dano de colisão extra.
/// </summary>
public class CorrentesDoCaosAbility : MonoBehaviour
{
    public float chainSpeed = 25f;
    public float maxRange = 10f;

    // Alvos presos
    private PlayerController leftTargetController;
    private PlayerController rightTargetController;

    private int ownerId;
    private float baseDmg = 10f;
    private float collisionDmg = 25f;

    public void Initialize(int id)
    {
        ownerId = id;
        StartCoroutine(ChainShotRoutine());
    }

    private IEnumerator ChainShotRoutine()
    {
        Debug.Log("[Torrak] Disparando Correntes Duplas...");

        RaycastHit2D hitLeft = Physics2D.Raycast(transform.position, Vector2.left, maxRange, LayerMask.GetMask("Player"));
        RaycastHit2D hitRight = Physics2D.Raycast(transform.position, Vector2.right, maxRange, LayerMask.GetMask("Player"));

        if (hitLeft.collider != null)
        {
            PlayerController pLeft = hitLeft.collider.GetComponent<PlayerController>();
            if (pLeft != null && pLeft.actorNumber != ownerId) leftTargetController = pLeft;
        }

        if (hitRight.collider != null)
        {
            PlayerController pRight = hitRight.collider.GetComponent<PlayerController>();
            if (pRight != null && pRight.actorNumber != ownerId) rightTargetController = pRight;
        }

        void SendDamage(int targetActor, float dmg, float kb, Vector2 dir, int lag, float stun)
        {
            if (LocalServerClient.Instance != null)
            {
                var dmgPacket = new CombatDealDamage
                {
                    TargetActorNumber = targetActor,
                    DamageAmount = dmg,
                    BaseKnockback = kb,
                    DirX = dir.x,
                    DirY = dir.y,
                    HitlagFrames = lag,
                    HitstunDuration = stun
                };
                LocalServerClient.Instance.SendPacket(PacketType.Combat_DealDamage, dmgPacket);
            }
        }

        if (leftTargetController != null && rightTargetController != null)
        {
            Debug.Log("[Torrak] Correntes Prenderam dois alvos! COLISÃO IMINENTE!");
            SendDamage(leftTargetController.actorNumber, baseDmg + collisionDmg, 30f, Vector2.right, 10, 1.5f);
            SendDamage(rightTargetController.actorNumber, baseDmg + collisionDmg, 30f, Vector2.left, 10, 1.5f);
        }
        else if (leftTargetController != null)
        {
            SendDamage(leftTargetController.actorNumber, baseDmg, 15f, Vector2.left, 5, 0.5f);
        }
        else if (rightTargetController != null)
        {
            SendDamage(rightTargetController.actorNumber, baseDmg, 15f, Vector2.right, 5, 0.5f);
        }
        else
        {
            Debug.Log("[Torrak] Correntes erraram!");
        }

        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }
}
