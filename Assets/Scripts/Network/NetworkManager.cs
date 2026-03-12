using UnityEngine;
using BrawlerShared.Enums;
using BrawlerShared.Packets;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        if (LocalServerClient.Instance != null)
        {
            LocalServerClient.Instance.OnAnyPacketReceived += HandleNetworkPackets;
        }
    }

    public void ConnectToServer()
    {
        // A conexão base TCP agora é iniciada pelo LocalAuthManager (LocalAuth)
        // ou pelo LocalServerClient Start().
        Debug.Log("[NetworkManager] A infraestrutura local assumiu a conexão.");
    }

    public void JoinMatchmaking(int gameMode)
    {
        Debug.Log($"[NetworkManager] Entrando na fila ({gameMode} players)...");
        var req = new MatchmakingJoinQueue { GameMode = gameMode, CharacterId = "Char_Solaris" };
        LocalServerClient.Instance.SendPacket(PacketType.Matchmaking_JoinQueue, req);
    }

    private void HandleNetworkPackets(BasePacket packet)
    {
        if (packet.Type == PacketType.Matchmaking_MatchFound)
        {
            var res = packet.GetPayload<MatchmakingMatchFound>();
            Debug.Log($"[NetworkManager] Partida encontrada! Sala: {res.RoomId}. Sou o ator: {res.ActorNumber}");

            // Grava o ID recebido pelo servidor autoritativo
            PlayerPrefs.SetInt("LocalActorNumber", res.ActorNumber);

            // Exemplo: Carregar a cena
            // UnityEngine.SceneManagement.SceneManager.LoadScene("BattleArenaScene");

            // Para fim de compatibilidade com GameManager, simula inicialização
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SpawnPlayerLocal();
            }
        }
    }
}
