// Copyright (c) Microsoft. All rights reserved.

// Test Azure AI Project connection with Azure Foundry

using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;

Console.WriteLine("Testing Azure AI Project Connection...");
Console.WriteLine();

// Azure Foundry Project Configuration
string endpoint = "https://openai-swdn.services.ai.azure.com/api/projects/openai-swdn-project";
string deploymentName = "gpt-5";

Console.WriteLine($"Endpoint: {endpoint}");
Console.WriteLine($"Model: {deploymentName}");
Console.WriteLine();

try
{
    const string TestAgentName = "PolicyPackTestAgent";

    // Get a client to create/retrieve/delete server side agents with Azure Foundry Agents.
    Console.WriteLine("Creating AIProjectClient with AzureCliCredential...");
    AIProjectClient aiProjectClient = new(new Uri(endpoint), new AzureCliCredential());
    Console.WriteLine("✓ AIProjectClient created successfully");
    Console.WriteLine();

    // Define the agent you want to create. (Prompt Agent in this case)
    Console.WriteLine($"Creating server-side agent '{TestAgentName}'...");
    var agentVersionCreationOptions = new AgentVersionCreationOptions(
        new PromptAgentDefinition(model: deploymentName)
        {
            Instructions = "You are a helpful assistant that extracts facts from text."
        });

    // Azure.AI.Agents SDK creates and manages agent by name and versions.
    var createdAgentVersion = aiProjectClient.Agents.CreateAgentVersion(
        agentName: TestAgentName,
        options: agentVersionCreationOptions);

    Console.WriteLine($"✓ Agent version created: {createdAgentVersion.Value.Id}");
    Console.WriteLine();

    // You can use an AIAgent with an already created server side agent version.
    AIAgent testAgent = aiProjectClient.AsAIAgent(createdAgentVersion.Value);

    Console.WriteLine("✓ AIAgent created successfully");
    Console.WriteLine();

    // Once you have the AIAgent, you can invoke it like any other AIAgent.
    Console.WriteLine("Testing agent with a sample prompt...");
    AgentSession session = await testAgent.GetNewSessionAsync();

    string testPrompt = "Extract facts from: John Smith filed a claim for water damage on January 15, 2024.";
    Console.WriteLine($"Prompt: {testPrompt}");
    Console.WriteLine();

    Console.WriteLine("Agent Response:");
    Console.WriteLine(await testAgent.RunAsync(testPrompt, session));
    Console.WriteLine();

    // Cleanup by agent name removes the agent version created.
    Console.WriteLine("Cleaning up test agent...");
    aiProjectClient.Agents.DeleteAgent(TestAgentName);
    Console.WriteLine("✓ Test agent deleted");
    Console.WriteLine();

    Console.WriteLine("✅ ALL TESTS PASSED - Azure AI Project connection works!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ ERROR: {ex.GetType().Name}");
    Console.WriteLine($"Message: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner: {ex.InnerException.Message}");
    }
    Console.WriteLine();
    Console.WriteLine("Stack Trace:");
    Console.WriteLine(ex.StackTrace);
}
