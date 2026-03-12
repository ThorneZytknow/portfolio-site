using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchmakingUI : MonoBehaviour
{
    [Header("UI do Jogador (LocalAuth)")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text softCurrencyText;
    [SerializeField] private TMP_Text premiumCurrencyText;

    [Header("UI de Matchmaking (LocalServer)")]
    [SerializeField] private Button findMatchButton;
    [SerializeField] private TMP_Text connectionStatusText;

    private void Start()
    {
        findMatchButton.interactable = true;
        findMatchButton.onClick.AddListener(OnFindMatchClicked);

        UpdateConnectionStatus("Conectando ao Servidor Local...");

        InvokeRepeating(nameof(UpdatePlayerEconomyUI), 1f, 2f);
    }

    private void OnFindMatchClicked()
    {
        UpdateConnectionStatus("Entrando na fila de Matchmaking...");
        findMatchButton.interactable = false;

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.JoinMatchmaking(2); // 2 players (1v1) by default
        }
    }

    private void UpdatePlayerEconomyUI()
    {
        if (LocalAuthManager.Instance != null && LocalAuthManager.Instance.IsLoggedIn)
        {
            playerNameText.text = string.IsNullOrEmpty(LocalAuthManager.Instance.PlayerId)
                ? "Aguardando Login..."
                : "Jogador: " + LocalAuthManager.Instance.PlayerId.Substring(0, 8);

            if (EconomyManager.Instance != null)
            {
                softCurrencyText.text = EconomyManager.Instance.SoftCurrency.ToString() + " SC";
                premiumCurrencyText.text = EconomyManager.Instance.PremiumCurrency.ToString() + " PC";
            }

            UpdateConnectionStatus("Conectado. Pronto para jogar!");
        }
    }

    private void UpdateConnectionStatus(string status)
    {
        if (connectionStatusText != null)
            connectionStatusText.text = status;
    }
}
