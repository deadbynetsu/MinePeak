# Procedência do PeakCraft Co-op

## Referência

Publicação: [PeakCraft Co-op](https://melty.gg/m/peakcraft-co-op), de **Issac**, remix de PeakCraft (aeironnsarmiento), baseado em SkyCraft (chasmlol).

- Versão do pacote: **0.2.1**.
- ID público do mod: `61f65117-912d-47c0-ab5d-952f4d6a69be`.
- ID público da release: `c1b27824-6c39-40db-bed8-f617bba57771`.
- Data pública da release: 16/10/2026.
- A ficha pública não liga um repositório GitHub do remix.

A referência usada nesta cópia é o pacote já disponível no **cache local do Melty** desta máquina. Não foi obtida removendo autenticação ou modificando DRM. O endpoint público de arquivos recusou o acesso sem sessão; essa recusa não foi contornada.

## Licença e avisos

O pacote inclui `LICENSE-PeakCraftCoop.txt` com a licença **MIT**, e `THIRD-PARTY-NOTICES.txt` identifica o código do mod como MIT. Esses documentos são preservados sem alteração nesta pasta, junto de `README.txt` da versão distribuída. A ficha pública tem um campo genérico de licença que diverge da descrição; a licença específica incluída no pacote é a referência para o código aqui reutilizado.

Créditos preservados: chasmlol/SkyCraft, aeironnsarmiento/PeakCraft e o remix PeakCraft Co-op de Issac. Componentes citados pelo pacote original, como Prism, Fabric API e Java, mantêm suas licenças próprias. A entrega local não inclui os jogos nem seus assemblies/texturas; não redistribui o runtime Prism/Java do cache.

## Reconstrução e alterações locais

Como não havia um repositório público do remix ligado à ficha, o C# de `coop/` foi **reconstruído da DLL MIT com ILSpy**. Não é a árvore de fontes original do autor. Os nomes e a organização refletem a reconstrução, com referências de compilação aos assemblies do PEAK instalado localmente. Esses assemblies proprietários não fazem parte da entrega.

`referencia-coop/fabric-reconstruido/` contém **74 arquivos Java reconstruídos com Vineflower 1.12.0** a partir do mod distribuído, para leitura e auditoria. Essa pasta não é um projeto compilável e não deve ser apresentada como as fontes originais do autor. A extensão Java local compilável está em `coop-fabric/`; o C# reconstruído e adaptado é compilável em `coop/`.

A revisão do JAR usado como dependência confirmou que ele contém o mod, sem incluir os assets dos jogos. O Minecraft e o PEAK continuam fornecendo seus próprios recursos a partir das instalações locais.

As alterações locais se concentram em:

- modo inicial Viewer/PEAK nativo;
- botão no menu de pausa e F8 para transferência reversível de controle, mantendo Minecraft aberto;
- devolução de controles/corpo ao PEAK ao desligar o modo Minecraft;
- extensão Fabric que lê o nome/UUID público do perfil SKLauncher e uma skin local, com fallback para a skin padrão;
- scripts de preparação, perfil externo BepInEx e abertura pela Steam.

O Co-op original selecionava Viewer por F8 e exigia reiniciar para retornar. Essa limitação foi alterada localmente; não deve ser atribuída à release original como se já tivesse o comportamento reversível.

## Limites da referência e da validação local

A publicação descreve compartilhamento de blocos/skins/poses em lobbies PEAK de até quatro jogadores, correção de quedas no vazio e testes solo/loopback. Ela também declara que **partida real entre dois jogadores, entrada tardia e compatibilidade com PEAK sem mod não foram verificadas**, e que o sinalizador do cume ainda não é suportado no modo Minecraft.

Não se deve chamar esta cópia de “100% funcional” apenas porque o plugin carrega ou um teste local funciona. O relatório `../VALIDACAO.md` é a autoridade para os testes de execução realmente feitos nesta máquina, distinguindo a versão clássica anterior e a Co-op atual.

O Co-op local já foi exercitado no aeroporto e na praia, com alternância de modos, skin do SKLauncher, blocos e um teste de loopback. Isso ainda não valida uma partida entre dois PCs, entrada tardia ou campanha completa. A versão clássica tem seu relatório histórico preservado em `../VALIDACAO-CLASSIC.md`.
