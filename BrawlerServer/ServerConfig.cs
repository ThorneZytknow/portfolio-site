using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BrawlerServer
{
    public class ServerConfig
    {
        public ServerSettings Server { get; set; } = new();
        public DatabaseSettings Database { get; set; } = new();
        public EconomySettings Economy { get; set; } = new();

        public class ServerSettings
        {
            public int TcpPort { get; set; } = 7777;
            public int UdpPort { get; set; } = 7778;
            public int MaxPlayers { get; set; } = 100;
            public int MaxRoomSize { get; set; } = 4;
            public int TickRate { get; set; } = 60;
        }

        public class DatabaseSettings
        {
            public string Path { get; set; } = "./brawler_local.db";
        }

        public class EconomySettings
        {
            public int SoftCurrencyPerWin { get; set; } = 150;
            public int SoftCurrencyPerLoss { get; set; } = 50;
            public int XpPerKill { get; set; } = 100;
            public int XpPerMatch { get; set; } = 200;
        }

        public static ServerConfig LoadConfig(ILogger logger)
        {
            string path = "appsettings.json";
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<ServerConfig>(json);
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Erro ao ler appsettings.json, usando defaults. Erro: {ex.Message}");
                }
            }
            else
            {
                logger.LogInformation("appsettings.json não encontrado. Criando um novo com valores padrão...");
                var defaultConfig = new ServerConfig();
                File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true }));
                return defaultConfig;
            }
            return new ServerConfig();
        }
    }
}
