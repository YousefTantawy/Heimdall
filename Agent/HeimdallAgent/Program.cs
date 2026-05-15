using HeimdallAgent;
using System.Net.Http.Headers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var hubUrl = Environment.GetEnvironmentVariable("HUB_URL")
            ?? throw new InvalidOperationException("Fatal: HUB_URL environment variable is missing.");

        var token = Environment.GetEnvironmentVariable("HEIMDALL_TOKEN")
            ?? throw new InvalidOperationException("Fatal: HEIMDALL_TOKEN environment variable is missing.");

        services.AddHttpClient("HubClient", client =>
        { 
            client.BaseAddress = new Uri(hubUrl); // Base address
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();