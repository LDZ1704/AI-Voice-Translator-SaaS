using Google.Cloud.Speech.V1;
using Google.Protobuf;

namespace AI_Voice_Translator_SaaS.Services
{
    public class GoogleSpeechService : ISpeechService
    {
        private readonly SpeechClient _speechClient;
        private readonly IStorageService _storageService;
        private readonly ILogger<GoogleSpeechService> _logger;

        public GoogleSpeechService(IStorageService storageService, ILogger<GoogleSpeechService> logger)
        {
            _storageService = storageService;
            _logger = logger;

            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Path.Combine(Directory.GetCurrentDirectory(), "google-credentials.json"));

            _speechClient = SpeechClient.Create();
        }

        public async Task<(bool Success, string Text, string Language, decimal Confidence)> TranscribeAsync(string audioFileUrl)
        {
            try
            {
                var audioStream = await _storageService.DownloadFileAsync(audioFileUrl);

                using var memoryStream = new MemoryStream();
                await audioStream.CopyToAsync(memoryStream);
                var audioBytes = memoryStream.ToArray();

                var config = new RecognitionConfig
                {
                    Encoding = RecognitionConfig.Types.AudioEncoding.Mp3,
                    SampleRateHertz = 16000,
                    LanguageCode = "vi-VN", // Default Vietnamese
                    AlternativeLanguageCodes = { "en-US", "ja-JP" },
                    EnableAutomaticPunctuation = true,
                    Model = "default"
                };

                var audio = RecognitionAudio.FromBytes(audioBytes);

                var response = await _speechClient.RecognizeAsync(config, audio);

                if (response.Results.Count == 0)
                {
                    return (false, string.Empty, string.Empty, 0);
                }

                var result = response.Results[0];
                var alternative = result.Alternatives[0];

                var confidence = (decimal)(alternative.Confidence * 100);
                var detectedLanguage = result.LanguageCode ?? "vi-VN";

                _logger.LogInformation($"Transcription completed. Confidence: {confidence}%");

                return (true, alternative.Transcript, detectedLanguage, confidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transcribing audio");
                return (false, ex.Message, string.Empty, 0);
            }
        }
    }
}