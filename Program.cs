using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SK_project3;
using System.Text.Json.Serialization;


// dotnet add package Microsoft.SemanticKernel.Connectors.Ollama --prerelease //
// dotnet add package Microsoft.SemanticKernel.Connectors.OpenAI //


class Program
{
    static async Task Main(string[] args)
    {
        // Kernel 초기화
        #pragma warning disable SKEXP0070
        var builder = Kernel.CreateBuilder();
        builder.AddOllamaChatCompletion("llava", new Uri("http://localhost:11434"));
        Kernel kernel = builder.Build();

        var chatService = kernel.GetRequiredService<IChatCompletionService>();


        // Add a plugin
        kernel.Plugins.AddFromType<LightsPlugin>("Lights");

        // 플러그인 연결 확인 코드
        Console.WriteLine("=== Checking Plugins ===");
        foreach (var plugin in kernel.Plugins)
        {
            Console.WriteLine($"Plugin Name: {plugin.Name}");
            Console.WriteLine("Functions:");
            var metadata = plugin.GetFunctionsMetadata();
            foreach (var function in metadata)
            {
                Console.WriteLine($"  - {function.Name}");
            }
        }
        Console.WriteLine("=====================");

        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        // 시스템 메시지 추가 및 실행 설정
        var history = new ChatHistory();

        history.AddSystemMessage(@"
        You are a helpful assistant that can control lights. 
        You have access to the 'Lights' plugin, which allows you to:
        - 'get_lights': Retrieve a list of all lights and their current state.
        - 'get_state': Get the state of a specific light by its ID.
        - 'change_state': Change the state of a specific light by providing its ID and new state.

        If the user asks about lights, use the appropriate function from the 'Lights' plugin.");

        while (true)
        {
            Console.Write("[You] ");
            string userQuestion = Console.ReadLine();

            if (string.Equals(userQuestion, "exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(userQuestion))
            {
                Console.WriteLine("[AI] Please enter a valid question.");
                continue;
            }

            history.AddUserMessage(userQuestion);

            // 여기에 try-catch 블록 추가
            try
            {
                var result = await chatService.GetChatMessageContentAsync(
                    history,
                    executionSettings: settings,
                    kernel: kernel);

                // 함수 호출 결과 검증
                if (result.Metadata?.TryGetValue("ToolCalls", out var toolCalls) == true)
                {
                    Console.WriteLine("Tool calls detected:");
                    Console.WriteLine(toolCalls);
                }
                else
                {
                    Console.WriteLine("No tool calls detected in the response");
                }

                // AI 응답 출력
                var responseContent = result.Content ?? "I apologize, but I couldn't generate a response."; // AI 응답이 없을 경우
                Console.WriteLine($"[AI] {responseContent}");
                history.AddAssistantMessage(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling GetChatMessageContentAsync: {ex.Message}");
            }
        }
    Console.WriteLine("Exit Program.");
    }

}
