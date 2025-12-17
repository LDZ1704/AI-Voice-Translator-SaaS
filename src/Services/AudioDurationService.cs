using AI_Voice_Translator_SaaS.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace AI_Voice_Translator_SaaS.Services
{
    public class AudioDurationService : IAudioDurationService
    {
        private readonly ILogger<AudioDurationService> _logger;

        public AudioDurationService(ILogger<AudioDurationService> logger)
        {
            _logger = logger;
        }

        public async Task<int> GetDurationAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return 0;
            }

            var tempFilePath = Path.GetTempFileName();
            try
            {
                await using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                return await GetDurationFromFileAsync(tempFilePath, extension);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate audio duration for file: {FileName}", file.FileName);
                return 0;
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temp file: {TempFilePath}", tempFilePath);
                    }
                }
            }
        }

        private async Task<int> GetDurationFromFileAsync(string filePath, string extension)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (extension == ".mp3" || extension == ".wav")
                    {
                        return GetDurationWithNAudio(filePath, extension);
                    }

                    if (extension == ".m4a")
                    {
                        return GetDurationForM4A(filePath);
                    }

                    return 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get duration from file: {FilePath}", filePath);
                    return EstimateDurationFromFileSize(filePath);
                }
            });
        }

        private int GetDurationWithNAudio(string filePath, string extension)
        {
            try
            {
                if (extension == ".mp3")
                {
                    using var reader = new Mp3FileReader(filePath);
                    var duration = (int)Math.Round(reader.TotalTime.TotalSeconds);
                    _logger.LogDebug("NAudio: MP3 duration = {Duration}s", duration);
                    return duration;
                }

                if (extension == ".wav")
                {
                    using var reader = new WaveFileReader(filePath);
                    var duration = (int)Math.Round(reader.TotalTime.TotalSeconds);
                    _logger.LogDebug("NAudio: WAV duration = {Duration}s", duration);
                    return duration;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NAudio failed to read duration for {Extension}, falling back to estimation", extension);
                return EstimateDurationFromFileSize(filePath);
            }

            return EstimateDurationFromFileSize(filePath);
        }

        private int GetDurationForM4A(string filePath)
        {
            try
            {
                using var reader = new MediaFoundationReader(filePath);
                var duration = (int)Math.Round(reader.TotalTime.TotalSeconds);
                _logger.LogDebug("NAudio: M4A duration = {Duration}s", duration);
                return duration;
            }
            catch (PlatformNotSupportedException ex)
            {
                _logger.LogWarning(ex, "MediaFoundationReader not supported on this platform (Windows only), using estimation");
                return EstimateDurationFromFileSize(filePath);
            }
            catch (DllNotFoundException ex)
            {
                _logger.LogWarning(ex, "MediaFoundation DLL not found (Windows Media Foundation required), using estimation");
                return EstimateDurationFromFileSize(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MediaFoundationReader failed to read M4A duration, using estimation");
                return EstimateDurationFromFileSize(filePath);
            }
        }

        private int EstimateDurationFromFileSize(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
                var extension = Path.GetExtension(filePath).ToLowerInvariant();

                double estimatedMinutes;

                // Different estimation based on file type
                if (extension == ".wav")
                {
                    estimatedMinutes = fileSizeMB / 10.0;
                }
                else if (extension == ".m4a")
                {
                    estimatedMinutes = fileSizeMB / 0.7;
                }
                else
                {
                    estimatedMinutes = fileSizeMB;
                }

                var estimatedSeconds = (int)Math.Round(estimatedMinutes * 60);
                
                if (estimatedSeconds > 7200)
                {
                    estimatedSeconds = 7200;
                }

                _logger.LogDebug(
                    "Estimated duration: {Seconds}s ({Minutes:F1}m) from file size: {SizeMB:F2}MB ({Extension})",
                    estimatedSeconds, estimatedMinutes, fileSizeMB, extension);

                return estimatedSeconds;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to estimate duration from file size");
                return 0;
            }
        }
    }
}

