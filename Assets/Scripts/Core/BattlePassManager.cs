using UnityEngine;
using BrawlerShared.Enums;
using BrawlerShared.Packets;

public class BattlePassManager : MonoBehaviour
{
    public static BattlePassManager Instance;

    [Header("Status do Passe de Batalha")]
    public int CurrentTier { get; private set; } = 1;
    public int BattlePassXP { get; private set; } = 0;
    public bool HasPremiumPass { get; private set; } = false;

    private const string PREMIUM_PASS_ITEM_ID = "Item_BattlePass_Season1";
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
        LocalAuthManager.OnLoginSuccessEvent += CheckPremiumPassStatus;
        LocalAuthManager.OnLoginSuccessEvent += LoadBattlePassProgress;
    }

    private void OnDisable()
    {
        LocalAuthManager.OnLoginSuccessEvent -= CheckPremiumPassStatus;
        LocalAuthManager.OnLoginSuccessEvent -= LoadBattlePassProgress;
    }

    private void Start()
    {
        if (LocalAuthManager.Instance != null && LocalAuthManager.Instance.IsLoggedIn)
        {
            CheckPremiumPassStatus();
            LoadBattlePassProgress();
        }

        if (LocalServerClient.Instance != null)
        {
            LocalServerClient.Instance.OnAnyPacketReceived += HandleBattlePassPackets;
        }
    }

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

    public void LoadBattlePassProgress()
    {
        Debug.Log("[LocalBattlePass] Solicitando progresso do passe ao Servidor...");
        LocalServerClient.Instance.SendPacket(PacketType.BattlePass_GetTier, new { });
    }

    private void HandleBattlePassPackets(BasePacket packet)
    {
        if (packet.Type == PacketType.BattlePass_TierResponse)
        {
            var res = packet.GetPayload<BattlePassTierResponse>();
            CurrentTier = res.CurrentTier;
            BattlePassXP = res.CurrentXP;
            HasPremiumPass = res.IsPremium;

            Debug.Log($"[LocalBattlePass] Tier Atual: {CurrentTier} | XP no Passe: {BattlePassXP}");
        }
    }

    public void AddBattlePassXP(int amount)
    {
        Debug.Log($"[LocalBattlePass] Solicitando adição de {amount} XP ao Passe via Servidor C#...");
        LocalServerClient.Instance.SendPacket(PacketType.BattlePass_AdvanceTier, new { XpEarned = amount });
    }

    public void BuyPremiumPass()
    {
        if (HasPremiumPass) return;
        Debug.Log("[LocalBattlePass] Tentando comprar o passe premium...");
        // Exemplo: LocalServerClient.Instance.SendPacket(PacketType.Economy_PurchaseItem, new { ItemId = PREMIUM_PASS_ITEM_ID });
    }
}
