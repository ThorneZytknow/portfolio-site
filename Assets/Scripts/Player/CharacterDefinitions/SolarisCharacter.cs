using UnityEngine;

/// <summary>
/// Solaris - O Herói Solar (Balanced).
/// Guerreiro equilibrado com espada de energia. Foco na acessibilidade e bom controle de alcance médio.
/// </summary>
public class SolarisCharacter : BaseCharacter
{
    protected override void Start()
    {
        base.Start();
        // Ações específicas de Solaris ao iniciar (partículas, sons passivos)
    }

    public override void OnCharacterSpawned()
    {
        base.OnCharacterSpawned();

        // Exemplo: Ativar uma aura passiva na mão que segura a espada
        if (characterData.spawnEffectPrefab != null)
        {
            Instantiate(characterData.spawnEffectPrefab, transform.position, Quaternion.identity);
        }
    }


    public void JuizoSolarRPC()
    {
        Debug.Log("[Solaris] Iniciando RPC do Juízo Solar!");

        if (controller.isLocalPlayer)
        {
            // O especial cai onde o mouse aponta ou em uma distância fixa em frente ao jogador
            Vector3 spawnPos = transform.position + new Vector3(transform.localScale.x * 5f, 0f, 0f); // 5 unidades a frente

            // Instancia o objeto controlador do especial
            GameObject juizoObj = Instantiate(Resources.Load<GameObject>("JuizoSolarPrefab"), spawnPos, Quaternion.identity);

            JuizoSolarAbility juizoLogic = juizoObj.GetComponent<JuizoSolarAbility>();
            if (juizoLogic != null)
            {
                juizoLogic.Initialize(abilitySystem.specialAbility, spawnPos, controller.actorNumber);
            }
        }
    }
}
