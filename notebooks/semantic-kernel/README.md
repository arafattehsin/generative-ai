# Semantic Kernel Agent Framework & Azure AI Foundry Agent Service
This folder contains .NET-based notebooks showcasing how to integrate with [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel) from scratch. It provides various helpers, plugins, and notebooks to help you quickly get started with building AI-driven applications for enterprises using the .NET ecosystem.

`Disclaimer: The documentation has been modified using AI with the latest Agent mode in GitHub Copilot.`

## 📜 Table of Contents

- [🔍 Overview](#-overview)
- [📂 Project Structure](#-project-structure)
- [🛠 Prerequisites](#-prerequisites)
- [⚡ Setup & Usage](#-setup--usage)
  - [▶ Getting the Project](#-getting-the-project)
  - [📖 Running the Notebooks](#-running-the-notebooks)
- [🔌 Plugins](#-plugins)
- [📓 Notebooks](#-notebooks)
- [📂 Resources](#-resources)
- [⚙ Configuration](#-configuration)
- [🤝 Contributing](#-contributing)
- [📜 License](#-license)

---

## 🔍 Overview

Semantic Kernel is an SDK that helps developers mix traditional programming with Large Language Models (LLMs). This project demonstrates how you can leverage Semantic Kernel in .NET applications, using helper classes, plugins, and interactive (Polyglot) notebooks.

It also features the latest Agent Framework as well as the Azure AI Foundry Service to work hand-in-hand with Semantic Kernel.

## 📂 Project Structure

Within this folder, you will find:

```plaintext
dotnet/
├── Helpers/
│   ├── GettingStarted.cs
│   ├── JsonResultTranslator.cs
│   ├── Settings.cs
│   └── Utilities.cs
├── settings.json
├── aifoundry-agents.dib
├── semantickernel-101.ipynb
├── semantickernel-agents.dib
└── README.md
Plugins/
├── BudgetAdvisor/
├── CityPlugin/
├── FlightTrackerPlugin/
├── FoodPlugin/
├── GuessPlugin/
└── ShoppingPlugin/
Resources/
├── agent_performance_metrics.txt
├── HiEnterprise Troubleshooting Guide.docx
├── historical_customer_interactions.txt
├── HR Policies.md
├── Leave and Attendance Records.pdf
├── Policy Document.pdf
└── sales_data.txt
```

---

## 🛠 Prerequisites

- [.NET 8 or later](https://dotnet.microsoft.com/download)
- [Visual Studio Code](https://code.visualstudio.com/)
- [Polyglot Notebooks](https://github.com/dotnet/interactive)

## ⚡ Setup & Usage

### ▶ Getting the Project

1. **Clone the Repository**  
   ```sh
   git clone https://github.com/arafattehsin/generative-ai.git
   cd generative-ai/notebooks/semantic-kernel/dotnet
   ```

### 📖 Running the Notebooks

- **semantickernel-101.ipynb**  
  - A quickstart notebook for Semantic Kernel basics and simple LLM calls.

- **semantickernel-agents.dib**  
  - Explore multi-agent orchestration, Copilot Studio integration, and advanced agent workflows.

- **aifoundry-agents.dib**  
  - Dive deeper into Azure AI Foundry Service and persistent, specialized AI agents.

---

## 🔌 Plugins

This project includes several example plugins, each within its own folder under `Plugins`. They demonstrate how to extend Semantic Kernel with custom functionality, such as:

- **BudgetAdvisor**: Manage financial data or plan budgets using LLMs.  
- **CityPlugin**: Provide city-specific information, possibly for travel or local insights.  
- **FlightTrackerPlugin**: Integrate flight data and track flight statuses.  
- **FoodPlugin**: Suggest recipes, meal planning, or nutritional advice.  
- **GuessPlugin**: A playful plugin that uses LLMs for guesswork or Q&A.  
- **ShoppingPlugin**: Offers shopping-related features, such as product recommendations.

---

## 📓 Notebooks

As of now, three notebooks are included for you to get started:

1. **semantickernel-101.ipynb**  
   - Covers the fundamentals of Semantic Kernel usage, prompt engineering, and basic LLM operations.  
   - Demonstrates how to set up prompts, connect to AI services, and run basic operations.  
   - Also covers multimodal capabilities.

2. **aifoundry-agents.dib**  
   - Dives deeper into Azure AI Foundry Service.  
   - Explores creating persistent, specialized AI agents with memory and personalized user interactions.  
   - Demonstrates advanced use cases like weather forecasting, food ordering, HR policy search, and data visualization.

3. **semantickernel-agents.dib**  
   - Demonstrates advanced agent-based workflows and multi-agent orchestration.  
   - Shows Copilot Studio agent integration and complex agent scenarios.

Feel free to create additional notebooks or modify these to suit your needs.

---

## 📂 Resources

This project also includes a `Resources` folder containing various files that are used by the notebooks and agents. These resources provide data and context for the AI agents to process and analyze. Here's a quick overview of the files:

- **agent_performance_metrics.txt**: Contains performance metrics for customer service agents.
- **HiEnterprise Troubleshooting Guide.docx**: A troubleshooting guide for resolving common customer issues.
- **historical_customer_interactions.txt**: Logs of past customer interactions for analysis.
- **HR Policies.md**: A markdown file detailing company HR policies.
- **Leave and Attendance Records.pdf**: A PDF document with employee leave and attendance data.
- **Policy Document.pdf**: A PDF file outlining company policies.
- **sales_data.txt**: A text file containing sales data for analysis.

These resources are utilized by various agents in the notebooks to demonstrate their capabilities, such as analyzing data, retrieving information, and providing insights.

---

## ⚙ Configuration

- **settings.json** and **Settings.cs** hold your keys and other configuration details. Make sure to fill these with your own credentials (e.g., Azure OpenAI or OpenAI API keys) before running the project.
- **Environment Variables**: If you prefer not to store credentials in plain text, you can configure your environment variables and adjust `Settings.cs` to read from them.
- You can add `settings.json` on your own by looking at the structure defined above.

---

## 🤝 Contributing

Contributions are welcome! If you have ideas for new plugins or improvements to the existing code, feel free to open an issue or submit a pull request. Please follow the repository’s guidelines and code of conduct.

---

## 📜 License

This project is licensed under the [MIT License](https://github.com/arafattehsin/generative-ai/blob/main/LICENSE). You are free to use, modify, and distribute this software as outlined in the license terms.

---

**⭐ If this has helped you then please don't forget to leave a star**

