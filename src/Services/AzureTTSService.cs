using AI_Voice_Translator_SaaS.Interfaces;
using Microsoft.CognitiveServices.Speech;

namespace AI_Voice_Translator_SaaS.Services
{
    public class AzureTTSService : ITTSService
    {
        private readonly string _subscriptionKey;
        private readonly string _region;
        private readonly IStorageService _storageService;
        private readonly ILogger<AzureTTSService> _logger;

        public AzureTTSService(IConfiguration configuration, IStorageService storageService, ILogger<AzureTTSService> logger)
        {
            _subscriptionKey = configuration["Azure:SpeechKey"];
            _region = configuration["Azure:SpeechRegion"];
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<(bool Success, string AudioFileUrl)> GenerateSpeechAsync(string text, string language, string voiceType)
        {
            string tempPath = null;

            try
            {
                var config = SpeechConfig.FromSubscription(_subscriptionKey, _region);

                config.SpeechSynthesisVoiceName = language switch
                {
                    "vi" => voiceType == "Male" ? "vi-VN-NamMinhNeural" : "vi-VN-HoaiMyNeural",
                    "en" => voiceType == "Male" ? "en-US-GuyNeural" : "en-US-JennyNeural",
                    "ja" => voiceType == "Male" ? "ja-JP-KeitaNeural" : "ja-JP-NanamiNeural",
                    _ => "en-US-JennyNeural"
                };

                using var synthesizer = new SpeechSynthesizer(config);
                var result = await synthesizer.SpeakTextAsync(text);

                if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    var fileName = $"{Guid.NewGuid()}.mp3";
                    tempPath = Path.Combine(Path.GetTempPath(), fileName);

                    await File.WriteAllBytesAsync(tempPath, result.AudioData);

                    using (var stream = File.OpenRead(tempPath))
                    {
                        var formFile = new FormFile(stream, 0, stream.Length, "file", fileName)
                        {
                            Headers = new HeaderDictionary(),
                            ContentType = "audio/mpeg"
                        };

                        var fileUrl = await _storageService.UploadFileAsync(formFile, "output");

                        _logger.LogInformation("Speech synthesis completed");
                        return (true, fileUrl);
                    }
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                    _logger.LogError($"Synthesis canceled: {cancellation.ErrorDetails}");
                    return (false, cancellation.ErrorDetails);
                }

                return (false, "Unknown error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating speech");
                return (false, ex.Message);
            }
            finally
            {
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