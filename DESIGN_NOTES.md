# Riftbound — Design Notes (protótipo v0.1)

## Gameplay loop

1. **Flux** regenera sozinho (1/s, máximo 10, começa com 5).
2. O jogador joga cartas do **deck rotativo**: 4 na mão e 4 na fila. A carta jogada vai para o fim da fila,
   e a próxima ("NEXT") ocupa o slot.
3. As unidades andam pela **rede territorial** (linhas entre Cores e Rifts), lutam sozinhas e **capturam Rifts**
   ficando dentro delas.
4. Quem controla **as 3 Rifts ao mesmo tempo** provoca o **RIFT BREAK**: as Rifts se partem (voltam a Neutral)
   e o Core inimigo fica **vulnerável por 8 s**. As unidades de quem provocou correm para o Core e causam dano.
5. Depois dos 8 s o mapa precisa ser reconquistado para um novo Rift Break.
6. A cada 45 s, um **RIFT SHIFT** muda a rede (abre rotas de flanco, fecha ligações ou desloca Rifts).
7. Vence quem zerar o Core adversário, ou quem tiver mais Stability em 00:00. Em caso de empate: **SUDDEN RIFT**.

A tensão central: mandar unidades para o Core durante o Break (dano) **ou** segurar as Rifts neutralizadas
(preparar o próximo Break e negar o do inimigo).

## Regras atuais

**Mapa** (simétrico por rotação de 180°, então é justo para os dois lados):

```
            ENEMY CORE (0, 5)
              |      \
              |     RIFT B (2.6, 1.3)
              |      /
            RIFT C (0, 0)
              |    /
      RIFT A (-2.6,-1.3)
              |  /
            PLAYER CORE (0, -5)
```

- Conexões iniciais: PCore–A, PCore–C, A–C, C–B, ECore–B, ECore–C.
- A é a Rift "de casa" do jogador, B a do inimigo, C é o centro disputado.

**Deploy:** na sua metade do mapa (a mais de 0.4 da linha central) **ou** num raio de 1.6 de qualquer Rift que você
controla. Conquistar território aumenta onde você pode jogar. O limite é de 24 unidades por time.

**Captura:** cada Rift tem `Control ∈ [-1, +1]` (+1 = Player, −1 = Enemy).
- Um time sozinho dentro: 1 unidade leva **5.5 s** de 0 até dono. Cada unidade extra soma +25% de velocidade
  (conta até 4 unidades).
- Rift inimiga: primeiro neutraliza (até 0 → NEUTRAL), depois captura.
- **CONTESTED** (os dois times dentro): o progresso congela (`ContestRule.Freeze`; alternativa: `MajorityWins`).
- Rift vazia: volta devagar (8 s) para o controle total do dono, ou para 0 se estiver neutra.

**Rift Break:** controlar 3/3 Rifts dispara o Break. O Core inimigo fica vulnerável por 8 s e as Rifts resetam
para neutras. Unidades só causam dano ao Core durante o Break (`coreDamage` por hit). Durante o próprio Break, cada
unidade só luta contra inimigos a até 1.0 de distância (exceto o Hunter) e segue para o Core.

**Rift Shift** (a cada 45 s; restaura a rede base e aplica UMA mudança simétrica):
- FLANK ROUTES OPEN: novas ligações PCore–B e ECore–A.
- CENTER LINKS COLLAPSE: fecha A–C e B–C (e abre os flancos para nada ficar isolado).
- RIFTS DRIFT: A e B se deslocam 0.8 numa direção aleatória, espelhadas entre si.
- CORE LINKS TO CENTER SEVERED: fecha PCore–C e ECore–C.

**Tempo:** 03:00. Em 00:00 vence a maior Stability. Se houver empate, começa o **SUDDEN RIFT**: Flux ×2,
captura ×2 e **o primeiro dano num Core decide a partida**. Se ninguém acertar um Core em 60 s, vence quem tiver
mais Rifts; se também empatar, é DRAW.

**IA das unidades:**
1. Inimigo à vista → engaja (Hunter sempre).
2. Rift Break próprio → vai para o Core inimigo.
3. Senão, escolhe uma Rift por pontuação: defender uma Rift ameaçada (100) > capturar uma neutra (70) > atacar uma
   inimiga (50) > manter uma própria (15). Desconta distância (4/unidade) e aglomeração (−7 por aliado além de 2 com
   o mesmo alvo). Há bônus para não abandonar uma captura em andamento, e Maw/Anchor preferem ficar segurando Rifts.

## Valores atuais de balanceamento

Partida: 180 s · Core 100 · Flux 5 → 10, +1/s · captura 5.5 s · Break 8 s · Shift 45 s · limite de 24 unidades/time.

| Carta | Custo | Tipo | HP | Dano | Intervalo (s) | DPS | Alcance | Visão | Vel. | Dano no Core/hit | Especial |
|---|---|---|---|---|---|---|---|---|---|---|---|
| MAW | 5 | Unidade | 1400 | 70 | 1.4 | 50 | 0.45 | 2.4 | 0.85 | 1.4 | segura Rift |
| BLINK | 2 | Unidade | 180 | 30 | 0.7 | 43 | 0.35 | 2.2 | 3 | 0.6 | |
| LEECH | 3 | Unidade | 450 | 25 | 1 | 25 | 1.4 | 2.6 | 1.4 | 0.6 | drena 0.35 Flux/s em Rift inimiga |
| ANCHOR | 4 | Unidade | 1000 | 45 | 1.2 | 38 | 0.45 | 2.4 | 0.9 | 0.8 | segura Rift, −50% de dano dentro de Rift |
| HUNTER | 3 | Unidade | 420 | 110 | 1.1 | 100 | 1.6 | 3.4 | 1.6 | 1.1 | prioriza unidades |
| SWARM | 3 | Unidade | 110 (×4) | 22 | 0.8 | 28 (×4) | 0.3 | 2.2 | 2.4 | 0.4 | cria 4 unidades |
| PULSE | 4 | Feitiço | – | 260 em área | – | – | raio 1.7 | – | – | – | instantâneo numa Rift |
| PARASITE | 3 | Feitiço | – | 700 após 10 s | – | – | – | – | – | – | se matar: 2 Spawnlings |
| *Spawnling* | – | Unidade | 140 | 20 | 0.8 | 25 | 0.35 | 2.4 | 2.2 | 0.3 | oculta, criada pelo Parasite |

Alcance é medido borda a borda, em unidades de mundo (o mapa tem ~7.8 × 11.6).

**Perfis do bot:**

| | Intervalo de decisão | Chance de erro | Counters | Feitiços inteligentes | Economia de Flux |
|---|---|---|---|---|---|
| EASY | 2.6 s (+0–1.2) | 45% | não | não | não |
| NORMAL | 1.5 s (+0–0.6) | 12% | sim | sim | não |
| HARD | 0.7 s (+0–0.25) | 0% | sim | sim | não (o campo `saveFluxUntil` existe; guardar Flux piorou o bot nos testes) |

### Resultados do simulador headless (bot vs bot, `Tools/HeadlessSim`)

| Confronto | Partidas | Vitórias (P / E) | Duração média | Rift Breaks/partida (P / E) | 1º Break |
|---|---|---|---|---|---|
| Normal vs Normal | 400 | 46% / 54% (entre 46% e 52% com outras seeds: justo) | 117 s | 2.5 / 3.0 | 40 s |
| Hard vs Normal | 200 | 58% / 42% | 100 s | 2.8 / 1.8 | 38 s |
| Normal vs Easy | 200 | 95% / 5% | 103 s | 4.6 / 0.3 | 33 s |
| Easy vs Easy | 100 | 49% / 51% | 159 s | 2.8 / 3.1 | 53 s |
| Jogador parado vs Normal | 40 | 0% / 100% | 42 s | – | 13 s |

Cada Rift Break defendido tira ~18 de Stability; um sem defesa tira ~33. Os primeiros números (4 s de captura e
dano no Core 2.5× maior) terminavam as partidas em ~75 s. Por isso o dano no Core foi reduzido e a captura foi
para 5.5 s. Humanos jogam mais devagar que bots, então partidas reais tendem a ser mais longas.

## Decisões de arquitetura

- **Simulação em C# puro** (`Simulation/`), separada das views. Isso permite testar e balancear rodando milhares de
  partidas fora da Unity, e deixa pronto o caminho para um servidor autoritativo.
- **Comandos** (`PlayCardCommand`) como única forma de agir. O jogador e o bot usam a mesma validação, então trocar o
  bot por rede não exige mexer em combate, captura ou movimento.
- **ScriptableObjects** para toda regra e número (`GameConfig`, `CardDefinition`). `DefaultContent` serve de fallback e
  "reset de fábrica".
- **Rede como grafo pequeno** (5 nós), com caminhos pré-calculados. Andar fora das linhas custa ×2.5, então as
  unidades seguem as conexões, e o Rift Shift muda rotas de verdade.
- **Tudo desenhado por código** (sprites gerados por SDF, `TextMesh`, uGUI montado por script). Não há prefabs para
  quebrar e o visual é deliberadamente de protótipo.
- Nada de física, singletons ou `Find*` por frame. Views e FX usam pool.

## Possíveis próximos passos

1. **Playtest com humanos** (o objetivo desta versão). Medir: duração real, quantos Rift Breaks, se o Break é o clímax,
   se o deploy ao redor das Rifts é entendido.
2. Ajustar o Rift Break: talvez exigir segurar 3/3 por ~2 s, ou dar ao defensor uma ferramenta de resposta (Core
   com ataque fraco, ou reforço grátis).
3. Deck building: escolher 8 de um pool maior e criar arquétipos (tank/hold, rush/flank, controle/spells).
4. Mais tipos de Rift Shift: Rift temporária, ponte de mão única, Rift que gera Flux extra.
5. Replays e determinismo: a simulação já é separada. Falta um passo de tempo fixo (fixed tick) e RNG por comando.
6. Multiplayer: servidor autoritativo rodando `Match` e clientes enviando `PlayCardCommand` com timestamp de tick.
7. Feedback: sons simples, vibração no Rift Break e números de dano.
8. Tutorial curto: "capture as 3 Rifts" → "ataque no Rift Break".
