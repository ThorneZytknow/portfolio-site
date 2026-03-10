using UnityEngine;
using Photon.Pun;

/// <summary>
/// Classe base abstrata para todos os personagens do jogo.
/// Ela conecta as informações de CharacterData ao objeto do jogador
/// e gerencia o registro inicial dos dados de mecânica.
/// </summary>
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(CombatSystem))]
[RequireComponent(typeof(AbilitySystem))]
public abstract class BaseCharacter : MonoBehaviourPunCallbacks
{
    [Header("Dados do Personagem")]
    public CharacterData characterData;

    protected PlayerController controller;
    protected CombatSystem combat;
    protected AbilitySystem abilitySystem;

    protected virtual void Awake()
    {
        controller = GetComponent<PlayerController>();
        combat = GetComponent<CombatSystem>();
        abilitySystem = GetComponent<AbilitySystem>();
    }

    protected virtual void Start()
    {
        if (characterData != null)
        {
            ApplyCharacterStats();
        }
        else
        {
            Debug.LogError($"[BaseCharacter] CharacterData não assinalado no prefab do {gameObject.name}");
        }
    }

    /// <summary>
    /// Aplica os atributos de velocidade, pulo, peso e dano definidos no ScriptableObject
    /// para os controladores físicos deste script instanciado.
    /// </summary>
    protected virtual void ApplyCharacterStats()
    {
        controller.moveSpeed = characterData.moveSpeed;
        controller.jumpForce = characterData.jumpForce;

        // Em um sistema mais complexo de física, 'weight' altera o multiplier do rigidbody
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Personagens mais pesados voam menos longe (maior massa)
            // Aqui estamos normalizando onde 100 de peso = 1f de massa.
            rb.mass = characterData.weight / 100f;
        }

        // Se houver um multiplicador de ataque, seria repassado ao Animator ou Cooldowns aqui
    }

    // Opcional: Cada personagem pode sobrescrever este método para lógicas exclusivas no Spawn
    public virtual void OnCharacterSpawned()
    {
        Debug.Log($"[BaseCharacter] {characterData.characterName} (Arquétipo: {characterData.archetype}) entrou na arena!");
    }
}
