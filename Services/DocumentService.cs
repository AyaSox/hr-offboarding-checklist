using Microsoft.EntityFrameworkCore;
using OffboardingChecklist.Data;
using OffboardingChecklist.Services;

namespace OffboardingChecklist.Services
{
    public interface IDocumentService
    {
        Task<string> UploadDocumentAsync(IFormFile file, int processId, string documentType, string uploadedBy);
        Task<(byte[] fileContent, string contentType, string fileName)> DownloadDocumentAsync(int documentId);
        Task DeleteDocumentAsync(int documentId);
        Task<IEnumerable<Models.OffboardingDocument>> GetDocumentsByProcessIdAsync(int processId);
        bool IsValidFileType(string fileName);
        long GetMaxFileSize();
    }

    public class DocumentService : IDocumentService
    {
        private readonly ILogger<DocumentService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;
        
        private readonly string[] _allowedExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".txt" };
        private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB

        public DocumentService(ILogger<DocumentService> logger, IWebHostEnvironment environment, ApplicationDbContext context)
        {
            _logger = logger;
            _environment = environment;
            _context = context;
        }

        public async Task<string> UploadDocumentAsync(IFormFile file, int processId, string documentType, string uploadedBy)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required");

            if (!IsValidFileType(file.FileName))
                throw new ArgumentException("Invalid file type");

            if (file.Length > _maxFileSize)
                throw new ArgumentException($"File size exceeds maximum limit of {_maxFileSize / (1024 * 1024)}MB");

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "documents");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var document = new Models.OffboardingDocument
            {
                FileName = file.FileName,
                FileType = documentType,
                FilePath = uniqueFileName, // Store relative path
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedBy = uploadedBy,
                OffboardingProcessId = processId,
                IsRequired = IsRequiredDocumentType(documentType),
                IsCompleted = true
            };

            _context.OffboardingDocuments.Add(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document {FileName} uploaded successfully for process {ProcessId}", file.FileName, processId);

            return filePath;
        }

        public async Task<(byte[] fileContent, string contentType, string fileName)> DownloadDocumentAsync(int documentId)
        {
            var document = await _context.OffboardingDocuments.FindAsync(documentId);
            if (document == null)
                throw new FileNotFoundException("Document not found");

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", "documents", document.FilePath);
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Physical file not found");

            var fileContent = await File.ReadAllBytesAsync(filePath);
            return (fileContent, document.ContentType, document.FileName);
        }

        public async Task DeleteDocumentAsync(int documentId)
        {
            var document = await _context.OffboardingDocuments.FindAsync(documentId);
            if (document == null)
                return;

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", "documents", document.FilePath);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _context.OffboardingDocuments.Remove(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document {FileName} deleted successfully", document.FileName);
        }

        public async Task<IEnumerable<Models.OffboardingDocument>> GetDocumentsByProcessIdAsync(int processId)
        {
            return await _context.OffboardingDocuments
                .Where(d => d.OffboardingProcessId == processId)
                .OrderByDescending(d => d.UploadedOn)
                .ToListAsync();
        }

        public bool IsValidFileType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _allowedExtensions.Contains(extension);
        }

        public long GetMaxFileSize() => _maxFileSize;

        private static bool IsRequiredDocumentType(string documentType)
        {
            var requiredTypes = new[] { "ExitInterview", "AssetReturnForm", "ClearanceCertificate" };
            return requiredTypes.Contains(documentType);
        }
    }
}