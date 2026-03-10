using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Sombra Espelhada (Down-Special) da Nyxara.
/// Instancia um clone visual translúcido que permanece estático no mapa.
/// O clone copia o próximo ataque Neutro ou Especial da jogadora com dano reduzido (60%).
/// </summary>
public class SombraEspelhadaAbility : MonoBehaviourPun
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

        // Se eu sou o dono do clone, inicio o timer de desaparecimento (4s)
        if (photonView.IsMine)
        {
            StartCoroutine(LifeTimer());
            // Inscreve a sombra para escutar eventos de ataque do dono
            // (Para protótipo simples, o AbilitySystem poderia chamar um evento `OnAttackFired` e a sombra assinar)
        }
    }

    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(4f); // Duração da sombra definida no GDD
        Debug.Log("[Nyxara] Sombra espelhada dissipada.");
        PhotonNetwork.Destroy(gameObject);
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
            GameObject proj = PhotonNetwork.Instantiate(attackToReplicate.vfxPrefabReference.name, transform.position, transform.rotation);
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
                PhotonView enemyView = enemy.GetComponent<PhotonView>();
                if (enemyView != null && enemyView.OwnerActorNr != ownerActorNumber)
                {
                    Vector2 hitDir = (enemy.transform.position - transform.position).normalized;
                    enemyView.RPC("TakeAdvancedDamageRPC", RpcTarget.All,
                        attackToReplicate.damage * 0.6f,
                        attackToReplicate.baseKnockback * 0.6f,
                        hitDir,
                        attackToReplicate.hitlagFrames,
                        attackToReplicate.hitstunDuration);
                }
            }
        }

        // Dissipa logo após copiar (Alto Risco/Recompensa - Combo setups)
        PhotonNetwork.Destroy(gameObject);
    }
}
