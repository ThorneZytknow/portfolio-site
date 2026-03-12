using UnityEngine;
using BrawlerShared.Enums;
using BrawlerShared.Packets;

public class LocalAuthManager : MonoBehaviour
{
    public static LocalAuthManager Instance;

    public string PlayerId { get; private set; }
    public string SessionToken { get; private set; }
    public bool IsLoggedIn { get; private set; }

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
    }

    private void Start()
    {
        // Conecta o socket primeiro, depois escuta a resposta
        if (LocalServerClient.Instance != null)
        {
            LocalServerClient.Instance.OnAnyPacketReceived += HandleAuthPackets;
            LocalServerClient.Instance.ConnectToServer();

            // Dá um tempo curto pro socket abrir e manda
            Invoke(nameof(Login), 1f);
        }
    }

    public void Login()
    {
        Debug.Log("[LocalAuth] Enviando pedido de login local...");

        var request = new AuthLoginRequest
        {
            Username = "DevUser_" + Random.Range(100, 999), // Mock rápido
            DeviceId = SystemInfo.deviceUniqueIdentifier
        };

        LocalServerClient.Instance.SendPacket(PacketType.Auth_LoginRequest, request);
    }

    private void HandleAuthPackets(BasePacket packet)
    {
        if (packet.Type == PacketType.Auth_LoginResponse)
        {
            var res = packet.GetPayload<AuthLoginResponse>();
            if (res.Success)
            {
                IsLoggedIn = true;
                PlayerId = res.PlayerId;
                SessionToken = res.SessionToken;

                PlayerPrefs.SetString("PlayerId", PlayerId);
                Debug.Log($"[LocalAuth] Sucesso! ID: {PlayerId}");

                OnLoginSuccessEvent?.Invoke();
            }
            else
            {
                Debug.LogError($"[LocalAuth] Erro: {res.ErrorMessage}");
            }
        }
    }
}
