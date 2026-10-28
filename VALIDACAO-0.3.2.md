# MinePeak 0.3.2 — colisão e escalada das construções

Verificação local de **28/10/2026**, no Windows 11, com **PEAK 3.0.1 e Minecraft Java 26.3 reais** no mesmo PC. As construções Minecraft sustentaram o personagem e permitiram escalada nos dois modos nos cenários descritos abaixo. A distribuição continua **experimental**: esta checagem não cobre toda a campanha nem uma partida entre dois computadores.

## O que mudou

A extensão Java do perfil **0.1.1** envia as formas de colisão dos blocos ao PEAK, separadas das malhas usadas para desenhá-los. O plugin cria superfícies físicas na camada `Terrain`, consultada pela escalada nativa. A colisão permanece ativa ao alternar entre Minecraft e PEAK; remover ou alterar blocos atualiza essas superfícies.

O exportador utiliza as formas do Minecraft, incluindo volumes parciais, em vez de transformar todos os objetos em cubos inteiros. As construções geradas são excluídas da exportação da montanha PEAK de volta ao Minecraft, evitando que uma construção quebrada reapareça como apoio invisível. Mensagens antigas de outra cena ou geração de colisão são rejeitadas.

A alteração Java é uma extensão do perfil existente. O JAR base Co-op foi preservado; não houve substituição de suas classes por classes da base clássica.

As seções com blocos recém-renderizados têm prioridade na fila de colisão. Sondagens de seções vazias usam somente o limite de tempo, sem ocupar a cota de quatro atualizações físicas por quadro. Em uma abertura nova com os binários finais, a plataforma foi exportada no mesmo segundo em que sua malha apareceu; os dois caminhos de escalada e a remoção foram repetidos com sucesso. Os hashes desses binários e os valores dessa repetição estão em `distribution_verification` no resumo das provas.

## Testes dentro dos jogos

Foi construída uma plataforma de pedra com topo em **Y = 6,000** e uma parede sobre ela, dentro do aeroporto real. As alturas desta tabela usam as coordenadas Unity do PEAK. A laje inferior colocada sobre a plataforma tem topo em **Y = 6,500**.

| Verificação | Resultado registrado |
| --- | --- |
| Carregamento da extensão | Minecraft carregou o perfil 0.1.1 e seus Mixins; registrou o envio de seções de colisão, e PEAK criou seus colisores. |
| Apoio nos dois modos | Minecraft ficou sobre a plataforma em Y = 6,000. Após retornar ao PEAK e cair de Y = 8, o corpo nativo pousou em aproximadamente Y = 6,029; o raio de apoio encontrou o topo Minecraft em Y = 6,000. |
| Laje de meio bloco | Minecraft ficou em Y = 6,500. No modo PEAK, o corpo ficou em aproximadamente Y = 6,529 e o raio encontrou o topo da laje em Y = 6,500. A forma não virou um cubo inteiro. |
| Escalada em PEAK nativo | Em `PeakOwns`, a ação principal normal agarrou `MinePeak block collider`. Os pés subiram de Y = 6,029 para Y = 9,120, cerca de **3,1 metros**; a stamina caiu de 1,000 para 0,767. |
| Escalada com a opção Minecraft ativa | O caminho de V transferiu temporariamente o controle para `NativeControl` e agarrou a mesma construção. Os pés subiram de Y = 6,000 para Y = 8,314, cerca de **2,3 metros**; a stamina caiu de 1,000 para 0,786. O controle voltou ao Minecraft ao encerrar. |
| Quebra normal de um bloco | O botão esquerdo, com picareta Minecraft, removeu um bloco da parede. A ponte registrou o estado de ar naquela posição; o total de volumes físicos caiu de **86 para 85**. |
| Remoção da plataforma e da parede | A remoção dos 74 blocos de teste restantes devolveu o total aos **11 volumes originais** do mundo. O corpo PEAK caiu até o piso do aeroporto, em Y = 0,555, e deixou de encontrar apoio Minecraft. |
| Ausência de colisão fantasma após atualização | Após novas atualizações de região, PEAK caminhou além do plano da parede removida, de Z ≈ 71,5 até Z = 74,687. Depois de uma nova geração de colisão, Minecraft também atravessou o antigo plano Z = 73 e chegou a Z = 77,635. O total continuou em 11 volumes e o apoio registrado era do aeroporto. |

Esses resultados exercitam a física real dos dois jogos. O `DevHarness`, ativado expressamente para o teste, enviou comandos de chat vanilla para construir e posicionar o personagem, entradas de movimento/ação e pedidos de captura. A escalada PEAK nativa recebeu entradas amostradas normais de movimento e ação principal; não recebeu atribuição artificial de stamina nem uma escalada forçada por código. O caminho de V foi exercitado pelo comando de diagnóstico que solicita a mesma transferência temporária de controle usada pela função.

Os blocos do teste existiam no Minecraft e foram exportados para o PEAK. Não foram usados objetos ou paredes artificiais criados diretamente no Unity para comprovar esta função. Foi feito backup local do mundo antes dos testes. As capturas e os registros identificam os cenários exercitados; eles não demonstram todos os tipos de bloco ou todas as regiões do mapa.

A região de teste foi restaurada e o personagem retornou ao ponto inicial. Os jogos encerraram normalmente, com salvamento do Minecraft. A configuração local voltou a Viewer/PEAK, tecla L, `Harness=false` e `CoopLoopback=false`.

## Compilação e regressão automatizada

| Verificação | Resultado |
| --- | --- |
| Plugin C# Co-op | Compilou com zero erros e quatro avisos já conhecidos da reconstrução. |
| Extensão Java 0.1.1 | Compilou com JDK 25 e carregou no Minecraft real, com aplicação dos Mixins e envio das formas de colisão. |
| Fome/vida | **12/12** casos passaram. |
| Colisão | **18/18** casos passaram: seis de regiões e 12 de mensagens/formas de blocos. |
| Cooperativo | **19/19** casos passaram com transporte simulado. |
| Total automatizado | **49/49** casos passaram. |

Os novos casos verificam decodificação de volumes, coordenadas negativas, laje de meio bloco, cerca que ultrapassa a seção, mensagem vazia de remoção, rejeição de cena/geração antiga, truncamento, limites de quantidade e coordenadas inválidas. Esses testes puros não instanciam um `MeshCollider` Unity nem executam a escalada PEAK; a evidência dessas duas funções está na execução real descrita acima.

Para repetir a regressão sem abrir os jogos, execute `Testar.ps1` com SDK .NET 10. Para repetir o teste funcional, crie uma plataforma e uma parede no Minecraft, confirme apoio e escalada nos dois modos e remova-as; verifique novamente depois da atualização da região.

## Provas

- [Resumo dos resultados](PROVAS/block-collision-0.3.2-results.json)
- [Registros PEAK](PROVAS/block-collision-0.3.2-peak.log)
- [Registros Minecraft](PROVAS/block-collision-0.3.2-minecraft.log)
- [Regressão automatizada](PROVAS/block-collision-0.3.2-tests.log)
- [Escalada no modo PEAK](PROVAS/collision-032-native-climbing.png)
- [Escalada com a opção Minecraft ativa](PROVAS/collision-032-minecraft-climbing.png)
- [Apoio sobre a laje](PROVAS/collision-032-slab.png)
- [Apoio sobre a plataforma](PROVAS/collision-032-platform.png)
- [Construção removida e retorno ao piso original](PROVAS/collision-032-removed.png)
- [Checksums dos mods e das provas](PROVAS/checksums.json)

## Limites que continuam pendentes

- A campanha completa até o cume e uma sessão entre duas contas Steam/dois PCs **não foram validadas**. Os 19 casos simulados de cooperativo não equivalem a uma partida pela internet.
- No modo Minecraft, V continua usando temporariamente o corpo e a escalada nativos do PEAK. A stamina, o alcance e as restrições de agarrar continuam pertencendo ao PEAK.
- A ponte Java precisa estar ativa. Se uma seção deixar de estar carregada no cliente oculto, o PEAK conserva sua última forma completa conhecida; isso não comprova atualização de edições remotas fora dos chunks carregados. Esse cenário multiplayer ainda precisa de teste real.
- A laje foi exercitada dentro do jogo; o formato de cerca foi verificado nos testes de mensagem. Isso não certifica toda forma, bloco dinâmico ou condição especial de colisão do Minecraft.
- A montanha original do PEAK continua não escavável. As limitações de inventários/NBT, entidades, redstone e simulação compartilhada permanecem descritas em [MULTIPLAYER.md](MULTIPLAYER.md).

A tecla de alternância continua **L**; a sua checagem está em [VALIDACAO-0.3.1.md](VALIDACAO-0.3.1.md). Os testes anteriores de equipamentos, alimentação e arremesso estão em [VALIDACAO-0.3.0.md](VALIDACAO-0.3.0.md).
