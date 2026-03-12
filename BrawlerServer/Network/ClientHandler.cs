using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BrawlerShared.Packets;

namespace BrawlerServer.Network
{
    public class ClientHandler : IDisposable
    {
        private readonly TcpClient _tcpClient;
        private readonly ILogger _logger;
        private readonly PacketProcessor _packetProcessor;
        private NetworkStream _stream;

        public string SessionToken { get; private set; }
        public string PlayerId { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(PlayerId);

        public IPEndPoint RemoteEndPoint { get; private set; }

        public ClientHandler(TcpClient tcpClient, PacketProcessor packetProcessor, ILogger logger)
        {
            _tcpClient = tcpClient;
            _packetProcessor = packetProcessor;
            _logger = logger;
            RemoteEndPoint = (IPEndPoint)_tcpClient.Client.RemoteEndPoint;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _stream = _tcpClient.GetStream();
            byte[] buffer = new byte[4096];
            StringBuilder jsonBuilder = new StringBuilder();

            try
            {
                while (!cancellationToken.IsCancellationRequested && _tcpClient.Connected)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0) break; // Cliente desconectou

                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    jsonBuilder.Append(data);

                    // Verifica se o pacote está completo (terminado com \n)
                    string content = jsonBuilder.ToString();
                    int newlineIndex;
                    while ((newlineIndex = content.IndexOf('\n')) >= 0)
                    {
                        string packetJson = content.Substring(0, newlineIndex);
                        content = content.Substring(newlineIndex + 1);
                        jsonBuilder.Clear();
                        jsonBuilder.Append(content);

                        await ProcessPacketAsync(packetJson);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning($"Desconexão anormal do cliente {RemoteEndPoint}: {ex.Message}");
            }
            finally
            {
                Dispose();
            }
        }

        private async Task ProcessPacketAsync(string json)
        {
            try
            {
                var basePacket = BasePacket.DeserializeWrapper(json);

                // Validação de segurança: apenas Auth_LoginRequest é permitido antes de autenticar
                if (!IsAuthenticated && basePacket.Type != BrawlerShared.Enums.PacketType.Auth_LoginRequest)
                {
                    _logger.LogWarning($"[Segurança] Bloqueado pacote {basePacket.Type} de cliente não autenticado.");
                    return;
                }

                // Injeta contexto (esta conexão) no processador central
                await _packetProcessor.ProcessIncomingPacket(basePacket, this);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao desserializar pacote de {RemoteEndPoint}: {ex.Message}");
            }
        }

        public async Task SendPacketAsync<T>(BrawlerShared.Enums.PacketType type, T payload)
        {
            if (!_tcpClient.Connected) return;

            try
            {
                byte[] data = BasePacket.Serialize(type, PlayerId ?? "Server", payload);
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao enviar pacote para {PlayerId}: {ex.Message}");
            }
        }

        public void SetAuthenticated(string playerId, string sessionToken)
        {
            PlayerId = playerId;
            SessionToken = sessionToken;
        }

        public void Dispose()
        {
            _logger.LogInformation($"Cliente {PlayerId ?? RemoteEndPoint.ToString()} desconectado.");
            _stream?.Dispose();
            _tcpClient?.Dispose();
        }
    }
}
