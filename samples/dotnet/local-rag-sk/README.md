
# Semantic Kernel and Kernel Memory with Onnx Models 

This sample demonstrates how to use local Phi-3 ONNX variant with Semantic Kernel and Kernel Memory, all in .NET. This covers text completion and embeddings. 

For a detailed blog post, read [here](https://arafattehsin.com/ai-copilot-offline-phi3-semantic-kernel/) & here.

## Features

- **Phi-3 Model for Text Completion**: Utilises the `Phi-3-mini-4k-instruct` ONNX model to generate conversational responses.
- **BERT-based Text Embedding**: Uses the `bge-micro-v2` model to generate text embeddings for semantic memory.
- **Semantic Kernel Integration**: Builds a kernel to manage text completion and embedding services.
- **Kernel Memory Plugin**: The system uses a custom memory plugin to store and retrieve document data, supporting external memory imports (like web pages and documents).
# Sample Project

This project demonstrates the use of Microsoft Semantic Kernel and ONNX models for chat completion and text embedding generation.

## Usage

1. Clone the repository:
    ```sh
    git clone https://github.com/arafattehsin/generative-ai.git
    cd your-repo
    ```

2. Install the required packages:
    ```sh
    dotnet restore
    ```

3. Download the models and place them in the specified paths:
    - PHI-3 model: `D:\models\Phi-3-mini-4k-instruct-onnx\cpu_and_mobile\cpu-int4-rtn-block-32`
    - BGE model: `D:\models\bge-micro-v2\onnx\model.onnx`
    - Vocabulary file: `D:\models\bge-micro-v2\vocab.txt`

2.  **Run the Application**

		`dotnet build`
		`dotnet run`
    
    
3.  **Interact with the Console**
    
    Once the application is running, you'll be presented with a command-line interface where you can enter queries. The system will generate a response using the Phi-3 model, incorporating information stored in its memory.
       
    `User > What is the HR policy?` 
    
    The system will fetch the answer based on the imported document (`HR Policy.docx`) and display a model-generated response.
    

## How it Works

-   **Kernel Creation**: The code initializes a `Kernel` using `OnnxRuntimeGenAIChatCompletion` for the Phi-3 model and `BertOnnxTextEmbeddingGeneration` for BERT embeddings.
-   **Memory Setup (KM)**: A kernel memory object is built to store and retrieve information. It supports text generation and embeddings, storing the data in a simple vector database.
-   **Interactive Mode**: The user can enter questions in the console, which will invoke the kernel's prompt engine to generate responses. The system retrieves relevant information from memory (documents and web pages) before generating the response.

## Future Improvements

-   Implement a more sophisticated memory search mechanism.
-   Add support for additional document types and memory sources.
-   Enhance token handling for large inputs.

## License

This project is licensed under the MIT License.