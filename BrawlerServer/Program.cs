using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using BrawlerServer.Database;
using BrawlerServer.Auth;
using BrawlerServer.Economy;
using BrawlerServer.Matchmaking;
using BrawlerServer.Network;

namespace BrawlerServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Configuração de Logs
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("Iniciando BrawlerGaaS Servidor Local...");

            // 2. Mock de Configuração Local (Em um projeto real, leria appsettings.json)
            string dbPath = "brawler_local.db";
            int tcpPort = 7777;
            int udpPort = 7778;

            // 3. Injeção de Dependências Manual (Core Services)
            var dbManager = new DatabaseManager(dbPath, loggerFactory.CreateLogger<DatabaseManager>());

            var authService = new AuthService(dbManager, loggerFactory.CreateLogger<AuthService>());
            var economyService = new EconomyService(dbManager, loggerFactory.CreateLogger<EconomyService>());
            var matchmakingService = new MatchmakingService(loggerFactory.CreateLogger<MatchmakingService>());

            // 4. Instanciar Rede
            var packetProcessor = new PacketProcessor(authService, economyService, matchmakingService, loggerFactory.CreateLogger<PacketProcessor>());
            var gameServer = new GameServer(tcpPort, udpPort, packetProcessor, loggerFactory.CreateLogger<GameServer>());

            // 5. Cancelamento Graceful (Ctrl+C)
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                logger.LogWarning("Desligando Servidor...");
                e.Cancel = true;
                cts.Cancel();
            };

            // 6. Iniciar Servidor Async
            try
            {
                await gameServer.StartAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Shutdown Completo.");
            }
        }
    }
}
