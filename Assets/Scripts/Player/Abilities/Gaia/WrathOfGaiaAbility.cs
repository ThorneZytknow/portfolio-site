using BrawlerShared.Enums;
using BrawlerShared.Packets;

using UnityEngine;

using System.Collections;

/// <summary>
/// Especial da Gaïa: Wrath of Gaia (Chuva de Meteoros)
/// Invoca 5 pedras incandescentes do teto num raio largo e aplica dano massivo onde caírem (Random).
/// A invocadora fica exposta, exigindo setup inteligente.
/// </summary>
public class WrathOfGaiaAbility : MonoBehaviour
{
    public float stormDuration = 3f;
    public int meteorCount = 5;
    public float stormRadius = 8f;

    private float meteorDamage = 14f;
    private float meteorKnockback = 12f;
    private int ownerId;

    public GameObject meteorPrefab;

    public void Initialize(float dmg, float kb, int id)
    {
        meteorDamage = dmg;
        meteorKnockback = kb;
        ownerId = id;

        StartCoroutine(MeteorShowerRoutine());
    }

    private IEnumerator MeteorShowerRoutine()
    {
        Debug.Log($"[Gaïa] Chuva de Meteoros iniciada por {stormDuration}s");

        float timeBetweenMeteors = stormDuration / meteorCount;

        if (meteorPrefab == null)
        {
            Debug.LogWarning("[Gaïa] Prefab do Meteoro não assinalado no WrathOfGaiaAbility");
            Destroy(gameObject);
            yield break;
        }

        for (int i = 0; i < meteorCount; i++)
        {
            float randX = Random.Range(-stormRadius, stormRadius);
            Vector3 spawnPos = transform.position + new Vector3(randX, 0, 0);

            GameObject meteor = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);

            AbilityProjectile logic = meteor.GetComponent<AbilityProjectile>();
            if (logic != null)
            {
                AbilityData mockData = ScriptableObject.CreateInstance<AbilityData>();
                mockData.damage = meteorDamage;
                mockData.baseKnockback = meteorKnockback;
                mockData.projectileSpeed = 20f;
                mockData.hitboxDuration = 2f;
                mockData.hitstunDuration = 0.4f;

                logic.Initialize(mockData, Vector2.down, ownerId);
            }

            yield return new WaitForSeconds(timeBetweenMeteors);
        }

        Debug.Log("[Gaïa] Chuva de Meteoros encerrada.");
        Destroy(gameObject); // Destrói o gerenciador da nuvem
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position - new Vector3(stormRadius, 0, 0), transform.position + new Vector3(stormRadius, 0, 0));
    }
}
