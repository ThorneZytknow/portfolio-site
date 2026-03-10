using UnityEngine;
using Photon.Pun;
using TMPro;
using System.Collections.Generic;

public class MatchResultsManager : MonoBehaviourPunCallbacks
{
    public static MatchResultsManager Instance;

    [Header("Estatísticas da Partida")]
    public int kills = 0;
    public int deaths = 0;
    public float damageDealt = 0f;
    public float survivalTime = 0f; // Em segundos
    private float matchStartTime = 0f;

    [Header("Tela de Resultados UI")]
    public GameObject resultsPanel; // Painel principal que cobre a tela
    public TMP_Text winnerText; // "VITÓRIA" ou "DERROTA"
    public TMP_Text statsText; // Exibição de Kills/Dano/Tempo
    public TMP_Text rewardText; // "Carregando recompensas..."

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        matchStartTime = Time.time;
        if (resultsPanel != null) resultsPanel.SetActive(false);
    }

    /// <summary>
    /// Chamado pelo GameManager ou StockSystem quando resta apenas 1 jogador vivo (ou tempo esgota)
    /// </summary>
    public void EndMatch(bool isWinner)
    {
        // Se a partida já acabou
        if (resultsPanel.activeSelf) return;

        survivalTime = Time.time - matchStartTime;
        Debug.Log($"[MatchResults] Partida Encerrada. Sobreviveu por: {survivalTime}s");

        ShowResultsScreen(isWinner);

        // Dispara requisição ao PlayFab para validar estatísticas e ganhar recompensas (Anti-Cheat Server-Side)
        if (ProgressionManager.Instance != null)
        {
            int finalPlacement = isWinner ? 1 : 2; // Simplificado para 1v1 ou Winner Takes All
            ProgressionManager.Instance.GrantMatchXP(kills, (int)survivalTime, finalPlacement);

            // EconomyManager também avalia se dropa moeda
            if (EconomyManager.Instance != null)
            {
                // A grant já está no CloudScript original, mas atualizamos UI aqui
                EconomyManager.Instance.GrantMatchRewards();
            }
        }
    }

    /// <summary>
    /// Exibe os dados locais na tela enquanto o servidor processa o XP
    /// </summary>
    private void ShowResultsScreen(bool isWinner)
    {
        if (resultsPanel != null) resultsPanel.SetActive(true);

        winnerText.text = isWinner ? "<color=yellow>VITÓRIA!</color>" : "<color=red>DERROTA</color>";

        statsText.text = $"Nocautes: {kills}\n" +
                         $"Mortes: {deaths}\n" +
                         $"Dano Causado: {Mathf.FloorToInt(damageDealt)}%\n" +
                         $"Tempo Vivo: {Mathf.FloorToInt(survivalTime)}s";

        rewardText.text = "Sincronizando XP com o Servidor...";

        // Timer automático para voltar ao Menu (Matchmaking) após a tela final
        Invoke(nameof(ReturnToLobby), 10f);
    }

    /// <summary>
    /// Desconecta da sala do Photon e volta para a cena inicial (Menu/Lobby)
    /// </summary>
    private void ReturnToLobby()
    {
        Debug.Log("[MatchResults] Voltando para o Lobby...");
        PhotonNetwork.LeaveRoom(); // Desconecta
    }

    public override void OnLeftRoom()
    {
        // Ao sair da sala de combate com sucesso, carrega a UI de Matchmaking (Scene)
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    // Métodos para acumular estatísticas ao longo da partida (Chamados pelo CombatSystem e StockSystem local)
    public void AddKill() { kills++; }
    public void AddDeath() { deaths++; }
    public void AddDamage(float amount) { damageDealt += amount; }
}
