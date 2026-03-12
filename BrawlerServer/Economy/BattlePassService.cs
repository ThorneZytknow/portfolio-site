using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using BrawlerServer.Database;
using BrawlerShared.Packets;

namespace BrawlerServer.Economy
{
    public class BattlePassService
    {
        private readonly DatabaseManager _db;
        private readonly ILogger<BattlePassService> _logger;

        public BattlePassService(DatabaseManager db, ILogger<BattlePassService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<BattlePassTierResponse> GetCurrentTierAsync(string playerId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Tier, XpAccumulated, IsPremium FROM BattlePass WHERE PlayerId = @Id AND Season = 1";
            cmd.Parameters.AddWithValue("@Id", playerId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new BattlePassTierResponse
                {
                    CurrentTier = reader.GetInt32(0),
                    CurrentXP = reader.GetInt32(1),
                    IsPremium = reader.GetBoolean(2)
                };
            }
            else
            {
                // Cria entrada inicial no passe caso não exista
                var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = "INSERT INTO BattlePass (PlayerId, Season, Tier, IsPremium, XpAccumulated) VALUES (@Id, 1, 1, 0, 0)";
                insertCmd.Parameters.AddWithValue("@Id", playerId);
                await insertCmd.ExecuteNonQueryAsync();

                return new BattlePassTierResponse { CurrentTier = 1, CurrentXP = 0, IsPremium = false };
            }
        }

        public async Task<bool> AdvanceTierAsync(string playerId, int xpEarned)
        {
            // Busca o progresso atual
            var currentStatus = await GetCurrentTierAsync(playerId);

            int xpRequiredPerTier = 1000;
            int newTotalXp = currentStatus.CurrentXP + xpEarned;
            int tiersGained = newTotalXp / xpRequiredPerTier;
            int remainderXp = newTotalXp % xpRequiredPerTier;

            if (tiersGained > 0)
            {
                int newTier = currentStatus.CurrentTier + tiersGained;

                using var conn = _db.GetConnection();
                await conn.OpenAsync();

                var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = "UPDATE BattlePass SET Tier = @Tier, XpAccumulated = @Xp WHERE PlayerId = @Id AND Season = 1";
                updateCmd.Parameters.AddWithValue("@Tier", newTier);
                updateCmd.Parameters.AddWithValue("@Xp", remainderXp);
                updateCmd.Parameters.AddWithValue("@Id", playerId);

                await updateCmd.ExecuteNonQueryAsync();

                _logger.LogInformation($"[BattlePass] {playerId} subiu {tiersGained} níveis! Nível atual: {newTier}");
                return true;
            }
            else
            {
                // Só atualiza o XP
                using var conn = _db.GetConnection();
                await conn.OpenAsync();

                var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = "UPDATE BattlePass SET XpAccumulated = @Xp WHERE PlayerId = @Id AND Season = 1";
                updateCmd.Parameters.AddWithValue("@Xp", remainderXp);
                updateCmd.Parameters.AddWithValue("@Id", playerId);
                await updateCmd.ExecuteNonQueryAsync();
            }

            return false;
        }
    }
}
