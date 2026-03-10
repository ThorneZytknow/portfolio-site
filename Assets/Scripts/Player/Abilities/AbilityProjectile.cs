using UnityEngine;
using Photon.Pun;

/// <summary>
/// Projétil comum utilizado por Habilidades que lançam magias, flechas, pedras, etc.
/// Ele carrega os dados da AbilityData para aplicar o dano quando colidir via OnTriggerEnter2D.
/// </summary>
public class AbilityProjectile : MonoBehaviourPun
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
        if (!photonView.IsMine) return; // Apenas quem lançou resolve a lógica do dano

        // Ignora a si mesmo ou aliados
        PhotonView hitView = collision.GetComponent<PhotonView>();
        if (hitView != null && hitView.OwnerActorNr != ownerActorNumber)
        {
            // Calcula direção (de onde bateu para onde o alvo vai voar)
            Vector2 hitDirection = (collision.transform.position - transform.position).normalized;
            hitDirection.y += 0.3f; // Ligeira inclinação para cima

            // Dispara o RPC de dano no alvo
            hitView.RPC("TakeAdvancedDamageRPC", RpcTarget.All,
                sourceAbility.damage,
                sourceAbility.baseKnockback,
                hitDirection,
                sourceAbility.hitlagFrames,
                sourceAbility.hitstunDuration);

            // Toca um efeito visual de colisão se existir
            if (sourceAbility.vfxPrefabReference != null)
            {
                PhotonNetwork.Instantiate(sourceAbility.vfxPrefabReference.name, transform.position, Quaternion.identity);
            }

            // Avisa o dono do ataque para contar estatística de dano no painel final
            if (MatchResultsManager.Instance != null)
            {
                MatchResultsManager.Instance.AddDamage(sourceAbility.damage);
            }

            // Destrói o projétil via Photon
            PhotonNetwork.Destroy(gameObject);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // Bateu no chão/parede, desaparece
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
