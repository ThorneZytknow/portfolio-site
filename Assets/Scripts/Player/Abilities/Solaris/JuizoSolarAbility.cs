using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Especial do Solaris: Juízo Solar
/// Um raio vertical carregado que atinge do teto até o chão na posição alvo.
/// Alto dano de Explosão e Knockback se canalizado com sucesso.
/// </summary>
public class JuizoSolarAbility : MonoBehaviourPun
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

        if (photonView.IsMine)
        {
            StartCoroutine(ChargeAndFireRoutine());
        }
    }

    private IEnumerator ChargeAndFireRoutine()
    {
        // Posição inicial visual (Aviso no chão/teto)
        Debug.Log($"[Solaris] Carregando Juízo Solar em {targetPosition}");

        // Mostra o pilar dourado no alvo por 1.5s
        if (sourceAbility.vfxPrefabReference != null)
        {
            PhotonNetwork.Instantiate(sourceAbility.vfxPrefabReference.name, targetPosition, Quaternion.identity);
        }

        yield return new WaitForSeconds(chargeTime);

        Debug.Log("[Solaris] Juízo Solar disparado!");

        // Raio caiu: OverlapCircle ou Box para causar dano explosivo na área do alvo
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(targetPosition, sourceAbility.hitboxRange, LayerMask.GetMask("Player"));

        bool hitSomeone = false;

        foreach (Collider2D enemy in hitEnemies)
        {
            PhotonView enemyView = enemy.GetComponent<PhotonView>();
            if (enemyView != null && enemyView.OwnerActorNr != ownerActorNumber)
            {
                hitSomeone = true;

                // Raio caindo de cima, esmaga o alvo para baixo (Spike/Meteor)
                Vector2 hitDirection = new Vector2(0f, -1f).normalized;

                enemyView.RPC("TakeAdvancedDamageRPC", RpcTarget.All,
                    sourceAbility.damage,
                    sourceAbility.baseKnockback,
                    hitDirection,
                    sourceAbility.hitlagFrames,
                    sourceAbility.hitstunDuration);

                // Incrementa estatística do lançador
                if (MatchResultsManager.Instance != null) MatchResultsManager.Instance.AddDamage(sourceAbility.damage);
            }
        }

        if (hitSomeone)
        {
            // Opcional: Efeito extra de cratera ou som de acerto massivo.
        }

        // Destrói o objeto controlador do especial via rede
        PhotonNetwork.Destroy(gameObject);
    }
}
