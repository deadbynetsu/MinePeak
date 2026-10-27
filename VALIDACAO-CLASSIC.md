# Validação real — PEAK x Minecraft — 26/10/2026

## O que foi executado

PEAK **3.0.1** da instalação Steam local e Minecraft Java **26.3** em dois processos reais no Windows. A versão Java foi iniciada pelo `runClient` do projeto Fabric, com nome local Steve, sem uma conta Melty ou edição das contas do SKLauncher. Não foi um servidor/datapack Minecraft, test double ou reprodução do mapa PEAK.

Base MIT: `aeironnsarmiento/PeakCraft` no commit `d07030ee527ee2a2facc989f5acae8a317968c02`, derivada de `chasmlol/SkyCraft`. Compilação .NET10 contra assemblies do PEAK local: zero erros/avisos. Build Java25: aprovado, incluindo os **21 testes** de `SkyRayTest` e `TriColliderTest`.

## Provas de execução

1. **Host PEAK:** plugin carregou, layout do protocolo e rings deram PASS, os cinco patches Harmony deram `patch ok`. Não foi necessário alterar C# para PEAK3.0.1.
2. **Ponte:** log PEAK confirmou `link up: Minecraft ...`, avatar recebido e estado `MinecraftOwns`; câmera e olho MC coincidiram. O aeroporto exportou colisões reais para Minecraft.
3. **Steve:** um nome offline sozinho escolhia Efe. O novo `LocalSteveMixin` limita a troca à aparência do jogador local quando a flag está ativa. O log confirmou a textura `minecraft:textures/entity/player/wide/steve.png`; F5 mostrou Steve no aeroporto e na ilha.
4. **Construção no aeroporto:** clique direito acertou a superfície PEAK; Minecraft publicou malha de **36 vértices / 1 seção**, vista no PEAK. Clique esquerdo no bloco produziu **0 vértices / 0 seções** e o bloco sumiu na imagem.
5. **Viagem PEAK:** interação com AirportGateKiosk abriu BoardingPass. O método normal de embarque iniciou a transição, carregou a ilha e devolveu controle à ponte após a queda na praia. O teste inicial do atalho dev `board`, antes de abrir o cartão, falhou por kiosk nulo; após a interação normal, funcionou. Nenhum patch de embarque foi introduzido.
6. **Construção na ilha:** clique direito no terreno nativo da praia emitiu `block mesh for section 0 -32 23: 36 vertices (1 sections...)`. Clique esquerdo no bloco emitido levou a `0 vertices (0 sections...)`. As duas imagens foram inspecionadas.
7. **Encerramento:** o comando de teste `quit` fechou PEAK e a flag de produção fez Minecraft encerrar; Gradle terminou com BUILD SUCCESSFUL. O atalho normal desativa o DevHarness.
8. **Atalho sem modo de teste:** iniciado novamente com `Harness=false`. O menu PEAK apareceu normalmente; clique em **Jogar offline** pela interface carregou o aeroporto, e a ponte chegou novamente a `MinecraftOwns`. Assim, o início da sessão não depende da automação de desenvolvimento.

### Capturas do próprio PEAK

- `PROVAS/steve-aeroporto.png`: Steve no aeroporto real, segurando bloco.
- `PROVAS/steve-ilha.png`: Steve na praia, malas do PEAK, destroços e montanha ao fundo.
- `PROVAS/ilha-bloco-colocado.png`: bloco Minecraft sobre o chão PEAK.
- `PROVAS/ilha-bloco-quebrado.png`: mesmo enquadramento após destruir o bloco, com partículas.

Não foram geradas ou montadas imagens para simular esses resultados. As capturas vêm de `ScreenCapture.CaptureScreenshot` dentro do processo PEAK.

## Alterações locais

- Atalhos e scripts PowerShell: preparação, perfil externo BepInEx e abertura pela Steam.
- Mixin cosmético Steve opcional, sem recursos gráficos proprietários embutidos.
- Fechar PEAK passa a encerrar o Minecraft associado.
- Binários dos mods em `binarios/`; fontes e licenças preservadas.

Arquivos de loader adicionados na pasta do jogo: `winhttp.dll` e `doorstop_config.ini`, com `enabled=false` para aberturas comuns. As flags do atalho ativam o perfil externo. Nenhuma substituição de PEAK.exe, assemblies PEAK, Steamworks ou componentes de autenticação. A Steam completou seu próprio início de sessão; não houve preenchimento de credenciais pelo agente. O lançamento direto de diagnóstico respeitou `RestartAppIfNecessary` do próprio jogo e foi encerrado/reiniciado pela Steam.

## O que ainda não está validado ou implementado

- Não percorri a campanha inteira nem validei o final do cume. O upstream informa que o final por sinalizador está incompleto.
- É solo: não há sincronização PEAK x Minecraft para outros jogadores.
- Não é possível escavar a montanha nativa; quebrar os blocos Minecraft colocados funciona.
- Escalada/stamina e itens nativos do PEAK têm as limitações do upstream quando Minecraft controla o corpo.
- Colisão é amostrada, não uma cópia exata de toda a geometria; objetos finos podem falhar.
- O lançamento testado usa a configuração de desenvolvimento Fabric. A interface do SKLauncher não foi automatizada; suas contas e perfis permaneceram intactos.

## Referências

[Fonte PeakCraft](https://github.com/aeironnsarmiento/PeakCraft), [README da integração](https://github.com/aeironnsarmiento/PeakCraft/blob/main/peak/README.md), [origem SkyCraft](https://github.com/chasmlol/SkyCraft). O código original é MIT, com créditos preservados. Não distribuí executáveis, texturas ou assemblies dos jogos no pacote de mods.
