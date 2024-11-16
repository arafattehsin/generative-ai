# Semantic Kernel for Developers (C#)

Get ready to unleash your inner AI wizard! This guide will take you on a journey from Prompt and Native functions to planners and connectors, and even includes Azure OpenAI's GPT-4 and DALL-E 3 models. Strap in, because we're about to turn up the natural language heat!

---

_Inspired by the great designers at the Microsoft OCTO (Semantic Kernel)_

## Prerequisites

To run this Polyglot notebook, ensure you have the following installed:

- [Visual Studio Code](https://code.visualstudio.com/Download)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download)
- [Polyglot Notebooks for VS Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode)

You'll also need an API key to access OpenAI models:

- [Azure OpenAI Service](https://learn.microsoft.com/en-us/azure/cognitive-services/openai/quickstart)

   - Access your API key using [these instructions](https://learn.microsoft.com/en-us/azure/cognitive-services/openai/reference).

### Model Deployments

Create the following deployments in your Azure OpenAI Service:

- `gpt-4` - GPT-4
- `dalle-3` - DALL-E 3
- `text-embedding-002` - text-embedding-002

Follow the [deployment guide](https://learn.microsoft.com/en-us/azure/cognitive-services/openai/how-to/create-resource?pivots=web-portal) for instructions.

---

## Getting Started

1. **Open the Notebook**: Launch the notebook in Visual Studio Code.

2. **Select the Kernel**: If prompted, select `.NET Interactive` as the kernel.

3. **Run a Test Cell**: Click the play button (`▶️`) on the left of the following code block to ensure everything is set up correctly:

   ```csharp
   Console.WriteLine("Microphone test. Check one. Two. Three.");
