# Código de 2022 — CONGELADO

Este diretório contém o código original do TCC, exatamente como foi entregue em 2022.

## Não edite nada aqui

Este código é **somente leitura**. Ele existe por três motivos:

1. **Registro arquivístico** da versão defendida no TCC.
2. **Fonte da migração** — `web-2022/js/dados*.js` é o único dataset sobrevivente, e
   `web-2022/index.html` contém a única cópia das geometrias das 33 vias.
3. **Documentação de defeitos** — vários arquivos aqui são a única evidência de problemas que a
   v2 corrige (por exemplo, `etl-2022/Class1.cs` é a única documentação do formato de texto do
   clima anterior a 29/04/2022).

Correções, melhorias e refatorações vão para a v2, na raiz do repositório. Se algo aqui precisar
mudar (o que deve ser raro), a mensagem de commit deve conter o marcador `[legacy]`.

## Conteúdo

| Diretório | Projeto original | Stack |
|---|---|---|
| `web-2022/` | `TCC-II-MapBox` | HTML + jQuery + Mapbox GL JS 2.7.0 |
| `collector-2022/` | `aspnet-webapi-master` | C# / ASP.NET Web API 5.0, .NET Framework 4.5.1 |
| `etl-2022/` | `ConversorDeTxtParaSQL` | C# / .NET Core 3.1 |

### Procedência

`web-2022/` veio do histórico deste repositório (movido com `git mv`, histórico preservado).

`collector-2022/` e `etl-2022/` foram importados do disco local em 24/07/2026. **Nunca estiveram
sob controle de versão** — não há histórico a preservar. Foram excluídos da importação os
diretórios `bin/`, `obj/`, `.vs/` e `packages/` (artefatos de build e restore do NuGet).

## Este código roda?

**Não.** As chaves de API foram removidas, o endpoint OpenWeather One Call 2.5 foi depreciado em
jun/2024, e os dados intermediários (arquivos `.txt` de coleta, banco SQL Server, `TCC.bak`,
`dadoscsv2.csv`) foram perdidos.

A visualização publicada no GitHub Pages continua funcionando porque os dados estão embutidos nos
arquivos `.js`.

Detalhes em `D:\dev\TCC\documentacao\mapbox\instalacao-e-uso.md`.
