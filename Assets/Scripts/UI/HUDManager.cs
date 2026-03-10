using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Photon.Pun;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [Header("UI Prefabs e Containers")]
    public GameObject playerHudPrefab; // Um prefab que contém: Nome, Dano(%) e Vidas(Ícones)
    public Transform hudContainer;     // O layout horizontal onde os perfis são organizados

    // Dicionário mapeando o ID do jogador no Photon para a interface correspondente
    private Dictionary<int, PlayerHUDElement> activeHuds = new Dictionary<int, PlayerHUDElement>();

    // Classe auxiliar para gerenciar as referências dentro do Prefab instanciado
    public class PlayerHUDElement
    {
        public TMP_Text nameText;
        public TMP_Text damageText;
        public TMP_Text stocksText;
        // Indicadores Simplificados (Textuais)
        public TMP_Text staminaText;
        public TMP_Text shieldText;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Registra um novo jogador na tela (Chamado pelo GameManager ou StockSystem no Spawn inicial)
    /// </summary>
    public void RegisterPlayer(int actorNumber, string playerName, int startingStocks)
    {
        if (activeHuds.ContainsKey(actorNumber)) return;

        // Instancia o prefab de status
        GameObject hudGo = Instantiate(playerHudPrefab, hudContainer);
        PlayerHUDElement newElement = new PlayerHUDElement();

        // Pega as referências (exemplo: buscando por nomes fixos no prefab)
        newElement.nameText = hudGo.transform.Find("NameText").GetComponent<TMP_Text>();
        newElement.damageText = hudGo.transform.Find("DamageText").GetComponent<TMP_Text>();
        newElement.stocksText = hudGo.transform.Find("StocksText").GetComponent<TMP_Text>();

        // Novos elementos da UI
        Transform staminaTransform = hudGo.transform.Find("StaminaText");
        if (staminaTransform != null) newElement.staminaText = staminaTransform.GetComponent<TMP_Text>();

        Transform shieldTransform = hudGo.transform.Find("ShieldText");
        if (shieldTransform != null) newElement.shieldText = shieldTransform.GetComponent<TMP_Text>();

        // Preenche com os dados iniciais
        newElement.nameText.text = playerName;
        newElement.damageText.text = "0%";
        newElement.stocksText.text = $"Vidas: {startingStocks}";
        newElement.damageText.color = Color.white; // Começa branco

        activeHuds.Add(actorNumber, newElement);
    }

    /// <summary>
    /// Atualiza os indicadores de Stamina
    /// </summary>
    public void UpdateStamina(int actorNumber, float currentStamina)
    {
        if (activeHuds.TryGetValue(actorNumber, out PlayerHUDElement hud) && hud.staminaText != null)
        {
            hud.staminaText.text = $"STA: {Mathf.FloorToInt(currentStamina)}";
        }
    }

    /// <summary>
    /// Atualiza a durabilidade do Escudo (Shield)
    /// </summary>
    public void UpdateShield(int actorNumber, float currentShield, bool isBroken)
    {
        if (activeHuds.TryGetValue(actorNumber, out PlayerHUDElement hud) && hud.shieldText != null)
        {
            if (isBroken)
            {
                hud.shieldText.text = "<color=red>SHIELD BREAK!</color>";
            }
            else
            {
                hud.shieldText.text = $"Escudo: {Mathf.FloorToInt(currentShield)}";
            }
        }
    }

    /// <summary>
    /// Atualiza o número percentual de dano, escurecendo a cor (vermelho) conforme cresce (Smash style)
    /// </summary>
    public void UpdateDamage(int actorNumber, float newDamage)
    {
        if (activeHuds.TryGetValue(actorNumber, out PlayerHUDElement hud))
        {
            hud.damageText.text = $"{Mathf.FloorToInt(newDamage)}%";

            // Interpolando do Branco (0%) para Vermelho Escuro (300%)
            float colorLerp = Mathf.Clamp01(newDamage / 300f);
            hud.damageText.color = Color.Lerp(Color.white, Color.red, colorLerp);
        }
    }

    /// <summary>
    /// Atualiza o número/ícones de vidas restantes na tela
    /// </summary>
    public void UpdateStocks(int actorNumber, int remainingStocks)
    {
        if (activeHuds.TryGetValue(actorNumber, out PlayerHUDElement hud))
        {
            hud.stocksText.text = $"Vidas: {remainingStocks}";
            if (remainingStocks <= 0)
            {
                hud.stocksText.text = "ELIMINADO";
                hud.stocksText.color = Color.gray;
            }
        }
    }
}
