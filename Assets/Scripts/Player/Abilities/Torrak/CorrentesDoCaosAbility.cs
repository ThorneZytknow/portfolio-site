using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Especial do Torrak: Correntes do Caos (Side Special simultâneo)
/// Lança dois projéteis de correntes em lados opostos. Se ambas acertarem alvos diferentes,
/// puxa ambos para o centro causando dano de colisão extra.
/// </summary>
public class CorrentesDoCaosAbility : MonoBehaviourPun
{
    public float chainSpeed = 25f;
    public float maxRange = 10f;

    // Alvos presos
    private PhotonView leftTargetView;
    private PhotonView rightTargetView;

    private int ownerId;
    private float baseDmg = 10f;
    private float collisionDmg = 25f;

    public void Initialize(int id)
    {
        ownerId = id;

        if (photonView.IsMine)
        {
            StartCoroutine(ChainShotRoutine());
        }
    }

    private IEnumerator ChainShotRoutine()
    {
        Debug.Log("[Torrak] Disparando Correntes Duplas...");

        // Usamos Raycast para simular as correntes rapidamente
        RaycastHit2D hitLeft = Physics2D.Raycast(transform.position, Vector2.left, maxRange, LayerMask.GetMask("Player"));
        RaycastHit2D hitRight = Physics2D.Raycast(transform.position, Vector2.right, maxRange, LayerMask.GetMask("Player"));

        // Se acertar pela esquerda e não for o próprio Torrak
        if (hitLeft.collider != null)
        {
            PhotonView vLeft = hitLeft.collider.GetComponent<PhotonView>();
            if (vLeft != null && vLeft.OwnerActorNr != ownerId) leftTargetView = vLeft;
        }

        // Se acertar pela direita e não for o próprio Torrak
        if (hitRight.collider != null)
        {
            PhotonView vRight = hitRight.collider.GetComponent<PhotonView>();
            if (vRight != null && vRight.OwnerActorNr != ownerId) rightTargetView = vRight;
        }

        // Se prendeu nos DOIS lados simultaneamente: Puxão de Colisão!
        if (leftTargetView != null && rightTargetView != null)
        {
            Debug.Log("[Torrak] Correntes Prenderam dois alvos! COLISÃO IMINENTE!");

            // Aplica stun longo e dano massivo em ambos
            leftTargetView.RPC("TakeAdvancedDamageRPC", RpcTarget.All, baseDmg + collisionDmg, 30f, Vector2.right, 10, 1.5f);
            rightTargetView.RPC("TakeAdvancedDamageRPC", RpcTarget.All, baseDmg + collisionDmg, 30f, Vector2.left, 10, 1.5f);
        }
        else if (leftTargetView != null) // Só esquerda
        {
            leftTargetView.RPC("TakeAdvancedDamageRPC", RpcTarget.All, baseDmg, 15f, Vector2.left, 5, 0.5f);
        }
        else if (rightTargetView != null) // Só direita
        {
            rightTargetView.RPC("TakeAdvancedDamageRPC", RpcTarget.All, baseDmg, 15f, Vector2.right, 5, 0.5f);
        }
        else
        {
            Debug.Log("[Torrak] Correntes erraram!");
        }

        // Tempo de animação de recolhimento
        yield return new WaitForSeconds(0.5f);

        PhotonNetwork.Destroy(gameObject);
    }
}
