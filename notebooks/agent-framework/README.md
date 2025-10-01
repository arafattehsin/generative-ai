# Microsoft Agent Framework - Getting Started

This folder contains notebooks and resources to help you get started with the **Microsoft Agent Framework**, an open-source .NET library in the Microsoft.Extensions.AI family for building reliable AI agents and multi-agent workflows.

## 📚 What's Inside

### Notebooks

- **[agentframework-101.dib](./dotnet/agentframework-101.dib)** - A comprehensive starter guide covering:
  - Single-agent chat and stateful threads
  - Function tools and human-in-the-loop approvals
  - Structured output, persistence, and multimodal capabilities
  - Agent-as-Function composition
  - Custom memory with multi-user personalization

### Resources

- **parking-sign.jpg** - Sample image for multimodal agent demonstrations

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 or later
- Azure OpenAI service endpoint and credentials
- Azure CLI (for `AzureCliCredential` authentication)

### Environment Setup

Before running the notebooks, configure the following environment variables:

```powershell
$env:AOI_ENDPOINT_SWDN = "https://<your-resource>.openai.azure.com"
$env:AOI_KEY_SWDN = "<your-api-key>"  # Optional if using Azure CLI auth
```

For Azure CLI authentication, run:

```powershell
az login
```

### Running the Notebooks

1. Open the `.dib` notebook in Visual Studio Code with the Polyglot Notebooks extension installed
2. Ensure your environment variables are set
3. Run the cells sequentially to explore different agent capabilities

## 🎯 Key Concepts Covered

- **AI Agents** - Create intelligent agents with specific instructions and personalities
- **Stateful Conversations** - Use `AgentThread` to maintain context across multiple interactions
- **Function Tools** - Enable agents to call external functions and APIs
- **Human-in-the-Loop** - Implement approval workflows for sensitive operations
- **Multimodal Support** - Work with images and other media types
- **Memory Management** - Persist conversations and personalize agent responses

## 📖 Learn More

- **Official Repository**: [github.com/microsoft/agent-framework](https://github.com/microsoft/agent-framework)
- **Blog Post**: [What is the Microsoft Agent Framework?](https://arafattehsin.com/what-is-the-microsoft-agent-framework)

## 💡 What is Microsoft Agent Framework?

Microsoft Agent Framework provides clear primitives for:

- Composing agents
- Calling tools
- Managing memory
- Persisting conversations
- Building multi-agent workflows

It builds on lessons from Microsoft Semantic Kernel and AutoGen, providing a forward-looking, unified path for agentic development at Microsoft.

## 🤝 Contributing

Contributions are welcome! Feel free to submit issues or pull requests to improve these examples.

## 📝 Author

**Arafat Tehsin**

For more insights and tutorials, visit my blog at [arafattehsin.com](https://arafattehsin.com)

---

_Note: This notebook uses preview packages. Package versions and APIs may change as the framework evolves._
