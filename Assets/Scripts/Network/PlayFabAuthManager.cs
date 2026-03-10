using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class PlayFabAuthManager : MonoBehaviour
{
    // Instância única para fácil acesso
    public static PlayFabAuthManager Instance;

    public string playFabTitleId; // ID do título fornecido no painel do PlayFab

    // Variável para armazenar o ID único do jogador no backend
    public string PlayFabId { get; private set; }

    // Indica se o jogador está logado e o token é válido
    public bool IsLoggedIn { get; private set; }

    // Evento disparado quando a autenticação assíncrona é concluída com sucesso
    public delegate void OnLoginSuccessAction();
    public static event OnLoginSuccessAction OnLoginSuccessEvent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        // Define o ID do título se não for configurado pelo inspector
        if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId))
        {
            PlayFabSettings.staticSettings.TitleId = playFabTitleId;
        }
    }

    private void Start()
    {
        // Tenta logar o jogador assim que o jogo inicia
        Login();
    }

    /// <summary>
    /// Realiza um login silencioso utilizando um Custom ID gerado pelo dispositivo do usuário
    /// </summary>
    public void Login()
    {
        Debug.Log("Iniciando login no PlayFab...");

        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier, // ID único do dispositivo (Útil para mobile)
            CreateAccount = true // Cria conta se não existir
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    /// <summary>
    /// Callback disparado em caso de sucesso na autenticação
    /// </summary>
    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Login PlayFab Bem-Sucedido! Criado na primeira vez? " + result.NewlyCreated);

        IsLoggedIn = true;
        PlayFabId = result.PlayFabId;

        // Agora que estamos logados no PlayFab, podemos buscar dados de jogador, inventário ou conectar ao Photon
        GetPlayerProfile(PlayFabId);

        // Exemplo: Buscar a economia e itens do jogador
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.GetUserInventory();
        }

        // Dispara o evento para outros Managers buscarem seus dados assíncronos
        OnLoginSuccessEvent?.Invoke();

        // Como o login é o pré-requisito, podemos iniciar o matchmaking do Photon aqui
        if (NetworkManager.Instance != null && !Photon.Pun.PhotonNetwork.IsConnected)
        {
            NetworkManager.Instance.ConnectToServer();
        }
    }

    /// <summary>
    /// Callback disparado em caso de falha na autenticação
    /// </summary>
    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError("Erro no login PlayFab:");
        Debug.LogError(error.GenerateErrorReport());
        IsLoggedIn = false;

        // Aqui poderia ser mostrada uma tela de "Tentar Novamente" para o usuário
    }

    /// <summary>
    /// Busca informações do perfil do jogador (ex: Nome, Avatar)
    /// </summary>
    public void GetPlayerProfile(string playFabId)
    {
        var request = new GetPlayerProfileRequest
        {
            PlayFabId = playFabId,
            ProfileConstraints = new PlayerProfileViewConstraints
            {
                ShowDisplayName = true
            }
        };

        PlayFabClientAPI.GetPlayerProfile(request, result =>
        {
            Debug.Log("Perfil Carregado. DisplayName: " + result.PlayerProfile.DisplayName);
            // Sincroniza o nome do jogador com o Photon
            if (result.PlayerProfile.DisplayName != null)
            {
                Photon.Pun.PhotonNetwork.NickName = result.PlayerProfile.DisplayName;
            }
        }, OnLoginFailure);
    }

    /// <summary>
    /// Atualiza o nome de exibição do jogador no servidor
    /// </summary>
    public void SetPlayerDisplayName(string newName)
    {
        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = newName
        };

        PlayFabClientAPI.UpdateUserTitleDisplayName(request, result =>
        {
            Debug.Log("Nome de exibição atualizado para: " + result.DisplayName);
            Photon.Pun.PhotonNetwork.NickName = result.DisplayName;
        }, OnLoginFailure);
    }
}
