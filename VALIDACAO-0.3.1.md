# MinePeak 0.3.1 — tecla L e checagem final

Verificação de **27/10/2026**, no Windows 11, com PEAK 3.0.1 e Minecraft Java 26.3 reais. Esta revisão altera o atalho de alternância de modos para **L**. Os demais controles permanecem B para equipamentos, Q para arremessar e V para escalar.

## O que foi conferido

| Verificação | Resultado |
| --- | --- |
| Configuração antiga com F8 | A inicialização migrou `ToggleModeKey = F8` para `L` e salvou a revisão de migração 1 |
| Novo padrão | Fontes e instalação nova usam L; o menu e o atalho local foram atualizados |
| F8 na partida | O personagem continuou em PEAK nativo (`PeakOwns`) |
| L na partida | Ativou Minecraft (`MinecraftOwns`), com transferência confirmada e `flags 0x5` (jogo sem tela Minecraft aberta) |
| L no chat aberto | Digitou a letra e manteve o modo Minecraft e o chat aberto (`flags 0x7`) |
| L para retornar ao PEAK | Restaurou `PeakOwns`, corpo sem cinemática e controles nativos |
| Processo Java | O mesmo processo permaneceu ligado durante a alternância |
| Painel de pausa | Mostrou **L alterna sem reiniciar** |
| Build C# | Zero erros; quatro avisos já conhecidos da reconstrução |
| Regressão automatizada | **37/37 passaram**: 12 vida/comida, seis colisão e 19 cooperativo |

Os acionamentos de L/F8 foram enviados ao teclado da janela real do PEAK pelo controle do computador, não pelo comando de desenvolvimento que força um modo. O DevHarness foi usado para registrar estados e capturas. Na instalação normal, ele e o loopback ficaram desligados ao encerrar a checagem.

L também é o atalho padrão de conquistas do Minecraft. A ponte reserva a tecla para alternar os modos durante a partida, evitando encaminhá-la simultaneamente à tela de conquistas. Quando o chat ou inventário Minecraft está aberto, a tecla continua disponível nessa interface e não alterna o modo.

A migração de F8 ocorre uma vez. Outras teclas personalizadas são preservadas. Os arquivos locais de configuração e conta não são publicados.

## Provas

- [Estados registrados](PROVAS/bind-0.3.1-log.txt)
- [Modo Minecraft ativado por L](PROVAS/bind-L-minecraft.png)
- [L digitado no chat](PROVAS/bind-L-chat.png)
- [Retorno ao PEAK por L](PROVAS/bind-L-native.png)
- [Painel de pausa mostrando L](PROVAS/bind-L-menu.png)
- [Checksums dos mods e provas](PROVAS/checksums.json)

SHA-256 da DLL testada e distribuída: `c585de49ccf302e2a00efdd9a92992ee8c20d66618dff5350e63f7b74c251b96`.

Os resultados funcionais de itens, escalada, construção e rede da revisão anterior estão em [VALIDACAO-0.3.0.md](VALIDACAO-0.3.0.md). Esta checagem não repete uma campanha completa e não valida uma sessão entre dois PCs/contas Steam. Esses limites continuam pendentes.

Para repetir a regressão automatizada, execute `Testar.ps1` com SDK .NET 10. Para jogar nesta máquina, abra **PEAK com Minecraft** e use **L** dentro da partida, com o chat/inventário fechados.
