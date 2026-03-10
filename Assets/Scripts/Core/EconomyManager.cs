using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class EconomyManager : MonoBehaviour
{
    // Singleton para fácil acesso à economia do jogador (GaaS)
    public static EconomyManager Instance;

    // Saldo atual de Soft Currency (Moedas ganhas jogando, Ex: "GC" - Gold Coins)
    public int SoftCurrency { get; private set; }

    // Saldo atual de Premium Currency (Moedas compradas com dinheiro, Ex: "PC" - Premium Coins)
    public int PremiumCurrency { get; private set; }

    // Lista de itens cosméticos ou passes de batalha do jogador
    public List<ItemInstance> PlayerInventory { get; private set; } = new List<ItemInstance>();

    private void Awake()
    {
        // Garante instância única
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
        // Ao iniciar, espera-se que o PlayFabAuthManager faça o login
        // A busca por inventário e moedas é chamada assim que o login é bem sucedido
    }

    /// <summary>
    /// Busca o inventário e o saldo de moedas virtuais do jogador logado
    /// </summary>
    public void GetUserInventory()
    {
        Debug.Log("Buscando inventário e moedas virtuais do jogador...");

        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
            OnGetUserInventorySuccess,
            OnEconomyError);
    }

    /// <summary>
    /// Processa o inventário recebido (LTV e Monetização)
    /// </summary>
    private void OnGetUserInventorySuccess(GetUserInventoryResult result)
    {
        Debug.Log("Inventário recebido com sucesso.");

        // Atualiza moedas
        if (result.VirtualCurrency != null)
        {
            // "GC" é o código para Gold Coins (Soft) e "PC" para Premium Coins (Hard)
            SoftCurrency = result.VirtualCurrency.ContainsKey("GC") ? result.VirtualCurrency["GC"] : 0;
            PremiumCurrency = result.VirtualCurrency.ContainsKey("PC") ? result.VirtualCurrency["PC"] : 0;

            Debug.Log($"Saldo do jogador: {SoftCurrency} Gold | {PremiumCurrency} Premium");
        }

        // Atualiza itens (Skins, Passe de Batalha, etc)
        PlayerInventory = result.Inventory;

        if (PlayerInventory.Count > 0)
        {
            Debug.Log($"O jogador possui {PlayerInventory.Count} item(s) no inventário.");
            foreach (var item in PlayerInventory)
            {
                Debug.Log($"- {item.DisplayName} ({item.ItemClass})");
                // Aqui podemos adicionar lógica para equipar skins dependendo da classe do item
            }
        }
        else
        {
            Debug.Log("O inventário do jogador está vazio.");
        }
    }

    /// <summary>
    /// Compra um item da loja usando moedas virtuais
    /// </summary>
    /// <param name="itemId">ID do item no catálogo do PlayFab</param>
    /// <param name="currencyCode">Código da moeda (Ex: "GC" ou "PC")</param>
    /// <param name="price">Preço esperado para validar a compra</param>
    public void PurchaseItem(string itemId, string currencyCode, int price)
    {
        Debug.Log($"Iniciando compra do item: {itemId} por {price} {currencyCode}");

        var request = new PurchaseItemRequest
        {
            CatalogVersion = "MainCatalog", // Nome do seu catálogo padrão no painel do PlayFab
            ItemId = itemId,
            VirtualCurrency = currencyCode,
            Price = price
        };

        PlayFabClientAPI.PurchaseItem(request,
            result =>
            {
                Debug.Log($"Compra de '{result.Items[0].DisplayName}' realizada com sucesso!");
                // Após a compra, atualizamos o inventário local
                GetUserInventory();
            },
            OnEconomyError);
    }

    /// <summary>
    /// Adiciona soft currency como recompensa por jogar (via Cloud Script)
    /// Para segurança, adições de moedas e XP devem ser validadas no servidor,
    /// portanto, chamamos um Cloud Script em vez de conceder moedas diretamente pelo cliente.
    /// </summary>
    public void GrantMatchRewards()
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "GrantMatchRewards", // O nome da função Node.js que você vai ter no Azure
            FunctionParameter = new { matchDuration = 300, isWinner = true }, // Exemplo de dados da partida
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result =>
            {
                Debug.Log("Recompensas de partida validadas pelo servidor.");
                // Ao receber a confirmação de que moedas/xp foram dadas, atualiza a tela
                GetUserInventory();
            },
            OnEconomyError);
    }

    /// <summary>
    /// Callback de erro nas chamadas de economia
    /// </summary>
    private void OnEconomyError(PlayFabError error)
    {
        Debug.LogError("Erro nas requisições de economia/inventário:");
        Debug.LogError(error.GenerateErrorReport());
    }
}
