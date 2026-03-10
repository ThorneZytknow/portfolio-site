using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Especial da Nyxara: Abismo Devorador
/// Cria um buraco negro que suga (puxa fisicamente) inimigos para o centro
/// e após 2 segundos explode causando Knockback e dano massivos.
/// </summary>
public class AbismoDevoradorAbility : MonoBehaviourPun
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

        if (photonView.IsMine)
        {
            StartCoroutine(BlackHoleRoutine());
        }
    }

    private void Update()
    {
        // Se a máquina que invocou for dona, ela aplica a força de sucção física nos alvos locais
        if (!photonView.IsMine) return;

        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, suctionRadius, LayerMask.GetMask("Player"));
        foreach (var target in targets)
        {
            PhotonView targetView = target.GetComponent<PhotonView>();
            if (targetView != null && targetView.OwnerActorNr != ownerId)
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
            PhotonView enemyView = enemy.GetComponent<PhotonView>();
            if (enemyView != null && enemyView.OwnerActorNr != ownerId)
            {
                Vector2 hitDirection = (enemy.transform.position - transform.position).normalized;
                hitDirection.y += 0.5f; // Joga pra cima

                enemyView.RPC("TakeAdvancedDamageRPC", RpcTarget.All,
                    explosionDamage,
                    explosionKnockback,
                    hitDirection,
                    8, // Hitlag alto
                    0.5f); // Hitstun
            }
        }

        PhotonNetwork.Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, suctionRadius);
    }
}
