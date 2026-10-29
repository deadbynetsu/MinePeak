# MinePeak — PEAK com modo Minecraft opcional

MinePeak usa a base **PeakCraft Local**; pastas e arquivos de configuração ainda mantêm nomes PeakCraft. A revisão **0.4.0** renova a interface, separa as construções por mapa preservando o inventário e corrige problemas de entrada, entrega de blocos, instalação e inicialização; a checagem está em **[VALIDACAO-0.4.0.md](VALIDACAO-0.4.0.md)**. A tecla de alternância continua **L**. A colisão das construções, equipamentos e escalada nativos e o cooperativo das revisões anteriores continuam documentados separadamente. Campanha completa e partida real entre dois PCs/contas permanecem pendentes.

**Abra `JOGAR.cmd` nesta pasta.** O PEAK da Steam executa o aeroporto, a ilha e a partida reais. O Minecraft Java roda oculto em segundo plano. Você começa com os controles nativos do PEAK e decide dentro do jogo quando ativar a movimentação, inventário e blocos do Minecraft. O guia rápido está em **[COMO-JOGAR.md](COMO-JOGAR.md)**.

Nesta máquina também foi criado o atalho **PEAK com Minecraft** na área de trabalho. Para recriá-lo depois de preparar outra pasta, execute `Criar-Atalho.ps1`.

A versão atual usa o **PeakCraft Co-op 0.2.1 de Issac**, remix do PeakCraft de aeironnsarmiento, baseado no SkyCraft de chasmlol. Os avisos e a licença MIT distribuídos com o pacote estão em `referencia-coop/`; a procedência e a reconstrução das fontes estão descritas em `referencia-coop/PROVENIENCIA.md`. Não é uma implementação criada do zero.

## Jogar nesta máquina

1. Deixe a Steam aberta com sua sessão normal e feche PEAK se ele já estiver aberto.
2. Execute **JOGAR.cmd**. Aguarde PEAK e o Minecraft oculto carregarem.
3. No PEAK, escolha **Jogar offline** para uma partida solo. O modo inicial é **Viewer/PEAK nativo**: você mantém o personagem, controles, escalada, stamina e itens do PEAK.
4. Dentro da partida, pressione **L** ou abra **Esc → MinePeak** e selecione **Minecraft**. Aguarde a transferência de controle e o HUD do Minecraft aparecer.
5. Para voltar ao PEAK normal, pressione **L** ou abra **Esc → MinePeak** e selecione **PEAK**. A alternância não reinicia os jogos.

A escolha é salva. Uma instalação nova começa em Viewer; se a execução anterior terminou em Minecraft, a configuração pode reabrir nesse modo. Você pode alternar novamente pelo painel ou **L**, com o chat e os menus Minecraft fechados.

A versão 0.3.1 migra automaticamente a configuração antiga `ToggleModeKey = F8` para `L` em `local-profile/BepInEx/config/PeakCraftCoop.cfg`. Outras teclas personalizadas são preservadas.

Não precisa abrir SKLauncher nem conectar a localhost. O atalho usa a instância Java isolada do projeto e lê a identidade pública do perfil ativo do SKLauncher. Não altera suas contas, seus tokens ou seus mundos. A sessão local não autentica em Realms ou servidores Microsoft; PEAK continua usando a autenticação e os lobbies normais da Steam.

## Interface e controles na tela

Durante a partida, um indicador compacto no canto superior direito mostra o modo atual e, quando disponíveis, os percentuais compartilhados de vida e comida. As dicas de equipamento e escalada aparecem enquanto você segura **B** ou **V**. O indicador é ocultado ao abrir chat, inventário, pausa ou carregamento.

O menu **Esc** reúne a seleção **PEAK / Minecraft**, o estado da ponte e uma lista curta dos controles no cartão **MinePeak**. O botão **Opções do Minecraft** fica disponível com o modo Minecraft ativo e conectado; ele fecha a pausa PEAK e abre as opções do Minecraft. A interface usa transparência discreta e bordas suaves, sem desfoque da imagem do jogo.

## Mapas e viagens

As construções e os contêineres locais do aeroporto e de cada cena da ilha ficam em saves separados. Ao embarcar ou retornar, a ponte leva os dados do jogador para o destino; os itens e a experiência comparados foram preservados nos testes descritos em [VALIDACAO-0.4.0.md](VALIDACAO-0.4.0.md). Isso não compartilha inventários entre amigos: cada jogador continua com seus dados locais.

Ao reabrir um mundo gerenciado que o Minecraft marca como experimental, a ponte solicita o backup oficial e verifica o arquivo antes de continuar. Aguarde essa etapa; se o backup ou a verificação falhar, a abertura é interrompida para preservar os dados. O tratamento fica limitado aos saves do projeto, sem alterar seus mundos externos ou os do SKLauncher.

A extensão usa o caminho atual `players/data/` do Minecraft 26.3 e prioriza os dados atuais do jogador, evitando restaurar um inventário legado. Atualize a DLL e a extensão Java juntas e feche ambos os jogos antes de instalar.

## Skin do usuário

Steve deixou de ser obrigatório. O lançador tenta usar o **nome e UUID do perfil ativo do SKLauncher** e sua própria skin já armazenada no cache local. No teste de arranque, o nome detectado foi `NeTsU_` e o mod registrou o carregamento de uma skin local **64×64**.

O kit inicial inclui armadura de ferro. Para ver a skin inteira com F5, abra E, entre na aba do inventário do personagem e retire capacete e armadura.

Se a skin não estiver no cache ou não puder ser carregada, o jogo usa a skin padrão do perfil Minecraft. Não há download de credenciais ou sincronização automática com todos os provedores de skins. Skins legadas 64×32 ainda não são aceitas por esta extensão.

Você pode escolher uma imagem local em `local-config.json`, preservando os outros campos já existentes:

```json
{
  "username": "SeuNome",
  "skinFile": "C:\\caminho\\minha-skin-64x64.png",
  "skinModel": "wide"
}
```

Use `slim` em `skinModel` para braços finos. As alterações de nome/skin entram na próxima abertura dos jogos. Não coloque senhas ou tokens nesse arquivo.

## Controles e modos

| Ação | Controle / modo |
| --- | --- |
| Alternar PEAK ↔ Minecraft | L ou Esc → MinePeak → PEAK / Minecraft |
| Controles nativos do PEAK | Modo Viewer/PEAK nativo |
| Usar itens PEAK sem desativar a opção Minecraft | Segure B; 1–4 selecionam e os botões do mouse usam |
| Arremessar o item PEAK equipado | Com B segurado, segure Q para carregar e solte Q |
| Escalada nativa temporária no modo Minecraft | Segure V junto à parede; WASD move, Espaço solta; solte V para retornar |
| Andar / pular / correr no Minecraft | WASD / Espaço / Ctrl |
| Agachar no Minecraft | Shift |
| Inventário | E, com modo Minecraft ativo; catálogo livre de blocos somente em Creative |
| Colocar / quebrar bloco Minecraft colocado | Botão direito / esquerdo |
| Câmera Minecraft em terceira pessoa | F5 |
| Interagir com PEAK no modo Minecraft | G; segure para ações como embarque |
| Menu PEAK / menu Minecraft | Esc / O |
| Configurar controles e sensibilidade Minecraft | Esc → MinePeak → Opções do Minecraft, com o modo Minecraft conectado |
| Survival / Creative Minecraft | `/gamemode survival` / `/gamemode creative` |

Com a sincronização de fome e vida habilitada, o Minecraft começa em **Sobrevivência**, para que comida, dano e cura tenham efeito normal. Para um teste rápido de construção, use `/give @s minecraft:stone 64`, escolha o bloco e mire no chão. Você pode selecionar `/gamemode creative` para construir livremente; nesse modo, a imunidade e as regras de comida do Creative continuam valendo. Somente os blocos Minecraft colocados são quebráveis; a montanha nativa não é escavável. A **0.3.2** envia as formas de colisão dos blocos ao PEAK para apoio e escalada nos dois modos. Use os controles de escalada do PEAK no modo nativo ou segure **V** no modo Minecraft. A stamina continua nativa; aguarde a conexão Java e o carregamento da seção. A [checagem 0.4.0](VALIDACAO-0.4.0.md) repetiu a escalada real de blocos nos dois modos, a quebra com picareta, a recolocação e a remoção dos apoios; a prova anterior de laje permanece na [0.3.2](VALIDACAO-0.3.2.md).

Para embarcar em modo nativo, use os controles normais do PEAK. Em modo Minecraft, olhe para o terminal e segure **G**, depois confirme a viagem no cartão de embarque.

O caminho de **B** transfere temporariamente o controle ao PEAK para usar sua comida e seus equipamentos, incluindo a carga nativa de arremesso por Q. **V** faz o mesmo para a escalada real, com o consumo de stamina e as restrições normais do jogo. Ao soltar, a ponte devolve o controle à posição Minecraft. Durante esses dois caminhos, o corpo e os braços são os do PEAK; sua aparência Minecraft retorna em seguida. Consumo de marshmallow, carga de arremesso e escalada com gasto de stamina passaram nos cenários reais da versão 0.3.0. Isso não certifica todos os equipamentos ou a sequência final de campanha; os resultados exatos estão em [VALIDACAO-0.3.0.md](VALIDACAO-0.3.0.md).

## Fome e vida compartilhadas

A nova extensão liga a comida do Minecraft à fome do PEAK e os corações aos ferimentos do PEAK. Comer em qualquer jogo repõe a comida compartilhada; dano e cura que alteram esses valores também aparecem no outro. Comida comum não passa a curar instantaneamente: a vida sobe quando o jogo aplica uma cura ou regeneração de fato.

PEAK não tem uma barra de HP igual à do Minecraft. A correspondência usa **Injury/ferimentos** como vida, enquanto a **stamina de escalada continua separada**. Frio, veneno e os outros estados nativos são preservados. O painel mostra os percentuais compartilhados de **Vida** e **Comida** nos dois modos.

Só o jogo que controla o personagem avança seu gasto natural de fome. Em PEAK nativo, o relógio de comida/regeneração alimentar do Minecraft fica pausado; no modo Minecraft, fica pausado apenas o aumento natural de fome do PEAK. Comer e usar curas ou poções continuam podendo alterar os valores compartilhados.

As opções ficam em `local-profile/BepInEx/config/PeakCraftCoop.cfg`, seção `[Vitals]`: `Enabled=true`, `StartInSurvival=true` e `ShowHud=true`. Veja [FOME-E-VIDA.md](docs/FOME-E-VIDA.md) para configuração e limites. Os **12 testes puros** e os testes reais de alimentação, dano e cura nos dois sentidos passaram. Consumo de bife, troca de modos, regeneração e renascimento no aeroporto também foram conferidos: [resultados e provas](VALIDACAO-FOME-VIDA.md).

## Jogar com amigos

Consulte **[MULTIPLAYER.md](MULTIPLAYER.md)** para o passo a passo em cada PC, o protocolo v2, a entrada tardia e seus limites. A lógica compartilhada passou em 21 testes automatizados, incluindo repetição de entrega quando a ponte está cheia; o roteiro entre dois PCs reais continua pendente.

A base Co-op contém compartilhamento de blocos, skins e poses pelos lobbies Photon do próprio PEAK, com até quatro jogadores. Use **Hospedar jogo**, convites Steam ou o código normal da sala. **Cada PC precisa dos mesmos mods/versões e de sua própria instalação e conta dos jogos.** Não compartilhe uma única conta Steam entre dois clientes.

O modo Viewer mantém o jogador sob controle nativo do PEAK e pode desenhar elementos compartilhados por amigos com os mods instalados. Não foi validada compatibilidade com amigos sem mod.

**A sessão real com dois computadores/contas e a entrada tardia ainda não foram validadas nesta entrega.** O funcionamento de um cliente local, código de rede presente ou teste loopback não prova uma partida cooperativa completa. A própria publicação Co-op 0.2.1 também registra esse limite. Não há promessa de jogo “100% funcional”.

## Estado de validação

Na validação histórica da base Co-op foram confirmados em execução: plugin e ponte carregados, nome/skin do SKLauncher no aeroporto e na praia, ativação pelo botão Esc, retorno nativo pela então tecla F8 sem trocar o processo Java, reativação com novo handshake, embarque e chegada à ilha `Level_83`, colocação/remoção de blocos e recuperação de uma queda artificial no vazio sem morte. O teste de compartilhamento foi **loopback no mesmo PC**, com três mensagens enviadas e recebidas e blocos remotos aplicados; ele não equivale a dois jogadores reais. O atalho atual é **L**.

Veja **VALIDACAO.md** e **PROVAS/** para os detalhes. O relatório clássico anterior está preservado em **VALIDACAO-CLASSIC.md**; seus 21 testes de colisão/raycast pertencem à base clássica, não são 21 novos testes do Co-op.

A preservação visual dos blocos no modo nativo **passou no teste local final**: a malha real permaneceu visível, incluindo seções já carregadas do mundo, enquanto o HUD, stamina e inventário do PEAK voltaram e o avatar/HUD Minecraft foram ocultados. A recepção de blocos remotos em Viewer, com Java ainda ativo, também passou no loopback local: uma mensagem enviada e recebida foi aplicada pelo Minecraft. Veja `PROVAS/coop-native-blocks-final.png` e `PROVAS/coop-loopback-final.png`. Isso não equivale a uma partida entre dois PCs.

Limites importantes da base: sem vínculo com Java, a visualização Viewer de blocos remotos usa cubos inteiros; com Java ativo, são preservadas as seções reais renderizadas pela ponte. A revisão atual acrescenta exportação dos colliders reais para reduzir falhas em paredes finas; isso ainda não certifica todas as formas e regiões do mapa. O caminho temporário de itens nativos permite executar ações do PEAK, mas a campanha inteira e o sinalizador no cume ainda exigem validação. O mapa real continua sendo do PEAK; não há reconstrução dele em um servidor Minecraft separado.

## Instalação e reversão

A pasta local está preparada com perfil externo BepInEx em `local-profile/`. O lançador ativa o loader somente por seus argumentos de abertura. `doorstop_config.ini` na pasta do jogo mantém `enabled=false`, portanto abrir PEAK normalmente pela biblioteca Steam continua sem esta integração. Não foram substituídos PEAK.exe, Steamworks ou os assemblies proprietários.

O instalador usa a versão Co-op e os arquivos de `binarios/` por padrão, mesmo que exista uma DLL antiga na pasta de compilação. Ele desativa a DLL clássica antiga para não carregar as duas pontes simultaneamente. Para parar de usar, feche os jogos e inicie PEAK normalmente pela Steam. Minecraft encerra junto com PEAK. Não abra duas cópias da integração no mesmo PC: elas usariam o mesmo canal local.

Em uma cópia nova extraída do ZIP, após ler/aceitar a [EULA do Minecraft](https://www.minecraft.net/eula), execute:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Preparar.ps1 -AceitarEula
```

Se necessário, acrescente `-PeakDir 'D:\caminho\PEAK'`. A preparação baixa Java e BepInEx com checksums. A primeira abertura pode baixar bibliotecas/assets oficiais do Minecraft. O pacote não distribui jogos, contas, Java nem o cache de bibliotecas.

## Fontes e diagnóstico

- `coop/`: reconstrução C# da DLL MIT Co-op, com as alterações locais de alternância reversível.
- `coop-fabric/`: extensão Java compilável do perfil/skin, fome/vida compartilhadas, formas físicas dos blocos e saves separados por cena.
- `tests/VitalsLedger.Tests/`: 12 testes puros de reconciliação de fome/vida, sem abrir os jogos.
- `referencia-coop/`: avisos, licença e procedência do pacote de referência.
- `referencia-coop/fabric-reconstruido/`: 74 arquivos Java reconstruídos por Vineflower 1.12.0 para auditoria; não formam um projeto compilável.
- `peak/`, `fabric/`, `protocol/`: fontes clássicas preservadas.
- `binarios/`: somente os mods compilados, sem assemblies ou texturas dos jogos.

Para compilar C#, copie `coop/Config.Local.props.template` para `coop/Config.Local.props`, ajuste os caminhos e use SDK .NET 10: `dotnet build coop/CoopLocal.csproj -c Release`. Para Java, use JDK 25, entre em `coop-fabric` e execute `..\fabric\gradlew.bat build`. Para instalar a DLL que você compilou, acrescente `-UsarBuildLocal` aos parâmetros de `Instalar-Local.ps1`; sem essa opção, o instalador mantém a DLL distribuída em `binarios/`. Preserve os avisos MIT e não publique os assemblies locais do PEAK usados como referências.

**119 testes passaram na execução local Windows.** Para executar os testes sem abrir os jogos, use SDK .NET 10 e JDK 17 ou superior e execute `./Testar.ps1`. O JDK 25 preparado pelo projeto atende ao requisito. O script procura Java em `JAVA_HOME` e no PATH; se necessário, informe `-JavaHome 'D:\caminho\jdk'`. A suíte cobre fome/vida, colisão, rede, segurança de entrada, separação de mensagens entre cenas, diário de saves e instalação. Os casos do inicializador Windows são executados nesse sistema; a CI também executa os testes portáveis em Linux. O [relatório 0.4.0](VALIDACAO-0.4.0.md) registra os resultados atuais. O [relatório 0.3.2](VALIDACAO-0.3.2.md) preserva a checagem de apoio e escalada das construções nos jogos reais.

Logs: `local-profile/minecraft-console.log`, `coop-fabric/run/logs/latest.log`, `local-profile/BepInEx/LogOutput.log`. Alguns nomes internos ainda dizem Skyrim por herança do SkyCraft; o host é PEAK. Avisos Realms na sessão local não significam por si só que a ponte falhou. O atalho normal deixa o DevHarness desativado.
