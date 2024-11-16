# Semantic Kernel Notebooks

This repository contains a collection of Polyglot Notebooks and supporting resources designed to help you explore and utilise [Semantic Kernel](https://github.com/microsoft/semantic-kernel), an open-source SDK by Microsoft that simplifies integrating Large Language Models (LLMs) such as OpenAI, Azure OpenAI and Hugging Face into applications.

The `notebooks/semantic-kernel` folder focuses on demonstrating core capabilities of Semantic Kernel, including plugin orchestration, multi-model support and AI-powered task automation.

## 📂 Folder Structure

### `dotnet/`
The `dotnet` folder contains Polyglot Notebooks showcasing how to use Semantic Kernel with .NET. These notebooks guide you through setting up Semantic Kernel, running AI functions and building intelligent applications.

#### Key Notebooks:
1. **Getting Started**: Learn how to configure and initialize Semantic Kernel with C#.
2. **Prompt Execution**: Execute prompts stored in files and interact with LLMs seamlessly.
3. **Dynamic Semantic Functions**: Create and manage Semantic Functions at runtime.
4. **Advanced Interactions**:
   - Implement chat experiences using Kernel Arguments.
   - Integrate function calling to perform specific tasks.
5. **Working with Vector Stores**: Explore embeddings and use vector databases.
6. **Image Generation**: Create images with DALL-E 3.
7. **Search Integration**: Use Bing Search via Kernel.

### Common Configurations
Ensure the following before running the notebooks:
- **Set API Keys**: Add your OpenAI or Azure OpenAI API credentials to `config/settings.json`.
- **Install Dependencies**:
  - .NET 8 SDK
  - Visual Studio Code with the [Polyglot extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode).

## 🛠 Running the Notebooks

### Visual Studio Code
1. Open the repository folder in VS Code.
2. Install the Polyglot Notebook extension.
3. Open a notebook and execute the cells to setup Azure OpenAI / OpenAI credentials

## 🌟 Why Semantic Kernel?
Semantic Kernel is ideal for developers seeking:
- **Flexibility**: Multi-language support (C#, Python, Java).
- **Modularity**: Easy integration of custom plugins and models.
- **Efficiency**: Simplified orchestration of AI-driven workflows.
- **Scalability**: Enterprise-ready features with robust observability.

For more details, visit the [official Semantic Kernel repository](https://github.com/microsoft/semantic-kernel).

---

Let me know if you need further refinements or additional details!
