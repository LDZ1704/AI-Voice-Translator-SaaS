CREATE DATABASE AIVoiceTranslator;
GO

USE AIVoiceTranslator;
GO

CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Email NVARCHAR(256) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NULL,
    DisplayName NVARCHAR(100) NOT NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT 'User', -- User, Admin
    SubscriptionTier NVARCHAR(20) NOT NULL DEFAULT 'Free', --Free, Premium
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastLoginAt DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE AudioFiles (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    OriginalFileUrl NVARCHAR(500) NOT NULL,
    FileSizeBytes BIGINT NOT NULL,
    DurationSeconds INT NULL,
    UploadedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Processing, Completed, Failed
    CONSTRAINT FK_AudioFiles_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE TABLE Transcripts (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    AudioFileId UNIQUEIDENTIFIER NOT NULL,
    OriginalText NVARCHAR(MAX) NOT NULL,
    DetectedLanguage NVARCHAR(10) NOT NULL, -- en, vi, ja, zh, fr
    Confidence DECIMAL(5,2) NULL, -- 0-100
    ProcessedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Transcripts_AudioFiles FOREIGN KEY (AudioFileId) REFERENCES AudioFiles(Id) ON DELETE CASCADE
);

CREATE TABLE Translations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TranscriptId UNIQUEIDENTIFIER NOT NULL,
    TargetLanguage NVARCHAR(10) NOT NULL,
    TranslatedText NVARCHAR(MAX) NOT NULL,
    TranslationEngine NVARCHAR(50) NOT NULL DEFAULT 'Gemini',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UserRating INT NULL CHECK (UserRating BETWEEN 1 AND 5),
    CONSTRAINT FK_Translations_Transcripts FOREIGN KEY (TranscriptId) REFERENCES Transcripts(Id) ON DELETE CASCADE
);

CREATE TABLE Outputs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TranslationId UNIQUEIDENTIFIER NOT NULL,
    OutputFileUrl NVARCHAR(500) NOT NULL,
    VoiceType NVARCHAR(20) NOT NULL DEFAULT 'Female', -- Male, Female
    VoiceModel NVARCHAR(50) NOT NULL DEFAULT 'Google', -- Google, OpenAI
    GeneratedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    DownloadCount INT NOT NULL DEFAULT 0,
    ExpiryDate DATETIME2 NOT NULL,
    CONSTRAINT FK_Outputs_Translations FOREIGN KEY (TranslationId) REFERENCES Translations(Id) ON DELETE CASCADE
);

CREATE TABLE AuditLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NULL,
    Action NVARCHAR(100) NOT NULL, -- Upload, Translate, Download, Login
    IpAddress NVARCHAR(45) NULL,
    UserAgent NVARCHAR(500) NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
);

-- Indexes for Performance
CREATE INDEX IX_AudioFiles_UserId ON AudioFiles(UserId);
CREATE INDEX IX_AudioFiles_Status ON AudioFiles(Status);
CREATE INDEX IX_Transcripts_AudioFileId ON Transcripts(AudioFileId);
CREATE INDEX IX_Translations_TranscriptId ON Translations(TranscriptId);
CREATE INDEX IX_Outputs_TranslationId ON Outputs(TranslationId);
CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);
CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp);
GO

CREATE PROCEDURE sp_GetUserDashboardStats
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SELECT 
        COUNT(DISTINCT af.Id) AS TotalUploads,
        SUM(af.DurationSeconds) AS TotalMinutesProcessed,
        COUNT(CASE WHEN af.Status = 'Completed' THEN 1 END) AS CompletedTranslations,
        COUNT(CASE WHEN af.Status = 'Failed' THEN 1 END) AS FailedTranslations
    FROM AudioFiles af
    WHERE af.UserId = @UserId;
END;
GO

CREATE PROCEDURE sp_GetRecentTranslations
    @UserId UNIQUEIDENTIFIER,
    @PageSize INT = 10,
    @PageNumber INT = 1
AS
BEGIN
    SELECT 
        af.Id AS AudioFileId,
        af.FileName,
        af.UploadedAt,
        af.Status,
        t.DetectedLanguage AS SourceLanguage,
        tr.TargetLanguage,
        tr.TranslatedText,
        o.OutputFileUrl,
        o.DownloadCount
    FROM AudioFiles af
    LEFT JOIN Transcripts t ON af.Id = t.AudioFileId
    LEFT JOIN Translations tr ON t.Id = tr.TranscriptId
    LEFT JOIN Outputs o ON tr.Id = o.TranslationId
    WHERE af.UserId = @UserId
    ORDER BY af.UploadedAt DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE VIEW vw_TranslationDetails AS
SELECT 
    af.Id AS AudioFileId,
    af.FileName,
    af.Status,
    u.Email AS UserEmail,
    u.DisplayName AS UserName,
    t.OriginalText,
    t.DetectedLanguage AS SourceLanguage,
    tr.TargetLanguage,
    tr.TranslatedText,
    tr.UserRating,
    o.OutputFileUrl,
    o.VoiceType,
    af.UploadedAt,
    tr.CreatedAt AS TranslatedAt
FROM AudioFiles af
INNER JOIN Users u ON af.UserId = u.Id
LEFT JOIN Transcripts t ON af.Id = t.AudioFileId
LEFT JOIN Translations tr ON t.Id = tr.TranscriptId
LEFT JOIN Outputs o ON tr.Id = o.TranslationId;
GO

CREATE TRIGGER trg_SetOutputExpiry
ON Outputs
AFTER INSERT
AS
BEGIN
    UPDATE Outputs
    SET ExpiryDate = DATEADD(DAY, 30, GeneratedAt)
    WHERE Id IN (SELECT Id FROM inserted);
END;
GO

USE master
DROP DATABASE AIVoiceTranslator
GO

USE AIVoiceTranslator;
GO
SELECT * FROM Users;
SELECT * FROM AudioFiles;
SELECT * FROM Transcripts;
SELECT * FROM Translations;
GO