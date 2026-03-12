# Brawler GaaS — Servidor Local (Offline First)

Bem-vindo à arquitetura **100% Local (Self-Hosted)** do nosso projeto Brawler GaaS. Nós removemos as dependências obrigatórias de nuvem pública (Photon Engine / Azure PlayFab) para que você possa desenvolver, debugar e jogar *tudo* direto do seu computador usando a stack `.NET 8`.

## 📂 O que mudou?

A solução Visual Studio **`BrawlerGaaS.sln`** agora contém o cérebro da operação:
1. **`BrawlerServer`**: Um Console App poderoso contendo o Matchmaking, as Salas TCP/UDP e as rotinas *Server-Side* para validar Combate, XP e Moedas (GaaS) de forma segura.
2. **`BrawlerShared`**: Onde as duas pontas da ponte se conectam. Contém a biblioteca de Pacotes (`BasePacket`, `Enums`) baseados puramente em `System.Text.Json` para envio ágil via Sockets.
3. O projeto Unity substituiu o `PhotonNetwork` e `PlayFabAPI` pelo `LocalServerClient.cs` e os Managers locais equivalentes. O banco de dados virou um arquivo `SQLite` minúsculo.

---

## 🚀 Como Rodar o Jogo em 3 Passos

### 1. Iniciar o Servidor C# (Visual Studio 2022)
1. Dê um duplo clique no arquivo `BrawlerGaaS.sln` na raiz do projeto para abri-lo no Visual Studio.
2. Defina o projeto `BrawlerServer` como **Startup Project** (Projeto de Inicialização).
3. Aperte **F5** (ou "Iniciar Depuração").
4. Uma tela de console preta aparecerá indicando que o servidor foi iniciado nas portas TCP `7777` e UDP `7778`. Ele criará o banco de dados `brawler_local.db` automaticamente se for a primeira vez.

### 2. Iniciar o Cliente Unity (Player 1)
1. Abra a pasta base na Unity (2022 LTS+).
2. Na cena `MainMenu`, certifique-se de que o GameObject "NetworkManager" possui o script `LocalServerClient.cs` ativado.
3. Dê Play no Editor.
4. Olhe o Console do seu servidor no Visual Studio: você verá o log verde avisando *"Nova conexão recebida"*, e que uma conta automática `DevUser_xxx` foi criada ou recuperada.

### 3. Iniciar a Partida Multiplayer (Player 2)
Para testar o combate com si mesmo, você precisa de dois clientes:
1. Pressione **Ctrl+B** (Build And Run) na Unity para gerar um `.exe` do jogo e rode essa janela em modo "Janela" pequena.
2. Dê "Play" no seu Editor da Unity ao mesmo tempo.
3. Clique em **"Encontrar Partida"** nos dois (o de build e o do editor).
4. O servidor detectará os dois jogadores (pois o tamanho da fila padrão é 2), criará uma Instância de `Room`, emitirá um `MatchFound` via TCP, e jogará ambos na arena.

---

## 🛠️ Modificando a Economia Local

Todo o seu "Painel GaaS" agora vive dentro do arquivo **`brawler_local.db`** (na pasta do `BrawlerServer/bin/Debug/...`).

Se você quiser dar a si mesmo milhares de moedas "PremiumCurrency" ou liberar passes de batalha para testar a loja:
1. Baixe e instale o [DB Browser for SQLite](https://sqlitebrowser.org/).
2. Arraste o arquivo `brawler_local.db` para dentro do programa.
3. Vá na aba "Browse Data" e edite as linhas da tabela `Players`. Suas alterações terão efeito instantâneo na próxima vez que você logar (o `EconomyManager.cs` puxará o novo saldo!).

---

## ⚠️ Troubleshooting (Problemas Comuns)
- **Erro de Porta Ocupada:** Se o Console App falhar ao iniciar reclamando de "Socket Exception", outra aplicação pode estar usando a porta 7777 ou 7778. Tente matar o processo no Gerenciador de Tarefas ou altere as portas em `Program.cs`.
- **Firewall do Windows:** A primeira vez que rodar, o Windows perguntará se você permite o tráfego do Console App. **Permita Redes Privadas e Públicas**, senão a comunicação UDP falhará.
- **NullReference no PhotonView:** Se encontrar scripts Unity reclamando do `photonView` ausente, é porque eles ainda estão na fila para refatoração. Lembre-se que a transição completa de toda a lógica requer limpar *todo* o histórico de classes antigas do PUN na sua base de código.
