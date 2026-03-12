using UnityEngine;
using System.IO;

[System.Serializable]
public class ServerConfigData
{
    public string serverHost;
    public int tcpPort;
    public int udpPort;
    public bool autoConnect;
    public bool debugLogs;
}

public class LocalConfig : MonoBehaviour
{
    public static LocalConfig Instance;
    public ServerConfigData Config { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        LoadConfig();
    }

    private void LoadConfig()
    {
        TextAsset configFile = Resources.Load<TextAsset>("LocalServerConfig");
        if (configFile != null)
        {
            Config = JsonUtility.FromJson<ServerConfigData>(configFile.text);
            Debug.Log($"[LocalConfig] Carregado: {Config.serverHost}:{Config.tcpPort}");
        }
        else
        {
            Debug.LogWarning("[LocalConfig] LocalServerConfig.json não encontrado em Resources. Usando defaults.");
            Config = new ServerConfigData
            {
                serverHost = "127.0.0.1",
                tcpPort = 7777,
                udpPort = 7778,
                autoConnect = true,
                debugLogs = true
            };
        }
    }
}
