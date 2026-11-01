# Como jogar MinePeak

MinePeak usa a base PeakCraft Local. Alguns nomes de pastas e arquivos de configuração continuam como PeakCraft para preservar a instalação; o painel dentro do jogo se chama **MinePeak**.

## Abrir a partida

1. Feche PEAK caso esteja aberto e mantenha a Steam conectada à sua conta.
2. Execute **JOGAR.cmd**. O lançador abre PEAK e o Minecraft em segundo plano. Não precisa abrir o SKLauncher nem conectar a um servidor Minecraft.
3. Para jogar sozinho, escolha **Jogar offline** no PEAK. Para uma sala com amigos, use os lobbies do próprio PEAK com a mesma versão dos mods em cada PC; veja [MULTIPLAYER.md](MULTIPLAYER.md).
4. Na partida, pressione **L** ou abra **Esc → MinePeak** e selecione **Minecraft**. Aguarde o inventário e os corações aparecerem.

O modo escolhido fica salvo. Use **L** novamente ou selecione **PEAK** no painel do menu Esc para voltar aos controles nativos. Feche chat e menus Minecraft antes de usar o atalho. Se esta for a primeira instalação, siga antes a preparação em [README.md](README.md#comece-a-jogar).

Na versão **0.3.1**, configurações antigas com F8 são migradas automaticamente para **L**. Se você já escolheu outra tecla personalizada, ela é preservada.

## A nova interface

A versão **0.4.2** mantém um pequeno indicador de modo, vida e comida. Efeitos nativos ativos, como veneno, aparecem abaixo no modo Minecraft. Dicas extras acompanham **B** para itens e **V** para escalar. O indicador é ocultado quando chat, inventário, pausa ou carregamento estão abertos, assim como na morte e no desmaio.

Abra **Esc** para ver a lista dos controles no cartão **MinePeak** e escolher **PEAK / Minecraft**. Para ajustar sensibilidade ou controles Minecraft, selecione **Opções do Minecraft** nesse cartão, com o modo Minecraft ativo e conectado. **O** continua abrindo o menu Minecraft durante a partida.

## Seu primeiro bloco

No modo Minecraft, use WASD para andar, Espaço para pular, Ctrl para correr e Shift para agachar. E abre o inventário; F5 alterna a câmera.

O modo inicial de sobrevivência permite fome, dano e cura. Para receber um material de teste, abra o chat com T e envie:

```text
/give @s minecraft:stone 64
```

Selecione o bloco na barra inferior e mire em uma superfície próxima. **Botão direito coloca; esquerdo quebra o bloco colocado.** A rocha e os objetos originais do PEAK não são escaváveis. Para um teste de construção livre, use `/gamemode creative`; para voltar à sobrevivência, `/gamemode survival`.

A partir da **0.3.2**, as construções enviam também suas formas físicas ao PEAK. Você pode usá-las como apoio nos dois modos. Para escalar uma parede de blocos, use a escalada normal do PEAK ou, com a opção Minecraft ativa, **segure V** e use WASD. A ponte precisa estar conectada ao Minecraft; aguarde as seções carregarem. Veja o [estado da validação](VALIDACAO-0.3.2.md).

## Interagir, comer e arremessar

**G** interage com o mundo do PEAK no modo Minecraft. Segure quando a ação exigir tempo. No aeroporto, mire no terminal, segure G e confirme a viagem no cartão de embarque.

Para abrir uma mochila no chão, **mire nela e segure G**. Mova o mouse para a opção do menu radial e **solte G para confirmar**. Continue segurando a tecla enquanto escolhe.

Para usar comida ou equipamento PEAK mantendo a opção Minecraft ativa:

1. **Segure B** para assumir temporariamente os controles nativos.
2. Escolha o espaço PEAK com **1, 2, 3 ou 4**.
3. Use o **botão do mouse** correspondente à ação do item; mantenha pressionado se ele exigir tempo para consumir.
4. Para arremessar o item equipado, **segure Q para carregar a força e solte Q**.
5. Solte B para retornar ao inventário e à movimentação Minecraft. Se um impulso nativo estiver em andamento, o PEAK mantém a física até o pouso se firmar.

Comidas Minecraft usam o fluxo normal do Minecraft: selecione o alimento e segure o botão direito. Comida e vida são compartilhadas; o outro jogo deve acompanhar a alteração. Comer não cura instantaneamente quando o alimento ou o jogo não têm esse efeito. Os ferimentos do PEAK correspondem aos corações; a stamina de escalada permanece separada. Veneno, frio e outros efeitos nativos também permanecem ativos: acompanhe o resumo no painel, mesmo com os corações cheios.

Durante o controle temporário B, o corpo e os braços são os do PEAK. Sua aparência Minecraft retorna ao encerrar a ação. Cada item mantém suas regras nativas de uso, carga e consumo. O indicador **Impulso PEAK** sinaliza quando a física nativa ainda está concluindo um movimento e seu pouso.

## Escalar

Aproxime-se de uma parede que o PEAK permita escalar, olhe para ela e **segure V**. Use WASD para movimentar-se e acompanhe a stamina nativa. **Mantenha V ao alcançar a borda**: o PEAK termina a subida e firma os pés antes do retorno automático ao Minecraft. Espaço solta a parede; soltar V cancela a assistência e devolve o controle.

Esse caminho usa a escalada do próprio PEAK, com suas restrições de alcance, parede e energia. Durante a escalada, aparece o corpo nativo. Ao terminar ou esgotar a energia, será necessário soltar V antes de tentar agarrar novamente.

## Sua skin

O lançador tenta ler o nome e uma skin já armazenada no perfil SKLauncher. **F5** mostra o personagem; armadura pode esconder parte da skin. O formato suportado pela extensão é 64×64. Sem uma imagem compatível, o jogo utiliza a aparência padrão.

Para definir uma imagem própria, veja a seção [Skin do usuário](README-PT.md#skin-do-usuário). Nome e skin entram na próxima abertura dos jogos.

## Quando o personagem morre ou desmaia

A morte, a reanimação e a câmera seguem o PEAK. Durante essa sequência, os corações, a barra de itens, a mão e o avatar parado do Minecraft desaparecem. Se o modo Minecraft estava selecionado, o corpo local do PEAK e seus derivados de morte ou petrificação ficam ocultos na sua visualização enquanto o personagem estiver incapacitado. Essa ocultação não altera sua física nem a possibilidade de um companheiro reanimá-lo; também não acrescenta um cadáver Minecraft animado nem oculta os corpos dos outros jogadores.

Os blocos construídos permanecem no mapa. Ao recuperar o controle, aguarde o retorno do HUD e da aparência Minecraft. Os objetos de cadáver continuam sendo do PEAK: um esqueleto deixado no cenário pode voltar a ser visível após a recuperação. No modo PEAK nativo, a apresentação original continua valendo; o mesmo acontece se a ponte Java desconectar. O corpo nativo durante as ações **B** e **V** continua normal.

## Se algo não funcionar

| O que aconteceu | O que verificar |
| --- | --- |
| PEAK abriu sem painel MinePeak no menu Esc | Abra pelo JOGAR.cmd, que ativa o perfil externo; a abertura normal pela Steam é separada. |
| Minecraft ainda não aparece / painel mostra Preparando | Espere a primeira instalação concluir. Confira `local-profile/minecraft-console.log`. |
| Opções do Minecraft está desativado | Selecione Minecraft e aguarde a ponte conectar; o botão precisa do jogo Java pronto. |
| Botão/tecla não alterna o modo | Feche o inventário, chat ou menu Minecraft e tente L dentro da partida; confira `ToggleModeKey` se usa uma tecla personalizada. |
| Mão parece escura | A cor vem da skin local; confira sua imagem antes de alterar o renderizador. |
| Bloco some atrás de uma pedra | A ocultação pelo cenário é esperada. A montanha continua sendo o cenário PEAK. |
| PEAK nativo atravessa o bloco construído | Confira se os mods vieram da mesma Release 0.4.2, se o Minecraft oculto está conectado e se a seção já carregou. A extensão Java permanece na versão 0.1.3. Reinicie pelo JOGAR.cmd após atualizar os arquivos. |
| Item PEAK não usa no controle Minecraft | Segure B, selecione 1–4 e use o mouse. |
| Escalada não agarra | Aproxime-se, mire numa parede válida e confira a stamina. Solte V antes de uma nova tentativa. |

Os jogos e mods podem mudar. Para relatar um erro, inclua as versões, a sequência de ações e um vídeo curto quando possível. Remova nomes, códigos de sala e caminhos pessoais dos logs públicos. Consulte **[VALIDACAO-0.4.2.md](VALIDACAO-0.4.2.md)** para a revisão atual; os relatórios **[0.4.1](VALIDACAO-0.4.1.md)**, **[0.4.0](VALIDACAO-0.4.0.md)**, **[0.3.2](VALIDACAO-0.3.2.md)**, **[0.3.1](VALIDACAO-0.3.1.md)** e **[0.3.0](VALIDACAO-0.3.0.md)** preservam suas provas históricas. O teste de uma campanha inteira até o cume e o encerramento, assim como uma sessão completa com dois PCs/contas, ainda está pendente. O mundo e o conteúdo original continuam sendo executados pelo PEAK.

### Informações PEAK no modo Minecraft (0.4.3)

Os anúncios de região, dia e horário aparecem sem cobrir o cenário. A faixa PEAK no canto inferior direito mostra os espaços 1, 2, 3 e 4 (mochila), apenas para consulta. Ela não recebe cliques nem altera teclas: os números continuam na barra Minecraft, e B continua habilitando o uso de itens PEAK.
