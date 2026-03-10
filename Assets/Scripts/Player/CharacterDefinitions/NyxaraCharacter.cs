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

    [PunRPC]
    public void PortalInversoRPC()
    {
        if (!photonView.IsMine) return;
        Debug.Log("[Nyxara] Lançando Portal Inverso (Up-Special)");

        // Teleporta Nyxara levemente para cima e executa um meteor smash
        transform.position += new Vector3(0, 4f, 0); // Exemplo simplificado de teleporte vertical

        // Em seguida instancia uma hitbox que desce rapidamente (Dropkick)
        // Isso normalmente usaria uma animação ou o `MeleeHitboxRoutine` do AbilitySystem, mas como é custom:
        GameObject portalAtk = PhotonNetwork.Instantiate("PortalDropPrefab", transform.position, Quaternion.identity);
        // Inicializa com os dados do UpAbility
    }

    [PunRPC]
    public void SombraEspelhadaRPC()
    {
        if (!photonView.IsMine) return;
        Debug.Log("[Nyxara] Criando Sombra Espelhada");

        GameObject sombra = PhotonNetwork.Instantiate("SombraNyxaraPrefab", transform.position, Quaternion.identity);
        SombraEspelhadaAbility sombraLogic = sombra.GetComponent<SombraEspelhadaAbility>();
        if (sombraLogic != null)
        {
            sombraLogic.Initialize(abilitySystem.downAbility, photonView.OwnerActorNr);
        }
    }

    [PunRPC]
    public void AbismoDevoradorRPC()
    {
        if (!photonView.IsMine) return;
        Debug.Log("[Nyxara] Conjurando Abismo Devorador");

        // Invoca o buraco negro a uma distância média
        Vector3 spawnPos = transform.position + new Vector3(transform.localScale.x * 3f, 0f, 0f);
        GameObject abismo = PhotonNetwork.Instantiate("AbismoDevoradorPrefab", spawnPos, Quaternion.identity);

        AbismoDevoradorAbility logic = abismo.GetComponent<AbismoDevoradorAbility>();
        if (logic != null)
        {
            logic.Initialize(abilitySystem.specialAbility.damage, abilitySystem.specialAbility.baseKnockback, photonView.OwnerActorNr);
        }
    }
}
