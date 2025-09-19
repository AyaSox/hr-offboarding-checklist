using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OffboardingChecklist.Services;
using System.Security.Claims;
using System.IO.Compression;
using OffboardingChecklist.Data;
using Microsoft.EntityFrameworkCore;

namespace OffboardingChecklist.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly IDocumentService _documentService;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DocumentsController(IDocumentService documentService, ApplicationDbContext context, IWebHostEnvironment env)
        {
            _documentService = documentService;
            _context = context;
            _env = env;
        }

        private string GetCurrentUserIdentifier() =>
            User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "Unknown";

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, int processId, string documentType)
        {
            try
            {
                if (file != null && file.Length > 0)
                {
                    var filePath = await _documentService.UploadDocumentAsync(
                        file, 
                        processId, 
                        documentType, 
                        GetCurrentUserIdentifier());

                    TempData["Success"] = $"Document '{file.FileName}' uploaded successfully!";
                }
                else
                {
                    TempData["Error"] = "Please select a file to upload.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Upload failed: {ex.Message}";
            }

            return RedirectToAction("Details", "OffboardingProcesses", new { id = processId });
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            try
            {
                var (fileContent, contentType, fileName) = await _documentService.DownloadDocumentAsync(id);
                return File(fileContent, contentType, fileName);
            }
            catch (FileNotFoundException)
            {
                TempData["Error"] = "File not found.";
                return RedirectToAction("Index", "OffboardingProcesses");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, int processId)
        {
            try
            {
                await _documentService.DeleteDocumentAsync(id);
                TempData["Success"] = "Document deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Delete failed: {ex.Message}";
            }

            return RedirectToAction("Details", "OffboardingProcesses", new { id = processId });
        }

        // API endpoint to get documents for a process
        [HttpGet("api/documents/process/{processId}")]
        public async Task<IActionResult> GetDocumentsByProcessId(int processId)
        {
            try
            {
                var documents = await _documentService.GetDocumentsByProcessIdAsync(processId);
                
                var result = documents.Select(d => new {
                    id = d.Id,
                    fileName = d.FileName,
                    fileType = d.FileType,
                    fileSize = d.FileSize,
                    uploadedOn = d.UploadedOn,
                    uploadedBy = d.UploadedBy,
                    isRequired = d.IsRequired,
                    description = d.Description
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAll(int processId)
        {
            if (processId <= 0)
            {
                TempData["Error"] = "Invalid process id.";
                return RedirectToAction("Index", "OffboardingProcesses");
            }

            var docs = await _context.OffboardingDocuments.Where(d => d.OffboardingProcessId == processId).ToListAsync();
            if (!docs.Any())
            {
                TempData["Warning"] = "No documents found for this process.";
                return RedirectToAction("Details", "OffboardingProcesses", new { id = processId });
            }

            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                foreach (var d in docs)
                {
                    var physical = Path.Combine(_env.WebRootPath, "uploads", "documents", d.FilePath);
                    if (System.IO.File.Exists(physical))
                    {
                        var entry = zip.CreateEntry(d.FileName, CompressionLevel.Fastest);
                        using var entryStream = entry.Open();
                        using var fs = System.IO.File.OpenRead(physical);
                        await fs.CopyToAsync(entryStream);
                    }
                }
            }
            ms.Position = 0;
            return File(ms.ToArray(), "application/zip", $"process_{processId}_documents_{DateTime.Now:yyyyMMddHHmmss}.zip");
        }
    }
}