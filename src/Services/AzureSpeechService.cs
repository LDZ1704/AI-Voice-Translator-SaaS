using AI_Voice_Translator_SaaS.Interfaces;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Text;

namespace AI_Voice_Translator_SaaS.Services
{
    public class AzureSpeechService : ISpeechService
    {
        private readonly string _subscriptionKey;
        private readonly string _region;
        private readonly IStorageService _storageService;
        private readonly ILogger<AzureSpeechService> _logger;

        public AzureSpeechService(IConfiguration configuration, IStorageService storageService, ILogger<AzureSpeechService> logger)
        {
            _subscriptionKey = configuration["Azure:SpeechKey"];
            _region = configuration["Azure:SpeechRegion"];
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<(bool Success, string Text, string Language, decimal Confidence)> TranscribeAsync(string audioFileUrl)
        {
            string tempPath = null;
            Stream audioStream = null;

            try
            {
                audioStream = await _storageService.DownloadFileAsync(audioFileUrl);

                tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");

                using (var fileStream = File.Create(tempPath))
                {
                    await audioStream.CopyToAsync(fileStream);
                }

                var config = SpeechConfig.FromSubscription(_subscriptionKey, _region);
                config.SpeechRecognitionLanguage = "vi-VN";
                var autoDetectConfig = AutoDetectSourceLanguageConfig.FromLanguages(new[] { "vi-VN", "en-US", "ja-JP" });

                using (var audioConfig = AudioConfig.FromWavFileInput(tempPath))
                using (var recognizer = new SpeechRecognizer(config, autoDetectConfig, audioConfig))
                {
                    var sb = new StringBuilder();
                    var tcs = new TaskCompletionSource<bool>();

                    recognizer.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrWhiteSpace(e.Result.Text))
                        {
                            sb.AppendLine(e.Result.Text);
                        }
                    };

                    recognizer.Canceled += (s, e) => { tcs.TrySetResult(true); };
                    recognizer.SessionStopped += (s, e) => { tcs.TrySetResult(true); };

                    await recognizer.StartContinuousRecognitionAsync();
                    await tcs.Task;
                    await recognizer.StopContinuousRecognitionAsync();

                    var text = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        var detectedLanguage = "vi-VN";
                        return (true, text, detectedLanguage, 90m);
                    }

                    return (false, "No speech recognized", "", 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Speech Recognition Error");
                return (false, ex.Message, "", 0);
            }
            finally
            {
                audioStream?.Dispose();

                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Could not delete temp file: {tempPath}");
                    }
                }
            }
        }
    }
}