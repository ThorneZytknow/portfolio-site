/**
 * Script em Nuvem (Cloud Script) para Azure PlayFab.
 * Arquivo a ser enviado para o painel de Game Manager do PlayFab (Automation > Cloud Script).
 * Seu objetivo é processar a lógica crítica de recompensas do lado do servidor (Server-Side),
 * evitando fraudes do cliente (Pay-to-Win, Cheat Engine).
 */

handlers.GrantMatchRewards = function (args, context) {
    var duration = args.matchDuration;
    var isWin = args.isWinner;

    // Obter o ID do Jogador (Automaticamente setado pela API no contexto do chamado)
    var currentPlayerId = currentPlayerId;

    log.info("Processando recompensa para o jogador: " + currentPlayerId);

    // Validação de Segurança Básica: Impedir matches muito rápidos (Hacks de Speed/Win)
    if (duration < 30) {
        log.error("Partida muito curta (Suspeita de fraude). Nenhuma recompensa concedida.");
        return { success: false, reason: "Match duration too short" };
    }

    // Lógica Base: Jogador ganha 1 Gold Coin (GC) por minuto jogado + Bônus de Vitória
    var minutesPlayed = Math.floor(duration / 60);
    var coinsToGrant = minutesPlayed * 1;

    if (isWin) coinsToGrant += 5;

    if (coinsToGrant <= 0) return { success: true, coinsGranted: 0 };

    // Chamada à API de Servidor do PlayFab para depositar a Soft Currency
    var result = server.AddUserVirtualCurrency({
        PlayFabId: currentPlayerId,
        VirtualCurrency: "GC",
        Amount: coinsToGrant
    });

    if (result && result.Balance) {
        return { success: true, coinsGranted: coinsToGrant, newBalance: result.Balance };
    } else {
        return { success: false, reason: "Server API error" };
    }
};

/**
 * GrantMatchXP: Calcula e concede XP (Nível da Conta e Passe de Batalha) após uma partida
 */
handlers.GrantMatchXP = function (args, context) {
    var duration = args.duration; // em segundos
    var kills = args.kills;
    var placement = args.placement; // 1 = Vencedor

    if (duration < 20) {
        return { success: false, reason: "Partida rápida demais para receber XP." };
    }

    // Calcula XP da Partida (Exemplo: 10 XP por Kill + 1 XP por 2 segundos vivos + Bônus de Vitória)
    var matchXP = (kills * 10) + Math.floor(duration / 2);
    if (placement === 1) matchXP += 50;

    log.info("Concedendo " + matchXP + " XP ao jogador.");

    // Atualiza a estatística "TotalXP" no PlayFab
    var getStats = server.GetPlayerStatistics({
        PlayFabId: currentPlayerId,
        StatisticNames: ["TotalXP"]
    });

    var currentXP = 0;
    if (getStats && getStats.Statistics && getStats.Statistics.length > 0) {
        currentXP = getStats.Statistics[0].Value;
    }

    var newXP = currentXP + matchXP;

    server.UpdatePlayerStatistics({
        PlayFabId: currentPlayerId,
        Statistics: [{ StatisticName: "TotalXP", Value: newXP }]
    });

    return { success: true, xpGranted: matchXP, totalXp: newXP };
};

/**
 * AdvanceBattlePassTier: Incrementa o progresso do Passe de Batalha com validação Server-Side
 */
handlers.AdvanceBattlePassTier = function (args, context) {
    var xpEarned = args.xpEarned;
    var hasPremium = args.hasPremium; // Confirmação local (Idealmente checar o inventário aqui no servidor também)

    // Pega o Tier Atual
    var getStats = server.GetPlayerStatistics({
        PlayFabId: currentPlayerId,
        StatisticNames: ["BattlePassTier", "BattlePassXP"]
    });

    var currentTier = 1;
    var currentPassXP = 0;

    if (getStats && getStats.Statistics) {
        for (var i = 0; i < getStats.Statistics.length; i++) {
            if (getStats.Statistics[i].StatisticName === "BattlePassTier") currentTier = getStats.Statistics[i].Value;
            if (getStats.Statistics[i].StatisticName === "BattlePassXP") currentPassXP = getStats.Statistics[i].Value;
        }
    }

    // Fórmula Exemplo: 1000 XP por Tier
    var xpRequiredPerTier = 1000;
    var newPassXP = currentPassXP + xpEarned;
    var tiersGained = Math.floor(newPassXP / xpRequiredPerTier);
    var remainderXP = newPassXP % xpRequiredPerTier;

    if (tiersGained > 0) {
        currentTier += tiersGained;
        log.info("Jogador subiu " + tiersGained + " níveis no Passe! Nível Atual: " + currentTier);

        // Aqui chamaria a API para conceder os itens desbloqueados com base na tabela do passe
        // ex: if (currentTier == 5) server.GrantItemsToUser(...)
    }

    server.UpdatePlayerStatistics({
        PlayFabId: currentPlayerId,
        Statistics: [
            { StatisticName: "BattlePassTier", Value: currentTier },
            { StatisticName: "BattlePassXP", Value: remainderXP }
        ]
    });

    return { success: true, currentTier: currentTier, xpProgress: remainderXP };
};

/**
 * PurchaseBattlePass: Debita a moeda Premium (PC) e concede o item do Passe de Batalha (GaaS)
 */
handlers.PurchaseBattlePass = function (args, context) {
    var passPrice = 500; // Preço do passe premium em Premium Coins (PC)
    var passItemId = "Item_BattlePass_Season1";

    // Subtrai a moeda (Se não tiver saldo suficiente, a API retornará erro)
    var subtractResult = server.SubtractUserVirtualCurrency({
        PlayFabId: currentPlayerId,
        VirtualCurrency: "PC",
        Amount: passPrice
    });

    if (subtractResult && subtractResult.Balance >= 0) {
        log.info("Moeda debitada com sucesso. Saldo restante: " + subtractResult.Balance);

        // Concede o item no inventário
        var grantResult = server.GrantItemsToUser({
            PlayFabId: currentPlayerId,
            ItemIds: [passItemId],
            CatalogVersion: "MainCatalog"
        });

        if (grantResult && grantResult.ItemGrantResults) {
            log.info("Passe Premium concedido com sucesso!");
            return { success: true, item: passItemId };
        } else {
            return { success: false, reason: "Failed to grant item" };
        }
    } else {
        return { success: false, reason: "Insufficient funds" };
    }
};

/**
 * Função exemplo de boas-vindas: Quando um jogador novo se cadastra
 */
handlers.OnUserAccountCreated = function (args, context) {
    var initialBonus = 50;
    server.AddUserVirtualCurrency({
        PlayFabId: currentPlayerId,
        VirtualCurrency: "GC",
        Amount: initialBonus
    });
};
