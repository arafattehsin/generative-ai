#pragma warning disable SKEXP0070
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0020

using local_rag_sk.Helpers;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.KernelMemory.SemanticKernel;
using Microsoft.KernelMemory.AI;

// Your PHI-3 model location 
var phi3modelPath = @"D:\models\Phi-3.5-mini-instruct-onnx\cpu_and_mobile\cpu-int4-awq-block-128-acc-level-4";
var bgeModelPath = @"D:\models\bge-micro-v2\onnx\model.onnx";
var vocabPath = @"D:\models\bge-micro-v2\vocab.txt";

// Load the model and services
var builder = Kernel.CreateBuilder();
builder.AddOnnxRuntimeGenAIChatCompletion("phi-3", phi3modelPath);
builder.AddBertOnnxTextEmbeddingGeneration(bgeModelPath, vocabPath);

// Build Kernel
var kernel = builder.Build();

// Create services such as chatCompletionService and embeddingGeneration
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

var config = new SemanticKernelConfig(); ;
var memory = new KernelMemoryBuilder()
    .WithSemanticKernelTextGenerationService(new OnnxRuntimeGenAITextCompletionService("phi-3", phi3modelPath), config, new DefaultGPTTokenizer())
    .WithSemanticKernelTextEmbeddingGenerationService(embeddingGenerator, config, new DefaultGPTTokenizer())
    .WithSimpleVectorDb()
    .Build<MemoryServerless>();

var memoryPlugin = kernel.ImportPluginFromObject(new MemoryPlugin(memory, waitForIngestionToComplete: true), "memory");

await memory.ImportWebPageAsync("https://raw.githubusercontent.com/arafattehsin/ParkingGPT/main/README.md", documentId: "doc001"); 
await memory.ImportDocumentAsync($"Documents/HR Policy.docx");

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("""
 
  _                    _   ____      _    ____            _ _   _       __  __                           _           
 | |    ___   ___ __ _| | |  _ \    / \  / ___| __      _(_) |_| |__   |  \/  | ___ _ __ ___   ___  _ __(_) ___  ___ 
 | |   / _ \ / __/ _` | | | |_) |  / _ \| |  _  \ \ /\ / / | __| '_ \  | |\/| |/ _ \ '_ ` _ \ / _ \| '__| |/ _ \/ __|
 | |__| (_) | (_| (_| | | |  _ <  / ___ \ |_| |  \ V  V /| | |_| | | | | |  | |  __/ | | | | | (_) | |  | |  __/\__ \
 |_____\___/ \___\__,_|_| |_| \_\/_/   \_\____|   \_/\_/ |_|\__|_| |_| |_|  |_|\___|_| |_| |_|\___/|_|  |_|\___||___/
                                                                                                    by Arafat Tehsin              
""");


// Start the conversation
while (true)
{
    // Get user input
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("User > ");
    var question = Console.ReadLine()!;

    // Settings for the Phi-3 execution
    OpenAIPromptExecutionSettings executionSettings = new()
    {
        ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions,
        MaxTokens = 100
    };

    // Invoke the kernel with the user input
    var response = kernel.InvokePromptStreamingAsync(
        promptTemplate: @"Question: {{$input}}
        Answer the question using the memory content: {{memory.ask $input}}",
        arguments: new KernelArguments(executionSettings)         
        {
            { "input", question }
        }
        );

    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("\nAssistant > ");

    //var result = response.Result.GetValue<string>();
    string combinedResponse = string.Empty;
    await foreach (var message in response)
    {
        //Write the response to the console
        Console.Write(message);
        combinedResponse += message;
    }

    Console.WriteLine();
}