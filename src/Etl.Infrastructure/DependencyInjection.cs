using System.Net.Http.Headers;
using Etl.Application.Abstractions;
using Etl.Infrastructure.Audit;
using Etl.Infrastructure.Extractors;
using Etl.Infrastructure.Options;
using Etl.Infrastructure.Staging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra las fuentes según la configuración JSON. Para agregar una fuente nueva:
    /// 1) implementar IExtractor, 2) registrarla aquí. El Worker y el orquestador no cambian.
    /// </summary>
    public static IServiceCollection AddEtlInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CsvSourceOptions>(configuration.GetSection("Sources:Csv"));
        services.Configure<DbSourceOptions>(configuration.GetSection("Sources:Database"));
        services.Configure<ApiSourceOptions>(configuration.GetSection("Sources:Api"));
        services.Configure<StagingOptions>(configuration.GetSection("Staging"));
        services.Configure<AnalyticsOptions>(configuration.GetSection("Analytics"));

        if (configuration.GetValue("Sources:Csv:Enabled", true))
            services.AddSingleton<IExtractor, CsvExtractor>();

        if (configuration.GetValue("Sources:Database:Enabled", true))
            services.AddSingleton<IExtractor, DatabaseExtractor>();

        if (configuration.GetValue("Sources:Api:Enabled", true))
        {
            services.AddSingleton<IExtractor, ApiExtractor>();
            services.AddHttpClient(ApiExtractor.ClientName, (sp, client) =>
                {
                    var opt = sp.GetRequiredService<IOptions<ApiSourceOptions>>().Value;
                    client.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
                    if (!string.IsNullOrWhiteSpace(opt.Token))
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opt.Token);
                })
                .AddStandardResilienceHandler();   // reintentos, timeout y circuit breaker
        }

        services.AddSingleton<IStagingWriter, JsonStagingWriter>();

        if (!string.IsNullOrWhiteSpace(configuration["Analytics:ConnectionString"]))
            services.AddSingleton<IEtlAuditLog, SqlEtlAuditLog>();
        else
            services.AddSingleton<IEtlAuditLog, NullEtlAuditLog>();

        return services;
    }
}
