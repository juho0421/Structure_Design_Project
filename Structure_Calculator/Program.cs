using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    private static string currentMode = "chat";
    private static ChatHistory chatHistory = new ChatHistory();

    static async Task Main(string[] args)
    {
        var kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion("gpt-4o", ConfigLoader.GetOpenAIKey())
            .Build();

        var dbHandler = new MongoDBHandler(ConfigLoader.GetMongoConnectionString(), "DesignStandard_DB");
        var structuralPlugin = new StructuralPlugin(dbHandler);
        var updatePlugin = new DataUpdatePlugin(dbHandler);

        kernel.Plugins.AddFromObject(structuralPlugin);
        kernel.Plugins.AddFromObject(updatePlugin);

        var chatService = kernel.Services.GetRequiredService<IChatCompletionService>();
        chatHistory.AddSystemMessage("You are a helpful AI assistant capable of not only general conversation, but also structural engineering design calculation.");
        Console.WriteLine("Structural Design AI. Use '!mode design' to switch to design mode or '!mode chat' for general chat. Type 'exit' to quit.");

        while (true)
        {
            Console.Write($"\n[{currentMode.ToUpper()} MODE] User: ");
            var userInput = Console.ReadLine();
            if (userInput?.ToLower() == "exit") break;

            string response;

            if (userInput.StartsWith("!mode", StringComparison.OrdinalIgnoreCase))
            {
                response = HandleModeSwitch(userInput);
            }
            else if (currentMode == "design")
            {
                response = await HandleDesignModeCommand(userInput, dbHandler, structuralPlugin, updatePlugin);
            }
            else
            {
                response = await GetGeneralChatResponse(chatService, userInput);
            }

            Console.WriteLine($"\nAI: {response}");

            chatHistory.AddUserMessage(userInput);
            chatHistory.AddAssistantMessage(response);
        }
    }

    static string HandleModeSwitch(string command)
    {
        if (command.Equals("!mode design", StringComparison.OrdinalIgnoreCase))
        {
            currentMode = "design";
            return GetDesignModeWelcomeMessage();
        }
        else if (command.Equals("!mode chat", StringComparison.OrdinalIgnoreCase))
        {
            currentMode = "chat";
            return "Switched to General Chat Mode.";
        }
        return "Unknown mode. Use '!mode design' or '!mode chat'.";
    }

    static string GetDesignModeWelcomeMessage()
    {
        return """
        [Structural Design Mode]
        Available commands:
        - check element all
        - check element [elementName]
        - check element [elementName] [propertyName]
        - edit element [elementName] [propertyName] [value]
        - check formula all
        - calculate [elementName] [formulaName]

        Type 'exit' to leave this mode.
        """;
    }

    static async Task<string> HandleDesignModeCommand(string command, MongoDBHandler dbHandler, StructuralPlugin structuralPlugin, DataUpdatePlugin updatePlugin)
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 3 && parts[0].Equals("check", StringComparison.OrdinalIgnoreCase) && parts[1].Equals("element", StringComparison.OrdinalIgnoreCase) && parts[2].Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return await CheckAllElements(dbHandler);
        }
        else if (parts.Length == 3 && parts[0].Equals("check", StringComparison.OrdinalIgnoreCase) && parts[1].Equals("element", StringComparison.OrdinalIgnoreCase))
        {
            return await CheckElementProperties(dbHandler, parts[2]);
        }
        else if (parts.Length == 4 && parts[0].Equals("check", StringComparison.OrdinalIgnoreCase) && parts[1].Equals("element", StringComparison.OrdinalIgnoreCase))
        {
            return await CheckElementPropertyValue(dbHandler, parts[2], parts[3]);
        }
        else if (parts.Length == 5 && parts[0].Equals("edit", StringComparison.OrdinalIgnoreCase))
        {
            return await updatePlugin.UpdateElementProperty(parts[2], parts[3], double.Parse(parts[4]));
        }
        else if (parts.Length == 3 && parts[0].Equals("check", StringComparison.OrdinalIgnoreCase) && parts[1].Equals("formula", StringComparison.OrdinalIgnoreCase) && parts[2].Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return CheckAllFormulas();
        }
        else if (parts.Length == 3 && parts[0].Equals("calculate", StringComparison.OrdinalIgnoreCase))
        {
            return await structuralPlugin.GetDesignCapacity(parts[1], parts[2]);
        }
        return "Invalid command. Check available commands.";
    }

    static async Task<string> CheckAllElements(MongoDBHandler dbHandler)
    {
        var elements = await dbHandler.GetAllElementNames();
        return "Available elements:\n- " + string.Join("\n- ", elements);
    }

    static async Task<string> CheckElementProperties(MongoDBHandler dbHandler, string elementName)
    {
        var properties = await dbHandler.GetElementPropertiesAsync(elementName);
        if (properties == null) return $"{elementName} does not exist.";
        return $"{elementName} properties:\n- " + string.Join("\n- ", properties.Select(p => $"{p.Name} ({p.Unit})"));
    }

    static async Task<string> CheckElementPropertyValue(MongoDBHandler dbHandler, string elementName, string propertyName)
    {
        var properties = await dbHandler.GetElementPropertiesAsync(elementName);
        if (properties == null) return $"{elementName} does not exist.";

        var property = properties.FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        if (property == null) return $"{elementName} does not contain '{propertyName}'.";

        return $"{elementName} - {property.Name}: {property.Value} {property.Unit}";
    }

    static string CheckAllFormulas()
    {
        return "Available formulas:\n- KDS_Compression_Capacity\n- CSA_Compression_Capacity\n- KDS_Shear_Capacity\n- CSA_Shear_Capacity";
    }


    static async Task<string> GetGeneralChatResponse(IChatCompletionService chatService, string userInput)
    {
        var result = await chatService.GetChatMessageContentAsync(chatHistory);
        return result.Content;
    }
}
