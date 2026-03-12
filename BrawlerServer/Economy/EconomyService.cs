using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using BrawlerServer.Database;
using BrawlerShared.Packets;

namespace BrawlerServer.Economy
{
    public class EconomyService
    {
        private readonly DatabaseManager _db;
        private readonly ILogger<EconomyService> _logger;

        public EconomyService(DatabaseManager db, ILogger<EconomyService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<EconomyGetBalanceResponse> GetBalanceAsync(string playerId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT SoftCurrency, PremiumCurrency FROM Players WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", playerId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new EconomyGetBalanceResponse
                {
                    SoftCurrency = reader.GetInt32(0),
                    PremiumCurrency = reader.GetInt32(1)
                };
            }

            return new EconomyGetBalanceResponse();
        }

        public async Task<bool> GrantMatchRewardsAsync(string playerId, int durationSeconds, bool isWinner)
        {
            if (durationSeconds < 30)
            {
                _logger.LogWarning($"[Economy] Recompensa negada para {playerId}. Partida muito curta ({durationSeconds}s). Suspeita de fraude.");
                return false;
            }

            int coinsToGrant = (durationSeconds / 60) * 10; // 10 SoftCurrency por minuto
            if (isWinner) coinsToGrant += 50;

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Players SET SoftCurrency = SoftCurrency + @Amount WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Amount", coinsToGrant);
            cmd.Parameters.AddWithValue("@Id", playerId);

            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows > 0)
            {
                _logger.LogInformation($"[Economy] +{coinsToGrant} SoftCurrency concedidos para {playerId}");
                return true;
            }
            return false;
        }
    }
}
