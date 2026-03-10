using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Especial do Veloci: Especial das Mil Lâminas (Cinematic Slash)
/// Dá um dash invisível gigantesco. Se acertar o adversário no caminho, trava-o num stun
/// de 1.2s executando 12 cortes cinematográficos finalizados num único Hitlag massivo.
/// </summary>
public class CinematicSlashAbility : MonoBehaviourPun
{
    private float dashDistance = 8f;
    private int ownerId;

    private float slashDamage = 3f;
    private int slashCount = 12;
    private float finalKnockback = 25f;

    public void Initialize(int id)
    {
        ownerId = id;

        if (photonView.IsMine)
        {
            StartCoroutine(EngageDashRoutine());
        }
    }

    private IEnumerator EngageDashRoutine()
    {
        Debug.Log("[Veloci] Dash das Mil Lâminas Iniciado!");

        // 1. Dash BoxCast Rápido na direção em que o jogador está olhando
        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, new Vector2(1f, 2f), 0f, dir, dashDistance, LayerMask.GetMask("Player"));

        if (hit.collider != null)
        {
            PhotonView targetView = hit.collider.GetComponent<PhotonView>();
            if (targetView != null && targetView.OwnerActorNr != ownerId)
            {
                // Acertou alguém! Entra no combo cinematográfico
                Debug.Log($"[Veloci] Mil Cortes Conectou no alvo: {targetView.Owner.NickName}");

                // Transporta o Veloci fisicamente pra frente do alvo travado
                transform.position = hit.collider.transform.position + new Vector3(dir.x * -1f, 0, 0);

                // Rotina de 12 cortes em 1.2 segundos (0.1s por corte)
                for (int i = 0; i < slashCount - 1; i++) // Os 11 primeiros
                {
                    targetView.RPC("TakeAdvancedDamageRPC", RpcTarget.All, slashDamage, 0f, Vector2.zero, 2, 0.15f); // Stunlocked
                    // PhotonNetwork.Instantiate(efeitoDeCorteBrancoVermelho)
                    yield return new WaitForSeconds(0.1f);
                }

                // Último corte (O finalizador pesado)
                Debug.Log("[Veloci] FINALIZADOR!");
                targetView.RPC("TakeAdvancedDamageRPC", RpcTarget.All, slashDamage, finalKnockback, dir + new Vector2(0, 0.5f), 15, 1f); // 15 frames hitlag
            }
        }
        else
        {
            // Errou o engage
            Debug.Log("[Veloci] Dash falhou, ninguém no caminho.");
        }

        yield return new WaitForSeconds(0.3f); // Recuo

        PhotonNetwork.Destroy(gameObject);
    }
}
