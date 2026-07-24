# TCC-II-MapBox — Visualização de Tráfego de Blumenau

Ferramenta de visualização espaço-temporal do congestionamento urbano de **Blumenau/SC**,
desenvolvida para o Trabalho de Conclusão de Curso de Bacharelado em Ciência da Computação (FURB).

**Aplicação (v1):** https://celrodriguesjunior.github.io/TCC-II-MapBox/

## O que é

Coleta o fluxo de trânsito de 33 vias em intervalos regulares, cruza com as condições climáticas
do momento e pinta as vias no mapa conforme o nível de congestionamento — permitindo filtrar por
dia da semana, clima, turno, período e hora.

A métrica é a razão entre a velocidade atual e a velocidade de fluxo livre da via: quanto menor,
mais congestionada.

O diferencial em relação a um mapa de trânsito comum é o cruzamento **tráfego × meteorologia**: a
pergunta de pesquisa é como o congestionamento varia conforme horário, dia da semana e condições
do tempo.

## Dataset

| | |
|---|---|
| Vias monitoradas | 33 (Centro, hospitais Santo Antônio e Santa Catarina, FURB, Vila Nova) |
| Registros | 219.499 |
| Período | 19/04/2022 a 12/06/2022 |
| Grade temporal | 10 minutos |
| Fontes | TomTom Traffic API (fluxo) + OpenWeatherMap (clima) |

## Estado do projeto

Este repositório está em **reestruturação (v2)**.

- **v1** — versão original defendida no TCC (2022), preservada pela tag `v1.0-tcc-original` e
  ainda publicada no GitHub Pages. O código está congelado em [`legacy/`](legacy/).
- **v2** — em desenvolvimento: Vite + TypeScript + MapLibre GL, dados em Apache Parquet
  consultados com DuckDB-WASM, e um coletor novo em .NET 8.

### Por que reestruturar

A v1 foi entregue sob pressão de prazo. Entre os problemas estruturais: um `index.html` de 1499
linhas com uma única função de 960 linhas, 32 MB de JavaScript carregado de forma bloqueante,
token de API commitado, camadas de cor com faixas sobrepostas, e um coletor sem nenhum tratamento
de erro — o que torna a completude do dataset original impossível de verificar.

## Estrutura

```
legacy/              código de 2022, congelado e somente leitura
  web-2022/          a visualização original + os dados coletados
  collector-2022/    coletor ASP.NET Web API (.NET Framework 4.5.1)
  etl-2022/          conversor txt → SQL → js (.NET Core 3.1)
```

A estrutura da v2 é adicionada progressivamente. Ver o plano de reestruturação na documentação.

## Documentação

A documentação completa — análise técnica da v1, inventário do dataset, instruções de execução e
o plano de reestruturação — é mantida fora deste repositório, em
`D:\dev\TCC\documentacao\mapbox`.

## Licença

Trabalho acadêmico. Os dados de tráfego são derivados da TomTom Traffic API e os dados
meteorológicos da OpenWeatherMap, sujeitos aos termos de uso dos respectivos provedores.
