using Npgsql;
namespace myFridge.Db;

public static class Extensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        services.AddScoped<NpgsqlConnection>(_ => new NpgsqlConnection(connectionString));

        return services;
    }
}
