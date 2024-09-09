#pragma warning disable SKEXP0070

using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.ML.OnnxRuntimeGenAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Onnx;
using Microsoft.SemanticKernel.Services;
using Microsoft.SemanticKernel.TextGeneration;

namespace local_rag_sk.Helpers
{
    /// <summary>
    /// Represents a text completion service using OnnxRuntimeGenAI.
    /// </summary>
    public sealed class OnnxRuntimeGenAITextCompletionService : ITextGenerationService, IDisposable
    {
        private readonly string _modelId;
        private readonly string _modelPath;
        private Model? _model;
        private Tokenizer? _tokenizer;

        private Dictionary<string, object?> AttributesInternal { get; } = new();

        /// <summary>
        /// Initializes a new instance of the OnnxRuntimeGenAITextCompletionService class.
        /// </summary>
        /// <param name="modelId">The name of the model.</param>
        /// <param name="modelPath">The generative AI ONNX model path for the text completion service.</param>
        /// <param name="loggerFactory">Optional logger factory to be used for logging.</param>
        public OnnxRuntimeGenAITextCompletionService(
            string modelId,
            string modelPath,
            ILoggerFactory? loggerFactory = null)
        {
            this._modelId = modelId;
            this._modelPath = modelPath;

            this.AttributesInternal.Add(AIServiceExtensions.ModelIdKey, this._modelId);
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object?> Attributes => this.AttributesInternal;

        private async IAsyncEnumerable<string> RunInferenceAsync(string prompt, PromptExecutionSettings? executionSettings, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            OnnxRuntimeGenAIPromptExecutionSettings onnxRuntimeGenAIPromptExecutionSettings = OnnxRuntimeGenAIPromptExecutionSettings.FromExecutionSettings(executionSettings);
            Sequences inputSequences = GetTokenizer().Encode(prompt);
            using GeneratorParams generatorParams = new GeneratorParams(GetModel());
            UpdateGeneratorParamsFromPromptExecutionSettings(generatorParams, onnxRuntimeGenAIPromptExecutionSettings);
            generatorParams.SetInputSequences(inputSequences);
            Generator generator = new Generator(GetModel(), generatorParams);
            try
            {
                while (!generator.IsDone())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return await Task.Run(delegate
                    {
                        generator.ComputeLogits();
                        generator.GenerateNextToken();
                        ReadOnlySpan<int> sequence = generator.GetSequence(0uL);
                        ReadOnlySpan<int> sequence2 = sequence.Slice(sequence.Length - 1, 1);
                        return GetTokenizer().Decode(sequence2);
                    }, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                }
            }
            finally
            {
                if (generator != null)
                {
                    ((IDisposable)generator).Dispose();
                }
            }
        }

        private Model GetModel() => this._model ??= new Model(this._modelPath);

        private Tokenizer GetTokenizer() => this._tokenizer ??= new Tokenizer(this.GetModel());

        private void UpdateGeneratorParamsFromPromptExecutionSettings(GeneratorParams generatorParams, OnnxRuntimeGenAIPromptExecutionSettings onnxRuntimeGenAIPromptExecutionSettings)
        {
            if (onnxRuntimeGenAIPromptExecutionSettings.TopP.HasValue)
            {
                generatorParams.SetSearchOption("top_p", onnxRuntimeGenAIPromptExecutionSettings.TopP.Value);
            }
            if (onnxRuntimeGenAIPromptExecutionSettings.TopK.HasValue)
            {
                generatorParams.SetSearchOption("top_k", onnxRuntimeGenAIPromptExecutionSettings.TopK.Value);
            }
            if (onnxRuntimeGenAIPromptExecutionSettings.Temperature.HasValue)
            {
                generatorParams.SetSearchOption("temperature", onnxRuntimeGenAIPromptExecutionSettings.Temperature.Value);
            }
            if (onnxRuntimeGenAIPromptExecutionSettings.RepetitionPenalty.HasValue)
            {
                generatorParams.SetSearchOption("repetition_penalty", onnxRuntimeGenAIPromptExecutionSettings.RepetitionPenalty.Value);
            }
            if (onnxRuntimeGenAIPromptExecutionSettings.PastPresentShareBuffer.HasValue)
            {
                generatorParams.SetSearchOption("past_present_share_buffer", onnxRuntimeGenAIPromptExecutionSettings.PastPresentShareBuffer.Value);
            }
            if (onnxRuntimeGenAIPromptExecutionSettings.NumReturnSequences.HasValue)
            {
                generatorParams.SetSearchOption("num_return_sequences", onnxRuntimeGenAIPromptExecutionSettings.NumReturnSequences.Value);
            }
            if (onnxRuntimeGenAIPromptExecutionSettings.NoRepeatNgramSize.HasValue)
            {
                generatorParams.SetSearchOption("no_repeat_ngram_size", onnxRuntimeGenAIPromptExecutionSettings.NoRepeatNgramSize.Value);
            }
            if (onnxRuntimeGenAIPromptExecutionSettings.MinTokens.HasValue)
            {
                generatorParams.SetSearchOption("min_length", onnxRuntimeGenAIPromptExecutionSettings.MinTokens.Value);
            }
            if (onnxRuntimeGenAIPromptExecutionSettings.MaxTokens.HasValue)
            {
                generatorParams.SetSearchOption("max_length", 2000);
            }
            if (onnxRuntimeGenAIPromptExecutionSettings.LengthPenalty.HasValue)
            {
                generatorParams.SetSearchOption("length_penalty", onnxRuntimeGenAIPromptExecutionSettings.LengthPenalty.Value);
            }
            if (onnxRuntimeGenAIPromptExecutionSettings.EarlyStopping.HasValue)
            {
                generatorParams.SetSearchOption("early_stopping", onnxRuntimeGenAIPromptExecutionSettings.EarlyStopping.Value);
            }
            if (onnxRuntimeGenAIPromptExecutionSettings.DoSample.HasValue)
            {
                generatorParams.SetSearchOption("do_sample", onnxRuntimeGenAIPromptExecutionSettings.DoSample.Value);
            }
            if (onnxRuntimeGenAIPromptExecutionSettings.DiversityPenalty.HasValue)
            {
                generatorParams.SetSearchOption("diversity_penalty", onnxRuntimeGenAIPromptExecutionSettings.DiversityPenalty.Value);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this._tokenizer?.Dispose();
            this._model?.Dispose();
        }

        public async Task<IReadOnlyList<TextContent>> GetTextContentsAsync(string prompt, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        {
            var result = new StringBuilder();

            await foreach (var content in this.RunInferenceAsync(prompt, executionSettings, cancellationToken).ConfigureAwait(false))
            {
                result.Append(content);
            }

            return new List<TextContent>
            {
                new(
                    modelId: this._modelId, text: result.ToString())
            };
        }

        public async IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(string prompt, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var result = new StringBuilder();

            await foreach (var content in this.RunInferenceAsync(prompt, executionSettings, cancellationToken).ConfigureAwait(false))
            {
                result.Append(content);
            }

            foreach (string word in result.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                yield return new StreamingTextContent($"{word} ");
            }
        }
    }
}

