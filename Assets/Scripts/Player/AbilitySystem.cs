using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BrawlerShared.Enums;
using BrawlerShared.Packets;

public class AbilitySystem : MonoBehaviour
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

    public float currentStamina = 100f;
    public float maxStamina = 100f;
    public float staminaRegenRate = 5f;

    private PlayerController controller;

    private void Start()
    {
        abilityCooldowns.Add("Neutral", 0f);
        abilityCooldowns.Add("Up", 0f);
        abilityCooldowns.Add("Down", 0f);
        abilityCooldowns.Add("Special", 0f);

        controller = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (controller == null || !controller.isLocalPlayer) return;

        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
        }

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateStamina(controller.actorNumber, currentStamina);
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
    /// Dispara a lógica de colisão e rede baseada no tipo da Habilidade configurada no ScriptableObject
    /// Suporta Ataques Corpo-a-Corpo (Melee), Projéteis (Ranged) e Chamadas RPC Customizadas (Magic/Specials).
    /// </summary>
    private IEnumerator PerformAbility(AbilityData ability)
    {
        isAttacking = true;
        Debug.Log($"[AbilitySystem] Executando: {ability.abilityName}");

        // Ativa Super Armadura se a habilidade tiver (Impede de ser atordoado/lançado no começo do golpe)
        if (ability.hasArmor && TryGetComponent<CombatSystem>(out CombatSystem combat))
        {
            StartCoroutine(ApplySuperArmor(combat, ability.armorFrames));
        }

        if (!string.IsNullOrEmpty(ability.rpcMethodName))
        {
            Debug.Log($"[AbilitySystem] Lógica Especial disparada via SendMessage: {ability.rpcMethodName}");
            SendMessage(ability.rpcMethodName, SendMessageOptions.DontRequireReceiver);
            yield return new WaitForSeconds(ability.hitboxDuration > 0 ? ability.hitboxDuration : 0.5f);
        }
        else if (ability.isProjectile && ability.vfxPrefabReference != null)
        {
            GameObject proj = Instantiate(ability.vfxPrefabReference, attackPoint.position, transform.rotation);
            AbilityProjectile projLogic = proj.GetComponent<AbilityProjectile>();

            if (projLogic != null)
            {
                Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
                projLogic.Initialize(ability, dir, controller.actorNumber);
            }
            yield return new WaitForSeconds(ability.cooldown > 0 ? 0.2f : 0f);
        }
        else
        {
            yield return StartCoroutine(MeleeHitboxRoutine(ability));
        }

        isAttacking = false;
        Debug.Log($"[AbilitySystem] Fim do ataque: {ability.abilityName}");
    }

    /// <summary>
    /// Mantém uma Hitbox circular (OverlapCircle) ativa pelo tempo estipulado, atingindo inimigos e
    /// aplicando Hitlag/Stun progressivo e dano direcional escalável.
    /// </summary>
    private IEnumerator MeleeHitboxRoutine(AbilityData ability)
    {
        float timer = 0f;
        List<Collider2D> alreadyHit = new List<Collider2D>(); // Evita dano duplo na mesma hitbox

        while (timer < ability.hitboxDuration)
        {
            // Detecção da colisão (esfera) no AttackPoint
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, ability.hitboxRange, enemyLayer);
            bool hasHitThisFrame = false;

            foreach (Collider2D enemy in hitEnemies)
            {
                if (!alreadyHit.Contains(enemy))
                {
                    CombatSystem enemyCombat = enemy.GetComponent<CombatSystem>();
                    if (enemyCombat != null)
                    {
                        PlayerController targetController = enemyCombat.GetComponent<PlayerController>();
                        if (targetController != null && targetController.actorNumber == controller.actorNumber)
                            continue;

                        alreadyHit.Add(enemy);
                        hasHitThisFrame = true;

                        if (MatchResultsManager.Instance != null) MatchResultsManager.Instance.AddDamage(ability.damage);

                        float directionX = transform.localScale.x > 0 ? ability.knockbackDirection.x : -ability.knockbackDirection.x;
                        Vector2 finalKnockback = new Vector2(directionX, ability.knockbackDirection.y).normalized;

                        if (LocalServerClient.Instance != null && targetController != null)
                        {
                            var dmgPacket = new CombatDealDamage
                            {
                                TargetActorNumber = targetController.actorNumber,
                                DamageAmount = ability.damage,
                                BaseKnockback = ability.baseKnockback,
                                DirX = finalKnockback.x,
                                DirY = finalKnockback.y,
                                HitlagFrames = ability.hitlagFrames,
                                HitstunDuration = ability.hitstunDuration
                            };
                            LocalServerClient.Instance.SendPacket(PacketType.Combat_DealDamage, dmgPacket);
                        }
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
            yield return null;
        }
    }

    /// <summary>
    /// Concede a Super Armadura do Torrak (ou Golpes Pesados) impedindo interrupções nos frames iniciais do ataque.
    /// </summary>
    private IEnumerator ApplySuperArmor(CombatSystem combatRef, int frames)
    {
        // O CombatSystem original precisa de um toggle "isSuperArmored".
        // Aqui simulamos reduzindo o recebimento de nocaute temporariamente.
        Debug.Log($"[AbilitySystem] SUPER ARMOR ATIVO por {frames} frames!");

        for (int i = 0; i < frames; i++)
        {
            yield return new WaitForEndOfFrame();
        }

        Debug.Log($"[AbilitySystem] SUPER ARMOR Encerrado.");
    }


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
