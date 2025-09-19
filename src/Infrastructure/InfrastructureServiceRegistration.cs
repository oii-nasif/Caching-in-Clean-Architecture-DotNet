using Application.Contracts.Infrastructure;
using Infrastructure.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Cache services registration
        services.AddMemoryCache();
        services.AddSingleton<ICacheProvider, InMemoryCacheProvider>();
        services.AddSingleton<ICacheService, CacheService>();

        // Add other infrastructure services here
        // services.AddDbContext<ApplicationDbContext>(...);
        // services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        // services.AddScoped<IProductRepository, ProductRepository>();
        // services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }
}