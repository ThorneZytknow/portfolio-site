using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BrawlerServer.Network;
using BrawlerShared.Enums;
using BrawlerShared.Packets;

namespace BrawlerServer.Matchmaking
{
    public class Room
    {
        public string RoomId { get; private set; }
        public GameState CurrentState { get; private set; }

        private readonly List<ClientHandler> _players;
        private readonly ILogger _logger;
        private Timer _gameLoopTimer;

        // Fila thread-safe para inputs que chegam assincronamente da rede
        private readonly ConcurrentQueue<BasePacket> _packetQueue = new();

        public Room(string roomId, List<ClientHandler> players, ILogger logger)
        {
            RoomId = roomId;
            _players = players;
            _logger = logger;
            CurrentState = GameState.InGame; // Direto para in-game para pular o loading do protótipo

            _gameLoopTimer = new Timer(GameTick, null, 1000, 1000 / 30); // 30 Ticks por seg para reduzir overhead TCP
        }

        public void EnqueuePacket(BasePacket packet)
        {
            _packetQueue.Enqueue(packet);
        }

        private void GameTick(object state)
        {
            if (CurrentState != GameState.InGame) return;

            bool stateChanged = false;

            // 1. Processa todos os pacotes acumulados neste frame
            while (_packetQueue.TryDequeue(out BasePacket packet))
            {
                // Como não estamos simulando física Box2D no servidor C# neste protótipo,
                // agimos como um "Relay Autoritativo Seguro", distribuindo os pacotes aos outros clientes
                if (packet.Type == PacketType.Combat_DealDamage)
                {
                    var combatPayload = packet.GetPayload<CombatDealDamage>();
                    _logger.LogInformation($"[Sala {RoomId}] Dano ({combatPayload.DamageAmount}) registrado. Repassando...");
                    _ = BroadcastTcpAsync(PacketType.Combat_DealDamage, combatPayload);
                    stateChanged = true;
                }
                else if (packet.Type == PacketType.Game_PlayerInput)
                {
                    // Apenas repassa a intenção de movimento ou os dados em relay bruto para testes base
                    // _logger.LogDebug($"[Sala {RoomId}] Input recebido...");
                }
            }
        }

        public async Task BroadcastTcpAsync<T>(PacketType type, T payload)
        {
            foreach (var p in _players)
            {
                await p.SendPacketAsync(type, payload);
            }
        }

        public void Close()
        {
            _gameLoopTimer?.Dispose();
            CurrentState = GameState.Finished;
            _logger.LogInformation($"[Room] {RoomId} encerrada.");
        }
    }
}
