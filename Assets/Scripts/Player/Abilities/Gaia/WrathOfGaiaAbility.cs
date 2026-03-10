using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Especial da Gaïa: Wrath of Gaia (Chuva de Meteoros)
/// Invoca 5 pedras incandescentes do teto num raio largo e aplica dano massivo onde caírem (Random).
/// A invocadora fica exposta, exigindo setup inteligente.
/// </summary>
public class WrathOfGaiaAbility : MonoBehaviourPun
{
    public float stormDuration = 3f;
    public int meteorCount = 5;
    public float stormRadius = 8f;

    private float meteorDamage = 14f;
    private float meteorKnockback = 12f;
    private int ownerId;

    public void Initialize(float dmg, float kb, int id)
    {
        meteorDamage = dmg;
        meteorKnockback = kb;
        ownerId = id;

        if (photonView.IsMine)
        {
            StartCoroutine(MeteorShowerRoutine());
        }
    }

    private IEnumerator MeteorShowerRoutine()
    {
        Debug.Log($"[Gaïa] Chuva de Meteoros iniciada por {stormDuration}s");

        float timeBetweenMeteors = stormDuration / meteorCount;

        for (int i = 0; i < meteorCount; i++)
        {
            // Sorteia posição dentro do raio x no teto
            float randX = Random.Range(-stormRadius, stormRadius);
            Vector3 spawnPos = transform.position + new Vector3(randX, 0, 0);

            // Instancia o projétil da pedra caindo
            GameObject meteor = PhotonNetwork.Instantiate("GaiaMeteorPrefab", spawnPos, Quaternion.identity);

            // O próprio projétil lidará com colisões ao cair usando a classe genérica AbilityProjectile
            AbilityProjectile logic = meteor.GetComponent<AbilityProjectile>();
            if (logic != null)
            {
                // Como não passamos o AbilityData inteiro pra não duplicar, construimos os atributos cruciais
                AbilityData mockData = ScriptableObject.CreateInstance<AbilityData>();
                mockData.damage = meteorDamage;
                mockData.baseKnockback = meteorKnockback;
                mockData.projectileSpeed = 20f; // Cai rápido
                mockData.hitboxDuration = 2f;
                mockData.hitstunDuration = 0.4f;

                // Direção pra baixo
                logic.Initialize(mockData, Vector2.down, ownerId);
            }

            yield return new WaitForSeconds(timeBetweenMeteors);
        }

        Debug.Log("[Gaïa] Chuva de Meteoros encerrada.");
        PhotonNetwork.Destroy(gameObject); // Destrói o gerenciador da nuvem
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position - new Vector3(stormRadius, 0, 0), transform.position + new Vector3(stormRadius, 0, 0));
    }
}
