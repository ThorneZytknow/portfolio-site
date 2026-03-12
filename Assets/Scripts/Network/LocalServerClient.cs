using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;
using BrawlerShared.Packets;
using BrawlerShared.Enums;

public class LocalServerClient : MonoBehaviour
{
    public static LocalServerClient Instance;

    public string serverIp = "127.0.0.1";
    public int tcpPort = 7777;

    private TcpClient tcpClient;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isConnected = false;

    // Fila Thread-Safe para que a thread de rede jogue os pacotes pra thread principal da Unity (Update)
    private ConcurrentQueue<BasePacket> packetQueue = new ConcurrentQueue<BasePacket>();

    // Eventos
    public delegate void OnPacketReceived(BasePacket packet);
    public event OnPacketReceived OnAnyPacketReceived;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ConnectToServer()
    {
        if (isConnected) return;

        // Puxa as configs do LocalConfig carregado do JSON
        if (LocalConfig.Instance != null && LocalConfig.Instance.Config != null)
        {
            serverIp = LocalConfig.Instance.Config.serverHost;
            tcpPort = LocalConfig.Instance.Config.tcpPort;
        }

        try
        {
            tcpClient = new TcpClient();
            tcpClient.Connect(serverIp, tcpPort);
            stream = tcpClient.GetStream();
            isConnected = true;

            Debug.Log($"[LocalServerClient] Conectado ao servidor TCP {serverIp}:{tcpPort}");

            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalServerClient] Falha ao conectar: {e.Message}");
        }
    }

    private void ReceiveLoop()
    {
        byte[] buffer = new byte[4096];
        StringBuilder jsonBuilder = new StringBuilder();

        try
        {
            while (isConnected && stream != null)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                jsonBuilder.Append(data);

                string content = jsonBuilder.ToString();
                int newlineIndex;
                while ((newlineIndex = content.IndexOf('\n')) >= 0)
                {
                    string packetJson = content.Substring(0, newlineIndex);
                    content = content.Substring(newlineIndex + 1);
                    jsonBuilder.Clear();
                    jsonBuilder.Append(content);

                    // Joga pra fila pra thread da Unity processar
                    BasePacket packet = BasePacket.DeserializeWrapper(packetJson);
                    packetQueue.Enqueue(packet);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LocalServerClient] Fio de rede encerrado: {e.Message}");
        }
        finally
        {
            isConnected = false;
        }
    }

    private void Update()
    {
        // Processa os pacotes na Main Thread (Unity segura para atualizar UI/Objetos)
        while (packetQueue.TryDequeue(out BasePacket packet))
        {
            OnAnyPacketReceived?.Invoke(packet);
        }
    }

    public void SendPacket<T>(PacketType type, T payload)
    {
        if (!isConnected || stream == null) return;

        try
        {
            string playerId = PlayerPrefs.GetString("PlayerId", "LocalPlayer");
            byte[] data = BasePacket.Serialize(type, playerId, payload);
            stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalServerClient] Erro ao enviar pacote {type}: {e.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        isConnected = false;
        stream?.Close();
        tcpClient?.Close();
        // A flag isConnected fará o loop principal do Thread sair naturalmente na próxima leitura
        // receiveThread?.Join(500); // Opcional, aguardar a thread finalizar
    }
}
