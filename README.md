# TCC-II-MapBox — Visualização de Tráfego

Ferramenta de visualização espaço-temporal do congestionamento urbano, desenvolvida para o Trabalho
de Conclusão de Curso de Bacharelado em Ciência da Computação (FURB) e em reestruturação desde
julho de 2026.

**Aplicação (v1):** https://celrodriguesjunior.github.io/TCC-II-MapBox/

> O nome do repositório é histórico: a v2 não usa mais o Mapbox e não monitora mais Blumenau. Não
> renomear — a URL acima é citação no PDF do TCC.

## O que é

Coleta o fluxo de trânsito de um conjunto de segmentos viários em intervalos regulares, cruza com
as condições climáticas do momento e pinta as vias no mapa conforme o nível de congestionamento —
permitindo filtrar por dia da semana, clima, turno, período e hora.

A métrica é a razão entre a velocidade atual e a velocidade de fluxo livre da via: quanto menor,
mais congestionada.

O diferencial em relação a um mapa de trânsito comum é o cruzamento **tráfego × meteorologia**: a
pergunta de pesquisa é como o congestionamento varia conforme horário, dia da semana e condições
do tempo.

## Dataset

| | v1 (2022) | v2 |
|---|---|---|
| Cidade | Blumenau/SC | Brusque/SC — bairro Rio Branco |
| Cobertura | 33 vias | 42 segmentos em 9 ruas |
| Registros | 219.499 | coleta não iniciada |
| Período | 19/04/2022 a 12/06/2022 | — |
| Grade temporal | 10 minutos | 10 / 30 / 90 min por camada |
| Fontes | TomTom Traffic API + OpenWeatherMap | TomTom Traffic API + Open-Meteo |

## Estado do projeto

Este repositório está em **reestruturação (v2)**.

- **v1** — versão original defendida no TCC (2022), preservada pela tag `v1.0-tcc-original` e
  ainda publicada no GitHub Pages. O código está congelado em [`legacy/`](legacy/).
- **v2** — alvo em **Brusque/SC, bairro Rio Branco**. Coletor em .NET 10 construído e validado;
  frontend em Vite + TypeScript + MapLibre GL com dados em Parquet consultados via DuckDB-WASM
  ainda pendente.

O dataset de Blumenau **não será migrado** — fica como registro histórico.

### Por que reestruturar

A v1 foi entregue sob pressão de prazo. Entre os problemas estruturais: um `index.html` de 1499
linhas com uma única função de 960 linhas, 32 MB de JavaScript carregado de forma bloqueante,
token de API commitado, camadas de cor com faixas sobrepostas, e um coletor sem nenhum tratamento
de erro — o que torna a completude do dataset original impossível de verificar.

## Estrutura

```
data/reference/      catálogo de segmentos e resultado da sondagem
src/
  Tcc.Domain/        geometria e catálogo tipado
  Tcc.Probe/         sondagem de cobertura da API (roda uma vez por área)
  Tcc.Collector/     coleta contínua de tráfego e clima
tests/               49 testes
legacy/              código de 2022, congelado e somente leitura
  web-2022/          a visualização original + os dados coletados
  collector-2022/    coletor ASP.NET Web API (.NET Framework 4.5.1)
  etl-2022/          conversor txt → SQL → js (.NET Core 3.1)
```

## Uso

```bash
dotnet run --project src/Tcc.Collector -- --dry-run
```

Projeta o volume diário de chamadas e encerra sem gastar nenhuma. Remover `--dry-run` inicia a
coleta. A chave da TomTom **nunca vai para o repositório** — use user-secrets ou a variável de
ambiente `Tcc__Collector__TomTom__ApiKey`.

## Documentação

Mantida fora deste repositório, em `D:\dev\TCC\documentacao\mapbox`:

| Documento | Conteúdo |
|---|---|
| `README.md` | Índice e estado do projeto |
| `estrutura-do-projeto.md` | Diagramas — pipeline, árvore, dependências, ciclo de leitura |
| `arquitetura-v2.md` | O sistema como está construído |
| `decisoes.md` | Registro de decisões e suas justificativas |
| `sondagem-cobertura.md` | Método e resultado da validação de cobertura |
| `diario-de-execucao.md` | Cronologia do desenvolvimento |
| `plano-reestruturacao-v2.md` | Plano ativo e progresso por fase |
| `analise-tecnica.md`, `inventario-legado.md`, `instalacao-e-uso.md` | Documentação da v1 |

## Licença

Trabalho acadêmico. Os dados de tráfego são derivados da TomTom Traffic API e os dados
meteorológicos da OpenWeatherMap, sujeitos aos termos de uso dos respectivos provedores.
