using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Playbook.Application.Common.Abstractions;
using Playbook.Infrastructure.Auth;
using Playbook.Infrastructure.Common;
using Playbook.Infrastructure.Mongo;
using Playbook.Infrastructure.Storage;

namespace Playbook.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Mongo
        services.AddSingleton<MongoBootstrapper>();
        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var bootstrapper = sp.GetRequiredService<MongoBootstrapper>();
            return bootstrapper.GetDatabase();
        });
        services.AddScoped<IUserRepository, MongoUserRepository>();
        services.AddScoped<IActivityRepository, MongoActivityRepository>();
        services.AddScoped<ICategoryRepository, MongoCategoryRepository>();

        // Auth
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtIssuer, JwtIssuer>();

        // Storage
        services.AddScoped<IBlobStore, AzureBlobStore>();

        // Common
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
