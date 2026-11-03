# Disqus Analytics

Coleta e analisa os comentários de podcasts que vivem em fóruns do Disqus.
O programa recebe um fórum, separa as discussões de cada podcast por título,
baixa os comentários, guarda tudo em SQLite e gera:

* `docs/statistics.json` — o relatório de estatísticas (para qualquer cliente);
* `docs/statistics.js` — o mesmo conteúdo como script, que faz a página
  funcionar aberta direto do disco;
* `docs/index.html` — uma página estática (HTML/CSS/JS) pronta para o
  GitHub Pages.

**Exemplo real:** os podcasts RPGuaxa (229 episódios, 5.006 comentários) e
Guaxaverso (176 episódios, 132 comentários), ambos dentro do fórum
`scicastpodcast` (Portal Deviante), que tem mais de 8.000 threads.

---

## O que ele faz

```
API do Disqus
     │  forums/details, forums/listThreads, threads/listPosts
     ▼
DisqusAnalytics.Sync ────► SQLite (data/disqus.db)
     │                        Forums, Discussions, Author, Comment
     │
     ▼
DisqusAnalytics.Analytics ──► docs/statistics.json + docs/statistics.js
                                     │
                                     ▼
                              docs/index.html (GitHub Pages)
```

Decisões que explicam o desenho:

1. **Filtro antes de baixar.** O Disqus não separa podcasts em fóruns: o
   Portal Deviante tem um único fórum com todos os programas. Baixar os
   comentários de 8.000 threads custaria ~8.000 requisições (≈9 h de cota)
   por dados que não interessam. Por isso cada podcast é um *alvo* definido
   por filtros de título e só as discussões relevantes têm comentários
   baixados (405 threads no exemplo).
2. **Sincronização retomável.** Cada discussão guarda `CommentsSyncedAt` e
   `SyncedCommentCount`. Uma nova execução só baixa o que mudou, e um
   HTTP 429 (cota da API) interrompe a coleta sem perder progresso.
3. **Estatísticas derivadas do banco.** Nenhuma métrica chama a API: rodar
   `stats` de novo não gasta cota.

---

## Requisitos

* .NET 8 SDK
* Uma chave pública da API do Disqus (Disqus → Settings → API)

---

## Como replicar

```bash
git clone <repositorio>
cd ranking-disqus
dotnet restore
dotnet build
```

Crie `src/DisqusAnalytics.Console/appsettings.Local.json` (o arquivo é
ignorado pelo git) a partir do exemplo:

```bash
cp src/DisqusAnalytics.Console/appsettings.Local.example.json \
   src/DisqusAnalytics.Console/appsettings.Local.json
```

No PowerShell:

```powershell
Copy-Item src/DisqusAnalytics.Console/appsettings.Local.example.json `
          src/DisqusAnalytics.Console/appsettings.Local.json
```

E preencha a chave:

```json
{
  "Disqus": {
    "ApiKey": "SUA_CHAVE_AQUI"
  }
}
```

Depois rode, na raiz do projeto:

```bash
dotnet run --project src/DisqusAnalytics.Console sync discussions
dotnet run --project src/DisqusAnalytics.Console sync comments
dotnet run --project src/DisqusAnalytics.Console stats
```

Abra `docs/index.html` (duplo clique funciona). Para publicar, veja
[Página estática](#página-estática).

---

## Comandos

| Comando | O que faz |
|---|---|
| `run` | sincroniza discussões + comentários e exporta o relatório |
| `sync` | igual a `run`, sem exportar |
| `sync discussions` | baixa/atualiza só as threads dos alvos (≈82 requisições) |
| `sync comments` | baixa só os comentários das threads relevantes (retomável) |
| `stats [caminho]` | gera e exporta as estatísticas (padrão: `docs/statistics.json`) |
| `quota [fórum]` | consulta quantas requisições restam na janela atual |

---

## Configuração

`src/DisqusAnalytics.Console/appsettings.json` é versionado; o
`appsettings.Local.json` sobrescreve os valores (use-o para a chave).

```json
{
  "Disqus": {
    "ApiKey": "",
    "RequestDelayMilliseconds": 250
  },
  "Database": {
    "Provider": "Sqlite",
    "ConnectionString": "Data Source=data/disqus.db"
  },
  "Analytics": {
    "OutputPath": "docs/statistics.json",
    "TopAuthors": 25,
    "TopDiscussions": 25,
    "TopWords": 100,
    "MinimumWordLength": 3
  },
  "Forums": [
    {
      "Name": "RPGuaxa",
      "Forum": "scicastpodcast",
      "Filters": {
        "TitleContains": [ "RPGuaxa" ],
        "TitleExclude": [ "Guaxaverso" ]
      }
    }
  ]
}
```

| Chave | Para que serve |
|---|---|
| `Disqus:ApiKey` | chave pública da API (obrigatória) |
| `Disqus:RequestDelayMilliseconds` | intervalo entre requisições; evita estourar a cota |
| `Database:ConnectionString` | caminho do SQLite, relativo à raiz do projeto |
| `Analytics:OutputPath` | caminho do relatório (o `statistics.js` sai ao lado) |
| `Analytics:TopAuthors` / `TopDiscussions` / `TopWords` | tamanho dos rankings |
| `Analytics:MinimumWordLength` | tamanho mínimo de palavra nas estatísticas |
| `Forums[].Name` | nome exibido no relatório |
| `Forums[].Forum` | shortname do fórum no Disqus |
| `Forums[].Filters.TitleContains` | o título precisa conter um destes termos |
| `Forums[].Filters.TitleExclude` | termos que tiram a discussão do alvo |

**Atenção ao `Local`:** o provedor de configuração mescla arrays por índice.
Se o `appsettings.Local.json` definir `Forums`, defina **todos** os alvos nele
— senão o alvo do arquivo base continua valendo na posição seguinte.

Para incluir outro programa do mesmo fórum basta acrescentar um item em
`Forums` e rodar `sync comments` de novo. Nenhum código muda.

---

## Como a coleta funciona

1. **Fórum:** é lido do banco; a API só é consultada na primeira execução.
2. **Discussões:** `forums/listThreads` pagina de 100 em 100 e o resultado é
   gravado em `Discussions`. Em cada página, `IsRelevant` é recalculado com
   os filtros de todos os alvos daquele fórum.
3. **Comentários:** para cada discussão relevante, `threads/listPosts` é
   paginado e cada página é gravada em `Author` e `Comment` (upsert por id).
   Discussões sem comentários são marcadas sem gastar requisição.

**Cota:** a chave pública permite **1.000 requisições por janela de 1 hora**;
o Disqus informa o saldo nos cabeçalhos `X-Ratelimit-*` e o comando `quota`
mostra o valor. Com 250 ms entre chamadas, 405 threads consomem ~450
requisições.

---

## Horários

O Disqus devolve os horários em **UTC sem o indicador de fuso**
(`"2025-08-18T15:44:28"`). O cliente lê esse valor como UTC
(`UtcDateTimeOffsetConverter`), o banco guarda o instante e o relatório
converte tudo para **America/Sao_Paulo (-03:00)** — inclusive nas agregações
por mês, dia da semana e hora. O horário de verão de 2018–2019 é respeitado
(comentários de dezembro de 2018 aparecem com `-02:00`).

Bancos criados antes dessa correção tinham os horários 3 horas adiantados; a
migração roda uma única vez, controlada por `PRAGMA user_version`
(`DatabaseInitializer`).

---

## O que é gerado

`docs/statistics.json` traz um bloco por alvo:

| Campo | Conteúdo |
|---|---|
| `totals` | comentários, autores, discussões, média de caracteres, concentração, vocabulário, etc. |
| `topAuthors` | ranking com comentários, média de caracteres, episódios e `%` do total |
| `topDiscussions` | episódios mais comentados (com link) |
| `commentsByMonth` | série mensal de comentários e autores |
| `topWords`, `topBigrams`, `rareWords` | palavras, pares e palavras usadas uma única vez |
| `wordLengths` | distribuição exata por comprimento (3 a 24 letras) |
| `hashtags`, `emojis` | usos, com contagem e autores |
| `newAuthorsByYear`, `newcomers` | estreias por ano, retenção e quem chegou por último |
| `activityByWeekdayHour` | heatmap de dia da semana × hora |
| `vocabularyByYear` | palavras em uso, novas, que sumiram e as exclusivas do ano |
| `longestComment`, `shortestComment` | recordes, com autor, data e episódio |
| `firstCommenters`, `lastCommenters` | quem abre e quem fecha a conversa |

Exemplo mínimo:

```json
{
  "generatedAt": "2026-11-02T22:33:13-03:00",
  "shows": [
    {
      "name": "RPGuaxa",
      "forum": "scicastpodcast",
      "totals": {
        "discussions": 229,
        "comments": 5006,
        "authors": 827,
        "averageCharacters": 667.1
      },
      "topWords": [ { "word": "episódio", "count": 5920, "authors": 592 } ],
      "emojis": [ { "emoji": "❤️", "count": 238, "authors": 57 } ]
    }
  ]
}
```

Tratamento do texto: HTML removido (`CommentText`), stopwords em português,
comparação sem acentos, hashtags separadas das palavras, emojis lidos como
elementos de texto (`👍🏻` conta como um) e repetições como `kkkkk`
descartadas.

---

## Página estática

```
docs/
├── index.html      estrutura e textos fixos
├── styles.css      tema escuro e layout responsivo
├── app.js          monta cartões, gráficos SVG e rankings
├── statistics.js   dados embutidos (window.DISQUS_STATS)
└── statistics.json dados para outros clientes
```

A página não usa CDN: os gráficos são SVG gerados pelo próprio `app.js`, com
fonte ajustada ao tamanho real do painel. Como `statistics.js` expõe os dados
como script, ela funciona com `file://`; servida por HTTP, também tenta
`fetch("statistics.json")`.

Publicar no GitHub Pages:

1. commit da pasta `docs`;
2. **Settings → Pages → Source: Deploy from a branch**, branch `main`,
   pasta `/docs`;
3. a página fica em `https://SEU_USUARIO.github.io/ranking-disqus/`.

Para atualizar os dados, rode `stats` e commite `docs/statistics.json` e
`docs/statistics.js`.

---

## Banco de dados

SQLite em `data/disqus.db`, criado por `EnsureCreated` (o projeto **não usa
migrations**). Alterações posteriores de esquema são aplicadas por
`DatabaseInitializer`, controladas por `PRAGMA user_version`.

| Tabela | Conteúdo |
|---|---|
| `Forums` | fórum Disqus (`Id`, `ShortName`, `Name`, `Url`, `LastSyncAt`) |
| `Discussions` | threads, `IsRelevant` e o estado da coleta de comentários |
| `Author` | nome, usuário, perfil e avatar |
| `Comment` | texto HTML, data, tamanho e flags (`IsDeleted`, `IsSpam`) |

---

## Estrutura

```
src/
├── DisqusAnalytics.Abstractions   interfaces, configurações e modelos do relatório
├── DisqusAnalytics.Domain         entidades, enums e limpeza de texto
├── DisqusAnalytics.Disqus         cliente HTTP, DTOs e conversão de datas
├── DisqusAnalytics.Persistence    DbContext, repositórios e inicialização
├── DisqusAnalytics.Sync           sincronização e filtros por alvo
├── DisqusAnalytics.Analytics      cálculo das estatísticas e stopwords
└── DisqusAnalytics.Console        comandos (run, sync, stats, quota)
docs/                              página estática + dados publicados
data/                              banco SQLite (ignorado pelo git)
```

---

## Limitações conhecidas

* **Comentários removidos e spam** não são devolvidos pela API; o relatório só
  vê o que está público.
* **Imagens e GIFs** chegam em um campo `media` que não é armazenado; por isso
  a contagem de comentários com imagem é sempre zero.
* **O ano corrente é parcial** — em 2026, “palavras que sumiram” fica inflado.
* **Cota** de 1.000 requisições/hora por chave; uma coleta completa cabe, mas
  duas seguidas podem estourar.
* **Filtro por título** é heurístico: `TitleExclude` existe porque os episódios
  do Guaxaverso citam “RPGuaxa” no título.

---

## Próximos passos

* Migrar para EF Core migrations (hoje é `EnsureCreated` + `user_version`).
* Guardar `parent` e `likes` dos comentários (exige re-sync) para grafo de
  conversas e curtidas.
* Curva de Zipf, lei de Heaps, Gini/HHI e legibilidade (Flesch pt-BR).
* Análise de sentimento e TF-IDF por episódio.
* Suporte a múltiplos fóruns na mesma execução.
