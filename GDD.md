# Game Design Document (GDD) - Brawler Platform GaaS

## 1. Visão Geral
Este documento define as diretrizes de design, mecânicas, estrutura técnica e estratégia de negócios para um jogo de luta estilo plataforma (Brawler), focado em partidas multiplayer dinâmicas e um elenco diversificado de personagens. O jogo é projetado desde o início como um *Games as a Service* (GaaS), visando um ciclo de vida longo e conteúdo em constante evolução.

### 1.1 Conceito Principal
O jogo é um Brawler Platform (semelhante ao *Super Smash Bros*), onde os jogadores se enfrentam em arenas dinâmicas, com o objetivo de nocautear os adversários arremessando-os para fora do cenário após acumularem dano.

### 1.2 Público-Alvo
Jogadores casuais e competitivos de jogos de luta e multiplayer online, atraídos por mecânicas acessíveis de aprender, mas difíceis de dominar, e por um ciclo contínuo de novidades (GaaS).

### 1.3 Plataformas
PC, Consoles (PlayStation, Xbox, Nintendo Switch) e Mobile (com controles adaptados ou suporte a gamepad).

---

## 2. Desenvolvimento Base

### 2.1 Engine de Jogo
- **Motor Principal:** Unity.
- **Motivação:** Capacidades robustas para desenvolvimento 2D e 3D, e facilidade na portabilidade multi-plataforma.

### 2.2 Artes e Design
- **Estilo Artístico:** 3D Cel-shaded (ou estilo 2.5D com modelagem 3D para personagens e ambientes), garantindo um visual limpo, vibrante e coeso que envelheça bem e facilite a criação contínua de novas skins e cenários.
- **Foco:** Ativos de alta qualidade otimizados para garantir performance fluida em todas as plataformas (60 FPS é mandatório para jogos de luta).

---

## 3. Linguagens de Programação
- **C# (C-Sharp):** Linguagem principal para a lógica do jogo (GamePlay, UI, integrações) dentro da Unity e para a lógica de negócios em servidores (ex: Photon Server).
- **C/C++:** Considerado para o núcleo de servidores de alta performance (ex: Photon Core) ou bibliotecas de física customizada, visando otimização máxima para gerenciamento de rede e pacotes (Rollback Netcode, se aplicável no futuro).

---

## 4. Estrutura de Rede e Servidores (Backend)
A arquitetura Cliente-Servidor deve ser robusta, focando em escalabilidade e baixa latência.

### 4.1 Comunicação Multiplayer
- **Solução:** Ecossistema Photon (Photon Server ou Photon Cloud) + PUN (Photon Unity Networking) ou Photon Quantum (para jogabilidade determinística de luta).
- **Requisitos:**
  - Balanceamento de carga de usuários (Matchmaking eficiente).
  - Sincronização de jogo via RPCs (Chamadas de Procedimento Remoto) e propriedades customizadas para estados de jogo não-críticos.
  - Otimização do tráfego de rede, utilizando protocolos UDP para inputs de combate (baixa latência) e TCP para dados cruciais de setup de partida.

### 4.2 Banco de Dados e Gerenciamento de Contas
- **BaaS (Backend as a Service):** Azure PlayFab ou Unity LiveOps (Cloud Save, Cloud Code, Authentication).
- **Gerenciamento:** Armazenamento de progresso de jogador, inventário (skins, itens cosméticos), autenticação de login (e-mail, OAuth) e placares competitivos (leaderboards).

---

## 5. Estrutura de Monetização (GaaS)
Modelo de receita contínua, rejeitando o formato de compra única. O foco é monetizar sem afetar o balanceamento competitivo.

### 5.1 Modelo Principal
- **Free-to-Play (F2P):** Jogo base 100% gratuito.

### 5.2 Microtransações
- **Itens Cosméticos:** Skins de personagens, roupas alternativas, efeitos visuais de nocaute/habilidades, emotes e banners de perfil.
- **Itens de Conveniência:** Aceleradores de progressão de XP, slots adicionais de loadout.
- **Restrição Estrita:** Nenhum item vendido pode conceder vantagens competitivas diretas (Zero "Pay-to-Win").

### 5.3 Produtos Recorrentes
- **Passe de Batalha (Season Pass):** Trilhas sazonais de recompensas (gratuitas e premium), incentivando engajamento diário/semanal e oferecendo cosméticos exclusivos.
- **Assinaturas Mensais (Opcional):** Benefícios contínuos como pequena quantia de moeda premium diária/mensal, bônus de XP no passe de batalha, e insígnias de assinante.

---

## 6. Gestão e Métricas SaaS
Sistema robusto de monitoramento via ferramentas de Analytics (ex: Unity Analytics, PlayFab Analytics, GameAnalytics).

### 6.1 Métricas Chave (KPIs)
- **CAC (Custo de Aquisição de Clientes):** Otimizar campanhas de marketing para atrair novos jogadores de forma eficiente.
- **LTV (Lifetime Value):** Maximizar o valor gerado por jogador ao longo de seu ciclo de vida. Objetivo: Relação LTV/CAC de 3:1 ou superior.
- **Churn Rate (Taxa de Cancelamento):** Monitorar a saída de jogadores e implementar LiveOps e eventos para reengajamento.
- **MRR (Receita Recorrente Mensal):** Acompanhar a previsibilidade financeira gerada por assinaturas e passes de batalha.
- **DAU/MAU (Daily/Monthly Active Users):** Medir o tamanho e engajamento da comunidade.

### 6.2 Retenção e LiveOps
- Realização de Testes A/B para loja e balanceamento.
- Eventos in-game regulares, desafios sazonais e atualizações de conteúdo via nuvem (novos personagens, mapas, modos temporários) para sustentar a retenção a longo prazo.

---

## 7. Cuidados Legais e Econômicos

### 7.1 Loot Boxes
- **Política:** Caso implementadas, deve haver **transparência total** nas probabilidades (drop rates) de obtenção de cada tier de item.
- **Conformidade:** Rigorosa observância das regulamentações de jogos de azar e proteção a menores em todas as jurisdições de publicação (ex: leis na Bélgica/Holanda, regulamentações ESRB/PEGI).

### 7.2 Moedas Virtuais, Criptomoedas e NFTs
- **Abordagem Segura:** O jogo utilizará "Moedas Virtuais Fechadas" (Premium Currency comprada com dinheiro real e Soft Currency ganha jogando).
- **Termos de Serviço (ToS):** Deixar explícito que moedas e itens virtuais do jogo *não possuem valor no mundo real* e *não são transferíveis/sacáveis* por dinheiro real (fiat).
- **Cripto/NFTs:** A integração de blockchain/NFTs traz riscos massivos de regulamentações financeiras rigorosas e leis de transmissão de dinheiro. Salvo se houver um departamento jurídico dedicado a Web3, **não é recomendado** o uso de NFTs neste projeto para evitar o bloqueio em plataformas como a Steam e complexidades legais de câmbio.
