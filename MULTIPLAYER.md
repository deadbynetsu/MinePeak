# Jogar MinePeak com amigos

MinePeak é esta distribuição local baseada no PeakCraft Co-op/SkyCraft, com os créditos MIT preservados.

O cooperativo usa a sala normal do **PEAK na Steam**, com o limite de participantes do próprio jogo (até quatro nesta versão). Cada PC executa seu PEAK e um Minecraft Java local oculto. A ponte compartilha blocos colocados/quebrados, a skin e a pose; o PEAK continua cuidando dos jogadores, equipamentos, escalada, viagem e partida.

**Estado: multiplayer experimental implementado.** A lógica de sincronização passou em **19 testes automatizados**, incluindo duas e três pontas simuladas e entrada tardia. Ainda falta uma partida completa entre **duas contas Steam e dois PCs reais**. O teste automatizado substitui o transporte Photon, a cena Unity e a memória compartilhada por simulações; ele não comprova conectividade Steam/Photon pela internet.

## Preparar cada PC

1. Instale a **mesma versão deste pacote** nos computadores e prepare-a conforme [README-PT.md](README-PT.md). Todos devem usar o mesmo Minecraft/Fabric/mods do perfil fornecido e uma versão compatível do PEAK.
2. Cada participante precisa de sua instalação do PEAK, sua própria conta Steam e os arquivos de Minecraft exigidos pela instalação local. O mod é gratuito; este repositório não distribui os jogos nem remove seus requisitos de licença/autenticação.
3. Inicie por **JOGAR.cmd** em cada computador, com a Steam conectada normalmente. Para multiplayer, use o lançador normal, **sem `-Teste`**: esse parâmetro abre uma sessão offline para diagnóstico.
4. Em `local-profile/BepInEx/config/PeakCraftCoop.cfg`, mantenha `[Coop] Share = true` e `[Dev] CoopLoopback = false`, `Harness = false`. O lançador normal já desativa o loopback e o harness.

Não é necessário abrir portas de um servidor Minecraft, conectar a `localhost`, abrir SKLauncher durante a partida ou compartilhar uma conta Steam. Cada ponte usa memória compartilhada somente dentro do seu próprio PC; a rede entre os PCs é a do PEAK.

## Entrar na mesma partida

1. Um participante hospeda uma sala pelo menu normal do PEAK.
2. Os demais entram por convite Steam ou pelo mecanismo de entrada/código que o PEAK mostrar para essa sala. Confirme primeiro que todos aparecem no aeroporto nativo.
3. Cada um ativa seu **modo Minecraft** no botão do menu Esc ou em **L**. A escolha é individual: um jogador pode usar Minecraft enquanto outro mantém PEAK nativo.
4. Em modo Minecraft, construa e quebre blocos normalmente. Os participantes com esta mesma versão recebem as edições. O personagem Minecraft remoto usa a skin transmitida pelo próprio perfil.
5. Para comida/equipamentos PEAK, segure **B**; para arremessar, segure e solte **Q** enquanto segura B. Para escalada nativa temporária, segure **V** perto de uma parede. Durante essas ações, o corpo nativo do PEAK aparece; ao retornar, a skin Minecraft volta. Os controles completos estão em [COMO-JOGAR.md](COMO-JOGAR.md).

A vida/fome compartilhada é **individual**: liga o Minecraft e o PEAK da mesma pessoa. Comer ou sofrer dano não altera automaticamente os outros jogadores.

## O que foi acrescentado na sincronização

| Caso | Comportamento implementado |
| --- | --- |
| Blocos feitos enquanto o anfitrião está sozinho | Permanecem no diário da sessão e são enviados a quem entra depois. |
| Um bloco é removido | O estado de ar é mantido no diário, para impedir que um snapshot antigo ressuscite o bloco. |
| Dois jogadores editam a mesma posição ao mesmo tempo | Carimbos de revisão + ator Photon dão um desempate determinístico. |
| Snapshot atrasado chega depois de uma edição nova | A revisão antiga é ignorada. |
| Mapa grande | Snapshots em lotes de até 32 estados; a ponte aplica até 16 alterações por quadro ao Minecraft. |
| O dono original da sala sai | Os pares que receberam as edições conservam o diário e podem responder às novas entradas; a transferência da sala/partida continua sendo responsabilidade do PEAK. |
| Troca aeroporto/ilha ou de sala | O diário é separado pela cena e reiniciado para a nova sessão. Mensagens atrasadas de outra cena são ignoradas. |
| B/V, desconexão Java ou retorno ao PEAK | O modo anunciado aos demais acompanha quem controla o personagem, para restaurar o scout nativo. |

O protocolo desta edição é **v2**. Todos devem atualizar juntos; a edição Co-op original v1 não participa corretamente destes snapshots.

## Limites atuais

- O diário registra até **65.536 coordenadas diferentes por cena/sessão**. Ao atingir esse limite, novas coordenadas deixam de ser anunciadas e o log informa o motivo; alterações nas coordenadas já conhecidas continuam possíveis.
- O diário existe em memória durante a sessão. Não é um sistema de salvamento compartilhado nem um servidor Minecraft persistente. Uma construção antiga do mundo Java local que antecede a sessão não é automaticamente descoberta/exportada para outro jogador.
- A sincronização cobre colocar/quebrar blocos pelo jogador e o identificador de seu estado. Não replica conteúdo/NBT de baús, inventários Minecraft, entidades, redstone nem todas as mudanças automáticas de fluidos e blocos.
- O jogador nativo do PEAK vê as construções, mas os blocos Minecraft ainda não fornecem colisores para o corpo nativo. **Use o modo Minecraft para ficar em cima das construções.**
- Sem Java ativo, a visualização de blocos remotos usa cubos simplificados. Com Java ativo, a ponte pode mostrar as seções reais do Minecraft.
- Compatibilidade com clientes sem este mod, a campanha inteira em grupo e o comportamento sob latência/perda de rede real continuam sem certificação.

## Repetir os testes automatizados

Com SDK .NET 10 instalado, na raiz do projeto:

```powershell
dotnet run --project tests/Multiplayer.Tests -c Release
```

`./Testar.ps1` executa também os testes de fome/vida e de regiões de colisão, abortando se qualquer projeto falhar. O workflow [tests.yml](.github/workflows/tests.yml) repete esses três projetos em Windows e Linux, sem assemblies ou recursos dos jogos. O build completo da DLL requer os assemblies locais do PEAK e permanece separado dessa CI pública.

Para gravar um novo relatório:

```powershell
dotnet run --project tests/Multiplayer.Tests -c Release -- --report PROVAS/multiplayer-protocol-results.json
```

O projeto de testes compila as classes reais `BlockRelay`, `BlockJournal`, `CoopProto` e `PosePacket`. Os substitutos de transporte/cena existem somente em `tests/Multiplayer.Tests/Stubs.cs`, não entram na DLL do jogo. Veja o [resultado desta execução](PROVAS/multiplayer-protocol-results.json).

## Roteiro de confirmação com dois PCs reais

Este roteiro ainda precisa ser executado; a tabela não indica aprovação prévia.

| Teste | Conferência nas duas telas |
| --- | --- |
| Entrada normal | Duas contas distintas na mesma sala PEAK, sem sessão offline. |
| Skin e movimento | A vê B e B vê A; andar, agachar, saltar e mudar a câmera não deixam um scout duplicado permanente. |
| Edições | A coloca 10 blocos e B remove 3; ambos terminam com as mesmas 7 posições. |
| Entrada tardia | A constrói e remove blocos antes de B entrar; após a sincronização, B recebe as posições finais, incluindo o ar. |
| Conflito simultâneo | Ambos colocam blocos diferentes na mesma coordenada; ao estabilizar, os dois veem o mesmo bloco. |
| B e V | Usar comida PEAK, arremessar e escalar restaura o corpo/pose corretos no outro PC. |
| Vida/fome | Comer e receber dano sincroniza os dois jogos locais, preservando os valores do outro participante. |
| Viagem | Os dois embarcam e chegam à mesma ilha, sem blocos do aeroporto surgindo na ilha. |
| Saída/troca do host | O PEAK mantém a sala quando aplicável; uma nova entrada recebe o diário dos participantes que ficaram. |

Logs úteis por PC: `local-profile/BepInEx/LogOutput.log`, `local-profile/minecraft-console.log` e `coop-fabric/run/logs/latest.log`. Linhas `coop: requested scene snapshot`, `coop: local block` e `coop: remote block` ajudam a comparar o envio e o recebimento. Capturas/relatórios de testes locais não equivalem a este teste com duas máquinas.
