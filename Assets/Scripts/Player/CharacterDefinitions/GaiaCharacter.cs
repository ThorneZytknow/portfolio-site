using UnityEngine;

/// <summary>
/// Gaïa - A Guardiã da Natureza (Controller / Zoner).
/// Manipula elementos para controlar a arena (raízes, espinhos, terremotos leves, meteoros).
/// Foca em fechar o espaço do oponente ao invés de golpes corpo-a-corpo massivos.
/// </summary>
public class GaiaCharacter : BaseCharacter
{
    [Header("Efeitos Passivos (Elementos)")]
    // Como Gaïa é elementalista, suas habilidades têm alcance maior.
    // Pode ter uma mecânica de regenerar saúde na grama, ou custo de stamina reduzido para certos elementos.
    public float staminaRegenMultiplier = 1.2f;

    protected override void ApplyCharacterStats()
    {
        base.ApplyCharacterStats();

        // Passiva da Gaïa: Regenera energia elemental mais rapidamente
        if (abilitySystem != null)
        {
            abilitySystem.staminaRegenRate *= staminaRegenMultiplier;
        }
    }

    public override void OnCharacterSpawned()
    {
        base.OnCharacterSpawned();

        // Spawn Effect: Raízes brotando do chão ou brilho esverdeado
        if (characterData.spawnEffectPrefab != null)
        {
            Instantiate(characterData.spawnEffectPrefab, transform.position, Quaternion.identity);
        }
    }


    public void RootSnareRPC()
    {
        if (!controller.isLocalPlayer) return;
        Debug.Log("[Gaïa] Invocando Raízes Aprisionadoras! (Down-Special)");

        // Invoca as raízes ligeiramente à frente do personagem para setupar traps
        Vector3 spawnPos = transform.position + new Vector3(transform.localScale.x * 2f, -1f, 0f); // chão
        GameObject raizes = Instantiate(Resources.Load<GameObject>("RaizesGaiaPrefab"), spawnPos, Quaternion.identity);
    }


    public void MeteorShowerRPC()
    {
        if (!controller.isLocalPlayer) return;
        Debug.Log("[Gaïa] Chamando a Ira da Natureza! (Special)");

        // Instancia o gestor da chuva de meteoros acima da arena
        Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y + 10f, 0f);
        GameObject shower = Instantiate(Resources.Load<GameObject>("GaiaMeteorShowerPrefab"), spawnPos, Quaternion.identity);

        WrathOfGaiaAbility logic = shower.GetComponent<WrathOfGaiaAbility>();
        if (logic != null)
        {
            // Pega o dano e knockback configurados no ScriptableObject
            logic.Initialize(abilitySystem.specialAbility.damage, abilitySystem.specialAbility.baseKnockback, controller.actorNumber);
        }
    }
}
