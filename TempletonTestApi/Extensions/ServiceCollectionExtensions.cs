using Microsoft.Extensions.Options;
using Refit;
using TempletonTestApi.Clients;
using TempletonTestApi.Contracts.Services;
using TempletonTestApi.Options;
using TempletonTestApi.Services;

namespace TempletonTestApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDependencies(this IServiceCollection services)
    {
        return services
            .AddClientOptions()
            .AddClients()
            .AddServices();
    }

    public static IServiceCollection AddClients(this IServiceCollection services)
    {
        return services
            .AddRefitClient<IHackerNewsClient>()
            .ConfigureHttpClient((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<HackerNewsOptions>>().Value;

                http.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            })
            .Services;
    }

    public static IServiceCollection AddClientOptions(this IServiceCollection services)
    {
        services.AddOptions<HackerNewsOptions>()
            .BindConfiguration(HackerNewsOptions.SectionName);

        services.AddOptions<HackerNewsServiceOptions>()
            .BindConfiguration(HackerNewsServiceOptions.SectionName);

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        return services.AddScoped<IHackerNewsService, HackerNewsService>();
    }
}
