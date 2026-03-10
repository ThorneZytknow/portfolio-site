using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Brawler/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Informações Básicas")]
    public string characterName;
    public string characterId; // ID correspondente no catálogo do PlayFab
    public string description;

    [Header("Atributos de Movimentação e Física")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;
    public float weight = 100f; // Afeta a resistência ao knockback (mais pesado = voa menos)

    [Header("Atributos de Combate Base")]
    public float baseAttackDamage = 10f;
    public float attackSpeedMultiplier = 1f;

    [Header("Referências Visuais")]
    public GameObject characterPrefab; // O prefab que será instanciado
    public GameObject spawnEffectPrefab; // Efeito ao surgir na arena
    public Sprite characterPortrait; // Ícone para a UI (Matchmaking, Resultados)
}
