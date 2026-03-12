using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using BrawlerServer.Database;

namespace BrawlerServer.Auth
{
    public class AuthService
    {
        private readonly DatabaseManager _db;
        private readonly ILogger<AuthService> _logger;

        // Cache em memória para os tokens de sessão ativos
        // Mapeia Token -> PlayerId
        private readonly ConcurrentDictionary<string, string> _activeSessions = new();

        public AuthService(DatabaseManager db, ILogger<AuthService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<(bool Success, string PlayerId, string Token, string ErrorMsg)> LoginOrRegisterAsync(string username, string rawPassword, string deviceId)
        {
            if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(deviceId))
                return (false, null, null, "Credenciais vazias");

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            string user = string.IsNullOrWhiteSpace(username) ? $"Guest_{deviceId.Substring(0, 5)}" : username;

            var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT Id, PasswordHash FROM Players WHERE Username = @User";
            checkCmd.Parameters.AddWithValue("@User", user);

            using var reader = await checkCmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                // Usuário existe
                string playerId = reader.GetString(0);
                string hash = reader.GetString(1);

                // Se a conta tem senha (não é conta Guest gerada dinamicamente via DeviceID sem hash)
                if (hash != "NOHASH")
                {
                    // Bloqueia se tentarem pular a verificação de senha mandando string vazia
                    if (string.IsNullOrWhiteSpace(rawPassword) || !BCrypt.Net.BCrypt.Verify(rawPassword, hash))
                    {
                        _logger.LogWarning($"[Auth] Tentativa de login falha para {user} (Senha Inválida ou Vazia).");
                        return (false, null, null, "Senha Incorreta");
                    }
                }

                string token = Guid.NewGuid().ToString();
                _activeSessions[token] = playerId;
                _logger.LogInformation($"[Auth] Jogador conectado: {user} ({playerId})");

                return (true, playerId, token, null);
            }
            else
            {
                // Novo usuário - Registro automático (Comum em F2P/Mobile para retenção rápida)
                string newPlayerId = Guid.NewGuid().ToString();
                string newHash = string.IsNullOrWhiteSpace(rawPassword) ? "NOHASH" : BCrypt.Net.BCrypt.HashPassword(rawPassword);

                var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = "INSERT INTO Players (Id, Username, PasswordHash) VALUES (@Id, @User, @Hash)";
                insertCmd.Parameters.AddWithValue("@Id", newPlayerId);
                insertCmd.Parameters.AddWithValue("@User", user);
                insertCmd.Parameters.AddWithValue("@Hash", newHash);

                await insertCmd.ExecuteNonQueryAsync();

                string token = Guid.NewGuid().ToString();
                _activeSessions[token] = newPlayerId;

                _logger.LogInformation($"[Auth] Nova conta criada: {user} ({newPlayerId})");
                return (true, newPlayerId, token, null);
            }
        }

        public bool ValidateSession(string token, out string playerId)
        {
            return _activeSessions.TryGetValue(token, out playerId);
        }

        public void Disconnect(string token)
        {
            _activeSessions.TryRemove(token, out _);
        }
    }
}
