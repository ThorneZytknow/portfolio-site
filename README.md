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
Para que a economia do jogo funcione sem riscos de hack:
1. Acesse seu painel no PlayFab > Automation > Cloud Script.
2. Copie todo o conteúdo do arquivo `PlayFabCloudScript.js` incluído neste repositório e cole no painel.
3. Salve e faça o **Deploy**. Isso permitirá que o `EconomyManager.cs` credite moedas de forma segura no fim das partidas.

### 4. Configuração das Cenas (Unity Editor)

#### Cena de Matchmaking (Menu Principal)
1. Crie um GameObject vazio chamado `NetworkManager` e adicione o script `NetworkManager.cs`.
2. Crie um GameObject vazio chamado `PlayFabManager` e adicione o script `PlayFabAuthManager.cs`. Adicione também o `EconomyManager.cs`.
3. Crie uma interface Canvas com botões e textos e conecte o script `MatchmakingUI.cs`.

#### Cena da Arena de Batalha (Gameplay)
1. Crie os pontos de *Spawn* na cena.
2. Adicione um GameObject `GameManager` com o script homônimo, preenchendo o array de `SpawnPoints`.
3. No seu *Player Prefab* (na pasta Resources):
   - Adicione `Rigidbody2D`, `BoxCollider2D`, e um componente `PhotonView`.
   - Adicione os scripts `PlayerController.cs` e `CombatSystem.cs`.
   - Arraste o `PlayerController` para a aba "Observed Components" dentro do `PhotonView` para habilitar a sincronização (Smooth Movement).
4. Arraste o script `CameraController.cs` para a `Main Camera`.

---

## 📜 Documentação do Projeto

Para regras detalhadas de Game Design, modelos de negócio (LTV/CAC, Passe de Batalha), e cuidados legais com Loot Boxes, consulte o documento:
👉 **[Game Design Document (GDD.md)](GDD.md)**

## 👤 Licença
Este código é um protótipo construído como demonstração de arquitetura Full-Stack voltada a GameDev.
