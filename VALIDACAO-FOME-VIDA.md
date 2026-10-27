# Fome e vida compartilhadas — validação em 27/10/2026

Executado com PEAK 3.0.1 da Steam e Minecraft Java 26.3 reais, no Windows desta instalação. Não foi uma simulação dos jogos. O PEAK não tem HP convencional: seus ferimentos (`Injury`) são o equivalente escolhido para os corações; `Hunger` corresponde à comida em sentido inverso. A stamina e os demais estados do PEAK continuam separados.

## Testes dentro dos jogos

| Ação executada | PEAK: vida / comida | Minecraft: vida / comida |
| --- | --- | --- |
| Preparar déficit de comida no PEAK | 20 / 10 | 20 / 10 |
| Executar a ação nativa `Action_RestoreHunger` do PEAK, restaurando 20% | 20 / 14 | 20 / 14 |
| Aplicar ferimentos ao estado PEAK | 16 / 14 | 16 / 14 |
| Executar a ação nativa de cura `Action_ModifyStatus`, removendo 10% de ferimentos | 18 / 14 | 18 / 14 |
| Preparar dano Minecraft sem regeneração por comida alta | 20 / 10 | 20 / 10 |
| Dano real Minecraft de quatro pontos (`/damage`, tipo magic) | 16 / 10 | 16 / 10 |
| Efeito real Minecraft de vida instantânea | 20 / 10 | 20 / 10 |
| Comer um bife no Minecraft, segurando o botão direito | 20 / 18 | 20 / 18 |
| Desligar Minecraft e devolver o controle ao PEAK | 20 / 18 | 20 / 18 |

As ações PEAK foram invocadas pelo DevHarness usando componentes originais do jogo e o item nativo equipado. Isso valida seus efeitos de alimentação e cura; não representa uma gravação de colher, animar e consumir todas as variedades de alimentos PEAK. O bife Minecraft foi consumido pelo fluxo normal de uso do item, com o jogador em Sobrevivência.

Na preparação do teste, a mão Minecraft selecionada estava vazia; recebeu oito bifes, dos quais um foi consumido. Nenhum item existente foi substituído. O teste terminou com os valores sincronizados e depois verificou o ciclo de morte/renascimento no aeroporto.

## Morte e recuperação

Uma morte Minecraft por `/kill` no aeroporto foi seguida por renascimento normal. O contador cumulativo passou de zero para uma morte, a entidade mudou de época e a ponte voltou a confirmar vida 20 / comida 20 nos dois jogos. Não ocorreu ciclo infinito de mortes. A exceção original do aeroporto foi preservada: o PEAK não mata seu personagem ali por uma morte Minecraft.

O código também protege a primeira conexão com um Minecraft já morto e usa um contador para não perder uma morte quando o respawn acontece entre duas leituras. Esses casos extremos e morte/ressurreição no cume ou em campanha não foram todos executados; não se declara cobertura completa do ciclo de campanha.

Também foi conferida a regeneração alimentar: em controle PEAK nativo, vida 16 / comida 20 ficaram estáveis no Minecraft oculto. Ao ativar o controle Minecraft, a regeneração levou ambos a vida 20 / comida 19, consumindo saturação/comida de forma compartilhada. O segundo jogo não avançou um relógio extra em paralelo.

## Testes automatizados e compilação

- Os 12 testes puros em `tests/VitalsLedger.Tests` passaram. Incluem mudanças simultâneas, mensagens repetidas, confirmações atrasadas, fila de mudanças PEAK, arredondamento, limites e valores inválidos.
- C# compilou com zero erros e os quatro avisos preexistentes do código reconstruído. Java compilou com sucesso.
- O protocolo adicional ocupa `0x900` e `0xA00`, depois da grade de água e antes do anel de entradas, sem alterar o layout original. Confirmações impedem aplicar comida/dano duas vezes.
- Ao refletir dados no PEAK, a ponte evita regravar estados iguais, preservando o acumulador fracionário de fome natural do jogo.

As provas selecionadas estão em `PROVAS/vitals-live-results.json`, `PROVAS/vitals-peak.log`, `PROVAS/vitals-minecraft.log` e nas imagens com prefixo `vitals-`. Os comandos de desenvolvimento ficam desativados no atalho normal.

## Limites

O PEAK arredonda estados em passos de 2,5%; pode haver até meio ponto de diferença visual em uma escala de 0 a 20. Absorção dourada, veneno, frio, peso e stamina não são transformados em uma única barra de vida. A mudança não certifica multiplayer entre dois PCs, campanha inteira ou todos os alimentos/efeitos de mods adicionais. Consulte `docs/FOME-E-VIDA.md` para configuração e comportamento dos dois modos.
