using BrawlerShared.Enums;
using BrawlerShared.Packets;

using UnityEngine;

using System.Collections;

/// <summary>
/// Especial do Solaris: Juízo Solar
/// Um raio vertical carregado que atinge do teto até o chão na posição alvo.
/// Alto dano de Explosão e Knockback se canalizado com sucesso.
/// </summary>
public class JuizoSolarAbility : MonoBehaviour
{
    private AbilityData sourceAbility;
    private int ownerActorNumber;

    // Configurações do Juízo
    private float chargeTime = 1.5f;
    private Vector2 targetPosition; // Posição clicada ou alvo

    public void Initialize(AbilityData data, Vector2 target, int ownerId)
    {
        sourceAbility = data;
        targetPosition = target;
        ownerActorNumber = ownerId;
        StartCoroutine(ChargeAndFireRoutine());
    }

    private IEnumerator ChargeAndFireRoutine()
    {
        Debug.Log($"[Solaris] Carregando Juízo Solar em {targetPosition}");

        if (sourceAbility.vfxPrefabReference != null)
        {
            Instantiate(sourceAbility.vfxPrefabReference, targetPosition, Quaternion.identity);
        }

        yield return new WaitForSeconds(chargeTime);

        Debug.Log("[Solaris] Juízo Solar disparado!");

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(targetPosition, sourceAbility.hitboxRange, LayerMask.GetMask("Player"));

        foreach (Collider2D enemy in hitEnemies)
        {
            PlayerController targetController = enemy.GetComponent<PlayerController>();
            if (targetController != null && targetController.actorNumber != ownerActorNumber)
            {
                Vector2 hitDirection = new Vector2(0f, -1f).normalized;

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

                if (MatchResultsManager.Instance != null) MatchResultsManager.Instance.AddDamage(sourceAbility.damage);
            }
        }

        Destroy(gameObject);
    }
}
