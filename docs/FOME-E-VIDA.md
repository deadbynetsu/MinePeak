# Fome e vida compartilhadas

A extensão mantém um conjunto de comida e vida para o jogador local dos dois jogos. Comer no Minecraft ou no PEAK altera a comida compartilhada. Dano e cura que alterem os corações do Minecraft ou os ferimentos do PEAK são refletidos no outro lado.

Comida comum conserva o comportamento do jogo: ela repõe comida, mas só aumenta a vida quando também produz uma cura ou regeneração real. Poções e outros efeitos que mudam a vida continuam sendo observados.

## O que cada barra representa

| Valor compartilhado | Minecraft | PEAK |
| --- | --- | --- |
| Comida | Nível de comida, de 0 a 20 | Inverso de `Hunger`: comida cheia equivale a fome zero |
| Vida | Corações, normalizados para uma escala de 0 a 20 | Inverso de `Injury`/ferimentos: vida cheia equivale a ferimentos zero |

PEAK não possui HP convencional igual ao Minecraft. Esta correspondência usa seus ferimentos como equivalente de vida. A stamina usada para escalar continua funcionando como stamina. Frio, veneno, peso e os demais estados do PEAK não são apagados nem convertidos todos em dano de corações.

O painel **Vida / Comida** mostra os percentuais compartilhados nos dois modos. A barra nativa de fome do PEAK cresce quando falta comida; por isso, seu sentido visual é o inverso do percentual de Comida.

## Trocar de modo e comer

- No modo Minecraft, use as comidas e curas normalmente pelo inventário Minecraft.
- No modo PEAK nativo, use os alimentos e equipamentos com os controles originais do PEAK.
- A troca por L ou pelo botão no menu não deve repor as barras por conta própria.
- Apenas o jogo que controla o personagem avança seu relógio natural de fome. O modo Minecraft pausa o aumento natural de fome do PEAK. O modo PEAK nativo pausa o processamento natural de fome e regeneração baseada em comida do Minecraft.

Essa pausa não bloqueia o uso de comida, curas ou efeitos fora desse relógio. Ela evita que dois gastos naturais sejam somados ao mesmo tempo enquanto um dos personagens está oculto.

O Minecraft começa em **Sobrevivência** quando a sessão de sincronização é iniciada, por padrão. Assim, seus mecanismos de fome, dano e cura ficam ativos. É possível selecionar `/gamemode creative` manualmente, mas a imunidade e as regras de alimentação do Creative continuam valendo. Use `/gamemode survival` para voltar ao funcionamento normal de sobrevivência.

## Configuração

Com os jogos fechados, edite `local-profile/BepInEx/config/PeakCraftCoop.cfg`:

```ini
[Vitals]
Enabled = true
StartInSurvival = true
ShowHud = true
```

| Opção | Efeito |
| --- | --- |
| `Enabled` | Habilita a sincronização local de comida e vida. |
| `StartInSurvival` | Inicia o Minecraft isolado em Sobrevivência ao estabelecer uma nova sessão da ponte; não força novamente a cada troca de modo. |
| `ShowHud` | Mostra o painel com os percentuais compartilhados. |

Uma mudança manual para Creative permanece durante a sessão atual; uma nova sessão da ponte pode aplicar novamente a opção `StartInSurvival`. As opções não alteram as contas ou os mundos do SKLauncher: o lançamento usa o mundo isolado do projeto.

## Precisão e recuperação

PEAK arredonda seus estados em passos de 2,5%. O Minecraft mostra comida em pontos inteiros, enquanto a ponte conserva a fração entre atualizações. Pequenas diferenças visuais de arredondamento, de até meio ponto na escala de 0 a 20, podem aparecer.

As mudanças são transmitidas como diferenças e confirmadas antes de reenviar uma nova mudança. Isso evita contar duas vezes a mesma refeição ou dano e conserva alterações que aconteçam nos dois jogos quase ao mesmo tempo.

O código distingue novas sessões, troca de cena, morte e renascimento. Há proteções para não ressuscitar por um pacote de cura atrasado e para não interpretar uma tela de morte já salva como uma morte nova do PEAK. Esses caminhos de recuperação precisam ser avaliados pelos testes de execução; a presença do código não certifica todos os casos.

## Validação desta função

Os 12 testes puros de reconciliação passaram, incluindo mudanças simultâneas, confirmações atrasadas, repetição de mensagens, limites e arredondamento. Podem ser executados com SDK.NET10:

```powershell
dotnet run --project tests/VitalsLedger.Tests -c Release
```

O build C# terminou com zero erros e quatro avisos já existentes na reconstrução. O build Java também passou. Os testes reais de alimentação, dano e cura nos dois sentidos passaram, incluindo consumo de bife Minecraft, troca de modos, regeneração e renascimento no aeroporto. Consulte [VALIDACAO-FOME-VIDA.md](../VALIDACAO-FOME-VIDA.md) para valores observados, procedimentos e limites.

Esta alteração não certifica campanha completa, cooperação entre dois PCs ou entrada tardia. Os demais limites da integração continuam descritos no README.
