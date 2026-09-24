using System.Collections;
using System.Runtime.InteropServices;
using ConfigCat.Client;

namespace ConfigCatInDotnetSample.Adapters;

/// <summary>
/// An <see cref="IConfigCatLogger"/> implementation that forwards log events to a <see cref="ILogger"/> instance.
/// </summary>
/// <param name="logger">The <see cref="ILogger"/> to forward log events to.</param>
public sealed class ConfigCatToMSLoggerAdapter : IConfigCatLogger
{
    // Based on: https://github.com/configcat/.net-sdk/blob/v10.0.0/src/ConfigCat.Extensions.Hosting/Adapters/ConfigCatToMSLoggerAdapter.cs

    // Implementing a configuration provider that logs to the application's logging infrastructure is tricky
    // because the provider must be registered before the DI container is built. As a workaround,
    // we buffer the log events in memory until the logger instance can be resolved from the DI container.

    private readonly List<LogEvent> _deferredEvents = new();
    private ILogger? _logger;

    ConfigCat.Client.LogLevel IConfigCatLogger.LogLevel
    {
        get => ConfigCat.Client.LogLevel.Debug;
        set { throw new NotSupportedException(); }
    }

    public void SetLogger(ILogger logger)
    {
        lock (_deferredEvents)
        {
            if (_logger is null)
            {
                try
                {
                    foreach (ref var evt in CollectionsMarshal.AsSpan(_deferredEvents))
                    {
                        LogCore(logger, evt.Level, evt.EventId, ref evt.Message, evt.Exception);
                    }
                }
                finally
                {
                    _deferredEvents.Clear();
                    _deferredEvents.Capacity = 0;
                }
            }

            _logger = logger;
        }
    }

    /// <inheritdoc/>
    public void Log(ConfigCat.Client.LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception = null)
    {
        ILogger? logger;

        lock (_deferredEvents)
        {
            logger = _logger;

            if (logger is null)
            {
                _deferredEvents.Add(new LogEvent(level, eventId, message, exception));
                return;
            }
        }

        LogCore(logger, level, eventId, ref message, exception);
    }

    private static void LogCore(ILogger logger, ConfigCat.Client.LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception)
    {
        var logLevel = level switch
        {
            ConfigCat.Client.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
            ConfigCat.Client.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
            ConfigCat.Client.LogLevel.Info => Microsoft.Extensions.Logging.LogLevel.Information,
            ConfigCat.Client.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
            _ => Microsoft.Extensions.Logging.LogLevel.None
        };

        logger.Log(logLevel, eventId.Id, state: new LogValues(message), exception, LogValues.Formatter);
    }

    // Support for structured logging.
    private struct LogValues(in FormattableLogMessage message) : IEnumerable<KeyValuePair<string, object?>>
    {
        public static readonly Func<LogValues, Exception?, string> Formatter = (state, _) => state.ToString();

        private FormattableLogMessage _message = message;

        public readonly IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            var argNames = _message.ArgNames;
            var argValues = _message.ArgValues;

            for (var i = 0; i < argNames.Length; i++)
            {
                yield return new KeyValuePair<string, object?>(argNames[i], argValues[i]);
            }

            yield return new KeyValuePair<string, object?>("{OriginalFormat}", _message.Format);
        }

        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => this._message.InvariantFormattedMessage;
    }

    private struct LogEvent(ConfigCat.Client.LogLevel level, LogEventId eventId, in FormattableLogMessage message, Exception? exception = null)
    {
        public readonly ConfigCat.Client.LogLevel Level = level;
        public readonly LogEventId EventId = eventId;
        public FormattableLogMessage Message = message;
        public readonly Exception? Exception = exception;
    }
}
