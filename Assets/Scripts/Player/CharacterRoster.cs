using UnityEngine;
using System.Collections.Generic;

public class CharacterRoster : MonoBehaviour
{
    // Singleton para fácil acesso ao elenco de personagens
    public static CharacterRoster Instance;

    [Header("Elenco Completo")]
    [Tooltip("Todos os personagens registrados no jogo (ScriptableObjects)")]
    public List<CharacterData> allCharacters = new List<CharacterData>();

    // Lista de IDs dos personagens que o jogador atual desbloqueou no PlayFab
    public HashSet<string> unlockedCharacterIds { get; private set; } = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private void OnEnable()
    {
        PlayFabAuthManager.OnLoginSuccessEvent += UpdateUnlockedCharacters;
    }

    private void OnDisable()
    {
        PlayFabAuthManager.OnLoginSuccessEvent -= UpdateUnlockedCharacters;
    }

    private void Start()
    {
        // Caso o login já tenha ocorrido antes deste script iniciar
        if (PlayFabAuthManager.Instance != null && PlayFabAuthManager.Instance.IsLoggedIn)
        {
            UpdateUnlockedCharacters();
        }
    }

    /// <summary>
    /// Verifica o inventário do PlayFab para determinar quais personagens o jogador possui.
    /// Chamado após o EconomyManager carregar o inventário.
    /// </summary>
    public void UpdateUnlockedCharacters()
    {
        unlockedCharacterIds.Clear();

        // No modelo F2P, geralmente existe um personagem ou mais liberados como "Base"
        // Adicionando um ID de exemplo como personagem inicial
        unlockedCharacterIds.Add("Char_BaseFighter");

        if (EconomyManager.Instance != null && EconomyManager.Instance.PlayerInventory != null)
        {
            foreach (var item in EconomyManager.Instance.PlayerInventory)
            {
                // Verifica se a classe do item no PlayFab é "Character"
                if (item.ItemClass == "Character")
                {
                    unlockedCharacterIds.Add(item.ItemId);
                }
            }
        }

        Debug.Log($"[CharacterRoster] Personagens desbloqueados: {unlockedCharacterIds.Count}");
    }

    /// <summary>
    /// Retorna os dados do personagem baseando-se no ID. Usado no Spawn do GameManager.
    /// </summary>
    public CharacterData GetCharacterData(string charId)
    {
        return allCharacters.Find(c => c.characterId == charId);
    }

    /// <summary>
    /// Verifica se o jogador pode usar este personagem
    /// </summary>
    public bool IsCharacterUnlocked(string charId)
    {
        return unlockedCharacterIds.Contains(charId);
    }
}
