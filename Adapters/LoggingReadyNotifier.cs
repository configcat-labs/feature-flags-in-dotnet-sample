namespace ConfigCatInDotnetSample.Adapters;

public sealed class LoggingReadyNotifier
{
    public void Notify(ILoggerFactory loggerFactory) => Event?.Invoke(loggerFactory);

    public event Action<ILoggerFactory>? Event;
}
