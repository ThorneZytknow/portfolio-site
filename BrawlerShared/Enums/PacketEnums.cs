namespace BrawlerShared.Enums
{
    public enum PacketType
    {
        // Autenticação
        Auth_LoginRequest,
        Auth_LoginResponse,

        // Matchmaking
        Matchmaking_JoinQueue,
        Matchmaking_MatchFound,
        Matchmaking_RoomCreated,

        // Jogo (Gameplay)
        Game_PlayerInput,
        Game_StateSync,
        Game_SpawnPlayer,

        // Combate
        Combat_DealDamage,
        Combat_ApplyKnockback,
        Combat_AbilityUse,

        // Economia GaaS
        Economy_GetBalance,
        Economy_BalanceResponse,
        Economy_GetInventory,
        Economy_InventoryResponse,
        Economy_GrantReward,
        Economy_RewardResponse,

        // Passe de Batalha
        BattlePass_GetTier,
        BattlePass_TierResponse,
        BattlePass_AdvanceTier,

        // Sistema Base
        Server_Ping,
        Server_Pong,
        Server_Disconnect
    }

    public enum CharacterType
    {
        Solaris,
        Nyxara,
        Torrak,
        Veloci,
        Gaia
    }

    public enum GameState
    {
        Waiting,
        Loading,
        InGame,
        Finished
    }
}
