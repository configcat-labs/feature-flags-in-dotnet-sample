using ConfigCatInDotnetSample.Adapters;
using ConfigCatInDotnetSample.Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder();

var loggingReadyNotifier = new LoggingReadyNotifier();

// Add ConfigCat as a configuration source (so it takes priority over all default configuration sources;
// to adjust priority, inspect builder.Configuration.Sources and use InsertConfigCat to insert the source
// at the right position; see also https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host#host-builder-settings)
builder.Configuration.AddConfigCat(source =>
{
    // Populate options from the currently available configuration (e.g., from appsettings.json)
    builder.Configuration.GetSection("ConfigCat:ConfigurationSource").Bind(source);

    // Enable case-insensitive matching for ConfigCat SDK Keys, which are case-sensitive by default
    source.CaseInsensitiveKeys = true;

    // Enable automatic reload of settings when the underlying polling loop detects changes to config data
    source.ReloadOnChange = true;

    // Enable the use of the MS logging infrastructure (for details, see ConfigCatToMSLoggerAdapter)
    source.LoggingReadyNotifier = loggingReadyNotifier;
});

// Register an options class that will be populated from configuration
builder.Services.Configure<ApplicationOptions>(builder.Configuration.GetSection("Application"));

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Build the application instance along with its underlying DI container
var app = builder.Build();

// Notify the ConfigCat configuration provider that the logging infrastructure is now available
loggingReadyNotifier.Notify(app.Services.GetRequiredService<ILoggerFactory>());

// Configure the request pipeline
app.UseHttpsRedirection();

// Define a route that handles GET requests to the root path
app.MapGet("/", async (HttpContext context, IOptionsSnapshot<ApplicationOptions> options, ILogger<Program> logger) =>
{
    bool featureFlagValue;
    string? textSettingValue;

    try
    {
        featureFlagValue = options.Value.MyFeatureFlag;
        textSettingValue = options.Value.MyTextSetting ?? "<null>";
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error while retrieving setting values.");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync("Internal Server Error");
        return;
    }

    logger.LogInformation($"{nameof(ApplicationOptions.MyFeatureFlag)} is set to: {featureFlagValue}");
    logger.LogInformation($"{nameof(ApplicationOptions.MyTextSetting)} is set to: {textSettingValue}");

    context.Response.StatusCode = StatusCodes.Status200OK;
    if (featureFlagValue)
    {
        await context.Response.WriteAsync(textSettingValue);
    }
    else
    {
        await context.Response.WriteAsync($"{nameof(ApplicationOptions.MyFeatureFlag)} is off. Turn it on to reveal the value of {nameof(ApplicationOptions.MyTextSetting)}.");
    }
});

// Launch the application
app.Run();
