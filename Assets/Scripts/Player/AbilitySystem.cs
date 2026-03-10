using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PhotonView))]
public class AbilitySystem : MonoBehaviourPunCallbacks
{
    [Header("Kit de Habilidades do Personagem")]
    public AbilityData neutralAbility;
    public AbilityData upAbility;
    public AbilityData downAbility;
    public AbilityData specialAbility;

    [Header("Configurações de Combate")]
    public Transform attackPoint;
    public LayerMask enemyLayer;

    // Dicionário de Cooldowns para gerenciar as quatro habilidades independentemente
    private Dictionary<string, float> abilityCooldowns = new Dictionary<string, float>();
    private bool isAttacking = false;

    // Stamina (Para ataques especiais ou esquivas)
    public float currentStamina = 100f;
    public float maxStamina = 100f;
    public float staminaRegenRate = 5f;

    private void Start()
    {
        // Inicializa cooldowns
        abilityCooldowns.Add("Neutral", 0f);
        abilityCooldowns.Add("Up", 0f);
        abilityCooldowns.Add("Down", 0f);
        abilityCooldowns.Add("Special", 0f);
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        // Regeneração natural da Stamina (com limite)
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
        }

        // Atualiza a Stamina na HUD
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateStamina(photonView.OwnerActorNr, currentStamina);
        }

        ProcessAbilityInputs();
    }

    /// <summary>
    /// Escuta os inputs de ataque e os mapeia para as habilidades
    /// </summary>
    private void ProcessAbilityInputs()
    {
        if (isAttacking) return; // Impede sobreposição

        // Habilidade Neutra (Botão de Ataque sem direção, ou Esquerda/Direita)
        if (Input.GetButtonDown("Fire1") && Input.GetAxisRaw("Vertical") == 0)
        {
            TryExecuteAbility("Neutral", neutralAbility);
        }
        // Habilidade Cima (Ataque + Cima)
        else if (Input.GetButtonDown("Fire1") && Input.GetAxisRaw("Vertical") > 0.5f)
        {
            TryExecuteAbility("Up", upAbility);
        }
        // Habilidade Baixo (Ataque + Baixo)
        else if (Input.GetButtonDown("Fire1") && Input.GetAxisRaw("Vertical") < -0.5f)
        {
            TryExecuteAbility("Down", downAbility);
        }
        // Habilidade Especial (Botão de Ataque Secundário / Fire2)
        else if (Input.GetButtonDown("Fire2"))
        {
            TryExecuteAbility("Special", specialAbility);
        }
    }

    /// <summary>
    /// Tenta disparar uma habilidade verificando Cooldowns e Stamina
    /// </summary>
    private void TryExecuteAbility(string key, AbilityData ability)
    {
        if (ability == null) return;

        // Checa tempo de recarga (Cooldown)
        if (Time.time < abilityCooldowns[key])
        {
            Debug.Log($"[AbilitySystem] Habilidade {ability.abilityName} está em Cooldown!");
            return;
        }

        // Checa custo de energia
        if (currentStamina < ability.staminaCost)
        {
            Debug.Log($"[AbilitySystem] Sem energia para {ability.abilityName}!");
            return;
        }

        // Gasta os recursos
        currentStamina -= ability.staminaCost;
        abilityCooldowns[key] = Time.time + ability.cooldown;

        // Dispara a rotina principal
        StartCoroutine(PerformAbility(ability));
    }

    /// <summary>
    /// Corrotina responsável por gerenciar a duração ativa da Hitbox da habilidade
    /// </summary>
    private IEnumerator PerformAbility(AbilityData ability)
    {
        isAttacking = true;
        Debug.Log($"[AbilitySystem] Executando: {ability.abilityName}");

        // Ativa a animação de ataque correspondente aqui (via Animator.SetTrigger(ability.abilityType))
        // Sincronizando visualmente para a rede
        photonView.RPC("TriggerAbilityAnimationRPC", RpcTarget.All, ability.abilityType);

        float timer = 0f;
        List<Collider2D> alreadyHit = new List<Collider2D>(); // Evita dano duplo na mesma hitbox

        // Durante os frames ativos da hitbox (hitboxDuration)
        while (timer < ability.hitboxDuration)
        {
            // Detecção da colisão (esfera) no AttackPoint
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, ability.hitboxRange, enemyLayer);
            bool hasHitThisFrame = false;

            foreach (Collider2D enemy in hitEnemies)
            {
                if (!alreadyHit.Contains(enemy))
                {
                    PhotonView enemyView = enemy.GetComponent<PhotonView>();
                    // Só acerta se for outro player (evita fogo amigo se for em times, aqui focamos no ID do view)
                    if (enemyView != null && !enemyView.IsMine)
                    {
                        alreadyHit.Add(enemy);
                        hasHitThisFrame = true;

                        // Incrementa o número de mortes (Kills) do atacante, mas deixaremos que o alvo lide com isso
                        if (MatchResultsManager.Instance != null) MatchResultsManager.Instance.AddDamage(ability.damage);

                        // Direção baseada na rotação atual do jogador (invertendo o X se estiver virado para a esquerda)
                        float directionX = transform.localScale.x > 0 ? ability.knockbackDirection.x : -ability.knockbackDirection.x;
                        Vector2 finalKnockback = new Vector2(directionX, ability.knockbackDirection.y).normalized;

                        // Envia os dados avançados pelo RPC do CombatSystem do alvo
                        enemyView.RPC("TakeAdvancedDamageRPC", RpcTarget.All,
                            ability.damage,
                            ability.baseKnockback,
                            finalKnockback,
                            ability.hitlagFrames,
                            ability.hitstunDuration);
                    }
                }
            }

            // Aplica Hitlag no Atacante
            if (hasHitThisFrame)
            {
                for (int i = 0; i < ability.hitlagFrames; i++)
                {
                    yield return new WaitForEndOfFrame();
                }
            }

            timer += Time.deltaTime;
            yield return null; // Espera o próximo frame
        }

        isAttacking = false;
        Debug.Log($"[AbilitySystem] Fim do ataque: {ability.abilityName}");
    }

    [PunRPC]
    public void TriggerAbilityAnimationRPC(string animType)
    {
        // Aqui chamamos o animador para tocar a animação correspondente em todos os clientes
        // ex: GetComponent<Animator>().SetTrigger("Attack_" + animType);
        Debug.Log($"[AbilitySystem] Todos veem a animação do ataque: {animType}");
    }

    // Para verificação de alcance no Editor
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.magenta;
        // Para visualização, desenha o neutro se existir
        if (neutralAbility != null)
            Gizmos.DrawWireSphere(attackPoint.position, neutralAbility.hitboxRange);
        else
            Gizmos.DrawWireSphere(attackPoint.position, 1f);
    }
}
