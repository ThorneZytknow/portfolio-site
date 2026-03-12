using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BrawlerServer.Network;
using BrawlerShared.Packets;

namespace BrawlerServer.Matchmaking
{
    public class MatchmakingService
    {
        private readonly ILogger<MatchmakingService> _logger;

        // Filas separadas por modo de jogo (ex: 2 = 1v1, 4 = FFA)
        private readonly ConcurrentDictionary<int, ConcurrentQueue<ClientHandler>> _queues = new();

        // Dicionário de salas ativas
        private readonly ConcurrentDictionary<string, Room> _activeRooms = new();

        public MatchmakingService(ILogger<MatchmakingService> logger)
        {
            _logger = logger;
            _queues[2] = new ConcurrentQueue<ClientHandler>();
            _queues[4] = new ConcurrentQueue<ClientHandler>();

            // Thread contínua que avalia as filas
            _ = Task.Run(ProcessMatchmakingLoop);
        }

        public void EnqueuePlayer(ClientHandler client, int gameMode)
        {
            if (!_queues.ContainsKey(gameMode))
            {
                _queues[gameMode] = new ConcurrentQueue<ClientHandler>();
            }

            _queues[gameMode].Enqueue(client);
        }

        private async Task ProcessMatchmakingLoop()
        {
            while (true)
            {
                foreach (var kvp in _queues)
                {
                    int modeSize = kvp.Key;
                    var queue = kvp.Value;

                    // Avalia se há jogadores suficientes E ativos
                    if (queue.Count >= modeSize)
                    {
                        var playersToGroup = new List<ClientHandler>();

                        // Varre a fila até ter o número de jogadores, ou até a fila acabar
                        while (playersToGroup.Count < modeSize && queue.TryDequeue(out var p))
                        {
                            if (p.IsAuthenticated) // Apenas jogadores logados e não desconectados
                            {
                                playersToGroup.Add(p);
                            }
                        }

                        if (playersToGroup.Count == modeSize)
                        {
                            CreateRoom(playersToGroup, modeSize);
                        }
                        else
                        {
                            // Se a fila esvaziou devido a desconexões e não atingiu a cota,
                            // devolve os jogadores válidos encontrados para a fila.
                            foreach (var p in playersToGroup)
                            {
                                queue.Enqueue(p);
                            }
                        }
                    }
                }

                await Task.Delay(1000); // Checa a cada 1 segundo
            }
        }

        // Tabela reversa para achar rapidamente a sala de um jogador
        private readonly ConcurrentDictionary<string, string> _playerToRoomMap = new();

        public void RoutePacketToRoom(string playerId, BasePacket packet)
        {
            if (_playerToRoomMap.TryGetValue(playerId, out string roomId))
            {
                if (_activeRooms.TryGetValue(roomId, out Room room))
                {
                    room.EnqueuePacket(packet);
                }
            }
        }

        private void CreateRoom(List<ClientHandler> players, int size)
        {
            string roomId = Guid.NewGuid().ToString();
            var room = new Room(roomId, players, _logger);
            _activeRooms.TryAdd(roomId, room);

            foreach (var p in players)
            {
                _playerToRoomMap[p.PlayerId] = roomId;
            }

            _logger.LogInformation($"[Matchmaking] Sala {roomId} criada com {players.Count} jogadores!");

            // Avisa os jogadores que a partida foi encontrada
            int actorId = 1;
            foreach (var p in players)
            {
                var msg = new MatchmakingMatchFound
                {
                    RoomId = roomId,
                    MaxPlayers = size,
                    ActorNumber = actorId++
                };

                _ = p.SendPacketAsync(BrawlerShared.Enums.PacketType.Matchmaking_MatchFound, msg);
            }
        }
    }
}
