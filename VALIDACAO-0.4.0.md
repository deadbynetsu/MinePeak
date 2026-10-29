# MinePeak 0.4.0 — interface e revisão de bugs

Checagem local de **29/10/2026**, no Windows 11, com **PEAK 3.0.1 e Minecraft Java 26.3 reais**, no mesmo PC. A interface foi renovada, a proteção de entrada foi conferida e as construções passaram a ser separadas por mapa. **119 testes automatizados passaram no Windows.** Esta checagem não cobre toda a campanha nem uma sessão entre dois computadores.

## Interface e entrada nos jogos reais

A lista permanente de controles deu lugar a um indicador pequeno de modo, vida e comida. O cartão **MinePeak** do menu Esc reúne **PEAK / Minecraft**, controles e **Opções do Minecraft**. O visual usa transparência discreta e bordas suaves; não captura nem desfoca a imagem do jogo.

| Cenário | Resultado observado |
| --- | --- |
| Partida com a ponte conectada | Indicador compacto, sem a sobreposição antiga de controles. |
| Menu Esc e opções | Cartão e botões funcionaram; Opções do Minecraft abriu as opções Java com a ponte conectada. |
| Chat e foco | Com chat aberto, o foco foi para a Steam e voltou ao PEAK. L escreveu `l` no chat, sem trocar o modo; o indicador permaneceu oculto. |
| Inventário | O indicador ficou oculto e segurar B não assumiu os equipamentos nativos. |
| B e V | Transferência temporária para o PEAK, dicas contextuais e retorno ao Minecraft conferidos nos dois caminhos. A escalada física de uma construção foi repetida nos dois modos, conforme abaixo. |
| Escala do painel | Inspeção em 1280×720, 1920×1080 e 1024×768, sem cortes ou sobreposição dos botões nativos observados nesses tamanhos. |

## Bugs encontrados e corrigidos

- **Entrega de blocos:** a fila só avança quando a ponte aceita a mensagem; a repetição usa o estado mais recente, inclusive remoções, para não perder edições nem recriar blocos antigos.
- **Entrada e foco:** chat/inventário são protegidos mesmo após mudança de foco. A liberação de teclas é repetida se o canal estiver cheio, antes de enviar novos comandos; a assinatura de texto é removida ao descarregar o plugin.
- **Instalação e início:** o instalador usa `binarios/` por padrão; uma DLL compilada localmente exige `-UsarBuildLocal`. O lançador valida arquivos/configuração, protege contra inicializações simultâneas e espera o Minecraft anterior terminar.
- **Construções de outro mapa:** um bloco do aeroporto apareceu indevidamente na ilha no teste anterior à correção. Agora cada cena gerenciada tem seu save local, e os dados do jogador acompanham a viagem por snapshots e um diário. Mundos externos e os mundos do SKLauncher não são alvos dessa troca.
- **Identidade das cenas:** cenas Addressables com `buildIndex = -1` podiam produzir identidade zero. O nome UTF-8 da cena agora gera uma identidade estável, preservando a identidade histórica do aeroporto. Mensagens antigas são filtradas e a exportação aguarda o mundo correto ficar pronto.
- **Dados atuais do jogador:** o Minecraft 26.3 grava em `players/data/`. Esse caminho substituiu a procura antiga por `playerdata/`; os dados atuais têm prioridade sobre snapshots anteriores, sem ressuscitar uma cópia legada do inventário.
- **Reabertura de mundo experimental:** a confirmação vanilla, invisível no cliente oculto, podia bloquear uma cena existente. A integração solicita o backup oficial do Minecraft e verifica tamanho/CRC de todas as entradas do ZIP antes de continuar. Erro de backup ou verificação interrompe a abertura.

A extensão Java do perfil é **0.1.2**. O JAR base Co-op foi preservado; DLL PEAK e extensão Java devem ser atualizadas juntas.

## Viagem e preservação dos dados

| Verificação | Resultado registrado |
| --- | --- |
| Aeroporto → ilha | Chegada a `Level_85`, identidade `2271922941` (Java assinado `-2023044355`), marca de cena aceita e nenhuma malha do aeroporto observada na ilha. |
| Construções separadas | Esmeralda e baú com sete diamantes do aeroporto estavam ausentes na ilha; comandos vanilla `execute if block … air` confirmaram ar nas duas posições. |
| Inventário na ida e na volta | Treze papéis identificados foram preservados. Os 32 registros de inventário comparados e os equipamentos, inclusive componentes, ficaram iguais entre origem e destino. |
| Experiência e Ender chest | Na ilha foram acrescentados 17 níveis e cinco ametistas identificadas em slot Ender antes vazio; o aeroporto recebeu 17 níveis e cinco itens. Comparados `Inventory`, equipamentos, `EnderItems`, campos `Xp*`, slot selecionado e `abilities`. |
| Retorno exercitado | O diagnóstico chamou a API nativa `endScreen.ReturnToAirport`; esmeralda e baú com sete diamantes foram restaurados no aeroporto. Esse caminho de teste não equivale a completar o cume. |
| Reinício com o JAR final | Treze papéis, 17 níveis e cinco ametistas Ender foram preservados; a cena `Level_85` já existente reabriu e devolveu o controle a `MinecraftOwns`. |
| Backup oficial na reabertura final | ZIP de **1.743.483 bytes** criado às **10:11:59**, entradas verificadas por CRC às **10:12:00**, mundo carregado às **10:12:01** e marca de cena aceita. Nenhuma malha do aeroporto foi observada. |
| Separação repetida com o JAR final | Às **10:13:15/17**, comandos vanilla confirmaram novamente ar nas posições da esmeralda e do baú do aeroporto. |
| Último retorno com o JAR final | `Inventory`, equipamentos, `EnderItems`, `XpLevel`, `XpTotal`, `XpP`, `SelectedItemSlot` e `abilities` iguais entre origem e destino: 32 registros, 13 papéis, 17 níveis e cinco itens Ender. Às **10:20:04/06**, a esmeralda e o baú com sete diamantes foram confirmados novamente no aeroporto. |
| Limpeza dos itens de teste | Removidos os 13 papéis; slot Ender novamente vazio, experiência em zero e contagens de pedra/pedregulho e desgaste da ferramenta restaurados. |
| Comparação final com o backup anterior aos testes | Após salvar, os dados canônicos do jogador ficaram **exatamente iguais** ao original em `Inventory`, equipamentos, `EnderItems`, `XpLevel`, `XpTotal`, `XpP`, `SelectedItemSlot` e `abilities`: **31 slots ocupados** no original e no final, Ender vazio e experiência zero. |
| Configuração entregue | Retorno a **Viewer/PEAK nativo**, tecla **L**, `Harness=false` e `CoopLoopback=false`. Ambos os jogos fechados após desativar o modo Minecraft às **10:22:26**. |

O tratamento de cena é limitado aos saves locais gerenciados pelo projeto. Diário atômico, snapshots e backups não garantem recuperação de toda falha ou perda de energia; os resultados acima cobrem os campos e viagens comparados.

## Construção, escalada e remoção repetidas

Foi criada no Minecraft uma plataforma com topo Unity **Y = 20** e uma parede, exportadas como **59 volumes físicos**. O diagnóstico enviou movimento e ação aos caminhos de controle reais; os apoios eram os blocos Minecraft, não uma parede artificial criada diretamente no Unity.

| Ação real | Resultado observado |
| --- | --- |
| V no modo Minecraft | `NativeControl` agarrou `MinePeak block collider`; pés de **Y = 20,000 para 21,307**, stamina de **1,000 para 0,874**. Retorno ao Minecraft ao terminar. |
| Escalada PEAK nativa | Entrada principal normal em `PeakOwns` agarrou o mesmo colisor; pés de **Y = 20,066 para 21,801**, stamina final **0,878**. O modo Minecraft foi retomado depois. |
| Quebra com mouse esquerdo e picareta de diamante | Removeu o bloco Minecraft `[24, -491, -72]`; evento local de ar e volumes físicos de **59 para 58**. |
| Colocação com mouse direito na mesma posição | Pedra colocada na mesma célula, revisão 2; volumes físicos voltaram a **59**. |
| Remoção da construção de teste | Removidos 46 blocos construídos, além da esmeralda e do baú de teste. Colisão voltou às **três seções e 11 volumes originais**; o apoio encontrado era o piso do aeroporto, sem colisor Minecraft. |

## Encerramento observado

No encerramento final, Minecraft registrou o salvamento dos jogadores e mundos e o processo terminou com `BUILD SUCCESSFUL`. PEAK e Java ficaram fechados. O PEAK apresentou **`0xC0000005`** ao sair: às **09:58** com o mod, após Alt+F4. Como controle, o **PEAK original foi iniciado pela Steam sem os argumentos Doorstop**, com `enabled=false` no loader e sem atualização dos registros BepInEx; o botão **SAIR** da tela inicial também terminou com o mesmo código às **10:00:06**. A falha é reproduzível nesta máquina sem carregar MinePeak; **a causa não foi determinada**. Não se declara encerramento limpo do PEAK nem uma causa específica.

## Compilação e regressão automatizada

| Área | Resultado local Windows |
| --- | --- |
| Plugin C# 0.4.0 | Zero erros; quatro avisos já conhecidos da reconstrução. |
| Extensão Java 0.1.2 | Compilada com JDK 25 e carregada no Minecraft real. |
| Fome/vida | **12/12** |
| Regiões e mensagens de colisão | **18/18** |
| Rede e histórico de blocos | **21/21** |
| Segurança de entrada | **8/8** |
| Identidade de cena e filtro de mensagens | **18/18** |
| Diário de saves e verificação de backups Java | **24/24** |
| Instalador | **9/9** |
| Inicializador Windows | **9/9** |
| Total local | **119/119** |

A [CI pública](https://github.com/deadbynetsu/MinePeak/actions/workflows/tests.yml) executa o commit ao publicar; seu estado fica no workflow e no badge do README. A suíte prevê **110 casos no Linux**, que não executa os nove casos específicos do inicializador Windows. Isso não é uma declaração antecipada do resultado da CI.

Para repetir, use **SDK .NET 10 e JDK 17+** e execute `./Testar.ps1`; o JDK 25 preparado atende ao requisito. Se necessário, informe `-JavaHome 'D:\caminho\jdk'`. Os testes usam arquivos temporários e transportes/processos simulados onde necessário; não substituem física Unity ou multiplayer real. O build completo usa assemblies locais do PEAK que não são publicados.

## Binários e provas

| Arquivo | SHA-256 |
| --- | --- |
| `binarios/PeakCraftCoop.dll` | `c65cac07b1bc502cfbc43843c57ce6512b724b83e8e88d925fa54ce83bb92453` |
| `binarios/peakcraft-profile-0.1.2.jar` | `4f705583065ea9a318c4123a55c8861c95415576072bb8b3f9e9ee96bc897d0b` |

- [Resultados resumidos](PROVAS/polish-040-results.json), [registros PEAK](PROVAS/polish-040-peak.log), [registros Minecraft](PROVAS/polish-040-minecraft.log) e [testes automatizados](PROVAS/polish-040-tests.log).
- Capturas: [partida](PROVAS/ui-040-gameplay.png), [pausa](PROVAS/ui-040-pause.png), [opções](PROVAS/ui-040-options.png), [chat após mudança de foco](PROVAS/ui-040-chat-focus.png), [escalada por V](PROVAS/ui-040-block-climb.png) e [escalada PEAK nativa](PROVAS/ui-040-native-block-climb.png).
- [Checksums dos mods e das provas](PROVAS/checksums.json).

## Limites

Campanha completa e partida entre duas contas Steam/dois PCs **não foram validadas**. A revisão repetiu escalada, quebra e colocação nos cenários descritos acima; isso não certifica toda situação de física. O formato de todos os blocos, itens, mapas e combinações de mods não foi certificado. A stamina continua nativa; a montanha PEAK não é escavável; inventários/NBT, entidades e redstone não formam um servidor cooperativo compartilhado ([detalhes](MULTIPLAYER.md)). Esta versão não é uma garantia de ausência de bugs.

As provas anteriores de [colisão 0.3.2](VALIDACAO-0.3.2.md), [L 0.3.1](VALIDACAO-0.3.1.md), [equipamentos e escalada 0.3.0](VALIDACAO-0.3.0.md) e [fome/vida](VALIDACAO-FOME-VIDA.md) permanecem preservadas.
