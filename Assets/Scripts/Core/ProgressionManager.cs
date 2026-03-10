using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance;

    [Header("Estatísticas Atuais")]
    public int PlayerLevel { get; private set; } = 1;
    public int CurrentXP { get; private set; } = 0;

    // Formula exemplo: XP_Necessária = Nível * 1000
    public int XpToNextLevel => PlayerLevel * 1000;

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
        PlayFabAuthManager.OnLoginSuccessEvent += LoadPlayerProgression;
    }

    private void OnDisable()
    {
        PlayFabAuthManager.OnLoginSuccessEvent -= LoadPlayerProgression;
    }

    private void Start()
    {
        // Caso a cena seja recarregada e já estejamos logados
        if (PlayFabAuthManager.Instance != null && PlayFabAuthManager.Instance.IsLoggedIn)
        {
            LoadPlayerProgression();
        }
    }

    /// <summary>
    /// Busca do banco de dados (Player Statistics) as infos de XP e Nível
    /// </summary>
    public void LoadPlayerProgression()
    {
        if (PlayFabAuthManager.Instance == null || !PlayFabAuthManager.Instance.IsLoggedIn) return;

        var request = new GetPlayerStatisticsRequest
        {
            StatisticNames = new List<string> { "PlayerLevel", "TotalXP" }
        };

        PlayFabClientAPI.GetPlayerStatistics(request, result =>
        {
            foreach (var stat in result.Statistics)
            {
                if (stat.StatisticName == "PlayerLevel")
                    PlayerLevel = stat.Value;
                else if (stat.StatisticName == "TotalXP")
                    CurrentXP = stat.Value;
            }
            Debug.Log($"[ProgressionManager] Nível: {PlayerLevel} | XP Total: {CurrentXP}");
        },
        error => Debug.LogError("Erro ao carregar estatísticas: " + error.GenerateErrorReport()));
    }

    /// <summary>
    /// Concede XP com base na performance do jogador ao fim de uma partida
    /// </summary>
    public void GrantMatchXP(int kills, int survivalTimeSeconds, int finalPlacement)
    {
        Debug.Log("[ProgressionManager] Solicitando recompensa de XP pela partida...");

        // Chama o servidor do PlayFab (Cloud Script) para evitar manipulação client-side
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "GrantMatchXP",
            FunctionParameter = new {
                kills = kills,
                duration = survivalTimeSeconds,
                placement = finalPlacement
            },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request, result =>
        {
            // O CloudScript retorna os novos valores de XP
            if (result.FunctionResult != null)
            {
                // Aqui o servidor dirá quanto XP foi ganho, e se subiu de nível
                Debug.Log($"XP Concedido! Validado no CloudScript. Resultados: {result.FunctionResult.ToString()}");

                // Recarrega as estatísticas locais para a UI (HUD ou Resultados) ser atualizada
                LoadPlayerProgression();

                // Se o BattlePass estiver ativo, repassa o XP da partida para lá
                if (BattlePassManager.Instance != null)
                {
                    BattlePassManager.Instance.AddBattlePassXP(100); // Exemplo: Valor fixo ou retornado pelo script
                }
            }
        },
        error => Debug.LogError("[ProgressionManager] Erro no script de XP: " + error.GenerateErrorReport()));
    }
}
