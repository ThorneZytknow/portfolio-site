using UnityEngine;
using BrawlerShared.Enums;
using BrawlerShared.Packets;

public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance;

    [Header("Estatísticas Atuais")]
    public int PlayerLevel { get; private set; } = 1;
    public int CurrentXP { get; private set; } = 0;

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
        LocalAuthManager.OnLoginSuccessEvent += LoadPlayerProgression;
    }

    private void OnDisable()
    {
        LocalAuthManager.OnLoginSuccessEvent -= LoadPlayerProgression;
    }

    private void Start()
    {
        if (LocalAuthManager.Instance != null && LocalAuthManager.Instance.IsLoggedIn)
        {
            LoadPlayerProgression();
        }

        if (LocalServerClient.Instance != null)
        {
            LocalServerClient.Instance.OnAnyPacketReceived += HandleProgressionPackets;
        }
    }

    public void LoadPlayerProgression()
    {
        Debug.Log("[ProgressionManager] Seria solicitado ao Servidor Local o Nível e XP...");
        // LocalServerClient.Instance.SendPacket(PacketType.Progression_GetStats, new {});
    }

    private void HandleProgressionPackets(BasePacket packet)
    {
        // Se o servidor enviasse um Progression_StatsResponse, leríamos aqui.
    }

    public void GrantMatchXP(int kills, int survivalTimeSeconds, int finalPlacement)
    {
        Debug.Log("[ProgressionManager] Solicitando recompensa de XP ao Servidor Local...");
        // Exemplo: LocalServerClient.Instance.SendPacket(PacketType.Economy_GrantReward, ...);

        // Simulação para o HUD não quebrar:
        CurrentXP += (kills * 10) + (survivalTimeSeconds / 2);
        if (CurrentXP >= XpToNextLevel) PlayerLevel++;

        if (BattlePassManager.Instance != null)
        {
            BattlePassManager.Instance.AddBattlePassXP(100);
        }
    }
}
