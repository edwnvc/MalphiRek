# ![MalphiRek](images/icon.png) MalphiRek

*Plugin para treinar mecânicas nas lutas do jogo aclamado pela crítica, Final Fantasy XIV - Dawntrail™.*

---

O MalphiRek simula mecânicas de ultimates direto no seu cliente, para você treinar sozinho na inn.
Ele cria um grupo de bots e os chefes, roda os casts, VFX, tethers e marcadores como na luta real, e
os bots fazem as mecânicas junto com você.

A diferença para o projeto original: **Splatoon e BossMod funcionam dentro da simulação.**

## Compatibilidade com outros plugins

Enquanto um cenário roda, o MalphiRek faz o jogo parecer uma luta de verdade para os outros plugins:

| O quê | Como fica |
|---|---|
| Zona | O Dalamud passa a ler a zona da ultimate (não a inn), então layouts e scripts presos à zona ativam |
| Combate e duty | "Em combate" e "em duty" ficam ligados durante o cenário; a cada reinício o combate cai por alguns frames, para os scripts resetarem |
| Grupo | Listas de prioridade e scripts enxergam os bots, mesmo se você estiver em grupo cross-world |
| Fase (scene) | A scene da fase é aplicada (TOP P5 = 6, P6 = 7, Dancing Mad P2 = 7, P3 = 8) |
| BossMod | O módulo da luta é levado para a fase em que o cenário começa, em vez de começar do P1 |

Tudo isso fica em `/anomech` → engrenagem → **Splatoon / BossMod compatibility** (ligado por padrão).
Desligar volta ao comportamento original.

### O que já foi testado no jogo

- **Dancing Mad P3 Black Hole:** Splatoon (lista de prioridade e scripts desenhando no chão) e Boss Mod
  (módulo começa no Earthquake e acompanha a luta até os Lasers).

### Limitações conhecidas

- **BossMod só é posicionado no Black Hole por enquanto.** Nos outros cenários o Splatoon funciona,
  mas o BossMod começa do P1. Cada cenário precisa do seu mapeamento (`BossModStart`).
- **BossMod Reborn não tem módulo da Dancing Mad**; nessa luta só o Boss Mod normal funciona.
- Plugins que contam tempo a partir do início do combate (ex.: DMU Mits) mostram a timeline do P1,
  porque o cenário começa no meio da luta.
- Ainda não testado: TOP, FRU, UWU, UCOB e multiplayer.

## ⚠️ Atenção

**Na zona simulada você fica sem tráfego do servidor.** Para a zona falsa ficar estável, o plugin
bloqueia os pacotes do servidor: entradas e saídas do grupo e ready checks só aparecem quando você sair.

## Cenários

Dancing Mad (Ultimate), Futures Rewritten (Ultimate), The Omega Protocol (Ultimate),
The Weapon's Refrain (Ultimate) e The Unending Coil of Bahamut (Ultimate). Abra `/anomech` na inn para
ver a lista completa.

## Instalação (desenvolvimento)

1. `dotnet build -c Release` na raiz do repositório.
2. No jogo: `/xlsettings` → **Experimental** → **Dev Plugin Locations** → adicione o caminho do
   `AnoMech.dll` gerado em `AnoMech/bin/Release/`.
3. Desative qualquer outra versão do AnoMech: as duas usam o mesmo nome interno.

> Não substitua a DLL com uma simulação rodando: o Dalamud recarrega o plugin, e a proteção do
> AnoMech fecha o jogo por segurança.

## Créditos e licença

O MalphiRek é uma versão modificada do [AnoMech](https://github.com/anomek/AnoMech), de
[Anomek](https://github.com/anomek) e colaboradores, a partir do fork de
[shifusen329](https://github.com/shifusen329/AnoMech). A integração com Splatoon e BossMod partiu do
trabalho de [cidoliveira](https://github.com/cidoliveira/AnoMech).

Distribuído sob a [GNU AGPL-3.0](LICENSE.md), como o original.
