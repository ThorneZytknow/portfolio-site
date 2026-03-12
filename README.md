# Brawler Platform GaaS - Unity Prototype

Este repositório contém a fundação de código (C#) para um jogo de luta estilo plataforma focado em *Games as a Service* (GaaS). A arquitetura foi estruturada para suportar partidas multiplayer dinâmicas usando **Photon PUN** e gerenciamento seguro de contas/economia via **Azure PlayFab**.

## 🛠️ Tecnologias Utilizadas

- **Engine:** Unity (Recomendado 2022.3 LTS ou superior)
- **Linguagem:** C# e JavaScript (Para Cloud Scripts)
- **Rede / Matchmaking:** [Photon Unity Networking (PUN 2)](https://assetstore.unity.com/packages/tools/network/pun-2-free-119922)
- **Backend / Economia:** [PlayFab SDK](https://github.com/PlayFab/UnitySDK)

---

## 🚀 Como Configurar o Projeto

Como este repositório contém os *Scripts* e não o projeto binário completo da Unity, siga estes passos para montar a cena em um novo projeto:

### 1. Preparação da Unity
1. Crie um novo projeto **2D** ou **3D URP** na Unity Hub.
2. Copie a pasta `Assets/Scripts` deste repositório para a pasta `Assets` do seu projeto.

### 2. Instalação das SDKs
1. **PUN 2:** Acesse a Unity Asset Store e importe o **PUN 2 - Free**. Durante a configuração, insira seu `AppId` gerado no painel da Photon Engine.
2. **PlayFab:** Baixe o PlayFab Unity Editor Extensions e faça o login no painel da Unity (`Window > PlayFab > Editor Extensions`).
   - Configure a SDK para instalar as bibliotecas necessárias.
   - Acesse o painel da PlayFab online, crie um novo título (Studio) e insira o `Title ID` nas configurações da Unity.

### 3. Configuração do Backend (PlayFab Cloud Script)
Para que a economia e os sistemas F2P (Passes, XP) do jogo funcionem sem riscos de hack:
1. Acesse seu painel no PlayFab > Automation > Cloud Script.
2. Copie todo o conteúdo do arquivo `PlayFabCloudScript.js` incluído neste repositório e cole no painel.
3. Salve e faça o **Deploy**. Isso permitirá que o `EconomyManager.cs` credite moedas e que o `ProgressionManager.cs` avalie o XP sem fraude no cliente.
4. Vá em **Players > Statistics** e crie as chaves `TotalXP`, `PlayerLevel`, `BattlePassTier`, e `BattlePassXP`.

### 4. Criação de Dados (ScriptableObjects)
1. No Unity Editor, clique com botão direito em qualquer pasta: `Create > Brawler > Character Data` para montar os atributos de um personagem.
2. Repita o processo com `Create > Brawler > Ability Data` para criar as habilidades Neutra, Cima, Baixo e Especial.
3. Preencha o Singleton `CharacterRoster` (na cena) arrastando os perfis para a lista principal.

### 5. Configuração das Cenas (Unity Editor)

#### Cena de Matchmaking (Menu Principal)
1. Crie um GameObject vazio chamado `NetworkManager` e adicione o script `NetworkManager.cs`.
2. Crie um GameObject vazio chamado `PlayFabManager` e adicione os scripts de gerenciamento GaaS (`PlayFabAuthManager.cs`, `EconomyManager.cs`, `ProgressionManager.cs`, `BattlePassManager.cs`, `CharacterRoster.cs`).
3. Crie uma interface Canvas com botões e textos e conecte o script `MatchmakingUI.cs`.

#### Cena da Arena de Batalha (Gameplay)
1. Crie os pontos de *Spawn* na cena. Adicione zonas de morte (colliders triggers com a tag `BlastZone`).
2. Adicione os managers de arena (`GameManager`, `MatchResultsManager` e `HUDManager` num Canvas).
3. No seu *Player Prefab* (na pasta Resources):
   - Adicione `Rigidbody2D`, `BoxCollider2D` e um `PhotonView`.
   - Adicione os scripts vitais: `PlayerController.cs`, `CombatSystem.cs`, `AbilitySystem.cs`, e `StockSystem.cs`.
   - Configure as habilidades (arrastando as AbilityData pro AbilitySystem) e defina `PlayerController` no Observed Components do PhotonView.
4. Arraste o script `CameraController.cs` para a `Main Camera`.

---

## 📜 Documentação do Projeto

Para regras detalhadas de Game Design, modelos de negócio (LTV/CAC, Passe de Batalha), mecânicas de Hitstun, Escudo e cuidados legais com Loot Boxes, consulte o documento:
👉 **[Game Design Document (GDD.md)](GDD.md)**

## 👤 Licença
Este código é um protótipo construído como demonstração de arquitetura Full-Stack voltada a GameDev.
