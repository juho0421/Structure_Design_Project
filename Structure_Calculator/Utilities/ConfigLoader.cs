using Microsoft.Extensions.Configuration;

public static class ConfigLoader
{
    private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

    public static string GetOpenAIKey() => Configuration["OpenAI:ApiKey"];
    public static string GetMongoConnectionString() => Configuration["MongoDB:ConnectionString"];
}
