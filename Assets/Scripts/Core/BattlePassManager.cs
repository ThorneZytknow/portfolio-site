using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class BattlePassManager : MonoBehaviour
{
    public static BattlePassManager Instance;

    [Header("Status do Passe de Batalha")]
    public int CurrentTier { get; private set; } = 1;
    public int BattlePassXP { get; private set; } = 0;
    public bool HasPremiumPass { get; private set; } = false;

    // Constante para a chave do item no inventário (PlayFab ID)
    private const string PREMIUM_PASS_ITEM_ID = "Item_BattlePass_Season1";
    // Quantidade de XP necessária por nível do passe
    public int XpPerTier = 1000;

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
        PlayFabAuthManager.OnLoginSuccessEvent += CheckPremiumPassStatus;
        PlayFabAuthManager.OnLoginSuccessEvent += LoadBattlePassProgress;
    }

    private void OnDisable()
    {
        PlayFabAuthManager.OnLoginSuccessEvent -= CheckPremiumPassStatus;
        PlayFabAuthManager.OnLoginSuccessEvent -= LoadBattlePassProgress;
    }

    private void Start()
    {
        // Ao iniciar, caso o evento já tenha disparado antes
        if (PlayFabAuthManager.Instance != null && PlayFabAuthManager.Instance.IsLoggedIn)
        {
            CheckPremiumPassStatus();
            LoadBattlePassProgress();
        }
    }

    /// <summary>
    /// Verifica se o jogador comprou o item do passe de batalha premium consultando o inventário
    /// </summary>
    public void CheckPremiumPassStatus()
    {
        if (EconomyManager.Instance != null && EconomyManager.Instance.PlayerInventory != null)
        {
            HasPremiumPass = false;
            foreach (var item in EconomyManager.Instance.PlayerInventory)
            {
                if (item.ItemId == PREMIUM_PASS_ITEM_ID)
                {
                    HasPremiumPass = true;
                    Debug.Log("[BattlePassManager] Passe Premium Ativo!");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Carrega as estatísticas do PlayFab para pegar o Nível (Tier) e XP atual do Passe
    /// </summary>
    public void LoadBattlePassProgress()
    {
        var request = new GetPlayerStatisticsRequest
        {
            StatisticNames = new List<string> { "BattlePassTier", "BattlePassXP" }
        };

        PlayFabClientAPI.GetPlayerStatistics(request, result =>
        {
            foreach (var stat in result.Statistics)
            {
                if (stat.StatisticName == "BattlePassTier")
                    CurrentTier = stat.Value;
                else if (stat.StatisticName == "BattlePassXP")
                    BattlePassXP = stat.Value;
            }
            Debug.Log($"[BattlePassManager] Tier Atual: {CurrentTier} | XP no Passe: {BattlePassXP}");
        },
        error => Debug.LogError("[BattlePassManager] Erro ao carregar estatísticas do passe: " + error.GenerateErrorReport()));
    }

    /// <summary>
    /// Adiciona XP ao Passe de Batalha. O cálculo de progressão real é feito via CloudScript para segurança,
    /// evitando manipulação local que daria recompensas gratuitas sem jogar.
    /// </summary>
    public void AddBattlePassXP(int amount)
    {
        Debug.Log($"[BattlePassManager] Solicitando adição de {amount} XP ao Passe via Nuvem...");

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "AdvanceBattlePassTier",
            FunctionParameter = new {
                xpEarned = amount,
                hasPremium = HasPremiumPass
            },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request, result =>
        {
            if (result.FunctionResult != null)
            {
                Debug.Log($"[BattlePassManager] Progresso atualizado pelo servidor: {result.FunctionResult.ToString()}");
                // Atualiza a tela com o novo tier e XP
                LoadBattlePassProgress();
            }
        },
        error => Debug.LogError("[BattlePassManager] Erro no script de progressão do passe: " + error.GenerateErrorReport()));
    }

    /// <summary>
    /// Compra o passe de batalha premium chamando o CloudScript (ou EconomyManager).
    /// </summary>
    public void BuyPremiumPass()
    {
        if (HasPremiumPass) return; // Evita compra duplicada
        Debug.Log("[BattlePassManager] Tentando comprar o passe premium...");

        // Usando o CloudScript para debitar a moeda premium (Ex: PC) e adicionar o item do passe ao inventário
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "PurchaseBattlePass",
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request, result =>
        {
            Debug.Log($"[BattlePassManager] Resposta da compra do passe: {result.FunctionResult.ToString()}");
            // Atualiza o inventário para reconhecer o item
            EconomyManager.Instance.GetUserInventory();
            // Verifica o status do passe novamente
            CheckPremiumPassStatus();
        },
        error => Debug.LogError("[BattlePassManager] Falha ao tentar comprar passe: " + error.GenerateErrorReport()));
    }
}
