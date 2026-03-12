using System;
using System.Text.Json;
using BrawlerShared.Enums;

namespace BrawlerShared.Packets
{
    /// <summary>
    /// Classe base serializável para toda a comunicação cliente-servidor.
    /// </summary>
    public class BasePacket
    {
        public PacketType Type { get; set; }
        public string PlayerId { get; set; }
        public long Timestamp { get; set; }
        public int SequenceNumber { get; set; }

        // Payload armazena os dados específicos do pacote serializados em string JSON
        // Isso permite desserializar primeiro a casca, identificar o tipo, e depois o conteúdo
        public string PayloadJson { get; set; }

        public BasePacket()
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static byte[] Serialize<T>(PacketType type, string playerId, T payload, int sequence = 0)
        {
            var packet = new BasePacket
            {
                Type = type,
                PlayerId = playerId,
                SequenceNumber = sequence,
                PayloadJson = JsonSerializer.Serialize(payload)
            };

            string json = JsonSerializer.Serialize(packet);
            return System.Text.Encoding.UTF8.GetBytes(json + "\n"); // Delimitador de fim de pacote para TCP
        }

        public static BasePacket DeserializeWrapper(string json)
        {
            return JsonSerializer.Deserialize<BasePacket>(json);
        }

        public T GetPayload<T>()
        {
            if (string.IsNullOrEmpty(PayloadJson))
                return default;

            return JsonSerializer.Deserialize<T>(PayloadJson);
        }
    }

    // --- PACOTES ESPECÍFICOS ---

    public class AuthLoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; } // Em um ambiente real, deve estar hasheado no cliente ou rodar via WSS/TLS
        public string DeviceId { get; set; } // Login anônimo
    }

    public class AuthLoginResponse
    {
        public bool Success { get; set; }
        public string SessionToken { get; set; } // Token JWT ou GUID
        public string PlayerId { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class MatchmakingJoinQueue
    {
        public string CharacterId { get; set; }
        public int GameMode { get; set; } // 1v1 = 2, FFA = 4
    }

    public class MatchmakingMatchFound
    {
        public string RoomId { get; set; }
        public int MaxPlayers { get; set; }
        public int ActorNumber { get; set; } // Antigo ID do Photon
    }

    public class GamePlayerInput
    {
        public float InputX { get; set; }
        public float InputY { get; set; }
        public bool JumpPressed { get; set; }
        public bool AttackPressed { get; set; }
        public bool SpecialPressed { get; set; }
        public bool ShieldPressed { get; set; }
    }

    public class GameStateSync
    {
        // Posição de todos os jogadores na sala
        public System.Collections.Generic.Dictionary<int, PlayerStateData> Players { get; set; } = new();
    }

    public class PlayerStateData
    {
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float VelX { get; set; }
        public float VelY { get; set; }
        public float DamagePercentage { get; set; }
        public int Stocks { get; set; }
        public bool IsShielding { get; set; }
    }

    public class CombatDealDamage
    {
        public int TargetActorNumber { get; set; }
        public float DamageAmount { get; set; }
        public float BaseKnockback { get; set; }
        public float DirX { get; set; }
        public float DirY { get; set; }
        public int HitlagFrames { get; set; }
        public float HitstunDuration { get; set; }
    }

    public class EconomyGrantReward
    {
        // O cliente envia esse payload vazio como um gatilho de "Minha partida acabou"
        // E o servidor confere o estado da sala para saber quem venceu.
    }

    public class EconomyGetBalanceResponse
    {
        public int SoftCurrency { get; set; }
        public int PremiumCurrency { get; set; }
    }

    public class BattlePassTierResponse
    {
        public int CurrentTier { get; set; }
        public int CurrentXP { get; set; }
        public bool IsPremium { get; set; }
    }
}
