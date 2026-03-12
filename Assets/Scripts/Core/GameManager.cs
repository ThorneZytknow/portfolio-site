using UnityEngine;
using BrawlerShared.Enums;
using BrawlerShared.Packets;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Tooltip("Prefab do jogador que deve estar na pasta Resources")]
    [SerializeField] private GameObject playerPrefab;

    [Tooltip("Pontos de surgimento na arena")]
    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Instancia o jogador na rede local
    /// </summary>
    public void SpawnPlayerLocal()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Prefab do Jogador não assinalado no GameManager!");
            return;
        }

        int actorNumber = PlayerPrefs.GetInt("LocalActorNumber", 1);

        Vector3 spawnPosition = Vector3.zero;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnIndex = actorNumber % spawnPoints.Length;
            spawnPosition = spawnPoints[spawnIndex].position;
        }

        // Instancia o objeto localmente
        GameObject myPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        Debug.Log("Jogador instanciado em: " + spawnPosition);

        // Configura o PlayerController como local
        PlayerController controller = myPlayer.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.isLocalPlayer = true;
            controller.actorNumber = actorNumber;
        }

        // Avisa ao Servidor que Spawnou (Opcional, pois no protótipo o TCP gerencia os connects)
        if (LocalServerClient.Instance != null)
        {
            LocalServerClient.Instance.SendPacket(PacketType.Game_SpawnPlayer, new { ActorId = actorNumber, CharId = "Char_Solaris" });
        }

        // Registra o jogador na câmera dinâmica
        CameraController camController = FindObjectOfType<CameraController>();
        if (camController != null)
        {
            camController.AddTarget(myPlayer.transform);
        }

        // Registra o jogador no HUDManager
        if (HUDManager.Instance != null)
        {
            int startingStocks = 3;
            HUDManager.Instance.RegisterPlayer(actorNumber, "Player " + actorNumber, startingStocks);
        }
    }
}
