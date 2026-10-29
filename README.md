<div align="center">

![MinePeak — construa o seu caminho até o pico](docs/images/peakcraft-local-banner.svg)

**O mundo real do PEAK. Sua skin, seu inventário e seus blocos do Minecraft.**

*MinePeak, baseado em PeakCraft Local / PeakCraft Co-op.*

[![Testes automatizados](https://github.com/deadbynetsu/MinePeak/actions/workflows/tests.yml/badge.svg)](https://github.com/deadbynetsu/MinePeak/actions/workflows/tests.yml)

[Baixar Release 0.4.0](https://github.com/deadbynetsu/MinePeak/releases/tag/v0.4.0) · [Como jogar](COMO-JOGAR.md) · [Instalação](README-PT.md) · [Multiplayer](MULTIPLAYER.md) · [Testes desta versão](VALIDACAO-0.4.0.md) · [Créditos](referencia-coop/PROVENIENCIA.md)

</div>

MinePeak conecta **PEAK, aberto pela Steam**, a uma instância de **Minecraft Java** em segundo plano. Explore o aeroporto e a ilha originais, ative o modo Minecraft dentro da partida e construa caminhos com blocos. Volte aos controles do PEAK quando quiser, sem reiniciar os jogos.

Esta é uma adaptação comunitária gratuita e experimental do **PeakCraft Co-op**. Os jogos continuam sendo necessários e não estão incluídos. O projeto não é afiliado a Mojang, Microsoft, Landfall ou Aggro Crab.

![Captura real: skin do perfil local na praia do PEAK, segurando um bloco](PROVAS/coop-skin-island.png)

*Captura real da integração, registrada na validação local. Não é uma imagem de conceito.*

## Construa, explore, alterne

- **Blocos no PEAK:** coloque e quebre blocos Minecraft no cenário original. A montanha do PEAK permanece intacta.
- **Modo opcional:** escolha PEAK ou Minecraft no painel do menu Esc ou alterne por **L**. A escolha fica salva.
- **Interface discreta:** um pequeno indicador mostra o modo, a vida e a comida. A lista de controles fica no menu Esc; dicas extras aparecem ao usar equipamentos, escalar ou interagir.
- **Construções por mapa:** aeroporto e ilhas mantêm suas próprias construções e contêineres locais. O inventário do jogador acompanha a viagem.
- **Sua identidade:** nome e skin 64×64 do perfil local do SKLauncher, quando disponíveis, com opção de imagem própria.
- **Fome e vida compartilhadas:** alimentação, dano e cura atravessam a ponte entre os jogos. A stamina de escalada continua sendo do PEAK.
- **Equipamento nativo:** segure B para usar os itens do PEAK; escolha com 1–4, use com o mouse e segure/solte Q para arremessar. Comida, bandagens e lançamento de cerca de 23 metros foram conferidos no jogo. Solte B para retornar ao Minecraft.
- **Escalada nativa:** segure V perto de uma parede e use WASD. A escalada e o consumo de stamina são executados pelo PEAK; solte V para voltar aos blocos.
- **Base cooperativa:** compartilhamento de blocos, skins e poses pelo lobby do próprio PEAK. Cada PC executa sua própria ponte local.

Durante B ou V, o corpo e os controles usados são temporariamente os nativos do PEAK. A aparência Minecraft retorna ao terminar. Consumo de marshmallow, carga de arremesso e escalada com gasto de stamina foram exercitados no jogo real. A colisão recebeu exportação dos colliders do cenário, incluindo malhas disponíveis somente na GPU. O [relatório 0.3.0](VALIDACAO-0.3.0.md) identifica os cenários e seus limites.

## Uma interface mais leve

A versão **0.4.0** substitui a sobreposição de controles por um painel compacto com transparência discreta, bordas suaves e texto mais legível. Durante a partida, o canto superior direito mostra apenas o modo e, quando disponíveis, vida e comida. As dicas de **B** e **V** aparecem enquanto essas ações estão em uso; chat, inventário e telas de carregamento ocultam o indicador.

Abra **Esc** para ver os controles em um único cartão **MinePeak**, selecionar **PEAK / Minecraft** e acessar **Opções do Minecraft** quando a ponte estiver conectada. Esse visual é inspirado em vidro; não usa desfoque da imagem do jogo.

![Painel MinePeak dentro do PEAK real](PROVAS/ui-040-pause.png)

Esta revisão separa as construções por mapa e preserva o inventário nas viagens. Mundos experimentais gerenciados são reabertos após backup oficial verificado. Também protege atalhos com o chat ou inventário abertos, repete a liberação de teclas quando a ponte está cheia e mantém edições de blocos na fila até a entrega. O instalador usa os binários incluídos na Release, evitando que uma compilação antiga substitua o mod atualizado. As condições e os resultados da revisão ficam no [relatório 0.4.0](VALIDACAO-0.4.0.md).

## Comece a jogar

Requisitos desta versão: **Windows 11 de 64 bits**, PEAK instalado pela Steam e acesso aos arquivos necessários do Minecraft Java. A combinação exercitada localmente foi **PEAK 3.0.1 + Minecraft Java 26.3**. A preparação instala Java 25 e BepInEx em pastas locais do projeto.

1. Baixe **MinePeak-0.4.0.zip** na [Release experimental](https://github.com/deadbynetsu/MinePeak/releases/tag/v0.4.0) e extraia para uma pasta própria. O ZIP inclui fontes e mods compilados; o arquivo `.sha256` ao lado permite conferir a integridade.
2. Leia a [EULA do Minecraft](https://www.minecraft.net/eula). Se concordar, abra PowerShell nessa pasta e prepare a instalação:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Preparar.ps1 -AceitarEula
   ```

   Para uma instalação PEAK não detectada automaticamente, acrescente `-PeakDir 'D:\caminho\PEAK'`.

3. Deixe a Steam aberta e execute **JOGAR.cmd**. Aguarde o Minecraft oculto e o PEAK carregarem.
4. Entre em uma partida e pressione **L**, ou abra **Esc → MinePeak → Minecraft**. O indicador mostra quando a ponte estiver pronta.

Na versão **0.3.1**, a tecla padrão mudou de F8 para **L**. Configurações antigas que ainda usavam F8 são migradas automaticamente; outras teclas personalizadas são preservadas. Feche o chat ou os menus Minecraft antes de alternar.

A primeira abertura pode demorar para baixar bibliotecas oficiais. O guia **[Como jogar](COMO-JOGAR.md)** mostra os controles, o primeiro bloco, alimentação e solução de problemas.

O lançador pode ler a identidade pública e uma skin já armazenada pelo SKLauncher. Ele não usa senhas ou tokens do launcher. A sessão local não autentica em Realms ou servidores Microsoft; a Steam mantém seu fluxo normal de autenticação e convites.

## Controles principais

| Ação | Controle |
| --- | --- |
| Ativar/desativar modo Minecraft | L ou Esc → MinePeak → PEAK / Minecraft |
| Andar / pular / correr / agachar no Minecraft | WASD / Espaço / Ctrl / Shift |
| Colocar / quebrar bloco colocado | Botão direito / esquerdo |
| Inventário / terceira pessoa | E / F5 |
| Interagir com o mundo do PEAK | G; segure nas interações que exigem tempo |
| Usar equipamentos do PEAK | Segure B; 1–4 selecionam, mouse usa |
| Arremessar item PEAK | Com B segurado, segure Q e solte |
| Escalar com a física e stamina do PEAK | Segure V perto da parede; WASD move, Espaço solta |
| Menu PEAK / menu Minecraft | Esc / O |
| Opções do Minecraft | Esc → MinePeak → Opções do Minecraft, com o modo Minecraft conectado |

A versão **0.3.2** acrescenta colisão das construções Minecraft ao PEAK: a ponte envia as formas físicas dos blocos para permitir apoio e escalada nativa. No modo PEAK, use os controles normais de escalada; com a opção Minecraft ativa, segure **V** junto à parede de blocos. Ambos usam a stamina e as regras de alcance do PEAK. A [checagem 0.4.0](VALIDACAO-0.4.0.md) repetiu a escalada real nos dois modos, a quebra com picareta, a colocação no mesmo espaço e a remoção da construção.

## Cooperativo e estado dos testes

Use os lobbies, convites Steam ou códigos de sala do próprio PEAK. Todos os participantes devem instalar a mesma versão do projeto e ter suas próprias instalações e sessões dos jogos. Não é preciso abrir um servidor Minecraft separado. O **[guia multiplayer](MULTIPLAYER.md)** explica a preparação de cada PC e a sincronização desta versão.

**Ainda não há certificação de uma partida completa entre dois PCs/contas.** Testes puros, mensagens de rede e loopback local ajudam a verificar a implementação, mas não provam latência real, entrada tardia ou campanha cooperativa completa. Veja os relatórios antes de planejar uma sessão longa.

| Área | Evidência disponível |
| --- | --- |
| PEAK e Minecraft reais conectados | Aeroporto, embarque, praia, alternância de modos e construção locais |
| Interface e viagens 0.4.0 | Painel, opções, chat após mudança de foco, reabertura de ilha com backup verificado e preservação dos dados comparados; veja [relatório](VALIDACAO-0.4.0.md) |
| Fome e vida | 12 testes puros e cenários reais nos dois jogos em [VALIDACAO-FOME-VIDA.md](VALIDACAO-FOME-VIDA.md) |
| Equipamentos e escalada | Marshmallow PEAK consumido com comida sincronizada, carga de arremesso e subida real com gasto de stamina |
| Colisão | 18 testes de regiões/mensagens; a 0.4.0 repetiu escalada real dos blocos nos dois modos, quebra, colocação e limpeza dos colisores; a prova histórica de laje permanece na 0.3.2 |
| Compartilhamento Co-op | 21 testes de protocolo/histórico de blocos, incluindo fila cheia e repetição de entrega, implementação Photon e loopback local; dois PCs ainda pendentes |
| Campanha completa e cume | Não percorridos integralmente nesta entrega |

As capturas e registros em **[PROVAS/](PROVAS/)** documentam o que foi executado. **[VALIDACAO-0.4.0.md](VALIDACAO-0.4.0.md)** registra a revisão de interface e bugs; **[VALIDACAO-0.3.2.md](VALIDACAO-0.3.2.md)** registra a colisão das construções; **[VALIDACAO-0.3.1.md](VALIDACAO-0.3.1.md)** registra a tecla L; **[VALIDACAO-0.3.0.md](VALIDACAO-0.3.0.md)** preserva a verificação de equipamentos, escalada, colisão e rede, e **[VALIDACAO.md](VALIDACAO.md)** distingue o Co-op dos testes históricos da base clássica. Não se declara compatibilidade perfeita com todas as colisões, alimentos, mods ou versões futuras dos jogos.

**119 testes passaram na execução local Windows.** Para repetir os testes sem abrir os jogos, use SDK .NET 10 e JDK 17 ou superior e execute `./Testar.ps1`. O JDK 25 preparado pelo projeto atende ao requisito; se ele não estiver em `JAVA_HOME` ou no PATH, informe `-JavaHome 'D:\caminho\jdk'`. A suíte cobre vida/comida, colisão, rede, segurança de entrada, separação de mensagens entre cenas, diário de saves e instalação; os casos específicos do inicializador Windows são executados nesse sistema. A CI pública também executa os testes portáveis em Linux. O build completo do plugin usa os assemblies da instalação local do PEAK.

## Fontes e contribuições

| Pasta | Conteúdo |
| --- | --- |
| `coop/` | Plugin PEAK Co-op reconstruído do pacote MIT e adaptado; C# compilável |
| `coop-fabric/` | Extensão Java compilável de identidade, skin, fome/vida, colisão e saves por cena |
| `tests/` | Testes independentes dos jogos |
| `referencia-coop/` | Licenças, avisos e procedência do pacote de referência |
| `referencia-coop/fabric-reconstruido/` | Java reconstruído para auditoria; não é um projeto compilável |
| `peak/`, `fabric/`, `protocol/` | Fontes preservadas da base clássica |
| `binarios/` | Mods compilados; os jogos e seus assemblies não estão incluídos |

Compilação e configuração estão em **[README-PT.md](README-PT.md)**. O instalador usa `binarios/` por padrão. Depois de compilar suas próprias alterações, use `Instalar-Local.ps1 -UsarBuildLocal` junto dos parâmetros de instalação para selecionar explicitamente a DLL local; sem essa opção, a compilação local é ignorada. Para contribuir, consulte **[CONTRIBUTING.md](CONTRIBUTING.md)**. Relatórios de bugs devem incluir versões, modo usado, passos e um trecho de log sem dados privados.

## Créditos e licença

MinePeak adapta o projeto local **PeakCraft Local**, derivado de **[PeakCraft Co-op, de Issac](https://melty.gg/m/peakcraft-co-op)**, remix do PeakCraft de **aeironnsarmiento**, baseado em **[SkyCraft, de chasmlol](https://github.com/chasmlol/SkyCraft)**. A arquitetura de dois jogos, a ponte e grande parte da implementação vêm desses projetos. Não é uma criação do zero.

A licença **[MIT](LICENSE)** e os avisos do pacote Co-op estão preservados. O C# Co-op foi reconstruído com ILSpy porque a ficha do remix não apontava um repositório de fontes. A [procedência](referencia-coop/PROVENIENCIA.md) detalha essa distinção. O [README upstream](README-UPSTREAM.md) registra a documentação anterior, incluindo suas limitações históricas; as instruções atuais são as desta página e do guia em português.

PEAK, Minecraft e suas marcas, executáveis e recursos pertencem aos respectivos titulares. Nenhum jogo, conta ou recurso proprietário é redistribuído; a integração não remove o DRM da Steam.
