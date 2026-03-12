using UnityEngine;

/// <summary>
/// Nyxara - A Feiticeira Sombria (Trickster / Zoner).
/// Manipula portais, clones e magia sombria. Extremamente leve e frágil, mas forte em reposicionamento.
/// </summary>
public class NyxaraCharacter : BaseCharacter
{
    [Header("Efeitos Especiais Passivos")]
    public float floatGravityMultiplier = 0.5f;

    protected override void ApplyCharacterStats()
    {
        base.ApplyCharacterStats();

        // Passiva da Nyxara: Gravidade mais leve (floaty jump) porque ela usa levitação
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale *= floatGravityMultiplier;
        }
    }

    public override void OnCharacterSpawned()
    {
        base.OnCharacterSpawned();

        // Spawn Effect: Nuvem de névoa ou fumaça roxa
        if (characterData.spawnEffectPrefab != null)
        {
            Instantiate(characterData.spawnEffectPrefab, transform.position, Quaternion.identity);
        }
    }


    public void PortalInversoRPC()
    {
        if (!controller.isLocalPlayer) return;
        Debug.Log("[Nyxara] Lançando Portal Inverso (Up-Special)");

        transform.position += new Vector3(0, 4f, 0);

        if (abilitySystem.upAbility.vfxPrefabReference != null)
        {
            GameObject portalAtk = Instantiate(abilitySystem.upAbility.vfxPrefabReference, transform.position, Quaternion.identity);
        }
    }


    public void SombraEspelhadaRPC()
    {
        if (!controller.isLocalPlayer) return;
        Debug.Log("[Nyxara] Criando Sombra Espelhada");

        if (abilitySystem.downAbility.vfxPrefabReference != null)
        {
            GameObject sombra = Instantiate(abilitySystem.downAbility.vfxPrefabReference, transform.position, Quaternion.identity);
            SombraEspelhadaAbility sombraLogic = sombra.GetComponent<SombraEspelhadaAbility>();
            if (sombraLogic != null)
            {
                sombraLogic.Initialize(abilitySystem.downAbility, controller.actorNumber);
            }
        }
    }


    public void AbismoDevoradorRPC()
    {
        if (!controller.isLocalPlayer) return;
        Debug.Log("[Nyxara] Conjurando Abismo Devorador");

        Vector3 spawnPos = transform.position + new Vector3(transform.localScale.x * 3f, 0f, 0f);

        if (abilitySystem.specialAbility.vfxPrefabReference != null)
        {
            GameObject abismo = Instantiate(abilitySystem.specialAbility.vfxPrefabReference, spawnPos, Quaternion.identity);

            AbismoDevoradorAbility logic = abismo.GetComponent<AbismoDevoradorAbility>();
            if (logic != null)
            {
                logic.Initialize(abilitySystem.specialAbility.damage, abilitySystem.specialAbility.baseKnockback, controller.actorNumber);
            }
        }
    }
}
