using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro; // TextMeshPro para UI moderna na Unity

public class MatchmakingUI : MonoBehaviourPunCallbacks
{
    [Header("UI do Jogador (PlayFab)")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text softCurrencyText;
    [SerializeField] private TMP_Text premiumCurrencyText;

    [Header("UI de Matchmaking (Photon)")]
    [SerializeField] private Button findMatchButton;
    [SerializeField] private TMP_Text connectionStatusText;

    private void Start()
    {
        // Garante que o botão comece desativado até conectar
        findMatchButton.interactable = false;
        findMatchButton.onClick.AddListener(OnFindMatchClicked);

        // Se já estiver conectado ao iniciar a tela
        if (PhotonNetwork.IsConnectedAndReady)
        {
            UpdateConnectionStatus("Conectado ao Master. Pronto para jogar!");
            findMatchButton.interactable = true;
        }
        else
        {
            UpdateConnectionStatus("Conectando aos servidores...");
        }

        InvokeRepeating(nameof(UpdatePlayerEconomyUI), 1f, 2f); // Atualiza os dados a cada 2 segundos
    }

    /// <summary>
    /// Método chamado pelo botão "Encontrar Partida"
    /// </summary>
    private void OnFindMatchClicked()
    {
        UpdateConnectionStatus("Buscando oponentes...");
        findMatchButton.interactable = false; // Impede múltiplos cliques

        // Solicita ao NetworkManager que entre numa sala aleatória
        if (NetworkManager.Instance != null)
        {
            PhotonNetwork.JoinRandomRoom();
        }
    }

    /// <summary>
    /// Tenta buscar os dados do EconomyManager/AuthManager e atualiza a tela
    /// </summary>
    private void UpdatePlayerEconomyUI()
    {
        if (PlayFabAuthManager.Instance != null && PlayFabAuthManager.Instance.IsLoggedIn)
        {
            // Atualiza o nome, pegando do PhotonNetwork (que foi setado pelo PlayFab)
            playerNameText.text = string.IsNullOrEmpty(PhotonNetwork.NickName)
                ? "Jogador Desconhecido"
                : PhotonNetwork.NickName;

            // Atualiza moedas do EconomyManager
            if (EconomyManager.Instance != null)
            {
                softCurrencyText.text = EconomyManager.Instance.SoftCurrency.ToString();
                premiumCurrencyText.text = EconomyManager.Instance.PremiumCurrency.ToString();
            }
        }
    }

    private void UpdateConnectionStatus(string status)
    {
        if (connectionStatusText != null)
            connectionStatusText.text = status;

        Debug.Log("[Matchmaking UI] " + status);
    }

    #region PUN Callbacks para Feedback de UI

    public override void OnConnectedToMaster()
    {
        UpdateConnectionStatus("Conectado ao Master. Pronto para jogar!");
        findMatchButton.interactable = true;
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        UpdateConnectionStatus("Desconectado: " + cause.ToString());
        findMatchButton.interactable = false;
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        UpdateConnectionStatus("Nenhuma sala disponível. Criando nova arena...");
        // A lógica de criar a sala já está no NetworkManager, aqui damos apenas o feedback
    }

    public override void OnJoinedRoom()
    {
        UpdateConnectionStatus("Sala encontrada! Entrando na arena...");
        // Desliga a UI principal ou carrega a cena de loading
        this.gameObject.SetActive(false);
    }

    #endregion
}
