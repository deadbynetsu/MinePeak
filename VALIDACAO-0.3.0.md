# MinePeak 0.3.0 — verificação da entrega

Data: **27/10/2026**. Ambiente real: Windows 11, PEAK 3.0.1 pela Steam, Minecraft Java 26.3, Fabric e Java 25. A DLL distribuída e a instalada durante a segunda rodada têm SHA-256 `66be83425753bcc908e8c9589a21a9829be48c68a7b63dce1a0c0504f79cc710`.

Esta checagem fecha os cenários desta versão. **Não é uma certificação de todas as funções dos dois jogos.** Foi usado um PC e uma conta Steam; não houve partida completa com duas contas, teste de campanha até o cume ou matriz de todos os alimentos/equipamentos.

## Testes automatizados

`Testar.ps1` executou **37 casos e todos passaram**: 12 de reconciliação de vida/comida, seis de distribuição vertical de colisores e 19 do cooperativo. Os testes de rede compilam as classes de produção `BlockRelay`, `BlockJournal` e `PosePacket`, com transportes Photon, Unity e memória compartilhada simulados. Cobrem dois/três pares, 1.050 blocos, entrada tardia, remoções, conflitos em ordem inversa, troca do host, pacotes inválidos e mudança de sessão/cena.

O plugin C# compilou com **zero erros e quatro avisos conhecidos** da reconstrução. A extensão Java não precisou mudar nesta revisão. A CI incluída repete os testes puros em Windows e Linux; esse teste Linux não significa suporte do jogo a Linux.

## Cenários executados dentro dos jogos

| Cenário | Resultado observado |
| --- | --- |
| Comida PEAK com preferência Minecraft ativa | Marshmallow real criado pelo próprio banco de itens do jogo, equipado e consumido pela ação primária. Comida **8 → 11** nos dois jogos; retorno automático ao Minecraft |
| Cura por item PEAK | Bandages real equipado e consumido. Vida **14 → 20** nos dois jogos |
| Arremesso carregado | Carga intermediária **0,539**, item em movimento com velocidade aproximada de **19,9 m/s** na direção do lançamento; distância **16,10 m**, depois **22,93 m**, medida pelo PEAK |
| Soltar equipamentos durante um arremesso | Encerramento antecipado do controle B gerou liberação e retorno ao Minecraft, sem item preso na mão; novo item observado no mundo |
| Escalada na parede original do aeroporto | `Climbing wall`, MeshCollider não legível pela CPU. O PEAK aceitou o agarre; stamina **0,487 → 0,282**, pés **y=1,168 → 3,421**. Depois, retorno ao Minecraft com novo número de transferência confirmado pelos dois lados |
| Parede fina sintética BoxCollider | Espessura **0,08 m**; tentativa de andar por três segundos parou o centro do jogador a **0,30 m** da face |
| Parede fina sintética com malha somente na GPU | Mesmo teste, **0,08 m** e bloqueio a **0,30 m**. Ao remover a parede, o jogador avançou além do seu plano |
| Malhas reais do aeroporto | Até **45 malhas prontas e zero indisponíveis** no percurso observado |
| Construção/quebra em Sobrevivência | Blocos consumidos/colocados e quebrados com picareta; alterações de pedra e ar chegaram ao loopback |
| Rede v2 nos processos reais | **7 alterações locais e 7 recebidas**, incluindo remoções; fila pendente **0**, diário com **10 coordenadas**. O receptor artificial fica deslocado três metros; não é um segundo jogador |
| Regressão de vida/comida | Nove cenários nos dois sentidos, incluindo dano, cura, consumo real de bife (**10 → 18**) e preservação ao voltar ao modo PEAK |

Os testes controlaram rotinas do próprio jogo através do DevHarness opcional. Os itens de teste vieram da instalação local do PEAK; o pacote não contém seus recursos. A primeira tentativa de construção mirava sob o próprio jogador e foi recusada normalmente pela colisão com ele. Mirar adiante permitiu a construção em Creative e Sobrevivência. O roteiro de regressão também recusou substituir um item em uma barra ocupada; o teste de consumo foi concluído com o bife existente no primeiro espaço.

## Vídeo da mão e mudanças de controle

O vídeo fornecido foi examinado. A mão é desenhada no quadro exportado pelo Minecraft; sua cor escura corresponde à skin local. Os movimentos centrais observados incluem a animação de soco. Não foi demonstrado um defeito específico de shader nessa gravação. Foi adicionada uma proteção concreta: o clique usado para retomar/focar o jogo só volta a ser encaminhado depois que os botões do mouse são soltos. Uma falha visual que ainda apareça em outra região/ângulo precisa de reprodução específica; não é anunciada como resolvida integralmente.

## O que mudou na colisão e no cooperativo

A ponte mantém a amostragem do terreno e agora também envia superfícies dos colliders reais: caixas, esferas, cápsulas e malhas, com leitura assíncrona da GPU quando necessária. Faces verticais são exportadas para todas as regiões de altura que atravessam. Objetos dinâmicos, formatos de malha não suportados e diferenças entre a malha original e o casco convexo calculado pelo PhysX continuam sendo limites. Não se garante equivalência de colisão para todo objeto do mapa.

O protocolo cooperativo v2 registra alterações por cena/sessão, inclui remoções, resolve conflitos de forma determinística e transfere construções a quem chega depois. Os jogadores remotos passam a ver o corpo PEAK durante B/V e cortes de ligação, evitando um avatar Minecraft com pose antiga. A versão v2 deve ser usada por todos os participantes. Detalhes e roteiro de duas contas estão em [MULTIPLAYER.md](MULTIPLAYER.md).

Cada Minecraft ainda executa sua própria simulação local. Inventários/baús NBT, entidades, redstone e evolução automática de fluidos não viram um servidor Minecraft compartilhado. Os blocos continuam sem colliders PEAK para o modo nativo. Vida/comida são sincronizadas entre os dois jogos **de cada jogador**, não entre todas as pessoas da sala.

## Provas e reprodução

- [Resultados dos 19 testes de rede](PROVAS/multiplayer-protocol-results.json)
- [11 verificações de execução](PROVAS/runtime-0.3.0-results.json)
- [Nove cenários da regressão de vida/comida](PROVAS/vitals-regression-0.3.0.json)
- [Trechos de log revisados, sem caminhos pessoais ou códigos de sala](PROVAS/runtime-0.3.0-log.txt)
- [Colisão de malha GPU](PROVAS/collision-gpu-final.png), [parede removida](PROVAS/collision-wall-removed.png), [consumo PEAK](PROVAS/native-eating.png), [retorno após escalada](PROVAS/native-climb-return.png) e [construção em Sobrevivência](PROVAS/multiplayer-v2-survival.png)
- [Checksums dos mods e provas](PROVAS/checksums.json)

Para repetir os testes puros: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Testar.ps1`, com SDK .NET 10. Para testar em jogo, siga [COMO-JOGAR.md](COMO-JOGAR.md). O lançador normal deixa o DevHarness e o loopback desligados.
