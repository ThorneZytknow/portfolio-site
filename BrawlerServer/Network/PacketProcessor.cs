using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BrawlerShared.Packets;
using BrawlerShared.Enums;
using BrawlerServer.Auth;
using BrawlerServer.Economy;
using BrawlerServer.Matchmaking;

namespace BrawlerServer.Network
{
    public class PacketProcessor
    {
        private readonly AuthService _authService;
        private readonly EconomyService _economyService;
        private readonly MatchmakingService _matchmakingService;
        private readonly ILogger<PacketProcessor> _logger;

        public PacketProcessor(
            AuthService authService,
            EconomyService economyService,
            MatchmakingService matchmakingService,
            ILogger<PacketProcessor> logger)
        {
            _authService = authService;
            _economyService = economyService;
            _matchmakingService = matchmakingService;
            _logger = logger;
        }

        public async Task ProcessIncomingPacket(BasePacket basePacket, ClientHandler client)
        {
            switch (basePacket.Type)
            {
                case PacketType.Auth_LoginRequest:
                    await HandleLoginRequest(basePacket, client);
                    break;

                case PacketType.Economy_GetBalance:
                    await HandleGetBalance(basePacket, client);
                    break;

                case PacketType.Economy_GrantReward:
                    await HandleGrantReward(basePacket, client);
                    break;

                case PacketType.Matchmaking_JoinQueue:
                    await HandleJoinQueue(basePacket, client);
                    break;

                case PacketType.Game_PlayerInput:
                case PacketType.Combat_DealDamage:
                    // Repassa inputs rápidos e combate diretamente para a Sala (Room) do jogador
                    _matchmakingService.RoutePacketToRoom(client.PlayerId, basePacket);
                    break;

                case PacketType.Server_Ping:
                    await client.SendPacketAsync(PacketType.Server_Pong, new { Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
                    break;

                default:
                    // _logger.LogInformation($"Pacote do tipo {basePacket.Type} não tratado no nível TCP.");
                    break;
            }
        }

        private async Task HandleLoginRequest(BasePacket packet, ClientHandler client)
        {
            var req = packet.GetPayload<AuthLoginRequest>();
            var result = await _authService.LoginOrRegisterAsync(req.Username, req.Password, req.DeviceId);

            var res = new AuthLoginResponse
            {
                Success = result.Success,
                PlayerId = result.PlayerId,
                SessionToken = result.Token,
                ErrorMessage = result.ErrorMsg
            };

            if (result.Success)
            {
                client.SetAuthenticated(result.PlayerId, result.Token);
            }

            await client.SendPacketAsync(PacketType.Auth_LoginResponse, res);
        }

        private async Task HandleGetBalance(BasePacket packet, ClientHandler client)
        {
            var balance = await _economyService.GetBalanceAsync(client.PlayerId);
            await client.SendPacketAsync(PacketType.Economy_BalanceResponse, balance);
        }

        private async Task HandleJoinQueue(BasePacket packet, ClientHandler client)
        {
            var req = packet.GetPayload<MatchmakingJoinQueue>();

            // Registra o cliente na fila do modo selecionado
            _matchmakingService.EnqueuePlayer(client, req.GameMode);

            _logger.LogInformation($"[{client.PlayerId}] Entrou na fila de Matchmaking ({req.GameMode} mode)");
        }

        private async Task HandleGrantReward(BasePacket packet, ClientHandler client)
        {
            // Opcionalmente verificar se o jogador estava em uma sala que acabou de terminar
            // Num sistema robusto, buscaríamos "duration" e "isWinner" dos logs da Sala gerenciada pelo _matchmakingService
            // Para efeitos desse protótipo funcional, acionamos o EconomyService de forma fixa
            // e bloqueamos floods validando o cache interno.

            bool granted = await _economyService.GrantMatchRewardsAsync(client.PlayerId, 65, true); // Mock seguro de tempo>60s
            if (granted)
            {
                // Após dar o prêmio, força o cliente a puxar o saldo novo enviando de volta
                await HandleGetBalance(packet, client);
            }
        }
    }
}
