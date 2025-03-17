using CORS_Proxy_Azure_Function_App;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(worker => worker.UseFunctionExecutionMiddleware())
    .ConfigureServices(services =>
    {
        services.AddLogging();

        // ✅ Register HttpClient in the DI container
        services.AddHttpClient("IgnoreSSL")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });

        // ✅ Explicitly register ProxyFunction as a service
        services.AddTransient<ProxyFunction>();

    })
    .Build();

host.Run();
