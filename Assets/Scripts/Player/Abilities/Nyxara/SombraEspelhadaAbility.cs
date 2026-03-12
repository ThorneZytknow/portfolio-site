using BrawlerShared.Enums;
using BrawlerShared.Packets;

using UnityEngine;

using System.Collections;

/// <summary>
/// Sombra Espelhada (Down-Special) da Nyxara.
/// Instancia um clone visual translúcido que permanece estático no mapa.
/// O clone copia o próximo ataque Neutro ou Especial da jogadora com dano reduzido (60%).
/// </summary>
public class SombraEspelhadaAbility : MonoBehaviour
{
    private AbilityData sourceAbility;
    private int ownerActorNumber;

    // Referência visual da sombra
    private GameObject cloneVisual;
    private bool hasClonedAttack = false;

    public void Initialize(AbilityData data, int ownerId)
    {
        sourceAbility = data;
        ownerActorNumber = ownerId;

        StartCoroutine(LifeTimer());
    }

    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(4f); // Duração da sombra definida no GDD
        Debug.Log("[Nyxara] Sombra espelhada dissipada.");
        Destroy(gameObject);
    }

    /// <summary>
    /// Chamado pelo AbilitySystem do jogador quando ele disparar um golpe compatível.
    /// </summary>
    public void ReplicateAttack(AbilityData attackToReplicate)
    {
        if (hasClonedAttack) return; // Só clona 1 vez por invocação

        hasClonedAttack = true;
        Debug.Log($"[Nyxara Sombra] Replicando ataque: {attackToReplicate.abilityName}");

        // Se for um ataque projetil (Fragmento das Trevas), a sombra lança também
        if (attackToReplicate.isProjectile && attackToReplicate.vfxPrefabReference != null)
        {
            // Instancia projétil copiando direção, porém partindo do transform do clone
            GameObject proj = Instantiate(attackToReplicate.vfxPrefabReference, transform.position, transform.rotation);
            AbilityProjectile logic = proj.GetComponent<AbilityProjectile>();

            if (logic != null)
            {
                // Criar um dado Mock com o dano e knockback reduzidos (60% conforme doc)
                AbilityData mockData = ScriptableObject.CreateInstance<AbilityData>();
                mockData.damage = attackToReplicate.damage * 0.6f;
                mockData.baseKnockback = attackToReplicate.baseKnockback * 0.6f;
                mockData.projectileSpeed = attackToReplicate.projectileSpeed;
                mockData.hitlagFrames = attackToReplicate.hitlagFrames;
                mockData.hitstunDuration = attackToReplicate.hitstunDuration;

                // Direção copia a rotação
                Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

                logic.Initialize(mockData, dir, ownerActorNumber);
            }
        }
        else
        {
            // Se for corpo-a-corpo, simula uma explosão de dano curto (OverlapCircle) no local do clone
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackToReplicate.hitboxRange, LayerMask.GetMask("Player"));

            foreach (Collider2D enemy in hitEnemies)
            {
                CombatSystem enemyCombat = enemy.GetComponent<CombatSystem>();
                if (enemyCombat != null)
                {
                    Vector2 hitDir = (enemy.transform.position - transform.position).normalized;
                    enemyCombat.ReceiveDamageLocally(
                        attackToReplicate.damage * 0.6f,
                        attackToReplicate.baseKnockback * 0.6f,
                        hitDir,
                        attackToReplicate.hitlagFrames,
                        attackToReplicate.hitstunDuration);
                }
            }
        }

        // Dissipa logo após copiar (Alto Risco/Recompensa - Combo setups)
        Destroy(gameObject);
    }
}
