# Riftbound — Protótipo mobile (Unity)

Protótipo de gameplay **Player vs Bot** para Android, em **portrait**, feito só com formas geométricas.
O objetivo é responder a uma pergunta: *"É divertido jogar várias partidas seguidas?"*

O núcleo do jogo: **controle territorial de 3 Rifts numa rede dinâmica, Rift Break e deck rotativo de unidades.**

> **Status da build Android:** **nenhum APK foi gerado ainda.** O ambiente onde este projeto foi escrito não tinha
> Unity Editor, Android Build Support, Android SDK nem NDK, e a política de rede bloqueava `download.unity3d.com`
> e `dl.google.com`. Isso também impede ativar uma licença Unity. O projeto já está configurado para Android e
> traz um script de build pronto. Para gerar o APK, siga [Build Android](#build-android) (1 clique no menu, ou 1 comando).

---

## 1. Versão da Unity

- **Unity 6 LTS (6000.0.x)**. O `ProjectSettings/ProjectVersion.txt` fixa `6000.0.47f1`, mas qualquer 6000.0.x
  (ou mais nova) serve. Se o Hub avisar sobre a versão, escolha a sua 6000.x instalada.
- Pipeline de render: **Built-in** (sem URP/HDRP).
- Dependências: somente pacotes nativos (`com.unity.ugui` + módulos built-in). Nenhum asset externo e nenhum serviço online.
- Input: **Input Manager (legado)**, já configurado em `ProjectSettings` (`Active Input Handling = Input Manager`).
  Se estiver como "Input System Package (New)", o menu *Riftbound > Validate Project Setup* troca para "Both".
  Depois disso, reinicie o Editor.
- Para Android, instale pelo Unity Hub os módulos **Android Build Support**, **OpenJDK** e **Android SDK & NDK Tools**.

## 2. Como abrir e jogar

1. Unity Hub → **Add project from disk** → selecione a pasta `Riftbound/`.
2. Abra a cena **`Assets/Riftbound/Scenes/Main.unity`** (ou menu *Riftbound > Open Main Scene*).
3. Aperte **Play**. A partida Player vs Bot começa na hora.
4. Na aba *Game*, escolha uma resolução portrait (ex.: 1080x1920) para ver o layout real.

Não existe menu inicial. Ao fim da partida aparece **PLAY AGAIN**, junto com a escolha de dificuldade do bot (EASY/NORMAL/HARD).

### Controles

| Ação | Celular (touch) | Editor (mouse/teclado) |
|---|---|---|
| Selecionar carta | tocar na carta | clicar na carta, ou teclas `1`–`4` |
| Jogar a carta | tocar no mapa (arrastar ajusta a posição; soltar confirma) | clicar no mapa (a prévia segue o cursor) |
| Cancelar | tocar na mesma carta, ou soltar o dedo fora do campo | clicar na mesma carta, ou `Esc` |
| Debug Mode | botão `DEBUG` (canto superior direito) | botão `DEBUG`, ou `F1` / `` ` `` |
| Pausar | botão PAUSE no painel de debug | `Espaço` |
| Reiniciar após o fim | PLAY AGAIN | PLAY AGAIN, ou `R` |

Com uma carta de unidade selecionada, a área válida fica destacada em azul: sua metade do mapa **e** um raio em volta
de cada Rift que você controla. A prévia fica **verde** em posição válida e **vermelha** em inválida.
Uma jogada inválida mostra um anel vermelho e o motivo ("NOT ENOUGH FLUX", "INVALID POSITION"...).

## 3. Arquitetura

```
Assets/Riftbound/
├── Scenes/Main.unity              # 1 GameObject com GameBootstrap; todo o resto é criado por código
├── Resources/RiftboundConfig.asset# GameConfig usado em runtime (regras + deck)
├── Config/Cards/*.asset           # 8 cartas + Spawnling (CardDefinition)
├── Editor/                        # build Android, geração de assets, validação do projeto
└── Scripts/
    ├── Core/        GameBootstrap (game manager: loop, restart, pausa, velocidade), Team/Palette
    ├── Config/      GameConfig, CardDefinition/UnitStats, BotProfile, MapLayout, DefaultContent
    ├── Simulation/  Match (partida inteira em C# puro, sem MonoBehaviour)
    │                MapNetwork (grafo + caminhos), Rift, RiftCaptureSystem, RiftBreakManager,
    │                RiftShiftManager, Core, FluxSystem, DeckController, CardSystem (valida/executa comandos),
    │                PlayCardCommand, Unit, UnitManager, UnitBrain (targeting/objetivo), UnitMotor (movimento),
    │                UnitCombat (ataques/Parasite), MatchStats
    ├── AI/          BotController (emite PlayCardCommand, igual ao jogador)
    ├── Controls/    PlayerInputController (touch + mouse → PlayCardCommand)
    ├── View/        MatchView, UnitView (pool), RiftView, CoreView, ShapeLibrary (sprites procedurais), CameraFitter
    └── UI/          HUDController, CardButton, DebugPanel, SafeAreaFitter, UIFactory
Tools/
├── HeadlessSim/                   # roda a simulação real fora da Unity (testes + balanceamento bot vs bot)
└── generate_unity_assets.py       # gerou os .asset/.meta/cena iniciais a partir de DefaultContent
```

Princípios:

- **Simulação separada da apresentação.** `Match` e os sistemas em `Simulation/` são C# puro: não conhecem
  GameObjects, câmera nem UI. As views só **leem** o estado. Isso permite rodar a partida headless (testes e
  balanceamento) e, no futuro, rodar a simulação num servidor.
- **Um único ponto de entrada para ações:** `Match.Submit(PlayCardCommand)`. Jogador e bot passam exatamente pela
  mesma validação (`CardSystem`): custo, posição, limite de unidades. Para multiplayer, basta trocar o
  `BotController` por uma fonte de comandos de rede. Combate, captura e movimento não mudam.
- **Ordem do frame** (em `GameBootstrap.Update`): input do jogador → bot → `Match.Tick(dt)` → views → HUD.
- **Sem singletons nem `FindObjectOfType` em Update.** O `GameBootstrap` monta tudo e passa as referências.
- **Performance:** loops força-bruta sobre ≤ 48 unidades (limite configurável), views de unidade em pool, FX em pool,
  textos só reconstruídos quando o valor muda e caminhos pré-calculados (Floyd–Warshall em 5 nós).
  Não há alocação por frame no caminho principal.

## 4. Onde ficam as configurações de balanceamento

Tudo é editável no **Inspector**, sem mexer em código:

| O quê | Onde |
|---|---|
| Duração da partida, Stability do Core, Flux (início/máx/regeneração), captura (tempo, bônus por unidade, regra de contestação), zona de deploy, limite de unidades, Rift Break, Rift Shift (on/off, intervalo), Sudden Rift, perfis do bot, layout do mapa | `Assets/Riftbound/Resources/RiftboundConfig.asset` (menu *Riftbound > Select Game Config*) |
| HP, dano, intervalo de ataque, alcance, visão, velocidade, custo, dano no Core, habilidades especiais, forma/tamanho | `Assets/Riftbound/Config/Cards/<Carta>.asset` |

Os valores padrão também existem em `Scripts/Config/DefaultContent.cs`, usados como fallback se o asset sumir.
*Riftbound > Regenerate Default Config Assets* restaura os assets a partir deles (**sobrescreve** as mudanças).

## 5. Como criar uma nova carta / unidade

**Nova unidade** (sem código):
1. `Project` → botão direito em `Assets/Riftbound/Config/Cards` → *Create > Riftbound > Card Definition*.
2. Preencha `cardId`, `displayName`, `fluxCost`, `kind = Unit` e os `unit` stats (HP, dano, velocidade, forma...).
   Os flags `prioritizeUnits`, `holdsRifts`, `riftDamageReduction` e `fluxDrainPerSecond` combinam os
   comportamentos existentes. `spawnCount > 1` cria um grupo, como o Swarm.
3. Arraste a carta para a lista `deck` do `RiftboundConfig` (o deck tem 8 cartas; troque uma existente ou aumente a lista).

**Nova mecânica de carta** (feitiço novo, habilidade nova):
1. Acrescente um valor em `CardKind` (e os campos necessários em `CardDefinition`).
2. Implemente a validação em `CardSystem.ValidateCard` e o efeito em `CardSystem.Execute`.
3. Opcional: prévia em `MatchView.ShowGhost`, dica no `HUDController` e uso pelo bot em `BotController.TrySpells`.

Habilidades passivas de unidade ficam em `UnitStats` + `UnitBrain` (decisão), `UnitCombat` (ataques) ou `Match.Tick`
(auras como a do Leech).

## 6. Como alterar as regras das Rifts

- Números (tempo de captura, bônus por unidade, recuperação quando vazia, raio, contestação `Freeze`/`MajorityWins`,
  raio de deploy): `RiftboundConfig`.
- Lógica de captura: `Simulation/RiftCaptureSystem.cs` (comentários explicam o modelo `Control ∈ [-1, +1]`).
- Rift Break (gatilho, duração, reset das Rifts): `Simulation/RiftBreakManager.cs` + `riftBreakDuration`/`resetRiftsOnBreak`.
- Rift Shift: `Simulation/RiftShiftManager.cs`. Cada tipo de shift é um `case` em `Apply`; ligue/desligue com
  `riftShiftEnabled` ou pelo botão `SHIFT: ON/OFF` do debug.
- Posições e conexões do mapa: `RiftboundConfig > map` (`MapLayout`).
- Prioridades das unidades (defender > capturar > atacar > Core no Rift Break): constantes no topo de `Simulation/UnitBrain.cs`.

## 7. Como alterar o comportamento do bot

- Dificuldades: `RiftboundConfig > botEasy / botNormal / botHard` (intervalo de decisão, jitter, chance de erro,
  uso de counters, feitiços inteligentes, economia de Flux, imprecisão de posicionamento).
- Lógica: `AI/BotController.cs`. `Decide()` tem a ordem de prioridades. `ScoreUnitCard()` pontua cada carta por
  propósito (capturar, lutar, empurrar, defender o Core) a partir dos stats, então cartas novas entram na
  avaliação automaticamente.
- O bot **não trapaceia**: usa o mesmo `Match.Submit`, o mesmo deck, o mesmo Flux e as mesmas regras de posicionamento.
- Medir o efeito de uma mudança sem abrir a Unity:
  `dotnet run --project Tools/HeadlessSim` (relatório bot vs bot) e `-- test` (testes de regras).

## 8. Build Android

Pré-requisitos: Unity 6 com **Android Build Support + OpenJDK + Android SDK & NDK Tools** (Unity Hub > Installs >
engrenagem > Add modules).

**Pelo Editor:** menu **Riftbound > Build Android APK**.

**Por linha de comando:**
```bash
<caminho-da-Unity>/Unity -batchmode -quit -projectPath /caminho/Riftbound -buildTarget Android \
  -executeMethod Riftbound.EditorTools.RiftboundBuild.BuildAndroidCommandLine -logFile build.log
```

Saída: **`Builds/Android/RiftboundPrototype.apk`**, assinado com a keystore de debug (instala por sideload).
O script aplica, antes de compilar:

- `com.riftbound.prototype`, versão 0.1.0 (code 1);
- orientação **Portrait** fixa;
- **IL2CPP**, **ARM64 + ARMv7**; min SDK **24 (Android 7.0)**, target SDK = maior instalado;
- APK (não AAB); cena `Main.unity` nas Build Settings.

Instalar: `adb install -r Builds/Android/RiftboundPrototype.apk`, ou copie o arquivo para o celular e abra
(permita "instalar apps desconhecidos").

## 9. Limitações conhecidas

- **Nenhum APK foi gerado neste ambiente**, e o projeto **não foi aberto numa Unity real** aqui (ver o topo deste
  arquivo). O C# foi compilado com sucesso contra as reference assemblies da Unity (UnityEngine 2021.3 + uGUI +
  UnityEditor), e a simulação foi executada headless (29 testes de regras + milhares de partidas bot vs bot). Mesmo
  assim, cena, `.asset` e `ProjectSettings` foram escritos à mão. Se a Unity reclamar de algo ao abrir o projeto:
  `Riftbound > Regenerate Default Config Assets` recria os assets, e o `GameBootstrap` também se cria sozinho na cena `Main`.
- `ProjectSettings.asset` é parcial. A Unity completa o resto com valores padrão ao abrir, e o script de build
  reaplica as configurações de Android.
- Visual 100% placeholder: textos em `TextMesh` com a fonte embutida da Unity, que pode ficar levemente borrada.
- Sem áudio, sem animações, sem física (movimento e separação simples, sem obstáculos).
- O bot é baseado em regras: não prevê o futuro e não faz combos elaborados.
- Só um dedo é usado no touch (multi-touch desligado de propósito).
- O Debug Mode cobre parte do campo (é uma ferramenta, não UI final).
- A regra de empate "Sudden Rift" tem limite de 60 s; depois disso vence quem tiver mais Rifts, senão é empate.
