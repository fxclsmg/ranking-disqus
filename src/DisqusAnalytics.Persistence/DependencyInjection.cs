using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Persistence.Data;
using DisqusAnalytics.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DisqusAnalytics.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"];
        var connectionString = configuration["Database:ConnectionString"];

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new InvalidOperationException(
                "O provedor do banco de dados não foi configurado.");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A connection string do banco de dados não foi configurada.");
        }

        if (!provider.Equals(
                "Sqlite",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"O provedor de banco '{provider}' não é suportado.");
        }

        var sqliteConnectionString =
            new SqliteConnectionStringBuilder(connectionString);

        var dataSource = sqliteConnectionString.DataSource;

        if (string.IsNullOrWhiteSpace(dataSource))
        {
            throw new InvalidOperationException(
                "O caminho do banco SQLite não foi configurado.");
        }

        if (!Path.IsPathRooted(dataSource))
        {
            dataSource = Path.GetFullPath(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    dataSource));
        }

        var databaseDirectory = Path.GetDirectoryName(dataSource);

        if (!string.IsNullOrWhiteSpace(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }

        sqliteConnectionString.DataSource = dataSource;

        services.AddDbContext<DisqusDbContext>(options =>
        {
            options.UseSqlite(
                sqliteConnectionString.ToString());
        });

        services.AddScoped<IForumRepository, ForumRepository>();
        services.AddScoped<IDiscussionRepository, DiscussionRepository>();

        return services;
    }
}
