using UnityEngine;

[CreateAssetMenu(fileName = "NewAbilityData", menuName = "Brawler/Ability Data")]
public class AbilityData : ScriptableObject
{
    [Header("Definição do Ataque")]
    public string abilityName;
    [Tooltip("Tipo do Input: Neutral, Up, Down, Special")]
    public string abilityType;
    public string description;

    [Header("Mecânicas de Combate")]
    public float damage = 10f; // Dano percentual base
    public float baseKnockback = 15f; // Força de nocaute
    public Vector2 knockbackDirection = new Vector2(1f, 0.5f); // Ângulo em que o inimigo será lançado
    public float hitboxRange = 1.2f; // Alcance do golpe (OverlapCircle)
    public float hitboxDuration = 0.2f; // Quantos frames/segundos a hitbox fica ativa
    public float cooldown = 0.5f; // Tempo de recarga após o uso
    public float staminaCost = 10f; // Custo de energia (se houver sistema de stamina)

    [Header("Propriedades Avançadas")]
    public bool hasArmor = false; // Define se o golpe possui Super Armor
    public int armorFrames = 0; // Por quantos frames a armadura fica ativa
    public string rpcMethodName; // Nome do RPC customizado, caso este ataque precise de lógica de rede extra
    public bool isProjectile = false; // Indica se invoca um projétil em vez de usar overlapSphere
    public float projectileSpeed = 0f;

    [Header("Feedbacks (Visuais e Gameplay)")]
    public GameObject vfxPrefabReference; // Partícula de acerto / invocação
    public AudioClip hitSound; // Efeito sonoro
    public int hitlagFrames = 4; // Congelamento da tela no impacto (para dar "peso")
    public float hitstunDuration = 0.3f; // Tempo que o alvo fica incapacitado
}
