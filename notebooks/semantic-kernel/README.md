# Agentic AI with Semantic Kernel
This folder contains a .NET based notebooks showcasing how to integrate with [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel) from the scratch. It provides various helpers, plugins and notebooks to help you quickly get started with building AI-driven applications with the enterprises using .NET ecosystem.

## 📜 Table of Contents

- [🔍Overview](#-overview)
- [📂Project Structure](#-project-structure)
- [🛠Prerequisites](#-prerequisites)
- [⚡Setup & Usage](#-setup--usage)
  - [▶Getting the Project](#-getting-the-project)
  - [📖Running the Notebooks](#-running-the-notebooks)
- [🔌Plugins](#-plugins)
- [📓Notebooks](#-notebooks)
- [⚙Configuration](#-configuration)
- [🤝Contributing](#-contributing)
- [📜License](#-license)

---

## 🔍 Overview

Semantic Kernel is an SDK that helps developers mix traditional programming with Large Language Models (LLMs). This project demonstrates how you can leverage Semantic Kernel in .NET applications, using helper classes, plugins and interactive (Polyglot) notebooks.

## 📂 Project Structure

Within this folder, you will find:

```plaintext
dotnet/
├── Helpers/
│   ├── GettingStarted.cs
│   ├── JsonResultTranslator.cs
│   ├── Settings.cs
│   └── Utilities.cs
├── Plugins/
│   ├── BudgetAdvisor/
│   ├── CityPlugin/
│   ├── FlightTrackerPlugin/
│   ├── FoodPlugin/
│   ├── GuessPlugin/
│   └── ShoppingPlugin/
├── getting-started.ipynb
├── settings.json
├── sk-agents-01.dib
├── README.md
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

- **getting-started.ipynb**  
  - Open this Jupyter Notebook with Visual Studio Code.
  - Execute cells to explore how Semantic Kernel can be used for prompt engineering and basic AI-driven workflows.

- **sk-agents-01.dib**  
  - Open this Jupyter Notebook with Visual Studio Code.
  - Execute the cells to see advanced scenarios of agent-based interactions with Semantic Kernel.

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

As of now, two notebooks are included for you to get started:

1. **getting-started.ipynb**  
   - A Jupyter Notebook that covers the fundamentals of Semantic Kernel usage.  
   - Shows how to set up prompts, connect to AI services, and run basic operations.
   - This also covers the multimodal capabilities 

2. **sk-agents-01.dib**  
   - A .NET Interactive Notebook for more advanced use cases.  
   - Demonstrates agent-based workflows and how to orchestrate multiple AI calls in a single pipeline.
   - This also covers the multi-model, multi-agent scenarios.

3. **sk-agents-02.dib** (Coming soon)
   - This will uncover some other latest capabilities. Currently work is in progress. 🚧

Feel free to create additional notebooks or modify these to suit your needs.

---

## ⚙ Configuration

- **settings.json** and **Settings.cs** hold your keys and other configuration details. Make sure to fill these with your own credentials (e.g., Azure OpenAI or OpenAI API keys) before running the project.

- **Environment Variables**: If you prefer not to store credentials in plain text, you can configure your environment variables and adjust `Settings.cs` to read from them.

---

## 🤝 Contributing

Contributions are welcome! If you have ideas for new plugins or improvements to the existing code, feel free to open an issue or submit a pull request. Please follow the repository’s guidelines and code of conduct.

---

## 📜 License

This project is licensed under the [MIT License](https://github.com/arafattehsin/generative-ai/blob/main/LICENSE). You are free to use, modify, and distribute this software as outlined in the license terms.

---

**⭐ If this has helped you then please don't forget to leave a star**

