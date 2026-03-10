using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
public class CombatSystem : MonoBehaviourPunCallbacks
{
    [Header("Atributos de Combate")]
    public float currentDamagePercentage = 0f; // Porcentagem de dano recebida (Quanto maior, mais longe voa)

    [Header("Configurações de Escudo (Shield)")]
    public float maxShieldHealth = 50f;
    public float currentShieldHealth;
    public float shieldDepletionRate = 10f; // Dreno por segundo com o escudo ativo
    public float shieldRegenRate = 5f;
    public bool isShielding = false;
    public bool isShieldBroken = false;

    [Header("Estados (Stun e Lag)")]
    public bool isHitstunned = false; // Incapacitado temporariamente após tomar um golpe forte
    public bool isHitlagging = false; // Congelado momentaneamente no momento do impacto (efeito dramático)

    // Referência visual do escudo (ex: uma bolha)
    public GameObject shieldVisual;

    private Rigidbody2D rb;
    private PlayerController controller; // Referência para desativar inputs durante Stun
    private Vector2 storedVelocity; // Para retomar velocidade após o Hitlag

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
        currentShieldHealth = maxShieldHealth;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        // Não permite defesa se estiver estunado, em hitlag ou com escudo quebrado
        if (isHitstunned || isHitlagging || isShieldBroken)
        {
            if (isShielding) StopShield();
            return;
        }

        // Lógica de Ativação do Escudo (Botão Defesa / Fire3 ou Gatilho)
        if (Input.GetButton("Fire3") && currentShieldHealth > 0)
        {
            if (!isShielding) ActivateShield();

            // Drena o escudo ativamente
            currentShieldHealth -= shieldDepletionRate * Time.deltaTime;

            if (currentShieldHealth <= 0)
            {
                BreakShield();
            }
        }
        else
        {
            if (isShielding) StopShield();

            // Regenera o escudo quando não está em uso
            if (currentShieldHealth < maxShieldHealth && !isShieldBroken)
            {
                currentShieldHealth += shieldRegenRate * Time.deltaTime;
            }
        }

        // Atualiza a vida do escudo na UI
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateShield(photonView.OwnerActorNr, currentShieldHealth, isShieldBroken);
        }
    }

    /// <summary>
    /// Ativa a bolha de defesa e avisa aos outros pela rede
    /// </summary>
    private void ActivateShield()
    {
        isShielding = true;
        // controller.enabled = false; // Opcional: Impedir movimento enquanto defende
        if (shieldVisual != null) shieldVisual.SetActive(true);
        photonView.RPC("SyncShieldStateRPC", RpcTarget.Others, true);
    }

    /// <summary>
    /// Desativa a defesa
    /// </summary>
    private void StopShield()
    {
        isShielding = false;
        // if (!isShieldBroken && !isHitstunned) controller.enabled = true;
        if (shieldVisual != null) shieldVisual.SetActive(false);
        photonView.RPC("SyncShieldStateRPC", RpcTarget.Others, false);
    }

    /// <summary>
    /// Quebra a guarda (Shield Break) aplicando um Stun punitivo
    /// </summary>
    private void BreakShield()
    {
        StopShield();
        isShieldBroken = true;
        Debug.Log("[CombatSystem] SHIELD BREAK! Jogador atordoado!");

        // Stun longo (ex: 3 segundos)
        StartCoroutine(HitstunRoutine(3f));

        // Recupera o escudo parcialmente e remove o estado de quebra após o stun
        Invoke(nameof(RestoreBrokenShield), 3f);
    }

    private void RestoreBrokenShield()
    {
        isShieldBroken = false;
        currentShieldHealth = maxShieldHealth * 0.5f; // Volta com meia vida
    }

    [PunRPC]
    public void SyncShieldStateRPC(bool state)
    {
        isShielding = state;
        if (shieldVisual != null) shieldVisual.SetActive(state);
    }

    /// <summary>
    /// Nova função de dano avançada chamada pelo AbilitySystem, incorporando Hitlag e Hitstun
    /// </summary>
    [PunRPC]
    public void TakeAdvancedDamageRPC(float damage, float baseKnockback, Vector2 hitDirection, int hitlagFrames, float hitstunDuration)
    {
        // Se o escudo estiver ativo, o dano vai para o escudo e não há knockback/stun (mas pode haver hitlag)
        if (isShielding && currentShieldHealth > 0)
        {
            currentShieldHealth -= (damage * 1.5f); // Dano bônus ao escudo
            Debug.Log($"[CombatSystem] Escudo absorveu o golpe. Restante: {currentShieldHealth}");

            if (currentShieldHealth <= 0)
            {
                BreakShield();
            }
            return;
        }

        // Sem escudo: aplica o dano na porcentagem do jogador
        currentDamagePercentage += damage;
        Debug.Log($"[CombatSystem] Acerto direto! Dano Acumulado: {currentDamagePercentage}%");

        // Notifica o HUDManager se ele existir
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateDamage(photonView.OwnerActorNr, currentDamagePercentage);
        }

        // Aplica o Hitlag (Congelamento visual/físico para impacto) em todos os clientes
        StartCoroutine(HitlagRoutine(hitlagFrames, hitDirection, baseKnockback, hitstunDuration));
    }

    /// <summary>
    /// Pausa momentaneamente a ação (Hitlag) antes de aplicar o Knockback e o Hitstun.
    /// É isso que dá "peso" e "impacto" aos golpes estilo Smash Bros.
    /// </summary>
    private System.Collections.IEnumerator HitlagRoutine(int frames, Vector2 knockbackDir, float baseKnockback, float stunDuration)
    {
        isHitlagging = true;

        // Salva a velocidade atual e para o personagem no ar/chão
        storedVelocity = rb.velocity;
        rb.velocity = Vector2.zero;
        rb.isKinematic = true; // Impede gravidade durante o hitlag

        // Animação de dor (Hit)
        // animator.SetTrigger("TakeHit");

        // Espera o número de frames estipulado
        for (int i = 0; i < frames; i++)
        {
            yield return new WaitForEndOfFrame();
        }

        // Fim do Hitlag, restaura física
        isHitlagging = false;
        rb.isKinematic = false;
        rb.velocity = storedVelocity; // Opcional, o empurrão geralmente sobrescreve isso

        // Se eu sou o dono do personagem, eu sofro a física autoritativa de Knockback
        if (photonView.IsMine)
        {
            ApplyKnockback(baseKnockback, knockbackDir);

            // Inicia o Hitstun (tempo incapacitado)
            StartCoroutine(HitstunRoutine(stunDuration));
        }
    }

    /// <summary>
    /// Impede o jogador de realizar inputs ou usar habilidades enquanto o alvo estiver atordoado
    /// </summary>
    private System.Collections.IEnumerator HitstunRoutine(float duration)
    {
        isHitstunned = true;

        // Desativa controles de movimentação
        if (controller != null) controller.enabled = false;

        yield return new WaitForSeconds(duration);

        // Retorna o controle ao jogador
        isHitstunned = false;
        if (controller != null && !isShieldBroken && !isShielding) controller.enabled = true;

        // Debug.Log("[CombatSystem] Recuperou-se do Hitstun.");
    }

    /// <summary>
    /// Lógica estilo Brawler: Quanto maior o dano acumulado, maior a força aplicada
    /// </summary>
    private void ApplyKnockback(float baseKnockback, Vector2 direction)
    {
        // Multiplicador de nocaute escalona com a porcentagem atual
        // Fórmula simplificada (Smash-like): Base * (1 + (Porcentagem / 100))
        float knockbackMultiplier = 1f + (currentDamagePercentage / 100f);
        float finalForce = baseKnockback * knockbackMultiplier;

        // Zera a velocidade atual para garantir que o empurrão seja consistente, independentemente se o jogador estava se movendo
        rb.velocity = Vector2.zero;

        // Aplica o impulso
        rb.AddForce(direction.normalized * finalForce, ForceMode2D.Impulse);

        Debug.Log($"Knockback Final Aplicado: {finalForce} na direção {direction}");

    }

    /// <summary>
    /// Função para limpar o dano acumulado após morrer e ressurgir
    /// </summary>
    public void ResetDamage()
    {
        currentDamagePercentage = 0f;
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateDamage(photonView.OwnerActorNr, currentDamagePercentage);
        }
    }
}
