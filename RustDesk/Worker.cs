using System.Diagnostics;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Options;

namespace RustDesk;

public class RustDeskSettings
{
    public string ConfigFilePath { get; set; } = "";
    public string DefaultRendezvousServer { get; set; } = "rs-ny.rustdesk.com:21116";
    public string DefaultCustomServer { get; set; } = "rs-ny1.rustdesk.com";
    public string RelayServerToCheck { get; set; } = "100.96.0.11";
    public string InternetCheckHost { get; set; } = "8.8.8.8";
    public string RustDeskExePath { get; set; } = @"C:\Program Files\RustDesk\RustDesk.exe";
    public int PingTimeoutMs { get; set; } = 3000;
    public int CheckIntervalMs { get; set; } = 30000;
    public int RestartDelayMs { get; set; } = 5000;
}

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RustDeskSettings _settings;

    public Worker(ILogger<Worker> logger, IOptions<RustDeskSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RustDesk Connection Monitor Service started at: {time}", DateTimeOffset.Now);
        _logger.LogInformation("Config file: {Path}", _settings.ConfigFilePath);
        _logger.LogInformation("Relay server to check: {Server}", _settings.RelayServerToCheck);
        _logger.LogInformation("Default server: {Server}", _settings.DefaultRendezvousServer);

        await Task.Delay(5000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckConnectionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during connection check");
            }

            await Task.Delay(_settings.CheckIntervalMs, stoppingToken);
        }
    }

    private async Task CheckConnectionAsync()
    {
        _logger.LogInformation("Checking internet connection...");

        bool hasInternet = await PingHostAsync(_settings.InternetCheckHost);

        if (!hasInternet)
        {
            _logger.LogWarning("No internet connection (cannot ping {Host}). Doing nothing.", _settings.InternetCheckHost);
            return;
        }

        _logger.LogInformation("Internet OK. Checking relay server {Server}...", _settings.RelayServerToCheck);
        bool canReachRelay = await PingHostAsync(_settings.RelayServerToCheck);

        if (canReachRelay)
        {
            _logger.LogInformation("Relay server {Server} is reachable. Resetting to relay server.", _settings.RelayServerToCheck);
            await ResetToRelayServerAsync();
        }
        else
        {
            _logger.LogWarning("Cannot reach relay server {Server}. Resetting to default.", _settings.RelayServerToCheck);
            await ResetToDefaultServerAsync();
        }
    }

    private async Task<bool> PingHostAsync(string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, _settings.PingTimeoutMs);

            if (reply.Status == IPStatus.Success)
            {
                _logger.LogDebug("Ping to {Host} successful, roundtrip time: {Time}ms", host, reply.RoundtripTime);
                return true;
            }
            else
            {
                _logger.LogDebug("Ping to {Host} failed with status: {Status}", host, reply.Status);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ping to {Host} failed with exception", host);
            return false;
        }
    }

    private async Task ResetToRelayServerAsync()
    {
        try
        {
            if (!File.Exists(_settings.ConfigFilePath))
            {
                _logger.LogError("Config file not found: {Path}", _settings.ConfigFilePath);
                return;
            }

            var relayServer = $"{_settings.RelayServerToCheck}:21116";
            _logger.LogInformation("Resetting to relay server: {Server}", relayServer);

            var configContent = $"""
                rendezvous_server = '{relayServer}'
                nat_type = 1
                serial = 0
                unlock_pin = ''
                trusted_devices = ''

                [options]
                direct-server = 'Y'
                key = 'ZdTwjNN1aMXSRl+9sH1I2yTpoon7WxBsWqJO5R+RHhI='
                disable-udp = 'N'
                custom-rendezvous-server = '{_settings.RelayServerToCheck}'
                access-mode = 'full'
                direct-access-port = '21118'
                av1-test = 'Y'
                """;

            File.WriteAllText(_settings.ConfigFilePath, configContent);
            _logger.LogInformation("Config file updated.");

            await RestartRustDeskAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset to relay server");
        }
    }

    private async Task ResetToDefaultServerAsync()
    {
        try
        {
            if (!File.Exists(_settings.ConfigFilePath))
            {
                _logger.LogError("Config file not found: {Path}", _settings.ConfigFilePath);
                return;
            }

            _logger.LogInformation("Resetting to default server: {Server}", _settings.DefaultRendezvousServer);

            var configContent = $"""
                nat_type = 1
                serial = 0
                unlock_pin = ''
                trusted_devices = ''

                [options]
                direct-access-port = '21118'
                av1-test = 'Y'
                access-mode = 'full'
                disable-udp = 'N'
                direct-server = 'Y'
                
                """;

            File.WriteAllText(_settings.ConfigFilePath, configContent);
            _logger.LogInformation("Config file updated.");

            await RestartRustDeskAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset to default server");
        }
    }

    private async Task RestartRustDeskAsync()
    {
        try
        {
            var processes = Process.GetProcessesByName("RustDesk");
            if (processes.Length > 0)
            {
                _logger.LogInformation("Stopping RustDesk ({Count} instance(s))...", processes.Length);
                foreach (var process in processes)
                {
                    process.Kill();
                    process.WaitForExit(5000);
                    process.Dispose();
                }
            }
            else
            {
                _logger.LogInformation("RustDesk is not running.");
            }

            _logger.LogInformation("Waiting {Delay}ms...", _settings.RestartDelayMs);
            await Task.Delay(_settings.RestartDelayMs);

            if (!File.Exists(_settings.RustDeskExePath))
            {
                _logger.LogError("RustDesk executable not found: {Path}", _settings.RustDeskExePath);
                return;
            }

            _logger.LogInformation("Starting RustDesk...");
            Process.Start(new ProcessStartInfo
            {
                FileName = _settings.RustDeskExePath,
                UseShellExecute = true
            });
            _logger.LogInformation("RustDesk restarted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart RustDesk");
        }
    }
}
