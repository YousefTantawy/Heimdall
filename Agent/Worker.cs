using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HeimdallAgent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _pcapInterface = "eth0";
    private readonly string _csvOutputDir = "/app/data/out";
    private readonly string _hubUrl;
    private readonly string _jwtToken;

    public Worker(ILogger<Worker> logger, IConfiguration config, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;

        _hubUrl = config["HUB_URL"] ?? "";
        _jwtToken = config["HEIMDALL_TOKEN"] ?? "";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Heimdall Agent active. Interface: {Interface}", _pcapInterface);
        Directory.CreateDirectory(_csvOutputDir);

        if (!string.IsNullOrEmpty(_jwtToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            string fileId = DateTime.UtcNow.Ticks.ToString();
            string tempPcap = $"/tmp/capture_{fileId}.pcap";
            string expectedCsv = Path.Combine(_csvOutputDir, $"capture_{fileId}.pcap_Flow.csv");

            try
            {
                // 1. Sniff live traffic for 10 seconds via tcpdump
                _logger.LogInformation("Sniffing network traffic (10s)...");
                using (var tcpdump = new Process())
                {
                    tcpdump.StartInfo = new ProcessStartInfo
                    {
                        FileName = "timeout",
                        Arguments = $"10 tcpdump -i {_pcapInterface} -w {tempPcap}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    tcpdump.Start();
                    await tcpdump.WaitForExitAsync(stoppingToken);
                }

                // 2. Pass the raw file directly to Java via Classpath targeting
                if (File.Exists(tempPcap) && new FileInfo(tempPcap).Length > 24)
                {
                    _logger.LogInformation("Extracting features with CICFlowMeter...");
                    using (var cfm = new Process())
                    {
                        cfm.StartInfo = new ProcessStartInfo
                        {
                            FileName = "java",
                            // Loads all supporting jars from lib and explicitly invokes the main class entrypoint
                            Arguments = $"-cp \"/app/CICFlowMeter/lib/*\" cic.cs.unb.ca.ifm.Cmd {tempPcap} {_csvOutputDir}",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        cfm.Start();
                        await cfm.WaitForExitAsync(stoppingToken);
                    }

                    // 3. If CSV was generated, parse rows and transmit
                    if (File.Exists(expectedCsv))
                    {
                        var rows = await File.ReadAllLinesAsync(expectedCsv, stoppingToken);

                        // Skip header line, ensure there is actual data
                        var flows = rows.Skip(1).Where(row => !string.IsNullOrWhiteSpace(row)).ToList();

                        if (flows.Count > 0)
                        {
                            _logger.LogInformation("Transmitting {Count} flows to Hub...", flows.Count);
                            var payload = new { AgentId = "Heimdall-Node-WSL", Flows = flows };

                            var response = await _httpClient.PostAsJsonAsync($"{_hubUrl}/api/ingest", payload, stoppingToken);
                            if (response.IsSuccessStatusCode)
                            {
                                _logger.LogInformation("Hub accepted batch successfully.");
                            }
                            else
                            {
                                _logger.LogWarning("Hub rejected batch. Status: {Code}", response.StatusCode);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in execution loop iteration.");
            }
            finally
            {
                // 4. Housekeeping: Delete temp files immediately to prevent disk bloating
                if (File.Exists(tempPcap)) File.Delete(tempPcap);
                if (File.Exists(expectedCsv)) File.Delete(expectedCsv);
            }

            // Small cooldown before restarting the next 10s capture window
            await Task.Delay(1000, stoppingToken);
        }
    }
}