using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Habilidade Customizada do Veloci: Teleport Dash
/// Dá um dash rápido que ignora colisão e ao finalizar invoca uma lâmina fantasma na direção oposta (atrás do inimigo ultrapassado).
/// </summary>
public class IlusaoPassoAbility : MonoBehaviourPun
{
    private AbilityData sourceAbility;
    private int ownerActorNumber;
    private float slashDelay = 0.2f;

    public void Initialize(AbilityData data, int ownerId)
    {
        sourceAbility = data;
        ownerActorNumber = ownerId;

        if (photonView.IsMine)
        {
            StartCoroutine(PhantomSlashRoutine());
        }
    }

    private IEnumerator PhantomSlashRoutine()
    {
        yield return new WaitForSeconds(slashDelay);

        Debug.Log("[Veloci] O rastro se materializa e ataca!");

        // Detecção da colisão circular nas costas do alvo (onde a Ilusão ficou presa no ar)
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, sourceAbility.hitboxRange, LayerMask.GetMask("Player"));

        foreach (Collider2D enemy in hitEnemies)
        {
            PhotonView enemyView = enemy.GetComponent<PhotonView>();
            if (enemyView != null && enemyView.OwnerActorNr != ownerActorNumber)
            {
                // Joga o alvo para trás
                Vector2 hitDirection = (transform.position - enemy.transform.position).normalized;
                hitDirection.y += 0.5f;

                enemyView.RPC("TakeAdvancedDamageRPC", RpcTarget.All,
                    sourceAbility.damage,
                    sourceAbility.baseKnockback,
                    hitDirection,
                    sourceAbility.hitlagFrames,
                    sourceAbility.hitstunDuration);

                // Incrementa estatística
                if (MatchResultsManager.Instance != null)
                {
                    MatchResultsManager.Instance.AddDamage(sourceAbility.damage);
                }
            }
        }

        // Fim da Ilusão
        PhotonNetwork.Destroy(gameObject);
    }
}
