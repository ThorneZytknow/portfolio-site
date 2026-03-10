using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    // Singleton pattern para fácil acesso ao NetworkManager
    public static NetworkManager Instance;

    [Tooltip("A versão do jogo. Usuários são separados por roomName e gameVersion")]
    [SerializeField] private string gameVersion = "1.0";

    private void Awake()
    {
        // Garante que exista apenas uma instância
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        // Sincroniza o carregamento da cena para todos os clientes
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        ConnectToServer();
    }

    /// <summary>
    /// Inicia a conexão com o servidor Master do Photon
    /// </summary>
    public void ConnectToServer()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Já conectado ao servidor Master do Photon. Juntando-se a uma sala aleatória.");
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            Debug.Log("Conectando ao servidor Master do Photon...");
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    #region PUN Callbacks

    public override void OnConnectedToMaster()
    {
        Debug.Log("Conectado ao Servidor Master com sucesso.");
        // Assim que conectado ao master, tenta entrar em uma sala aleatória
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarningFormat("Desconectado do servidor. Razão: {0}", cause);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("Falha ao entrar em sala aleatória (Nenhuma sala disponível). Criando uma nova sala...");
        // Se falhar (ex: nenhuma sala criada), cria uma nova com opções padrão
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 4 };
        PhotonNetwork.CreateRoom(null, roomOptions);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Entrou na sala com sucesso. Jogadores na sala: " + PhotonNetwork.CurrentRoom.PlayerCount);
        // Após entrar na sala, se for o Master Client (Host), pode carregar a cena do jogo
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Carregando cena da arena de batalha...");
            // Exemplo: PhotonNetwork.LoadLevel("BattleArenaScene");
            // Para efeitos de teste inicial, não carregamos a cena aqui ainda,
            // o GameManager lidará com o spawn
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.LogFormat("Jogador {0} entrou na sala", newPlayer.NickName);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.LogFormat("Jogador {0} saiu da sala", otherPlayer.NickName);
    }

    #endregion
}
