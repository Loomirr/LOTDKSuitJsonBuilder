using DiscordRPC;
using DiscordRPC.Logging;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BatmanSuitJsonBuilder;

internal sealed class DiscordPresenceService : IDisposable
{
    private const string EnvironmentApplicationIdName = "BATMAN_SUIT_JSON_BUILDER_DISCORD_APP_ID";
    private const string LocalConfigFileName = "DiscordPresence.local.json";
    private const string LegacyConfigFileName = "DiscordPresence.json";
    private const string DefaultApplicationId = "1517274305292140706";
    private const string DefaultLargeImageText = "Batman Suit JSON Builder";

    private DiscordRpcClient? _client;
    private DiscordPresenceOptions _options = new();
    private readonly Timestamps _sessionTimestamp = Timestamps.Now;
    private bool _initialized;
    private bool _failed;

    public bool IsActive => _initialized && !_failed && _client is not null;

    public void Initialize()
    {
        if (_initialized || _failed)
        {
            return;
        }

        _options = LoadOptions();
        var applicationId = ResolveApplicationId(_options);

        if (!_options.Enabled || string.IsNullOrWhiteSpace(applicationId) || IsPlaceholderApplicationId(applicationId))
        {
            return;
        }

        try
        {
            _client = new DiscordRpcClient(applicationId.Trim())
            {
                Logger = new ConsoleLogger { Level = LogLevel.Warning }
            };

            _initialized = _client.Initialize();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Discord Rich Presence failed to initialize: {ex}");
            _failed = true;
            SafeDisposeClient();
        }
    }

    public void SetActivity(string details, string state)
    {
        if (!IsActive || _client is null)
        {
            return;
        }

        try
        {
            var presence = new RichPresence
            {
                Details = string.IsNullOrWhiteSpace(details) ? "Building suit JSON" : details.Trim(),
                State = string.IsNullOrWhiteSpace(state) ? "Ready" : state.Trim(),
                Timestamps = _sessionTimestamp
            };

            var largeImageKey = _options.GetLargeImageKey();
            if (!string.IsNullOrWhiteSpace(largeImageKey))
            {
                presence.Assets = new Assets
                {
                    LargeImageKey = largeImageKey.Trim(),
                    LargeImageText = string.IsNullOrWhiteSpace(_options.LargeImageText)
                        ? DefaultLargeImageText
                        : _options.LargeImageText.Trim()
                };
            }

            _client.SetPresence(presence);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Discord Rich Presence update failed: {ex}");
        }
    }

    public void SetPageActivity(int pageIndex, string? suitName = null)
    {
        var label = CleanSuitLabel(suitName);

        switch (pageIndex)
        {
            case 0:
                SetActivity("Setting up a custom suit", label);
                break;
            case 1:
                SetActivity("Editing suit changes", label);
                break;
            case 2:
                SetActivity("Editing equipment replacements", label);
                break;
            case 3:
                SetActivity("Preparing suit.json export", label);
                break;
            default:
                SetActivity("Building suit JSON", label);
                break;
        }
    }

    public void Clear()
    {
        if (!IsActive || _client is null)
        {
            return;
        }

        try
        {
            _client.ClearPresence();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Discord Rich Presence clear failed: {ex}");
        }
    }

    public void Dispose()
    {
        try
        {
            Clear();
        }
        finally
        {
            SafeDisposeClient();
            _initialized = false;
        }
    }

    private void SafeDisposeClient()
    {
        try
        {
            _client?.Dispose();
        }
        catch
        {
            // Discord should never block app shutdown.
        }
        finally
        {
            _client = null;
        }
    }

    private static string CleanSuitLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Batman Suit JSON Builder";
        }

        return value.Trim();
    }

    private static bool IsPlaceholderApplicationId(string applicationId)
    {
        var normalized = applicationId.Trim();
        return normalized.Equals("PUT_YOUR_DISCORD_APPLICATION_ID_HERE", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("PUT_YOUR_DISCORD_APPLICATION_CLIENT_ID_HERE", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("YOUR_DISCORD_APPLICATION_ID_HERE", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("YOUR_CLIENT_ID_HERE", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("0", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveApplicationId(DiscordPresenceOptions options)
    {
        var environmentValue = Environment.GetEnvironmentVariable(EnvironmentApplicationIdName);
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return environmentValue.Trim();
        }

        var configValue = options.GetApplicationId();
        if (!string.IsNullOrWhiteSpace(configValue))
        {
            return configValue.Trim();
        }

        return DefaultApplicationId;
    }

    private static DiscordPresenceOptions LoadOptions()
    {
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        var localPath = Path.Combine(dataDirectory, LocalConfigFileName);
        var legacyPath = Path.Combine(dataDirectory, LegacyConfigFileName);

        if (File.Exists(localPath))
        {
            return LoadOptionsFromPath(localPath);
        }

        if (File.Exists(legacyPath))
        {
            return LoadOptionsFromPath(legacyPath);
        }

        return DiscordPresenceOptions.CreateDefault();
    }

    private static DiscordPresenceOptions LoadOptionsFromPath(string path)
    {
        try
        {
            var options = JsonSerializer.Deserialize<DiscordPresenceOptions>(
                File.ReadAllText(path),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });

            return DiscordPresenceOptions.MergeWithDefault(options);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Discord Rich Presence config failed to load: {ex}");
            return new DiscordPresenceOptions { Enabled = false };
        }
    }
}

internal sealed class DiscordPresenceOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    // Discord calls this the Application ID in the Developer Portal.
    // The DiscordRPC package names the same value client ID internally.
    [JsonPropertyName("applicationId")]
    public string ApplicationId { get; set; } = string.Empty;

    // Kept so older private configs named "clientId" still work.
    [JsonPropertyName("clientId")]
    public string LegacyClientId { get; set; } = string.Empty;

    // Use a Discord Developer Portal Rich Presence asset key here.
    // Leave blank to show no large image.
    [JsonPropertyName("largeImageKey")]
    public string LargeImageKey { get; set; } = string.Empty;

    [JsonPropertyName("largeImageText")]
    public string LargeImageText { get; set; } = string.Empty;

    // Kept so older local configs using a direct GIF/image URL can still work if Discord accepts it.
    // New public configs should prefer largeImageKey.
    [JsonPropertyName("largeImageUrl")]
    public string LegacyLargeImageUrl { get; set; } = string.Empty;

    public static DiscordPresenceOptions CreateDefault() => new()
    {
        Enabled = true,
        ApplicationId = string.Empty,
        LargeImageKey = string.Empty,
        LegacyLargeImageUrl = DefaultLargeImageUrl,
        LargeImageText = DefaultText
    };

    public static DiscordPresenceOptions MergeWithDefault(DiscordPresenceOptions? options)
    {
        if (options is null)
        {
            return CreateDefault();
        }

        options.ApplicationId = options.ApplicationId?.Trim() ?? string.Empty;
        options.LegacyClientId = options.LegacyClientId?.Trim() ?? string.Empty;
        options.LargeImageKey = options.LargeImageKey?.Trim() ?? string.Empty;
        options.LegacyLargeImageUrl = options.LegacyLargeImageUrl?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(options.LargeImageKey) && string.IsNullOrWhiteSpace(options.LegacyLargeImageUrl))
        {
            options.LegacyLargeImageUrl = DefaultLargeImageUrl;
        }

        options.LargeImageText = string.IsNullOrWhiteSpace(options.LargeImageText)
            ? DefaultText
            : options.LargeImageText.Trim();

        return options;
    }

    public string GetApplicationId()
    {
        return !string.IsNullOrWhiteSpace(ApplicationId) ? ApplicationId : LegacyClientId;
    }

    public string GetLargeImageKey()
    {
        return !string.IsNullOrWhiteSpace(LargeImageKey) ? LargeImageKey : LegacyLargeImageUrl;
    }

    private const string DefaultLargeImageUrl = "https://media1.giphy.com/media/v1.Y2lkPTc5MGI3NjExbHlyeHFrMjd0cGYxdXU1aGc2ZjJnZTFheXZ3NWJuam0yaGhyYTc5NyZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/wD7RLvX3zt0m2jG5xj/giphy.gif";
    private const string DefaultText = "Batman Suit JSON Builder";
}
