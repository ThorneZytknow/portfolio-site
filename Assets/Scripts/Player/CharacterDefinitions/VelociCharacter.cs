using UnityEngine;

/// <summary>
/// Veloci - O Espadachim Ágil Acrobático (Speedster / Combo).
/// Ninja ultrarrápido com duas lâminas curtas, baseado em combos longos e cancelamentos de velocidade.
/// Muito leve, mas excelente poder de recuperação no ar.
/// </summary>
public class VelociCharacter : BaseCharacter
{
    [Header("Efeitos Passivos (Mobilidade Extra)")]
    // Como Veloci é acrobata, seu controle de direção no ar (Air Control)
    // ou saltos duplos seriam gerenciados pelo seu PlayerController estendido (ou via multiplicadores)
    public float extraAirSpeed = 1.3f;

    protected override void ApplyCharacterStats()
    {
        base.ApplyCharacterStats();

        // Passiva de Veloci: Move-se mais livremente no ar, porém é muito leve para voar longe
        // A velocidade de movimento no chão definida no CharacterData é alta, mas a massa é baixa.
    }

    public override void OnCharacterSpawned()
    {
        base.OnCharacterSpawned();

        // Spawn Effect: Folhas ao vento ou rastro ninja
        if (characterData.spawnEffectPrefab != null)
        {
            Instantiate(characterData.spawnEffectPrefab, transform.position, Quaternion.identity);
        }
    }


    public void TeleportDashRPC()
    {
        if (!controller.isLocalPlayer) return;
        Debug.Log("[Veloci] Ilusão de Passo! (Down-Special)");

        Vector3 spawnDashSlash = transform.position; // Posição atual de onde a ilusão ficará

        // Teleporta o jogador uma curta distância na direção que está olhando
        float dashDistance = 4f * Mathf.Sign(transform.localScale.x);
        transform.position += new Vector3(dashDistance, 0, 0);

        // Instancia o rastro e executa dano (IlusaoPassoAbility cuida da Hitbox nas costas do alvo)
        GameObject dashSlash = Instantiate(Resources.Load<GameObject>("IlusaoPassoSlashPrefab"), spawnDashSlash, Quaternion.identity);
        IlusaoPassoAbility logic = dashSlash.GetComponent<IlusaoPassoAbility>();
        if (logic != null)
        {
            logic.Initialize(abilitySystem.downAbility, controller.actorNumber);
        }
    }


    public void CinematicSlashRPC()
    {
        if (!controller.isLocalPlayer) return;
        Debug.Log("[Veloci] Especial das Mil Lâminas!");

        // Lança o jogador num dash gigantesco. Se acertar o inimigo, começa a cinematic
        // Delegado para script CinematicSlashAbility anexado ao projétil de engage ou hitbox
        GameObject cinematicTrigger = Instantiate(Resources.Load<GameObject>("MilCortesTrigger"), transform.position, Quaternion.identity);
        CinematicSlashAbility logic = cinematicTrigger.GetComponent<CinematicSlashAbility>();
        if (logic != null)
        {
            logic.Initialize(controller.actorNumber);
        }
    }
}
