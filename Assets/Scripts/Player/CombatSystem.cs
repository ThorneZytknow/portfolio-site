using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
public class CombatSystem : MonoBehaviourPunCallbacks
{
    [Header("Atributos de Combate")]
    public float maxHealth = 100f; // Percentual base antes de voar longe (Smash Bros style)
    private float currentDamagePercentage = 0f; // Porcentagem de dano recebida (Quanto maior, mais longe voa)

    [Header("Configurações de Ataque")]
    public Transform attackPoint;
    public float attackRange = 0.8f;
    public LayerMask enemyLayer;
    public float baseAttackDamage = 10f;
    public float knockbackForce = 15f;

    [Header("Efeitos e Feedback")]
    public float attackCooldown = 0.5f;
    private float nextAttackTime = 0f;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Se o PhotonView for deste jogador local e ele pressionar ataque (botão principal)
        if (photonView.IsMine && Input.GetButtonDown("Fire1") && Time.time >= nextAttackTime)
        {
            PerformAttack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    /// <summary>
    /// Realiza a detecção de hit (Hitbox) localmente e avisa os alvos que foram atingidos
    /// </summary>
    private void PerformAttack()
    {
        // Detecção em área (overlap circle)
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            // Pega o PhotonView do inimigo atingido para sincronizar a agressão
            PhotonView enemyPhotonView = enemy.GetComponent<PhotonView>();
            if (enemyPhotonView != null && !enemyPhotonView.IsMine)
            {
                // Calcula a direção do nocaute (de onde o ataque veio em relação ao alvo)
                Vector2 hitDirection = (enemy.transform.position - transform.position).normalized;
                hitDirection.y += 0.5f; // Adiciona elevação para o "lançamento" (Launch Angle)

                // Envia o dano pela rede via RPC usando a classe alvo (CombatSystem)
                enemyPhotonView.RPC("TakeDamageRPC", RpcTarget.All, baseAttackDamage, knockbackForce, hitDirection);
            }
        }

        Debug.Log("Jogador atacou!");
        // Aqui chamaria animações locais (animator.SetTrigger("Attack"))
    }

    /// <summary>
    /// RPC (Remote Procedure Call): Disparado em todas as instâncias do jogo para que vejam o jogador sofrendo dano
    /// e aplicar o Knockback fisicamente na máquina dona do alvo
    /// </summary>
    /// <param name="damage">Porcentagem/Dano aplicado</param>
    /// <param name="baseKnockback">Força base antes do multiplicador</param>
    /// <param name="hitDirection">Vetor de direção</param>
    [PunRPC]
    public void TakeDamageRPC(float damage, float baseKnockback, Vector2 hitDirection)
    {
        currentDamagePercentage += damage;

        // Feedback visual para todos (piscar em vermelho, partículas)
        Debug.Log($"Jogador sofreu {damage}% de dano. Dano Acumulado: {currentDamagePercentage}%");

        // Se este cliente for o DONO deste player atingido, aplica o empurrão (Física autoritativa do cliente dono)
        if (photonView.IsMine)
        {
            ApplyKnockback(baseKnockback, hitDirection);
        }
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

        // Verificação simples de "Morte/Ring Out"
        // Se o dano for extremo ou se o player sair dos limites (Bounds)
        CheckRingOut();
    }

    /// <summary>
    /// Verifica se o jogador voou para fora da tela/limites
    /// </summary>
    private void CheckRingOut()
    {
        // Por hora, apenas baseamos em acumular "dano crítico" para testes de lógica
        if (currentDamagePercentage >= 300f)
        {
            Debug.Log("JOGADOR ELIMINADO! RING OUT!");
            // Resetar posição, aplicar mortes e ressurgir (Respawn)
            if (photonView.IsMine)
            {
                currentDamagePercentage = 0f;
                // Exemplo: Transportar para o ponto 0
                transform.position = Vector3.zero;
                rb.velocity = Vector2.zero;
                photonView.RPC("UpdateRespawnUI", RpcTarget.AllViaServer);
            }
        }
    }

    [PunRPC]
    public void UpdateRespawnUI()
    {
        Debug.Log("Um jogador ressurgiu na arena!");
    }

    // Apenas para visualização do alcance do ataque na tela do Editor (Scene view)
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
