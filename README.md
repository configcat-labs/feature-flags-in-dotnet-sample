# Feature flags in an ASP.NET Core application sample app

**[Read the blog post here](https://configcat.com/blog/2021/10/10/aspnetcore-options-pattern/)**

This sample app demonstrates how to use feature flags in an ASP.NET Core (.NET 10) application with ConfigCat's .NET SDK. It shows how to read your feature flags through the [Options pattern](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options), the same way you would read values from `appsettings.json` or environment variables.

ConfigCat is registered as a configuration source in `Program.cs` (the SDK Key is read from `appsettings.json`), so ConfigCat settings behave like any other .NET configuration values. Settings named with a double underscore, such as `Application__MyFeatureFlag`, map to nested configuration keys such as `Application:MyFeatureFlag`. Values are reloaded automatically when you change them in the ConfigCat Dashboard.

When a GET request is sent to the root endpoint, the app logs the values of `MyFeatureFlag` and `MyTextSetting`. If the feature flag is on, the response shows the value of `MyTextSetting`. Otherwise, it shows a message saying that the flag is off.

## Build & Run

### Prerequisites

- The [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) installed.
- A [ConfigCat account](https://app.configcat.com/auth/signup) and the SDK Key for your config.
- The [ConfigCat .NET SDK version 10.0.0](https://www.nuget.org/packages/ConfigCat.Client/10.0.0) (restored automatically from NuGet when you build the project).
- A code editor, such as [Visual Studio Code](https://code.visualstudio.com/) with the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension.
- [Git](https://git-scm.com/downloads) installed to clone this sample project.

### Building and running the project

1. Clone this sample project.

    ```bash
    git clone https://github.com/configcat-labs/feature-flags-in-dotnet-sample.git
    cd feature-flags-in-dotnet-sample
    ```

2. In your [ConfigCat Dashboard](https://app.configcat.com), create the following settings:

    | Key | Type | Description |
    | --- | --- | --- |
    | `Application__MyFeatureFlag` | Feature Flag (boolean) | Controls whether the text setting is revealed. |
    | `Application__MyTextSetting` | Text | The value shown when the feature flag is on. |

3. In development, add your ConfigCat SDK Key to `appsettings.Development.json`, or to `appsettings.json` for production. Replace the `SdkKey` placeholder under `ConfigCat:ConfigurationSource`.

4. Run the app.

    ```bash
    dotnet run
    ```

5. Open [http://localhost:5041](http://localhost:5041) in your browser. If `Application__MyFeatureFlag` is on, the page shows the value of `Application__MyTextSetting`. Otherwise, it shows a message saying that the flag is off.

6. Toggle the feature flag ON and OFF in the ConfigCat Dashboard, then refresh the page. The app will check for changes every 5 seconds in Development and every 60 seconds by default.

## Learn more

Useful links to technical resources.

- [ConfigCat .NET SDK documentation](https://configcat.com/docs/sdk-reference/dotnet/)
- [Options pattern in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)
- [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)

[**ConfigCat**](https://configcat.com) supports many other frameworks and languages. Check out the full list of supported SDKs [here](https://configcat.com/docs/sdk-reference/overview/).

You can also explore other code samples for various languages, frameworks, and topics in [ConfigCat labs](https://github.com/configcat-labs) on GitHub.

Keep up with ConfigCat on [X](https://x.com/configcat), [Facebook](https://www.facebook.com/configcat), [LinkedIn](https://www.linkedin.com/company/configcat/), [GitHub](https://github.com/configcat), and the [News & Product Updates](https://configcat.com/docs/news/) page.

## Author

[adams85](https://github.com/adams85)

## Contributions

Contributions are welcome!
