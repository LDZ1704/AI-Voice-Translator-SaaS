using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Models;
using AI_Voice_Translator_SaaS.Repositories;
using System.Text;

namespace AI_Voice_Translator_SaaS.Jobs
{
    public class ProcessAudioJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISpeechService _speechService;
        private readonly ITranslationService _translationService;
        private readonly ITTSService _ttsService;
        private readonly ILogger<ProcessAudioJob> _logger;

        public ProcessAudioJob(IUnitOfWork unitOfWork, ISpeechService speechService, ITranslationService translationService, ITTSService ttsService, ILogger<ProcessAudioJob> logger)
        {
            _unitOfWork = unitOfWork;
            _speechService = speechService;
            _translationService = translationService;
            _ttsService = ttsService;
            _logger = logger;
        }

        public async Task ProcessAsync(Guid audioFileId, string targetLanguage)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation($"[Job Start] Processing audio: {audioFileId}");

                var audioFile = await _unitOfWork.AudioFiles.GetByIdAsync(audioFileId);
                if (audioFile == null)
                {
                    _logger.LogError($"Audio file not found: {audioFileId}");
                    return;
                }

                audioFile.Status = "Processing";
                _unitOfWork.AudioFiles.Update(audioFile);
                await _unitOfWork.SaveChangesAsync();

                // STEP 1: STT
                _logger.LogInformation("[Step 1/3] Starting STT...");
                var sttStart = stopwatch.ElapsedMilliseconds;

                var transcriptionResult = await _speechService.TranscribeAsync(audioFile.OriginalFileUrl);

                var sttTime = stopwatch.ElapsedMilliseconds - sttStart;
                _logger.LogInformation($"[Step 1/3] STT completed in {sttTime}ms");

                if (!transcriptionResult.Success)
                {
                    await MarkAsFailed(audioFile, "STT failed");
                    return;
                }

                var transcript = new Transcript
                {
                    AudioFileId = audioFile.Id,
                    OriginalText = transcriptionResult.Text,
                    DetectedLanguage = transcriptionResult.Language,
                    Confidence = transcriptionResult.Confidence,
                    ProcessedAt = DateTime.UtcNow
                };

                await _unitOfWork.Transcripts.AddAsync(transcript);
                await _unitOfWork.SaveChangesAsync();

                // STEP 2: Translation
                _logger.LogInformation("[Step 2/3] Starting Translation...");
                var translationStart = stopwatch.ElapsedMilliseconds;

                // Với audio dài, đoạn text có thể rất lớn -> chia nhỏ để tránh timeout Gemini
                var translationResult = await TranslateLargeTextAsync(
                    transcriptionResult.Text,
                    transcriptionResult.Language,
                    targetLanguage
                );

                var translationTime = stopwatch.ElapsedMilliseconds - translationStart;
                _logger.LogInformation($"[Step 2/3] Translation completed in {translationTime}ms");

                if (!translationResult.Success)
                {
                    _unitOfWork.Transcripts.Remove(transcript);
                    await _unitOfWork.SaveChangesAsync();
                    await MarkAsFailed(audioFile, $"Translation failed: {translationResult.TranslatedText}");
                    return;
                }

                var translation = new Translation
                {
                    TranscriptId = transcript.Id,
                    TargetLanguage = targetLanguage,
                    TranslatedText = translationResult.TranslatedText,
                    TranslationEngine = "AzureTranslator",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Translations.AddAsync(translation);
                await _unitOfWork.SaveChangesAsync();

                // STEP 3: TTS
                _logger.LogInformation("[Step 3/3] Starting TTS...");
                var ttsStart = stopwatch.ElapsedMilliseconds;

                var ttsResult = await _ttsService.GenerateSpeechAsync(
                    translationResult.TranslatedText,
                    targetLanguage,
                    "Female"
                );

                var ttsTime = stopwatch.ElapsedMilliseconds - ttsStart;
                _logger.LogInformation($"[Step 3/3] TTS completed in {ttsTime}ms");

                if (!ttsResult.Success)
                {
                    await MarkAsFailed(audioFile, "TTS failed");
                    return;
                }

                var output = new Output
                {
                    TranslationId = translation.Id,
                    OutputFileUrl = ttsResult.AudioFileUrl,
                    VoiceType = "Female",
                    VoiceModel = "Azure",
                    GeneratedAt = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddDays(30)
                };

                await _unitOfWork.Outputs.AddAsync(output);
                await _unitOfWork.SaveChangesAsync();

                audioFile.Status = "Completed";
                _unitOfWork.AudioFiles.Update(audioFile);
                await _unitOfWork.SaveChangesAsync();

                stopwatch.Stop();
                _logger.LogInformation($"[Job Complete] Total time: {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedMilliseconds / 1000}s)");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"[Job Error] Failed after {stopwatch.ElapsedMilliseconds}ms");

                var audioFile = await _unitOfWork.AudioFiles.GetByIdAsync(audioFileId);
                if (audioFile != null)
                {
                    await MarkAsFailed(audioFile, ex.Message);
                }
            }
        }

        private async Task MarkAsFailed(AudioFile audioFile, string reason)
        {
            audioFile.Status = "Failed";
            _unitOfWork.AudioFiles.Update(audioFile);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogError($"Audio {audioFile.Id} marked as failed: {reason}");
        }

        private async Task<(bool Success, string TranslatedText)> TranslateLargeTextAsync(
            string text,
            string sourceLanguage,
            string targetLanguage)
        {
            const int chunkSize = 1200;

            if (string.IsNullOrWhiteSpace(text))
            {
                return (false, "Empty transcription");
            }

            var chunks = SplitIntoChunks(text, chunkSize);
            var sb = new StringBuilder();

            for (int i = 0; i < chunks.Count; i++)
            {
                var part = chunks[i];
                _logger.LogInformation($"[Translation Chunk {i + 1}/{chunks.Count}] length={part.Length}");

                var result = await _translationService.TranslateAsync(part, sourceLanguage, targetLanguage);
                if (!result.Success)
                {
                    _logger.LogError($"Translation failed at chunk {i + 1}/{chunks.Count}: {result.TranslatedText}");
                    return (false, result.TranslatedText);
                }

                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }
                sb.Append(result.TranslatedText);
            }

            return (true, sb.ToString());
        }

        private static List<string> SplitIntoChunks(string text, int maxChunkSize)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                return result;
            }

            var span = text.AsSpan();
            while (span.Length > 0)
            {
                var length = Math.Min(span.Length, maxChunkSize);
                var slice = span.Slice(0, length);
                var lastSpace = slice.LastIndexOf(' ');
                if (lastSpace > maxChunkSize * 0.7)
                {
                    length = lastSpace;
                    slice = span.Slice(0, length);
                }

                result.Add(slice.ToString().Trim());
                span = span.Slice(length).TrimStart();
            }

            return result;
        }
    }
}