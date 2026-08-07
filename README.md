# Disqus Analytics

Sistema genérico para coleta, armazenamento e análise de dados de fóruns Disqus.

O sistema recebe um fórum Disqus, sincroniza threads e comentários, armazena os dados localmente, aplica filtros configuráveis e gera arquivos JSON com estatísticas para consumo por um frontend estático.

---

# 1. Objetivo do Projeto

Criar uma ferramenta capaz de analisar comunidades baseadas em Disqus.

Exemplos de análises:

* Ranking de comentaristas.
* Posts/discussões com mais comentários.
* Usuários mais ativos.
* Usuários que escrevem mais caracteres.
* Usuários mais concisos.
* Evolução da comunidade.
* Estatísticas por período.
* Estatísticas por discussão.

O frontend não acessará o Disqus diretamente.

Fluxo:

```
Disqus API
    |
    |
Backend .NET
    |
    +-- Banco 
    |
    +-- Processamento estatístico
    |
    +-- JSON
            |
            |
      GitHub Pages
```

---

# 2. Tecnologias

## Backend

* .NET 8
* C#
* Entity Framework Core
* SQLite
* HttpClient
* System.Text.Json

## Frontend

Inicialmente:

* HTML
* CSS
* JavaScript

Hospedagem:

* GitHub Pages

---

# 3. Criar a solução

Criar diretório:

```bash
mkdir DisqusAnalytics

cd DisqusAnalytics
```

Criar solução:

```bash
dotnet new sln -n DisqusAnalytics
```

---

# 4. Criar projetos


---

## Abstractions

Interfaces compartilhadas.

```bash
dotnet new classlib -n DisqusAnalytics.Abstractions -o src/DisqusAnalytics.Abstractions
```

## Domínio

Responsável pelas entidades e regras básicas.

```bash
dotnet new classlib -n DisqusAnalytics.Domain -o src/DisqusAnalytics.Domain
```

---

## Persistência

Responsável pelo banco de dados.

```bash
dotnet new classlib -n DisqusAnalytics.Persistence -o src/DisqusAnalytics.Persistence
```

---

## Cliente Disqus

Responsável pela comunicação com a API.

```bash
dotnet new classlib -n DisqusAnalytics.Disqus -o src/DisqusAnalytics.Disqus
```

---

## Sincronização

Responsável por baixar e atualizar dados.

```bash
dotnet new classlib -n DisqusAnalytics.Sync -o src/DisqusAnalytics.Sync
```

---

## Analytics

Responsável pelos cálculos estatísticos.

```bash
dotnet new classlib -n DisqusAnalytics.Analytics -o src/DisqusAnalytics.Analytics
```

---

## Aplicação Console

Ponto de entrada inicial.

```bash
dotnet new console -n DisqusAnalytics.Console -o src/DisqusAnalytics.Console
```

---

# 5. Adicionar projetos na Solution

```bash
dotnet sln add src/DisqusAnalytics.Abstractions

dotnet sln add src/DisqusAnalytics.Domain

dotnet sln add src/DisqusAnalytics.Persistence

dotnet sln add src/DisqusAnalytics.Disqus

dotnet sln add src/DisqusAnalytics.Sync

dotnet sln add src/DisqusAnalytics.Analytics

dotnet sln add src/DisqusAnalytics.Console
```

---

# 6. Referências entre projetos

## Abstractions depende do Domain

```bash
dotnet add src/DisqusAnalytics.Abstractions reference src/DisqusAnalytics.Domain
```

## Persistence depende de Domain e Abstractions

```bash
dotnet add src/DisqusAnalytics.Persistence reference src/DisqusAnalytics.Domain

dotnet add src/DisqusAnalytics.Persistence reference src/DisqusAnalytics.Abstractions
```

---

## Disqus depende do Domain e Abstractions

```bash
dotnet add src/DisqusAnalytics.Disqus reference src/DisqusAnalytics.Domain

dotnet add src/DisqusAnalytics.Persistence reference src/DisqusAnalytics.Abstractions
```

---

## Sync depende de:

```bash
dotnet add src/DisqusAnalytics.Sync reference src/DisqusAnalytics.Domain

dotnet add src/DisqusAnalytics.Sync reference src/DisqusAnalytics.Abstractions

dotnet add src/DisqusAnalytics.Sync reference src/DisqusAnalytics.Disqus

dotnet add src/DisqusAnalytics.Sync reference src/DisqusAnalytics.Persistence

dotnet add src/DisqusAnalytics.Sync reference src/DisqusAnalytics.Analytics
```

---

## Analytics depende do Domain e Abstractions

```bash
dotnet add src/DisqusAnalytics.Analytics reference src/DisqusAnalytics.Domain

dotnet add src/DisqusAnalytics.Persistence reference src/DisqusAnalytics.Abstractions
```

---

## Console depende dos Sync

```bash
dotnet add src/DisqusAnalytics.Console reference src/DisqusAnalytics.Sync
```

---

# 7. Pacotes NuGet

## Entity Framework SQLite

Executar:

```bash
dotnet add src/DisqusAnalytics.Persistence package Microsoft.EntityFrameworkCore.Sqlite
```

---

## Ferramenta EF

Instalar:

```bash
dotnet tool install --global dotnet-ef
```

Verificar:

```bash
dotnet ef --version
```

---

# 8. Estrutura final

```
DisqusAnalytics

│
├── DisqusAnalytics.sln
│
├── src
│
│   ├── DisqusAnalytics.Abistractions
│   │
│   │   └── Interfaces
│   │
│   ├── DisqusAnalytics.Domain
│   │
│   │   ├── Entities
│   │   └── Models
│   │
│   ├── DisqusAnalytics.Persistence
│   │
│   │   ├── Context
│   │   ├── Configurations
│   │   └── Repositories
│   │
│   ├── DisqusAnalytics.Disqus
│   │
│   │   ├── Client
│   │   ├── DTOs
│   │   └── Responses
│   │
│   ├── DisqusAnalytics.Sync
│   │
│   │   ├── Services
│   │   └── Workers
│   │
│   ├── DisqusAnalytics.Analytics
│   │
│   │   ├── Statistics
│   │   └── Reports
│   │
│   └── DisqusAnalytics.Console
│
└── data
    |
    └── disqus.db
```

---

# 9. Fases do desenvolvimento

---

# Fase 1 - Base do projeto

Objetivo:

Criar estrutura funcional.

Responsabilidades:

* Criar solução.
* Criar projetos.
* Configurar dependências.
* Configurar SQLite.
* Criar entidades.

Resultado:

Aplicação compilando.

---

# Fase 2 - Integração Disqus API

Objetivo:

Comunicar com o Disqus.

Responsabilidades:

Criar:

```
IDisqusClient
```

Implementar:

```
ListThreadsAsync()

ListPostsAsync()

```

Suportar:

* API Key.
* Paginação por cursor.
* Tratamento de erros.

Resultado:

Dados retornados da API.

---

# Fase 3 - Sincronização

Objetivo:

Persistir dados.

Responsabilidades:

* Buscar threads.
* Atualizar threads existentes.
* Buscar comentários.
* Atualizar usuários.
* Evitar duplicação.

Resultado:

Banco SQLite populado.

---

# Fase 4 - Filtros

Objetivo:

Identificar dados relevantes.

Responsabilidades:

Configurar regras:

Exemplo:

```json
{
 "titleContains":[
   "texto"
 ]
}
```

Resultado:

Threads relevantes marcadas.

---

# Fase 5 - Estatísticas

Objetivo:

Transformar dados em informações.

Responsabilidades:

Criar:

StatisticsService

Exemplos:

* Top comentaristas.
* Posts mais comentados.
* Média de tamanho.
* Usuários mais prolíficos.
* Usuários mais frequentes.

---

# Fase 6 - Exportação JSON

Objetivo:

Gerar dados para frontend.

Saída:

```
/output/statistics.json
```

Exemplo:

```json
{
 "forum":"meuforum",
 "generated":"2026-08-05",
 "ranking":[]
}
```

---

# Fase 7 - Frontend

Objetivo:

Visualização.

Responsabilidades:

* Ler JSON.
* Criar gráficos.
* Mostrar rankings.
* Criar filtros.

Tecnologias:

* HTML
* JavaScript
* Chart.js

Hospedagem:

GitHub Pages.

---

# 10. Executar o projeto

Restaurar:

```bash
dotnet restore
```

Compilar:

```bash
dotnet build
```

Executar:

```bash
dotnet run \
--project src/DisqusAnalytics.Console
```

---

# 11. Comandos úteis

Limpar:

```bash
dotnet clean
```

Atualizar pacotes:

```bash
dotnet list package
```

Ver projetos:

```bash
dotnet sln list
```

Executar migrations:

```bash
dotnet ef database update
```

---

# 12. Evoluções futuras

Possíveis melhorias:

* Migrar SQLite para PostgreSQL.
* Criar API ASP.NET.
* Criar Worker agendado.
* Dashboard autenticado.
* Cache da API.
* Múltiplos fóruns simultâneos.
* Sistema de plugins de análise.

