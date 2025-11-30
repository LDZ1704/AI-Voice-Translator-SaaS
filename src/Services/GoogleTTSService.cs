using Google.Cloud.TextToSpeech.V1;

namespace AI_Voice_Translator_SaaS.Services
{
    public class GoogleTTSService : ITTSService
    {
        private readonly TextToSpeechClient _ttsClient;
        private readonly IStorageService _storageService;
        private readonly ILogger<GoogleTTSService> _logger;

        public GoogleTTSService(IStorageService storageService, ILogger<GoogleTTSService> logger)
        {
            _storageService = storageService;
            _logger = logger;

            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Path.Combine(Directory.GetCurrentDirectory(), "google-credentials.json"));

            _ttsClient = TextToSpeechClient.Create();
        }

        public async Task<(bool Success, string AudioFileUrl)> GenerateSpeechAsync(string text, string language, string voiceType)
        {
            try
            {
                var languageCode = language switch
                {
                    "vi" => "vi-VN",
                    "en" => "en-US",
                    "ja" => "ja-JP",
                    "zh" => "zh-CN",
                    "fr" => "fr-FR",
                    _ => "en-US"
                };

                var voice = new VoiceSelectionParams
                {
                    LanguageCode = languageCode,
                    SsmlGender = voiceType.ToLower() == "male" ? SsmlVoiceGender.Male : SsmlVoiceGender.Female
                };

                var audioConfig = new AudioConfig
                {
                    AudioEncoding = AudioEncoding.Mp3,
                    SpeakingRate = 1.0,
                    Pitch = 0.0
                };

                var input = new SynthesisInput
                {
                    Text = text
                };

                var response = await _ttsClient.SynthesizeSpeechAsync(input, voice, audioConfig);

                var fileName = $"{Guid.NewGuid()}.mp3";
                var tempPath = Path.Combine(Path.GetTempPath(), fileName);

                await File.WriteAllBytesAsync(tempPath, response.AudioContent.ToByteArray());

                using var stream = File.OpenRead(tempPath);
                var formFile = new FormFile(stream, 0, stream.Length, "file", fileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "audio/mpeg"
                };

                var fileUrl = await _storageService.UploadFileAsync(formFile, "output");

                File.Delete(tempPath);

                _logger.LogInformation($"TTS completed: {language}, {voiceType}");

                return (true, fileUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating speech");
                return (false, string.Empty);
            }
        }
    }
}