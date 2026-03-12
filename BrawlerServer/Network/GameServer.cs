using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BrawlerServer.Network
{
    public class GameServer
    {
        private readonly int _tcpPort;
        private readonly int _udpPort;
        private readonly PacketProcessor _packetProcessor;
        private readonly ILogger<GameServer> _logger;

        private TcpListener _tcpListener;
        private UdpClient _udpListener;

        private readonly ConcurrentDictionary<string, ClientHandler> _clients = new();

        public GameServer(int tcpPort, int udpPort, PacketProcessor packetProcessor, ILogger<GameServer> logger)
        {
            _tcpPort = tcpPort;
            _udpPort = udpPort;
            _packetProcessor = packetProcessor;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _tcpListener = new TcpListener(IPAddress.Any, _tcpPort);
            _tcpListener.Start();

            _udpListener = new UdpClient(_udpPort);

            _logger.LogInformation($"Servidor Iniciado. TCP: {_tcpPort} | UDP: {_udpPort}");

            // Loop de aceitação de clientes TCP
            var tcpAcceptTask = AcceptClientsAsync(cancellationToken);
            // Loop de recebimento de pacotes UDP rápidos (Combate)
            var udpReceiveTask = ReceiveUdpAsync(cancellationToken);

            await Task.WhenAll(tcpAcceptTask, udpReceiveTask);
        }

        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync(cancellationToken);
                    _logger.LogInformation($"Nova conexão recebida de: {tcpClient.Client.RemoteEndPoint}");

                    var handler = new ClientHandler(tcpClient, _packetProcessor, _logger);
                    string connectionId = Guid.NewGuid().ToString();
                    _clients.TryAdd(connectionId, handler);

                    // Processa o cliente de forma assíncrona para não bloquear a thread de Accept
                    _ = handler.StartAsync(cancellationToken).ContinueWith(t =>
                    {
                        _clients.TryRemove(connectionId, out _);
                        if (handler.IsAuthenticated)
                        {
                            // Avisar outros serviços da desconexão
                            _logger.LogInformation($"Limpando sessão do jogador {handler.PlayerId}");
                        }
                    });
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError($"Erro ao aceitar cliente: {ex.Message}");
                }
            }
        }

        private async Task ReceiveUdpAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult result = await _udpListener.ReceiveAsync(cancellationToken);
                    // Roteamento UDP direto seria repassado para a sala (Room.cs)
                    // _logger.LogDebug($"[UDP] Pacote recebido de {result.RemoteEndPoint}");
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Erro no socket UDP: {ex.Message}");
                }
            }
        }
    }
}
