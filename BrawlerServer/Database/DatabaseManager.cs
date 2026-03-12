using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace BrawlerServer.Database
{
    /// <summary>
    /// Gerencia a conexão com o banco de dados local (SQLite) e cria as tabelas caso não existam.
    /// Ideal para um ambiente GaaS self-hosted.
    /// </summary>
    public class DatabaseManager
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseManager> _logger;

        public DatabaseManager(string dbPath, ILogger<DatabaseManager> logger)
        {
            _connectionString = $"Data Source={dbPath};";
            _logger = logger;

            InitializeDatabase().Wait();
        }

        public SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        private async Task InitializeDatabase()
        {
            _logger.LogInformation("Verificando banco de dados SQLite...");

            using var connection = GetConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();

            // Tabela de Jogadores
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Players (
                    Id TEXT PRIMARY KEY,
                    Username TEXT UNIQUE NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    SoftCurrency INTEGER DEFAULT 500,
                    PremiumCurrency INTEGER DEFAULT 0,
                    XP INTEGER DEFAULT 0,
                    Level INTEGER DEFAULT 1,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS Inventory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlayerId TEXT NOT NULL,
                    ItemId TEXT NOT NULL,
                    ItemType TEXT NOT NULL,
                    AcquiredAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(PlayerId) REFERENCES Players(Id)
                );

                CREATE TABLE IF NOT EXISTS BattlePass (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlayerId TEXT NOT NULL,
                    Season INTEGER DEFAULT 1,
                    Tier INTEGER DEFAULT 1,
                    IsPremium BOOLEAN DEFAULT 0,
                    XpAccumulated INTEGER DEFAULT 0,
                    FOREIGN KEY(PlayerId) REFERENCES Players(Id)
                );
            ";

            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Tabelas do banco de dados prontas para uso.");
        }
    }
}
