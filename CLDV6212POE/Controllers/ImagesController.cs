using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using CLDV6212POE.Services;
using Microsoft.Extensions.Logging;
using System;

namespace CLDV6212POE.Controllers
{
    public class ImagesController : Controller
    {
        private readonly BlobImageService _blobService;
        private readonly ILogger<ImagesController> _logger;

        public ImagesController(BlobImageService blobService, ILogger<ImagesController> logger)
        {
            _blobService = blobService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("=== LOADING IMAGES PAGE ===");
                var blobs = await _blobService.ListBlobsAsync();
                _logger.LogInformation($"Found {((System.Collections.Generic.IEnumerable<CLDV6212POE.Services.BlobInfo>)blobs).Count()} blobs");
                return View(blobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load images: {Error}", ex.Message);
                ViewBag.Error = $"Failed to load images: {ex.Message}";
                return View(new List<CLDV6212POE.Services.BlobInfo>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                _logger.LogInformation("=== UPLOAD REQUEST RECEIVED ===");
                _logger.LogInformation($"File received: {file?.FileName ?? "null"}");
                _logger.LogInformation($"File size: {file?.Length ?? 0} bytes");
                _logger.LogInformation($"Content type: {file?.ContentType ?? "unknown"}");

                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("Upload failed: No file selected");
                    TempData["Error"] = "No file selected";
                    return RedirectToAction("Index");
                }

                // Validate file type (optional - add your requirements)
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    _logger.LogWarning($"Upload failed: Invalid file type {fileExtension}");
                    TempData["Error"] = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}";
                    return RedirectToAction("Index");
                }

                // Create unique blob name to avoid conflicts
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var blobName = $"{timestamp}_{Path.GetFileName(file.FileName)}";

                _logger.LogInformation($"Generated blob name: {blobName}");

                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    ms.Position = 0;

                    _logger.LogInformation("Starting blob upload...");
                    var blobUrl = await _blobService.UploadFileAsync(blobName, ms, file.ContentType);

                    _logger.LogInformation("=== UPLOAD SUCCESS ===");
                    _logger.LogInformation($"Blob URL: {blobUrl}");

                    TempData["Success"] = $"File '{file.FileName}' uploaded successfully!";
                    TempData["BlobUrl"] = blobUrl;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload failed: {Error}", ex.Message);
                TempData["Error"] = $"Upload failed: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Diagnostics()
        {
            try
            {
                _logger.LogInformation("=== RUNNING DIAGNOSTICS ===");
                var diagnostics = await _blobService.GetDiagnosticsAsync();
                return Json(diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Diagnostics failed: {Error}", ex.Message);
                return Json(new { Error = ex.Message, Timestamp = DateTime.UtcNow });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestUpload()
        {
            try
            {
                _logger.LogInformation("=== RUNNING TEST UPLOAD ===");
                var result = await _blobService.TestUploadAsync();
                _logger.LogInformation("Test upload completed successfully");

                TempData["Success"] = "Test upload completed successfully!";
                TempData["TestResult"] = result;

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test upload failed: {Error}", ex.Message);
                TempData["Error"] = $"Test upload failed: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string blobName)
        {
            try
            {
                if (string.IsNullOrEmpty(blobName))
                {
                    TempData["Error"] = "Blob name is required";
                    return RedirectToAction("Index");
                }

                _logger.LogInformation($"Deleting blob: {blobName}");
                var deleted = await _blobService.DeleteBlobAsync(blobName);

                if (deleted)
                {
                    TempData["Success"] = $"File '{blobName}' deleted successfully!";
                }
                else
                {
                    TempData["Warning"] = $"File '{blobName}' was not found";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete failed: {Error}", ex.Message);
                TempData["Error"] = $"Delete failed: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}