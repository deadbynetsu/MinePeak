# Fontes Java reconstruídas do PeakCraft Co-op

Esta pasta é uma **referência reconstruída por decompilação**, não a árvore de fontes original do autor e não um projeto Gradle pronto para compilar. O código foi reconstruído do binário MIT `binarios/peakcraftcoop-fabric.jar` distribuído com o PeakCraft Co-op 0.2.1, cuja procedência está em `../PROVENIENCIA.md`.

O runtime continua usando o JAR do mod. Nenhum JAR, script de lançamento, addon Fabric ou arquivo do jogo foi alterado nesta etapa. Estas fontes servem para leitura e auditoria das rotinas da ponte, modo Co-op, sincronização, renderização e correção de queda.

## Ferramenta e reprodução

- Ferramenta: **Vineflower 1.12.0**, obtida no [release oficial](https://github.com/Vineflower/vineflower/releases/tag/1.12.0).
- Artefato: `https://github.com/Vineflower/vineflower/releases/download/1.12.0/vineflower-1.12.0.jar`.
- SHA-256 da ferramenta: `1dfcfe974395734fa467ce620661c7623d05ba83670de0529b1fbd63ff548b9d`.
- SHA-256 do JAR de entrada: `704fdcd98c2111fcaf09a81688d29c421c1a54b8db32833d1cb09853fe13a4ea`.
- JVM de execução: Azul OpenJDK 25.0.5, Windows.
- Data da reconstrução: 26/10/2026.

Comando equivalente, a partir do workspace de trabalho:

```powershell
java -Xmx2G -jar work/java-decompile/vineflower-1.12.0.jar --folder --skip-extra-files --thread-count=2 --add-external=work/gradle-peakcraft/caches/fabric-loom/26.3/minecraft-client.jar outputs/PeakCraft-Local/binarios/peakcraftcoop-fabric.jar outputs/PeakCraft-Local/referencia-coop/fabric-reconstruido
```

O Minecraft client JAR foi fornecido somente como **referência externa de tipos** ao decompilador. Seus arquivos de classes, assets e recursos não foram copiados para esta pasta. A opção `--skip-extra-files` impediu a cópia de recursos não Java do JAR de entrada.

## Resultado e limites

- Entrada: **105 arquivos `.class`** do mod.
- Saída: **74 arquivos `.java`**; classes internas/anônimas foram incorporadas aos arquivos das classes externas.
- Todas as classes externas de entrada têm arquivo Java correspondente.
- O processo de reconstrução terminou sem mensagens `WARN` ou `ERROR` do decompilador.
- Não foi realizada compilação destas fontes reconstruídas; elas podem precisar de ajustes de tipos, nomes de variáveis, anotações e configuração de build para serem recompiladas.
- Nomes de variáveis, organização e comentários originais podem estar ausentes ou reconstruídos. Não há garantia de equivalência de recompilação bit a bit.
- Não foram copiados assets de Minecraft, Prism, runtime Java, contas, tokens ou conteúdo de usuário.

`MANIFESTO.json` contém contagens, hashes e lista dos arquivos Java produzidos. A licença MIT do mod foi preservada em `LICENSE.txt`; créditos e avisos completos continuam em `../LICENSE-PeakCraftCoop.txt` e `../THIRD-PARTY-NOTICES.txt`.
