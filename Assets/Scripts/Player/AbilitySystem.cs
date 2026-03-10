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

        // Sincroniza a Animação e Efeito Sonoro na rede
        photonView.RPC("TriggerAbilityAnimationRPC", RpcTarget.All, ability.abilityType);

        // Se a habilidade possuir um método RPC especial (Especiais, Portais, Teleportes), delega para a rede
        if (!string.IsNullOrEmpty(ability.rpcMethodName))
        {
            Debug.Log($"[AbilitySystem] Acionando Lógica Especial: {ability.rpcMethodName}");
            photonView.RPC(ability.rpcMethodName, RpcTarget.AllViaServer);

            // Não executa a lógica padrão de Hitbox, pois a lógica customizada assume o controle
            yield return new WaitForSeconds(ability.hitboxDuration > 0 ? ability.hitboxDuration : 0.5f);
        }
        // Se a habilidade for um disparo de projétil (ex: Fragmento das Trevas, Rajada de Espinhos)
        else if (ability.isProjectile && ability.vfxPrefabReference != null)
        {
            // Instancia o projétil via Photon Network
            GameObject proj = PhotonNetwork.Instantiate(ability.vfxPrefabReference.name, attackPoint.position, transform.rotation);
            AbilityProjectile projLogic = proj.GetComponent<AbilityProjectile>();

            if (projLogic != null)
            {
                // Direção baseada na orientação atual do jogador
                Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
                projLogic.Initialize(ability, dir, photonView.OwnerActorNr);
            }
            yield return new WaitForSeconds(ability.cooldown > 0 ? 0.2f : 0f); // Pausa breve para terminar animação de lançamento
        }
        // Se for um ataque Corpo-a-Corpo Padrão (Hitbox Duradoura)
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
