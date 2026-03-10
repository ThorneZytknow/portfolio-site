using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(PhotonView))]
public class GameManager : MonoBehaviourPunCallbacks
{
    [Tooltip("Prefab do jogador que deve estar na pasta Resources")]
    [SerializeField] private GameObject playerPrefab;

    [Tooltip("Pontos de surgimento na arena")]
    [SerializeField] private Transform[] spawnPoints;

    private void Start()
    {
        // Ao carregar a cena, verifica se está conectado
        if (PhotonNetwork.IsConnectedAndReady)
        {
            SpawnPlayer();
        }
        else
        {
            Debug.LogWarning("Tentando carregar a cena do GameManager sem conexão ativa com o Photon. Redirecionando ou tentando conectar...");
            // Exemplo fallback
            NetworkManager.Instance.ConnectToServer();
        }
    }

    /// <summary>
    /// Instancia o jogador na rede usando PhotonNetwork.Instantiate
    /// </summary>
    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Prefab do Jogador não assinalado no GameManager!");
            return;
        }

        // Seleciona um ponto de spawn aleatório ou baseado no ID do jogador
        int spawnIndex = Random.Range(0, spawnPoints.Length);
        Vector3 spawnPosition = Vector3.zero;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Tenta usar o ID do jogador (módulo) para não caírem no mesmo ponto se possível
            spawnIndex = PhotonNetwork.LocalPlayer.ActorNumber % spawnPoints.Length;
            spawnPosition = spawnPoints[spawnIndex].position;
        }
        else
        {
            Debug.LogWarning("Nenhum SpawnPoint configurado, surgindo na origem.");
        }

        // Instancia o objeto em todos os clientes
        GameObject myPlayer = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
        Debug.Log("Jogador instanciado em: " + spawnPosition);

        // Registra o jogador na câmera dinâmica
        CameraController camController = FindObjectOfType<CameraController>();
        if (camController != null)
        {
            camController.AddTarget(myPlayer.transform);
        }
        else
        {
            Debug.LogWarning("[GameManager] CameraController não encontrado na cena!");
        }

        // Registra o jogador no HUDManager
        if (HUDManager.Instance != null)
        {
            // O jogador começa com 3 vidas por padrão de código, aqui poderia vir da sala custom properties
            int startingStocks = 3;
            // Se o StockSystem for instanciado antes, ele avisa. Mas como estamos criando agora:
            HUDManager.Instance.RegisterPlayer(PhotonNetwork.LocalPlayer.ActorNumber, PhotonNetwork.NickName, startingStocks);
        }

        // Emite um RPC para garantir que clientes remotos também registrem minha cópia nas câmeras deles
        photonView.RPC("RegisterRemotePlayerRPC", RpcTarget.Others, myPlayer.GetComponent<PhotonView>().ViewID);
    }

    [PunRPC]
    public void RegisterRemotePlayerRPC(int viewID)
    {
        PhotonView pView = PhotonView.Find(viewID);
        if (pView != null)
        {
            CameraController camController = FindObjectOfType<CameraController>();
            if (camController != null) camController.AddTarget(pView.transform);

            if (HUDManager.Instance != null)
            {
                int startingStocks = 3;
                HUDManager.Instance.RegisterPlayer(pView.OwnerActorNr, pView.Owner.NickName, startingStocks);
            }
        }
    }

    #region Callbacks do Photon (Gestão da Sala)

    public override void OnLeftRoom()
    {
        Debug.Log("Deixou a sala atual. Voltando para o menu.");
        // Ação típica: Carregar cena do menu inicial
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.LogFormat("Novo desafiante chegou: {0}", newPlayer.NickName);

        // Se for o host (Master), pode atualizar placares, estados do jogo, etc.
        if (PhotonNetwork.IsMasterClient)
        {
            UpdateRoomState();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.LogFormat("Jogador {0} fugiu da batalha.", otherPlayer.NickName);
    }

    /// <summary>
    /// Exemplo de método para gerenciar estados globais do jogo
    /// </summary>
    private void UpdateRoomState()
    {
        // Pode fechar a sala se estiver cheia
        if (PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false; // Ninguém mais entra
            Debug.Log("A sala está cheia! Início do combate.");
            // Inicia timer de contagem, etc
        }
    }

    #endregion
}
