using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Models;
using AI_Voice_Translator_SaaS.Repositories;
using AI_Voice_Translator_SaaS.Services;

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
            try
            {
                _logger.LogInformation($"Starting to process audio file: {audioFileId}");

                var audioFile = await _unitOfWork.AudioFiles.GetByIdAsync(audioFileId);
                if (audioFile == null)
                {
                    _logger.LogError($"Audio file not found: {audioFileId}");
                    return;
                }

                audioFile.Status = "Processing";
                _unitOfWork.AudioFiles.Update(audioFile);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Step 1: Transcribing audio...");
                var transcriptionResult = await _speechService.TranscribeAsync(audioFile.OriginalFileUrl);

                if (!transcriptionResult.Success)
                {
                    audioFile.Status = "Failed";
                    _unitOfWork.AudioFiles.Update(audioFile);
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogError($"Transcription failed: {transcriptionResult.Text}");
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

                _logger.LogInformation($"Transcription saved. Text length: {transcriptionResult.Text.Length}");

                _logger.LogInformation($"Step 2: Translating to {targetLanguage}...");
                var translationResult = await _translationService.TranslateAsync(
                    transcriptionResult.Text,
                    transcriptionResult.Language,
                    targetLanguage
                );

                if (!translationResult.Success)
                {
                    audioFile.Status = "Failed";
                    _unitOfWork.AudioFiles.Update(audioFile);
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogError($"Translation failed: {translationResult.TranslatedText}");
                    return;
                }

                var translation = new Translation
                {
                    TranscriptId = transcript.Id,
                    TargetLanguage = targetLanguage,
                    TranslatedText = translationResult.TranslatedText,
                    TranslationEngine = "Gemini",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Translations.AddAsync(translation);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Translation saved.");

                _logger.LogInformation("Step 3: Generating speech...");
                var ttsResult = await _ttsService.GenerateSpeechAsync(
                    translationResult.TranslatedText,
                    targetLanguage,
                    "Female"
                );

                if (!ttsResult.Success)
                {
                    audioFile.Status = "Failed";
                    _unitOfWork.AudioFiles.Update(audioFile);
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogError("TTS generation failed");
                    return;
                }

                var output = new Output
                {
                    TranslationId = translation.Id,
                    OutputFileUrl = ttsResult.AudioFileUrl,
                    VoiceType = "Female",
                    VoiceModel = "Google",
                    GeneratedAt = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddDays(30)
                };

                await _unitOfWork.Outputs.AddAsync(output);
                await _unitOfWork.SaveChangesAsync();

                audioFile.Status = "Completed";
                _unitOfWork.AudioFiles.Update(audioFile);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"✓ Processing completed successfully for audio file: {audioFileId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing audio file: {audioFileId}");

                var audioFile = await _unitOfWork.AudioFiles.GetByIdAsync(audioFileId);
                if (audioFile != null)
                {
                    audioFile.Status = "Failed";
                    _unitOfWork.AudioFiles.Update(audioFile);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
        }
    }
}