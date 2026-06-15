# Samples

This folder contains runnable Generative AI samples that demonstrate agent orchestration, Azure OpenAI, Semantic Kernel, Microsoft Agent Framework, Model Context Protocol, and full-stack application patterns.

Use this page as the starting catalog. Each sample has its own README with prerequisites, configuration, and run instructions.

## .NET Samples

The .NET samples cover enterprise agents, orchestration workflows, MCP servers, Semantic Kernel integrations, and Azure OpenAI application patterns.

| Sample                                                                         | Description                                                                                                                                                                                      |
| ------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| [A2A-CustomerService](dotnet/A2A-CustomerService/)                             | Multi-agent customer service system using A2A (Agent2Agent) Protocol with specialized agents (Front Desk, Billing, Technical, Orchestrator) built with Semantic Kernel and React frontend.       |
| [ConvertHeicMCP](dotnet/ConvertHeicMCP/)                                       | Model Context Protocol (MCP) server that exposes tools for converting HEIC image files to JPG or PNG formats.                                                                                    |
| [CustomCopilot](dotnet/CustomCopilot/)                                         | Semantic Kernel-based virtual assistant with plugins for flight tracking, weather updates, and place suggestions using Azure OpenAI.                                                             |
| [local-rag-sk](dotnet/local-rag-sk/)                                           | Local RAG (Retrieval-Augmented Generation) sample using Phi-3 ONNX model with Semantic Kernel and Kernel Memory for offline text completion and embeddings.                                      |
| [maf-concurrent-orchestration](dotnet/maf-concurrent-orchestration/)           | Concurrent agent orchestration using Microsoft Agent Framework with fan-out/fan-in pattern — three parallel expert reviewers (Security, Compliance, Finance) for enterprise customer onboarding. |
| [maf-groupchat-orchestration](dotnet/maf-groupchat-orchestration/)             | Chair-led group chat orchestration using Microsoft Agent Framework workflows, Microsoft Foundry agents, and Foundry Toolbox over MCP for employee onboarding.                                    |
| [maf-skill-incident-command-center](dotnet/maf-skill-incident-command-center/) | Supply chain incident triage using MAF Skills with progressive disclosure — file-based skill packages for incident triage and stakeholder communications.                                        |
| [maf-workflow-orchestration](dotnet/maf-workflow-orchestration/)               | **PolicyPack Builder** — sequential MAF workflow transforming draft policies into polished, compliant communications through a 6-step AI-powered pipeline.                                       |
| [microsoft-agent-sk](dotnet/microsoft-agent-sk/)                               | Microsoft 365 Agent SDK sample demonstrating integration with Semantic Kernel's Agent Framework.                                                                                                 |
| [openai-multimodal-chat](dotnet/openai-multimodal-chat/)                       | Multimodal chat samples featuring text-based and speech-enabled chat bots using Azure OpenAI and Azure Cognitive Speech Services.                                                                |
| [spend-dashboard](dotnet/spend-dashboard/)                                     | Finance spend control dashboard with Agent Framework backend featuring function tools and human-in-the-loop approvals for invoice status and payment release workflows.                          |
| [techmart-portal](dotnet/techmart-portal/)                                     | Enterprise-grade distributed multi-agent system using .NET Aspire orchestration with specialized A2A agents (Inventory, Finance, Support) and human-in-the-loop workflows.                       |

## Python Samples

The Python samples focus on Microsoft Agent Framework and OpenAI integration patterns.

| Sample                                                     | Description                                                                                                                                                                                                |
| ---------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [maf-chatkit-integration](python/maf-chatkit-integration/) | **SwiftRover** - A demo showcasing Microsoft Agent Framework integrated with OpenAI ChatKit, featuring real-time flight tracking (AviationStack API) and AI-powered parking sign analysis (GPT-4o Vision). |

## Notes

- Prefer the README inside each sample for the exact SDK version, package restore, secrets, and run commands.
- Samples that use Azure OpenAI or Microsoft Foundry require your own endpoint, model deployment, and authentication configuration.
- Microsoft Agent Framework samples restore SDK packages from NuGet unless a sample README explicitly says otherwise.
