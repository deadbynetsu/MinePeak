# Validação — PeakCraft Co-op local — 26/10/2026

Atualização de 27/10/2026: a sincronização de fome e vida foi implementada e testada nos dois jogos. Veja [VALIDACAO-FOME-VIDA.md](VALIDACAO-FOME-VIDA.md) para os resultados dessa função. O restante desta página registra a validação anterior.

## Escopo e versão

Este relatório descreve a **versão Co-op atual**, adaptada do pacote MIT **PeakCraft Co-op 0.2.1 de Issac**. O relatório anterior da versão clássica com Steve obrigatório foi preservado, sem alteração, em **VALIDACAO-CLASSIC.md**. Uma prova clássica não certifica automaticamente o código Co-op.

Foram executados PEAK **3.0.1 real** da Steam e Minecraft Java **26.3 real** em processos separados no Windows. O host é o próprio PEAK, com seu aeroporto, embarque e ilha; não é um mapa recriado em um servidor Minecraft.

O C# Co-op foi reconstruído da DLL MIT com ILSpy e compilado contra os assemblies locais do PEAK: **zero erros e quatro avisos da reconstrução**. O build Java da extensão `coop-fabric` foi aprovado. Os **21 testes de colisão/raycast** citados no histórico são da base clássica; não constituem uma suíte de 21 testes da nova versão Co-op.

## Evidências verificadas em execução

| Caminho | Resultado observado |
| --- | --- |
| Inicialização do host | Protocolo e rings deram PASS; cinco patches Harmony aplicados. |
| Abertura normal | Atalho sem `-Teste`, `Harness=false` e `CoopLoopback=false`; entrada em Jogar offline pela interface e alternância F8 nos dois sentidos funcionaram. |
| Modo inicial | Arranque com controle nativo do PEAK, estado `PeakOwns`. |
| Perfil SKLauncher | Nome `NeTsU_` detectado; mod carregou skin local 64×64. A aparência foi vista no aeroporto e na praia. |
| Botão no menu Esc | A ativação transferiu controle por `Handoff` → `PeakMenu` → `MinecraftOwns`. |
| Retorno por F8 | Voltou a `PeakOwns` sem mudar o PID Java. No modo desligado, corpo PEAK deixou de ser cinemático (`kinematic False`), stamina nativa estava em 1 e o HUD nativo voltou. |
| Reativação | Novo handshake/teleporte de tomada de controle concluído corretamente, sem reiniciar Minecraft. |
| Embarque | Interação com o terminal do aeroporto por G, cartão de embarque e transição para a ilha real `Level_83`. |
| Colocar blocos na praia | `/give stone 64` e clique direito produziram malhas de 36 e depois 60 vértices, com blocos visíveis no PEAK. |
| Quebrar blocos colocados | Clique esquerdo reduziu malhas para 36 e depois zero; os blocos desapareceram da imagem. |
| Recuperação de vazio | Queda artificial por `/tp @s ~ -1200 ~` acionou `void guard ... put back at`, recolocando o jogador perto de Y=-511, sem morte observada nesse teste. |
| Compartilhamento loopback | Modo de desenvolvimento enviou três mensagens e recebeu três. O participante remoto sintético `-1` injetou três blocos a leste; Minecraft registrou `applied remote block`. |
| Blocos preservados no modo nativo | Teste final aprovado: seções reais continuaram visíveis ao desligar Minecraft, incluindo blocos já carregados do mundo, enquanto HUD/stamina/inventário PEAK voltaram e avatar/HUD Minecraft foram ocultados. |
| Recepção enquanto Viewer com Java ativo | Teste final local aprovado: uma mensagem enviada e recebida em loopback foi aplicada pelo Minecraft após retornar a `PeakOwns`. Não é uma sessão entre dois PCs. |
| Instalador | Nove verificações em um ambiente de teste isolado passaram. |
| Conteúdo do JAR | Revisão confirmou apenas o mod, sem assets dos jogos incluídos no JAR usado como dependência. |

Os cliques, movimentos e transições ocorreram nos jogos reais. A automação de desenvolvimento facilitou a montagem dos cenários; ela não substituiu o processo PEAK por um simulador. O teste loopback, porém, usa um participante sintético e é descrito separadamente para não ser confundido com cooperação entre duas máquinas.

O mundo isolado usado nos testes foi preservado em `local-backup/mundo-de-testes-coop`, fora do ZIP. A abertura normal final criou um mundo limpo e restaurou os itens iniciais. Ao terminar, o modo ficou em Viewer/nativo e os jogos foram encerrados para liberar a máquina.

## Provas e registros

As novas capturas e trechos de log ficam em **PROVAS/**. A imagem `PROVAS/coop-skin-island.png` registra a skin do perfil local na praia da ilha. O diretório também conserva imagens clássicas cujos nomes começam por `steve-`; elas são históricas e pertencem ao relatório **VALIDACAO-CLASSIC.md**.

`PROVAS/coop-native-blocks-final.png` registra um bloco de pedra junto ao HUD, stamina e inventário nativos do PEAK, sem HUD/avatar Minecraft. `PROVAS/coop-loopback-final.png` registra o teste local final de recepção em Viewer.

Logs de execução: `local-profile/BepInEx/LogOutput.log`, `local-profile/minecraft-console.log` e `coop-fabric/run/logs/latest.log`. Esses arquivos são rotativos; as cópias selecionadas de evidência devem ser consultadas em PROVAS quando disponíveis. Não foram geradas imagens por IA para simular as provas.

## Testes finais de blocos no modo nativo — aprovados localmente

O ajuste preservou as seções reais do RenderRing ao desligar o modo Minecraft. O teste visual final mostrou o bloco de pedra com HUD, stamina e inventário do PEAK, sem manter o avatar/HUD Minecraft. A preservação inclui seções reais já carregadas do mundo, além de blocos novos conhecidos pela camada Viewer.

Também foi exercitado o recebimento enquanto Viewer/nativo com Java ativo. O log registrou `ownership: MinecraftOwns -> PeakOwns`, bloco local em `(-3, -511, -98)`, participante remoto sintético `-1` aplicando em `(0, -511, -98)` e mensagem Minecraft `applied remote block`. O contador desse teste final foi **uma mensagem enviada e uma recebida**. Os caminhos de preservação visual e recepção em Viewer passaram localmente; o participante é loopback, não outro jogador ou PC.

Com Java vinculado, a ponte mantém as seções de malha reais. O desenho simplificado de Viewer é usado sem esse vínculo e pode representar blocos remotos como cubos inteiros.

Mesmo quando o bloco é desenhado no modo nativo, **não há colisor PEAK para o bloco Minecraft**. Para ficar em cima de uma construção ou usá-la como apoio, ative o modo Minecraft. Preservação visual e colisão são funções diferentes.

## Limites e trabalho não validado

- **Nenhuma partida real entre dois PCs/contas foi validada.** Três mensagens em loopback não provam entrada em sala, sincronização sob latência ou comportamento de dois jogadores independentes.
- Entrada tardia e compatibilidade com amigos sem os mods ainda não foram validadas. Cada participante precisa das mesmas versões dos mods e de sua própria instalação e conta/sessão dos jogos.
- Não foi percorrida uma campanha inteira até seu final. O sinalizador do cume ainda não está integrado ao controle Minecraft. Desligar o modo devolve os controles e itens ao PEAK, mas isso não certifica uma campanha completa.
- O terreno nativo da montanha não é escavável. Os blocos colocados pelo Minecraft são os que podem ser quebrados pela ponte.
- A colisão entre os jogos é amostrada e pode diferir da geometria original, sobretudo em objetos finos. Não há colisor nativo PEAK para os blocos Minecraft.
- O perfil/skin local foi lido sem alterar as contas do SKLauncher. Isso não valida todos os provedores de skins ou formatos; a extensão atual aceita skin local 64×64 e mantém fallback para a skin padrão.

Por esses motivos, o resultado não é declarado “100% funcional”. Há caminhos locais comprovados, uma implementação de rede que passou pelo teste loopback e limites explícitos ainda a validar.

## Origem e licenças

Veja `referencia-coop/PROVENIENCIA.md`, `LICENSE-PeakCraftCoop.txt` e `THIRD-PARTY-NOTICES.txt`. O pacote MIT de referência estava disponível no cache local do Melty. O C# de `coop/` é uma reconstrução compilável; os 74 arquivos em `referencia-coop/fabric-reconstruido/`, gerados por Vineflower 1.12.0, são para auditoria e não formam um projeto Java compilável. A extensão local compilável está em `coop-fabric/`.

Não foram distribuídos os executáveis, texturas ou assemblies proprietários de Minecraft/PEAK no pacote de mods. A Steam conserva sua autenticação normal; o trabalho não remove verificações de conta ou DRM.
