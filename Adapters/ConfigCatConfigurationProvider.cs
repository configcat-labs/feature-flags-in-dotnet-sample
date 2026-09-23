using System.Globalization;
using ConfigCat.Client;
using ConfigCat.Client.Configuration;

namespace ConfigCatInDotnetSample.Adapters;

/// <summary>
/// Provides configuration key-value pairs that are obtained from ConfigCat.
/// </summary>
public sealed class ConfigCatConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly StringComparer _keyComparer;
    private readonly bool _throwOnInitFailure;
    private readonly bool _reloadOnChange;
    private bool _isReload;
    private readonly Lock _reloadLock = new();
    private Task _pendingReloadTask = Task.CompletedTask;
    private readonly LoggingReadyNotifier? _loggingReadyNotifier;
    private readonly ConfigCatToMSLoggerAdapter? _loggerAdapter;
    private readonly IConfigCatClient _client;
    private IConfigCatClientSnapshot? _initialSnapshot;

    public ConfigCatConfigurationProvider(ConfigCatConfigurationSource source)
    {
        _keyComparer = source.CaseInsensitiveKeys ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        _throwOnInitFailure = source.ThrowOnInitFailure;
        _reloadOnChange = source.ReloadOnChange;

        _loggingReadyNotifier = source.LoggingReadyNotifier;
        _loggerAdapter = _loggingReadyNotifier is not null ? new ConfigCatToMSLoggerAdapter() : null;

        var pollingMode = PollingModes.AutoPoll(source.PollInterval, source.MaxInitWaitTime);

        _client = ConfigCatClient.Get(source.SdkKey, o =>
        {
            o.Logger = _loggerAdapter;
            o.PollingMode = pollingMode;

            if (_reloadOnChange)
            {
                // If automatic reload on change is enabled, we subscribe to the config changed event
                // to get notified when the underlying polling loop detects changes to config data.
                o.ConfigChanged += HandleConfigChanged;
            }
        });

        Initialization = Task.Run(WaitForInitializationAsync);

        _loggingReadyNotifier?.Event += OnLoggingReady;
    }

    /// <summary>
    /// Gets a <see cref="Task"/> that completes when the provider finishes initialization.
    /// </summary>
    public Task<bool> Initialization { get; }

    private async Task<bool> WaitForInitializationAsync()
    {
        // Waits for the client to obtain the initial config data, but no longer than MaxInitWaitTime.
        // Throws OperationCanceledException (yielding a canceled task) if the provider
        // (and with it, the underlying client) is disposed before the operation completes.
        var cacheState = await _client.WaitForReadyAsync();

        if (!_reloadOnChange)
        {
            // Stop further communication with the ConfigCat CDN.
            _client.SetOffline();
        }

        var snapshot = _client.Snapshot();
        Load(snapshot);
        _initialSnapshot = snapshot;

        if (cacheState == ClientCacheState.NoFlagData)
        {
            var message = new FormattableLogMessage($"Failed to obtain ConfigCat config data within {nameof(AutoPoll.MaxInitWaitTime)}.");

            if (_throwOnInitFailure)
            {
                throw new TimeoutException(message.InvariantFormattedMessage);
            }

            if (_loggerAdapter is not null)
            {
                _loggerAdapter.Log(ConfigCat.Client.LogLevel.Error, 0, ref message);
            }
            else
            {
                Console.Error.WriteLine(message.InvariantFormattedMessage);
            }

            return false;
        }

        return true;
    }

    private void HandleConfigChanged(object? sender, EventArgs e)
    {
        var configCatClient = (IConfigCatClient)sender!;
        var snapshot = configCatClient.Snapshot();

        if (!Initialization.IsCompleted)
        {
            // Ignore config changed events while initialization is in progress
            // (that is, before WaitForInitializationAsync completes).
            return;
        }

        if (Interlocked.Exchange(ref _initialSnapshot, null) is ConfigCatClientSnapshot initialSnapshot
            && EqualityComparer<ConfigCatClientSnapshot>.Default.Equals(snapshot, initialSnapshot))
        {
            // Ignore the event if the same changes have already been loaded concurrently.
            // (There's a race condition between this event handler and WaitForInitializationAsync.)
            return;
        }

        Load(snapshot);
    }

    private void Load(ConfigCatClientSnapshot snapshot)
    {
        var data = new Dictionary<string, string?>(_keyComparer);

        foreach (var key in snapshot.GetAllKeys())
        {
            var normalizedKey = key.Replace("__", ConfigurationPath.KeyDelimiter);
            var value = snapshot.GetValue<object?>(key, defaultValue: null);
            data[normalizedKey] = Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        Data = data;

        OnReload();
    }

    /// <inheritdoc />
    public override void Load()
    {
        lock (_reloadLock)
        {
            if (!_isReload)
            {
                _isReload = true;

                // Ignore the initial Load call.
                return;
            }

            if (!Initialization.IsCompleted || !_pendingReloadTask.IsCompleted)
            {
                // Ignore subsequent Load calls (reload requests) while initialization or a forced reload operation
                // is in progress (that is, ensure that concurrent reload requests are deduplicated).
                return;
            }

            _pendingReloadTask = Task.Run(async () =>
            {
                if (!_reloadOnChange)
                {
                    // Temporarily enable communication with the ConfigCat CDN.
                    _client.SetOnline();
                }

                try
                {
                    // Attempts to obtain the latest config data immediately (outside of the polling loop).
                    // Raises a config changed event if the config data has changed.
                    // Throws OperationCanceledException (yielding a canceled task) if the provider
                    // (and with it, the underlying client) is disposed before the operation completes.
                    var refreshResult = await _client.ForceRefreshAsync();

                    if (!_reloadOnChange && refreshResult.IsSuccess)
                    {
                        // When automatic reload on change is disabled, we're not subscribed to
                        // the config changed event, so we need to call the event handler manually.
                        HandleConfigChanged(_client, EventArgs.Empty);
                    }
                }
                finally
                {
                    if (!_reloadOnChange)
                    {
                        _client.SetOffline();
                    }
                }
            });
        }
    }

    private void OnLoggingReady(ILoggerFactory loggerFactory)
    {
        _loggingReadyNotifier!.Event -= OnLoggingReady;
        var logger = loggerFactory.CreateLogger(typeof(ConfigCatClient).FullName + "$");
        _loggerAdapter!.SetLogger(logger);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _loggingReadyNotifier?.Event -= OnLoggingReady;

        _client.Dispose();
    }
}
