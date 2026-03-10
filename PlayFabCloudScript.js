/**
 * Script em Nuvem (Cloud Script) para Azure PlayFab.
 * Arquivo a ser enviado para o painel de Game Manager do PlayFab (Automation > Cloud Script).
 * Seu objetivo é processar a lógica crítica de recompensas do lado do servidor (Server-Side),
 * evitando fraudes do cliente (Pay-to-Win, Cheat Engine).
 */

handlers.GrantMatchRewards = function (args, context) {
    // args contem parâmetros enviados pelo cliente C#
    var duration = args.matchDuration; // Segundos jogados
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

    // Bônus se vencer
    if (isWin) {
        coinsToGrant += 5;
    }

    // Se o cálculo der zero moedas, não há necessidade de chamar a API
    if (coinsToGrant <= 0) {
        return { success: true, coinsGranted: 0 };
    }

    log.info("Recompensando jogador com: " + coinsToGrant + " GC");

    // Chamada à API de Servidor do PlayFab para depositar a Soft Currency
    var result = server.AddUserVirtualCurrency({
        PlayFabId: currentPlayerId,
        VirtualCurrency: "GC",
        Amount: coinsToGrant
    });

    if (result && result.Balance) {
        return {
            success: true,
            coinsGranted: coinsToGrant,
            newBalance: result.Balance
        };
    } else {
        log.error("Erro ao adicionar moedas na conta do PlayFab.");
        return { success: false, reason: "Server API error" };
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

    log.info("Bônus de iniciante de " + initialBonus + " GC depositado.");
};
