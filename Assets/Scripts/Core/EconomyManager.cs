using UnityEngine;
using System.Collections.Generic;
using BrawlerShared.Enums;
using BrawlerShared.Packets;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance;

    public int SoftCurrency { get; private set; }
    public int PremiumCurrency { get; private set; }

    // Mock das classes do PlayFab para não quebrar outros scripts
    public class MockItemInstance { public string ItemId; public string ItemClass; public string DisplayName; }
    public List<MockItemInstance> PlayerInventory { get; private set; } = new List<MockItemInstance>();

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

    private void Start()
    {
        if (LocalServerClient.Instance != null)
        {
            LocalServerClient.Instance.OnAnyPacketReceived += HandleEconomyPackets;
        }
    }

    public void GetUserInventory()
    {
        Debug.Log("[LocalEconomy] Solicitando saldo ao servidor local...");
        LocalServerClient.Instance.SendPacket(PacketType.Economy_GetBalance, new { });
    }

    private void HandleEconomyPackets(BasePacket packet)
    {
        if (packet.Type == PacketType.Economy_BalanceResponse)
        {
            var res = packet.GetPayload<EconomyGetBalanceResponse>();
            SoftCurrency = res.SoftCurrency;
            PremiumCurrency = res.PremiumCurrency;

            Debug.Log($"[LocalEconomy] Saldo atualizado: {SoftCurrency} SC | {PremiumCurrency} PC");
        }
    }

    public void GrantMatchRewards()
    {
        Debug.Log("[LocalEconomy] Requisitando recompensas de partida ao servidor local.");
        LocalServerClient.Instance.SendPacket(PacketType.Economy_GrantReward, new { MatchDuration = 300, IsWinner = true });
    }
}
