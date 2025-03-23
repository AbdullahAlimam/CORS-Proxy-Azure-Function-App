using CORSProxy.Functions;
using CORSProxy.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

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

        // ✅ Build the ServiceProvider
        var serviceProvider = services.BuildServiceProvider();


        // ✅ Initialize IHttpClientFactory globally
        HttpClientFactoryProvider.Initialize(serviceProvider.GetRequiredService<IHttpClientFactory>());

        // ✅ Retrieve IConfiguration from the built ServiceProvider
        var config = serviceProvider.GetRequiredService<IConfiguration>();

        // ✅ Load configuration for validators
        OriginValidator.LoadConfiguration(config);
        ProxyResponseHandler.LoadConfiguration(config);
    })
    .Build();

host.Run();
