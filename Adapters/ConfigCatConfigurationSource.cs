namespace ConfigCatInDotnetSample.Adapters;

/// <summary>
/// Represents a ConfigCat config as an <see cref="IConfigurationSource"/>.
/// </summary>
public sealed class ConfigCatConfigurationSource : IConfigurationSource
{
    private ConfigCatConfigurationProvider? _provider;

    /// <summary>
    /// The SDK Key that identifies the ConfigCat config-environment pair from which to obtain settings.
    /// </summary>
    public string SdkKey { get; set; } = string.Empty;

    /// <summary>
    /// Specifies how frequently to check for changed config data.<br/>
    /// (Default value is 60 seconds. Minimum value is 1 second. Maximum value is <see cref="int.MaxValue"/> milliseconds.)
    /// </summary>
    public TimeSpan? PollInterval { get; set; }

    /// <summary>
    /// Specifies the maximum wait time to obtain the initial config data.<br/>
    /// (Default value is 5 seconds. Maximum value is <see cref="int.MaxValue"/> milliseconds. Negative values mean infinite waiting.)
    /// </summary>
    public TimeSpan? MaxInitWaitTime { get; set; }

    /// <summary>
    /// Specifies whether to throw a <see cref="TimeoutException"/> during initialization, thereby terminating the application,
    /// if the config data cannot be obtained within the configured <see cref="MaxInitWaitTime"/>.
    /// (Defaults to <see langword="false"/>, in which case an error message will only be logged.)
    /// </summary>
    public bool ThrowOnInitFailure { get; set; }

    /// <summary>
    /// Controls whether to use case-insensitive key matching (as most configuration providers do), despite ConfigCat keys being case-sensitive.
    /// (Default value is <see langword="false"/>.)
    /// </summary>
    public bool CaseInsensitiveKeys { get; set; }

    /// <summary>
    /// Controls whether the source will be loaded if the ConfigCat config data changes.
    /// (Default value is <see langword="false"/>.)
    /// </summary>
    public bool ReloadOnChange { get; set; }

    /// <summary>
    /// Specifies the notifier object used to signal when the <see cref="ILoggerFactory"/> becomes available.
    /// (If not specified, the underlying ConfigCat client will default to its built-in console logging.)
    /// </summary>
    public LoggingReadyNotifier? LoggingReadyNotifier { get; set; }

    public ConfigCatConfigurationProvider PreBuild(IConfigurationBuilder builder)
    {
        _provider?.Dispose();
        return _provider = new ConfigCatConfigurationProvider(this);
    }

    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var provider = _provider ?? PreBuild(builder);
        _provider = null;
        return provider;
    }
}
