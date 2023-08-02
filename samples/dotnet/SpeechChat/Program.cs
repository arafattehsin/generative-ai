using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace SpeechChat
{
    class Program
    {

        // Enter the deployment name you chose when you deployed the model.
        static string engine = "text-davinci-002";

        // This example requires environment variables named "SPEECH_KEY" and "SPEECH_REGION"
        static string speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
        static string speechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION");

        static bool conversationEnded = false;
        static AOIHelper serviceHelper = new AOIHelper();

        // Prompts Azure OpenAI with a request and synthesizes the response.
        async static Task AskOpenAI(string prompt)
        {
            // Ask Azure OpenAI
            var response = serviceHelper.GetResponse(prompt);
            Console.WriteLine($"Azure OpenAI response: {response}");

            

            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            // The language of the voice that speaks.
            speechConfig.SpeechSynthesisVoiceName = "en-AU-NatashaNeural";
            var audioOutputConfig = AudioConfig.FromDefaultSpeakerOutput();

            using (var speechSynthesizer = new SpeechSynthesizer(speechConfig, audioOutputConfig))
            {
                var speechSynthesisResult = await speechSynthesizer.SpeakTextAsync(response).ConfigureAwait(true);

                if (speechSynthesisResult.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    Console.WriteLine($"Speech synthesized to speaker for text: [{response}]");
                }
                else if (speechSynthesisResult.Reason == ResultReason.Canceled)
                {
                    var cancellationDetails = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                    Console.WriteLine($"Speech synthesis canceled: {cancellationDetails.Reason}");

                    if (cancellationDetails.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"Error details: {cancellationDetails.ErrorDetails}");
                    }
                }
            }
        }

        // Continuously listens for speech input to recognize and send as text to Azure OpenAI
        async static Task ChatWithOpenAI()
        {
            // Should be the locale for the speaker's language.
            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechRecognitionLanguage = "en-US";

            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);


            while (!conversationEnded)
            {
                Console.WriteLine("Azure OpenAI is listening. Say 'Good bye' or press Ctrl-Z to end the conversation.");

                // Get audio from the microphone and then send it to the TTS service.
                var speechRecognitionResult = await speechRecognizer.RecognizeOnceAsync();

                switch (speechRecognitionResult.Reason)
                {
                    case ResultReason.RecognizedSpeech:
                        if (speechRecognitionResult.Text == "Goodbye.")
                        {
                            Console.WriteLine("Conversation ended.");
                            conversationEnded = true;
                        }
                        else
                        {
                            Console.WriteLine($"Recognized speech: {speechRecognitionResult.Text}");
                            await AskOpenAI(speechRecognitionResult.Text).ConfigureAwait(true);
                        }
                        break;
                    case ResultReason.NoMatch:
                        Console.WriteLine($"No speech could be recognized: ");
                        break;
                    case ResultReason.Canceled:
                        var cancellationDetails = CancellationDetails.FromResult(speechRecognitionResult);
                        Console.WriteLine($"Speech Recognition canceled: {cancellationDetails.Reason}");
                        if (cancellationDetails.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"Error details={cancellationDetails.ErrorDetails}");
                        }
                        break;
                }
            }
        }

        async static Task Main(string[] args)
        {
            try
            {
                await ChatWithOpenAI().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
