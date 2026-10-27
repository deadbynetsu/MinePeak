# Contribuir com MinePeak

Correções, testes reproduzíveis e documentação em português são bem-vindos. MinePeak usa a base PeakCraft Local. Antes de mudar uma área, identifique se ela pertence ao Co-op atual (`coop/`, `coop-fabric/`) ou à base clássica preservada (`peak/`, `fabric/`). As instruções de compilação ficam em [README-PT.md](README-PT.md#fontes-e-diagnóstico).

## Relatar um problema

Inclua versão do Windows, PEAK e mods; modo ativo; passos desde a abertura; comportamento esperado e observado; e um trecho do log da mesma execução. Um vídeo curto ajuda em colisão, câmera e animação.

Remova credenciais, códigos de sala, endereços de rede, caminhos pessoais e qualquer informação de outras pessoas antes de publicar. Não envie a pasta de contas do launcher, o perfil local inteiro ou seus mundos sem revisão.

## Alterar o código

Mantenha as licenças e créditos upstream. O C# Co-op foi reconstruído de um pacote MIT: não o apresente como a árvore original do autor. Os arquivos Java reconstruídos em `referencia-coop/fabric-reconstruido/` são referência de auditoria, não um projeto compilável.

Descreva o problema concreto e o comportamento depois da alteração. Execute os testes pertinentes; registre versão, cenário e resultado. Diferencie teste puro, loopback e sessão real com dois computadores. Não chame um teste local de certificação de campanha ou multiplayer.

Não inclua executáveis, assemblies, assets ou caches proprietários dos jogos. `coop/Config.Local.props`, `local-config.json`, `local-profile/`, `runtime/`, mundos, senhas e tokens devem permanecer locais. Os assemblies PEAK usados na compilação vêm da instalação de quem compila.

## Licença

Contribuições ao código deste projeto seguem a licença [MIT](LICENSE), respeitando as licenças específicas dos componentes e os avisos preservados em [referencia-coop/](referencia-coop/).
