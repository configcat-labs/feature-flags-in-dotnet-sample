using ConfigCatInDotnetSample.Adapters;

namespace Microsoft.Extensions.Configuration;

public static class ConfigCatConfigurationExtensions
{
    private static ConfigCatConfigurationSource CreateSourceWithInitializedProvider(IConfigurationBuilder builder, Action<ConfigCatConfigurationSource> configureSource)
    {
        var source = new ConfigCatConfigurationSource();
        configureSource(source);

        var provider = source.PreBuild(builder);

        // In general, sync-over-async should be avoided. However, this method will be called in the startup phase
        // of the application, so block waiting for a few seconds is usually acceptable. If not, you can consider
        // making the builder extension methods async or returning the initialization task and awaiting it manually.
        provider.Initialization.GetAwaiter().GetResult();

        return source;
    }

    /// <summary>
    /// Adds an <see cref="IConfigurationProvider"/> that loads configuration values from a ConfigCat config.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="configureSource">Configures the source.</param>
    /// <param name="notifyLoggingReady">A delegate that should be called as soon as the DI container is built and the logging infrastructure becomes available.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddConfigCat(this IConfigurationBuilder builder, Action<ConfigCatConfigurationSource> configureSource)
    {
        var source = CreateSourceWithInitializedProvider(builder, configureSource);
        return builder.Add(source);
    }

    /// <summary>
    /// Adds an <see cref="IConfigurationProvider"/> that loads configuration values from a ConfigCat config, inserted at the specified <see cref="index"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="index">The zero-based index at which the provider should be inserted.</param>
    /// <param name="configureSource">Configures the source.</param>
    /// <param name="notifyLoggingReady">A delegate that should be called as soon as the DI container is built and the logging infrastructure becomes available.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddConfigCat(this IConfigurationBuilder builder, int index, Action<ConfigCatConfigurationSource> configureSource)
    {
        var source = CreateSourceWithInitializedProvider(builder, configureSource);
        builder.Sources.Insert(index, source);
        return builder;
    }
}
