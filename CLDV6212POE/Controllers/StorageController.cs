using CLDV6212POE.Models;
using CLDV6212POE.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using BlobInfo = CLDV6212POE.Models.BlobInfo;

namespace CLDV6212POE.Controllers
{
    public class StorageController : Controller
    {
        private readonly BlobImageService _blobService;
        private readonly FileContractService _fileService;
        private readonly OrderQueueService _queueService;  // customernotification queue
        private readonly StudentFilesQueueService _studentFilesQueueService;  // studentfiles queue
        private readonly ILogger<StorageController> _logger;

        public StorageController(
            BlobImageService blobService,
            FileContractService fileService,
            OrderQueueService queueService,
            StudentFilesQueueService studentFilesQueueService,
            ILogger<StorageController> logger)
        {
            _blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
            _studentFilesQueueService = studentFilesQueueService ?? throw new ArgumentNullException(nameof(studentFilesQueueService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ===== BYPASS ALL FORM ISSUES - CREATE SAMPLE IMAGE =====
        [HttpGet]
        public async Task<IActionResult> CreateSampleImage()
        {
            try
            {
                _logger.LogInformation("=== CREATING SAMPLE IMAGE ===");
                
                // Create a simple 1x1 pixel PNG image in memory
                var pngBytes = new byte[] {
                    0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
                    0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
                    0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // 1x1 pixel
                    0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, 0xDE,
                    0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54, // IDAT chunk
                    0x08, 0x99, 0x01, 0x01, 0x00, 0x00, 0x00, 0xFF, 0xFF,
                    0x00, 0x00, 0x00, 0x02, 0x00, 0x01, 0xE2, 0x21, 0xBC, 0x33,
                    0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 // IEND
                };

                var fileName = $"sample-image-{DateTime.UtcNow:yyyyMMddHHmmss}.png";
                
                using var stream = new MemoryStream(pngBytes);
                var blobUrl = await _blobService.UploadFileAsync(fileName, stream, "image/png");
                
                _logger.LogInformation("Sample image created successfully: {Url}", blobUrl);
                TempData["Success"] = $"Sample image created! File: {fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sample image creation failed");
                TempData["Error"] = $"Sample image creation failed: {ex.Message}";
            }

            return RedirectToAction(nameof(BlobStorage));
        }

        // ===== DEBUG METHOD - GET VERSION FOR TESTING =====
        [HttpGet]
        public async Task<IActionResult> SimpleUploadTest()
        {
            try
            {
                _logger.LogInformation("=== SIMPLE UPLOAD TEST STARTING ===");
                _logger.LogInformation("Current time: {Time}", DateTime.UtcNow);

                // Create a simple test file
                var testContent = $"Simple test file created at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}\nContainer: studentimages\nAccount: klmazureapp1\nTest ID: {Guid.NewGuid()}";
                var testBytes = System.Text.Encoding.UTF8.GetBytes(testContent);
                var testFileName = $"simple-test-{DateTime.UtcNow:yyyyMMddHHmmss}.txt";

                _logger.LogInformation("Test file: {FileName}, Size: {Size} bytes", testFileName, testBytes.Length);

                using var testStream = new MemoryStream(testBytes);
                
                _logger.LogInformation("=== ATTEMPTING UPLOAD ===");
                var blobUrl = await _blobService.UploadFileAsync(testFileName, testStream, "text/plain");
                _logger.LogInformation("Upload returned URL: {BlobUrl}", blobUrl);

                TempData["Success"] = $"Simple test upload completed! File: {testFileName}";
                TempData["BlobUrl"] = blobUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== SIMPLE UPLOAD TEST FAILED ===");
                TempData["Error"] = $"Simple upload test failed: {ex.Message}";
            }

            return RedirectToAction(nameof(BlobStorage));
        }

        // ===== DEBUG METHOD - POST VERSION =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DebugUploadTest()
        {
            try
            {
                _logger.LogInformation("=== DEBUG UPLOAD TEST STARTING ===");
                _logger.LogInformation("Current time: {Time}", DateTime.UtcNow);
                _logger.LogInformation("Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

                // Create a simple test file
                var testContent = $"Debug test file created at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}\nContainer: studentimages\nAccount: klmazureapp1\nTest ID: {Guid.NewGuid()}";
                var testBytes = System.Text.Encoding.UTF8.GetBytes(testContent);
                var testFileName = $"debug-test-{DateTime.UtcNow:yyyyMMddHHmmss}.txt";

                _logger.LogInformation("=== TEST FILE PREPARATION ===");
                _logger.LogInformation("- Name: {FileName}", testFileName);
                _logger.LogInformation("- Size: {Size} bytes", testBytes.Length);
                _logger.LogInformation("- Content length: {ContentLength}", testContent.Length);
                _logger.LogInformation("- Content preview: {Content}", testContent.Substring(0, Math.Min(100, testContent.Length)));

                // Test stream creation
                using var testStream = new MemoryStream(testBytes);
                _logger.LogInformation("- Stream created: Length={StreamLength}, CanRead={CanRead}, CanSeek={CanSeek}", 
                    testStream.Length, testStream.CanRead, testStream.CanSeek);

                // Test blob service availability
                _logger.LogInformation("=== TESTING BLOB SERVICE ===");
                _logger.LogInformation("- BlobService instance: {IsNull}", _blobService == null ? "NULL" : "OK");
                
                if (_blobService == null)
                {
                    throw new InvalidOperationException("BlobImageService is not initialized!");
                }

                // Get diagnostics before upload
                var preDiagnostics = await _blobService.GetDiagnosticsAsync();
                _logger.LogInformation("=== PRE-UPLOAD DIAGNOSTICS ===");
                _logger.LogInformation("Container exists: {ContainerExists}", preDiagnostics.GetValueOrDefault("ContainerExists", "Unknown"));
                _logger.LogInformation("Storage account: {StorageAccount}", preDiagnostics.GetValueOrDefault("StorageAccount", "Unknown"));
                _logger.LogInformation("Blob count: {BlobCount}", preDiagnostics.GetValueOrDefault("BlobCount", "Unknown"));

                // Attempt upload with detailed logging
                _logger.LogInformation("=== CALLING BLOB SERVICE UPLOAD ===");
                _logger.LogInformation("Calling UploadFileAsync with parameters:");
                _logger.LogInformation("- blobName: {BlobName}", testFileName);
                _logger.LogInformation("- contentType: text/plain");
                _logger.LogInformation("- stream position: {Position}", testStream.Position);

                string blobUrl;
                try
                {
                    blobUrl = await _blobService.UploadFileAsync(testFileName, testStream, "text/plain");
                    _logger.LogInformation("=== UPLOAD METHOD RETURNED ===");
                    _logger.LogInformation("Returned URL: {BlobUrl}", blobUrl ?? "NULL");
                }
                catch (Exception uploadEx)
                {
                    _logger.LogError(uploadEx, "=== UPLOAD METHOD THREW EXCEPTION ===");
                    _logger.LogError("Upload exception type: {ExceptionType}", uploadEx.GetType().Name);
                    _logger.LogError("Upload exception message: {Message}", uploadEx.Message);
                    if (uploadEx.InnerException != null)
                    {
                        _logger.LogError("Upload inner exception: {InnerMessage}", uploadEx.InnerException.Message);
                    }
                    throw;
                }

                if (string.IsNullOrEmpty(blobUrl))
                {
                    throw new InvalidOperationException("Upload returned null or empty URL!");
                }

                _logger.LogInformation("=== UPLOAD COMPLETED, VERIFYING ===");
                _logger.LogInformation("Upload successful, returned URL: {BlobUrl}", blobUrl);

                // Wait for Azure consistency
                _logger.LogInformation("Waiting 3 seconds for Azure consistency...");
                await Task.Delay(3000);

                // Try to list blobs to see if it appears
                _logger.LogInformation("=== VERIFYING UPLOAD BY LISTING BLOBS ===");
                var blobs = await _blobService.ListBlobsAsync();
                _logger.LogInformation("Total blobs found after upload: {Count}", blobs.Count());

                var foundBlob = blobs.FirstOrDefault(b => b.Name == testFileName);

                if (foundBlob != null)
                {
                    _logger.LogInformation("=== SUCCESS: TEST BLOB FOUND ===");
                    _logger.LogInformation("- Name: {Name}", foundBlob.Name);
                    _logger.LogInformation("- Size: {Size} bytes", foundBlob.Size);
                    _logger.LogInformation("- URL: {Url}", foundBlob.Url);
                    _logger.LogInformation("- Content Type: {ContentType}", foundBlob.ContentType);
                    _logger.LogInformation("- Last Modified: {LastModified}", foundBlob.LastModified);

                    TempData["Success"] = $"✅ DEBUG TEST SUCCESSFUL! File '{testFileName}' uploaded and verified. Size: {foundBlob.Size} bytes";
                    TempData["BlobDetails"] = $"URL: {foundBlob.Url}";
                }
                else
                {
                    _logger.LogError("=== PROBLEM: TEST BLOB NOT FOUND ===");
                    _logger.LogError("Upload returned URL but blob not found in listing!");
                    _logger.LogError("Expected blob name: {ExpectedName}", testFileName);
                    _logger.LogError("Total blobs in container: {Count}", blobs.Count());
                    
                    if (blobs.Any())
                    {
                        _logger.LogInformation("Existing blobs in container:");
                        foreach (var blob in blobs.Take(5))
                        {
                            _logger.LogInformation("- {BlobName} ({Size} bytes)", blob.Name, blob.Size);
                        }
                    }

                    TempData["Error"] = $"❌ Upload appeared successful (URL: {blobUrl}) but file '{testFileName}' not found in container listing! This suggests a consistency issue.";
                }

                // Get post-upload diagnostics
                var postDiagnostics = await _blobService.GetDiagnosticsAsync();
                _logger.LogInformation("=== POST-UPLOAD DIAGNOSTICS ===");
                _logger.LogInformation("Container exists: {ContainerExists}", postDiagnostics.GetValueOrDefault("ContainerExists", "Unknown"));
                _logger.LogInformation("Blob count: {BlobCount}", postDiagnostics.GetValueOrDefault("BlobCount", "Unknown"));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== DEBUG UPLOAD TEST FAILED ===");
                _logger.LogError("Exception type: {ExceptionType}", ex.GetType().Name);
                _logger.LogError("Exception message: {Message}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);

                TempData["Error"] = $"❌ Debug upload test failed: {ex.Message}";
                TempData["TechnicalError"] = $"Type: {ex.GetType().Name}\nMessage: {ex.Message}";

                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception type: {InnerType}", ex.InnerException.GetType().Name);
                    _logger.LogError("Inner exception message: {InnerMessage}", ex.InnerException.Message);
                    TempData["TechnicalError"] += $"\nInner: {ex.InnerException.Message}";
                }
            }

            return RedirectToAction(nameof(BlobStorage));
        }

        // ===== QUEUE MANAGEMENT =====
        public IActionResult Queue()
        {
            _logger.LogInformation("[StorageController] Loading Queue management page");
            return View();
        }

        public IActionResult QueueTest()
        {
            _logger.LogInformation("[StorageController] Loading Queue test page");
            return View();
        }

        public IActionResult QueueSimple()
        {
            _logger.LogInformation("[StorageController] Loading Simple Queue page");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string message, string queueType = "CustomerNotification")
        {
            _logger.LogWarning("===== SENDMESSAGE ACTION CALLED =====");
            _logger.LogWarning($"Message received: '{message}'");
            _logger.LogWarning($"QueueType received: '{queueType}'");

            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    TempData["Error"] = "Please enter a message to send to the queue.";
                    _logger.LogWarning("[StorageController] Empty message attempted");
                    return RedirectToAction(nameof(Queue));
                }

                // Determine which queue to use
                string queueName;
                if (queueType == "StudentFiles")
                {
                    queueName = "studentfiles";
                }
                else
                {
                    queueName = "customernotification";
                }

                // Create structured message with comprehensive details
                var queueMessage = new
                {
                    Id = Guid.NewGuid(),
                    Message = message.Trim(),
                    QueueType = queueType,
                    Timestamp = DateTime.UtcNow,
                    Source = "CLDV6212POE-WebApp",
                    ProcessingStatus = "Pending",
                    StudentId = "ST10083941",
                    Environment = "Development",
                    Priority = queueType == "CustomerNotification" ? "High" : "Normal",
                    RetryCount = 0
                };

                var jsonMessage = JsonSerializer.Serialize(queueMessage, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("[StorageController] === SENDING QUEUE MESSAGE ===");
                _logger.LogInformation("[StorageController] Queue Type: {QueueType}", queueType);
                _logger.LogInformation("[StorageController] Message ID: {MessageId}", queueMessage.Id);
                _logger.LogInformation("[StorageController] Target Queue: {QueueName}", queueName);

                // Send to appropriate queue
                if (queueType == "StudentFiles")
                {
                    await _studentFilesQueueService.SendMessageAsync(jsonMessage);
                }
                else
                {
                    await _queueService.SendMessageAsync(jsonMessage);
                }

                _logger.LogInformation("[StorageController] === QUEUE MESSAGE SENT SUCCESSFULLY ===");
                _logger.LogInformation("[StorageController] Check Azure Portal: Storage accounts → klmazureapp1 → Queues → {QueueName}", queueName);

                TempData["Success"] = $"Message sent to '{queueName}' queue successfully!";
                TempData["MessageDetails"] = $"ID: {queueMessage.Id}, Time: {queueMessage.Timestamp:HH:mm:ss UTC}";
                TempData["QueueName"] = queueName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StorageController] === QUEUE SEND FAILED ===");
                _logger.LogError("[StorageController] Queue Type: {QueueType}, Content: {Message}", queueType, message);

                TempData["Error"] = $"Failed to send message to queue: {ex.Message}";
            }

            return RedirectToAction(nameof(Queue));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveMessage()
        {
            try
            {
                _logger.LogInformation("[StorageController] === RECEIVING QUEUE MESSAGE ===");
                _logger.LogInformation("[StorageController] Queue: customernotification");

                var message = await _queueService.ReceiveMessageAsync();

                if (message != null)
                {
                    _logger.LogInformation("[StorageController] Message received successfully");

                    try
                    {
                        // Try to parse and format JSON for better display
                        var jsonDoc = JsonDocument.Parse(message);
                        var formattedMessage = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                        TempData["ReceivedMessage"] = $"Message received from Azure Queue 'customernotification':\n\n{formattedMessage}";
                        _logger.LogInformation("[StorageController] Structured JSON message received and parsed");
                    }
                    catch (JsonException)
                    {
                        // If not JSON, display as plain text
                        TempData["ReceivedMessage"] = $"Message received from Azure Queue 'customernotification':\n\n{message}";
                        _logger.LogInformation("[StorageController] Plain text message received");
                    }
                }
                else
                {
                    TempData["Info"] = "No messages currently available in the queue.";
                    _logger.LogInformation("[StorageController] No messages found in queue");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StorageController] === QUEUE RECEIVE FAILED ===");
                TempData["Error"] = $"Failed to receive message from queue: {ex.Message}";
            }

            return RedirectToAction(nameof(Queue));
        }

        // ===== BLOB STORAGE MANAGEMENT =====
        public async Task<IActionResult> BlobStorage()
        {
            try
            {
                _logger.LogInformation("[StorageController] === LOADING BLOB STORAGE PAGE ===");
                _logger.LogInformation("[StorageController] Container: studentimages");

                // Load existing blobs to display
                var blobs = await _blobService.ListBlobsAsync();
                ViewBag.Blobs = blobs.ToList();
                ViewBag.BlobCount = blobs.Count();

                // Add comprehensive diagnostics
                var diagnostics = await _blobService.GetDiagnosticsAsync();
                ViewBag.Diagnostics = diagnostics;

                // Add portal information
                ViewBag.StorageAccount = "klmazureapp1";
                ViewBag.ContainerName = "studentimages";
                ViewBag.PortalUrl = "https://portal.azure.com";

                _logger.LogInformation("[StorageController] Blob Storage page loaded successfully with {BlobCount} blobs", blobs.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StorageController] === BLOB STORAGE PAGE LOAD FAILED ===");

                ViewBag.Error = $"Error loading blob storage: {ex.Message}";
                ViewBag.Blobs = new List<BlobInfo>();
                ViewBag.BlobCount = 0;
                ViewBag.Diagnostics = new Dictionary<string, object>
                {
                    ["Error"] = ex.Message,
                    ["Timestamp"] = DateTime.UtcNow
                };
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                _logger.LogInformation("[StorageController] === IMAGE UPLOAD REQUEST RECEIVED ===");
                _logger.LogInformation("[StorageController] Request time: {Time}", DateTime.UtcNow);
                _logger.LogInformation("[StorageController] File parameter: {IsNull}", file == null ? "NULL" : "NOT NULL");
                
                if (file != null)
                {
                    _logger.LogInformation("[StorageController] File details:");
                    _logger.LogInformation("[StorageController] - Name: {FileName}", file.FileName);
                    _logger.LogInformation("[StorageController] - Size: {Size} bytes", file.Length);
                    _logger.LogInformation("[StorageController] - Content Type: {ContentType}", file.ContentType);
                }

                if (file == null || file.Length == 0)
                {
                    var error = "Please select an image file to upload.";
                    _logger.LogWarning("[StorageController] VALIDATION FAILED: {Error}", error);
                    _logger.LogWarning("[StorageController] File null: {IsNull}, File length: {Length}", 
                        file == null, file?.Length ?? 0);
                    TempData["Error"] = error;
                    return RedirectToAction(nameof(BlobStorage));
                }

                // Enhanced file validation
                var allowedTypes = new[] {
                    "image/jpeg", "image/jpg", "image/png", "image/gif",
                    "image/bmp", "image/webp", "image/svg+xml"
                };

                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg" };

                if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()) ||
                    !allowedExtensions.Contains(fileExtension))
                {
                    var error = "Only image files (JPEG, PNG, GIF, BMP, WebP, SVG) are allowed.";
                    _logger.LogWarning("[StorageController] Invalid file type: {ContentType}, Extension: {Extension}",
                        file.ContentType, fileExtension);
                    TempData["Error"] = error;
                    return RedirectToAction(nameof(BlobStorage));
                }

                // Validate file size (max 10MB for images)
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (file.Length > maxFileSize)
                {
                    var error = $"File size cannot exceed {maxFileSize / (1024 * 1024)}MB. Current size: {file.Length / (1024 * 1024):F1}MB";
                    _logger.LogWarning("[StorageController] File too large: {Size} bytes", file.Length);
                    TempData["Error"] = error;
                    return RedirectToAction(nameof(BlobStorage));
                }

                // Generate unique, descriptive filename
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                var uniqueId = Guid.NewGuid().ToString("N")[..8]; // First 8 characters
                var cleanFileName = Path.GetFileNameWithoutExtension(file.FileName)
                    .Replace(" ", "-")
                    .Replace("_", "-");

                var uniqueFileName = $"{timestamp}_{uniqueId}_{cleanFileName}{fileExtension}";

                _logger.LogInformation("[StorageController] === UPLOAD DETAILS ===");
                _logger.LogInformation("[StorageController] Original filename: {OriginalName}", file.FileName);
                _logger.LogInformation("[StorageController] Generated filename: {GeneratedName}", uniqueFileName);
                _logger.LogInformation("[StorageController] File size: {Size:N0} bytes ({SizeMB:F2} MB)", file.Length, file.Length / (1024.0 * 1024.0));
                _logger.LogInformation("[StorageController] Content type: {ContentType}", file.ContentType);
                _logger.LogInformation("[StorageController] Target container: studentimages");

                // Perform upload with proper stream handling
                string blobUrl;
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    _logger.LogInformation("[StorageController] File loaded into memory stream, initiating Azure upload...");
                    blobUrl = await _blobService.UploadFileAsync(uniqueFileName, stream, file.ContentType);
                }

                // Success response with comprehensive information
                var successMessage = $"Image '{file.FileName}' uploaded successfully!";
                _logger.LogInformation("[StorageController] === UPLOAD SUCCESS ===");
                _logger.LogInformation("[StorageController] {SuccessMessage}", successMessage);
                _logger.LogInformation("[StorageController] Blob URL: {BlobUrl}", blobUrl);
                _logger.LogInformation("[StorageController] Stored as: {UniqueFileName}", uniqueFileName);

                TempData["Success"] = successMessage;
                TempData["BlobFileName"] = uniqueFileName;
                TempData["BlobOriginalName"] = file.FileName;
                TempData["BlobSize"] = file.Length.ToString();
                TempData["BlobUrl"] = blobUrl;

                // Detailed portal instructions
                TempData["PortalInstructions"] = "To view in Azure Portal: Storage accounts → klmazureapp1 → Containers → studentimages";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StorageController] === UPLOAD FAILED ===");
                _logger.LogError("[StorageController] File: {FileName}, Size: {Size}", file?.FileName ?? "unknown", file?.Length ?? 0);

                var errorMessage = $"Upload failed: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" (Details: {ex.InnerException.Message})";
                    _logger.LogError("[StorageController] Inner exception: {InnerException}", ex.InnerException.Message);
                }

                TempData["Error"] = errorMessage;
            }

            return RedirectToAction(nameof(BlobStorage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBlob(string blobName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(blobName))
                {
                    TempData["Error"] = "Blob name is required for deletion.";
                    return RedirectToAction(nameof(BlobStorage));
                }

                _logger.LogInformation("[StorageController] === DELETING BLOB ===");
                _logger.LogInformation("[StorageController] Blob name: {BlobName}", blobName);

                var deleted = await _blobService.DeleteBlobAsync(blobName);

                if (deleted)
                {
                    TempData["Success"] = $"Successfully deleted '{blobName}' from Azure storage.";
                    _logger.LogInformation("[StorageController] Blob deleted successfully: {BlobName}", blobName);
                }
                else
                {
                    TempData["Warning"] = $"Blob '{blobName}' was not found (may have already been deleted).";
                    _logger.LogWarning("[StorageController] Blob not found for deletion: {BlobName}", blobName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StorageController] === BLOB DELETION FAILED ===");
                _logger.LogError("[StorageController] Blob name: {BlobName}", blobName);
                TempData["Error"] = $"Error deleting blob: {ex.Message}";
            }

            return RedirectToAction(nameof(BlobStorage));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadBlob(string blobName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(blobName))
                {
                    TempData["Error"] = "Blob name is required for download.";
                    return RedirectToAction(nameof(BlobStorage));
                }

                _logger.LogInformation("[StorageController] === DOWNLOADING BLOB ===");
                _logger.LogInformation("[StorageController] Blob name: {BlobName}", blobName);

                var stream = await _blobService.DownloadFileAsync(blobName);
                var contentType = GetContentTypeFromFileName(blobName);

                _logger.LogInformation("[StorageController] Blob download successful: {BlobName}", blobName);
                return File(stream, contentType, blobName);
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("[StorageController] Blob not found for download: {BlobName}", blobName);
                TempData["Error"] = $"File '{blobName}' was not found.";
                return RedirectToAction(nameof(BlobStorage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StorageController] === BLOB DOWNLOAD FAILED ===");
                _logger.LogError("[StorageController] Blob name: {BlobName}", blobName);
                TempData["Error"] = $"Error downloading file: {ex.Message}";
                return RedirectToAction(nameof(BlobStorage));
            }
        }

        // ===== FILE SHARE MANAGEMENT =====
        public async Task<IActionResult> FileShare()
        {
            try
            {
                _logger.LogInformation("[StorageController] === LOADING FILE SHARE PAGE ===");
                _logger.LogInformation("[StorageController] File share: contracts");

                var files = await _fileService.ListFilesAsync("contracts");
                ViewBag.Files = files.ToList();
                ViewBag.FileCount = files.Count();
                ViewBag.ShareName = "contracts";
                ViewBag.StorageAccount = "klmazureapp1";

                _logger.LogInformation("[StorageController] File Share page loaded with {FileCount} files", files.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StorageController] === FILE SHARE PAGE LOAD FAILED ===");

                ViewBag.Error = $"Error loading file share: {ex.Message}";
                ViewBag.Files = new List<string>();
                ViewBag.FileCount = 0;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadContract(IFormFile file, string directory = "contracts")
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    TempData["Error"] = "Please select a contract file to upload.";
                    return RedirectToAction(nameof(FileShare));
                }

                // Validate file size (max 50MB for contracts)
                const long maxFileSize = 50 * 1024 * 1024; // 50MB
                if (file.Length > maxFileSize)
                {
                    TempData["Error"] = $"File size cannot exceed {maxFileSize / (1024 * 1024)}MB. Current size: {file.Length / (1024 * 1024):F1}MB";
                    return RedirectToAction(nameof(FileShare));
                }

                // Validate file types (documents)
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf", ".odt" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Only document files (PDF, DOC, DOCX, TXT, RTF, ODT) are allowed for contracts.";
                    return RedirectToAction(nameof(FileShare));
                }

                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                var uniqueFileName = $"{timestamp}_{file.FileName}";

                _logger.LogInformation("[StorageController] === CONTRACT UPLOAD ===");
                _logger.LogInformation("[StorageController] Original: {OriginalName}", file.FileName);
                _logger.LogInformation("[StorageController] Stored as: {StoredName}", uniqueFileName);
                _logger.LogInformation("[StorageController] Directory: {Directory}", directory);
                _logger.LogInformation("[StorageController] Size: {Size:N0} bytes", file.Length);

                using var stream = file.OpenReadStream();
                await _fileService.UploadFileAsync(directory, uniqueFileName, stream);

                TempData["Success"] = $"Contract '{file.FileName}' uploaded successfully to '{directory}' directory!";
                TempData["ContractDetails"] = $"Stored as: {uniqueFileName}";
                TempData["PortalInstructions"] = "Check Azure Portal: Storage accounts → klmazureapp1 → File shares → contracts";

                _logger.LogInformation("[StorageController] Contract upload successful: {FileName}", uniqueFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StorageController] === CONTRACT UPLOAD FAILED ===");
                TempData["Error"] = $"Contract upload failed: {ex.Message}";
            }

            return RedirectToAction(nameof(FileShare));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(string directory, string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
                {
                    TempData["Error"] = "Directory and file name are required.";
                    return RedirectToAction(nameof(FileShare));
                }

                _logger.LogInformation("[StorageController] Downloading file: {Directory}/{FileName}", directory, fileName);

                var stream = await _fileService.DownloadFileAsync(directory, fileName);
                if (stream != null)
                {
                    var contentType = GetContentTypeFromFileName(fileName);
                    _logger.LogInformation("[StorageController] File download successful: {FileName}", fileName);
                    return File(stream, contentType, fileName);
                }

                TempData["Error"] = "File not found.";
                return RedirectToAction(nameof(FileShare));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StorageController] File download error: {Directory}/{FileName}", directory, fileName);
                TempData["Error"] = $"Error downloading file: {ex.Message}";
                return RedirectToAction(nameof(FileShare));
            }
        }

        // ===== API ENDPOINTS =====
        [HttpGet]
        public async Task<IActionResult> ListBlobs()
        {
            try
            {
                _logger.LogInformation("[StorageController] API: List blobs request");

                var blobs = await _blobService.ListBlobsAsync();
                var diagnostics = await _blobService.GetDiagnosticsAsync();

                return Json(new
                {
                    success = true,
                    containerName = "studentimages",
                    storageAccount = "klmazureapp1",
                    diagnostics = diagnostics,
                    timestamp = DateTime.UtcNow,
                    portalUrl = "https://portal.azure.com",
                    portalPath = "Storage accounts → klmazureapp1 → Containers → studentimages",
                    instructions = new
                    {
                        step1 = "Go to Azure Portal",
                        step2 = "Navigate to Storage accounts",
                        step3 = "Click on 'klmazureapp1'",
                        step4 = "Click on 'Containers'",
                        step5 = "Look for 'studentimages' container",
                        step6 = "Click on container to view contents"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StorageController] API: List blobs failed");
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListFiles(string directory = "contracts")
        {
            try
            {
                var files = await _fileService.ListFilesAsync(directory);
                return Json(new
                {
                    success = true,
                    data = files,
                    count = files.Count(),
                    directory = directory,
                    message = $"Found {files.Count()} files in directory '{directory}'",
                    timestamp = DateTime.UtcNow,
                    portalUrl = $"https://portal.azure.com -> Storage accounts -> klmazureapp1 -> File shares -> {directory}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StorageController] API: List files failed for directory: {Directory}", directory);
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    directory = directory,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // ===== DIAGNOSTICS =====
        public async Task<IActionResult> Diagnostics()
        {
            var diagnostics = new Dictionary<string, object>();

            _logger.LogInformation("[StorageController] === RUNNING COMPREHENSIVE DIAGNOSTICS ===");

            try
            {
                // Blob storage diagnostics
                var blobs = await _blobService.ListBlobsAsync();
                var blobDiagnostics = await _blobService.GetDiagnosticsAsync();

                diagnostics["BlobStorage"] = new
                {
                    Status = "Connected",
                    BlobCount = blobs.Count(),
                    Container = "studentimages",
                    Details = blobDiagnostics,
                    PortalPath = "Storage accounts → klmazureapp1 → Containers → studentimages",
                    LastChecked = DateTime.UtcNow
                };

                _logger.LogInformation("[StorageController] Blob diagnostics completed - {BlobCount} blobs", blobs.Count());
            }
            catch (Exception ex)
            {
                diagnostics["BlobStorage"] = new
                {
                    Status = "Error",
                    Message = ex.Message,
                    Container = "studentimages",
                    LastChecked = DateTime.UtcNow
                };
                _logger.LogError(ex, "[StorageController] Blob diagnostics failed");
            }

            try
            {
                // File share diagnostics
                var files = await _fileService.ListFilesAsync("contracts");
                diagnostics["FileShare"] = new
                {
                    Status = "Connected",
                    FileCount = files.Count(),
                    Share = "contracts",
                    PortalPath = "Storage accounts → klmazureapp1 → File shares → contracts",
                    LastChecked = DateTime.UtcNow
                };

                _logger.LogInformation("[StorageController] File share diagnostics completed - {FileCount} files", files.Count());
            }
            catch (Exception ex)
            {
                diagnostics["FileShare"] = new
                {
                    Status = "Error",
                    Message = ex.Message,
                    Share = "contracts",
                    LastChecked = DateTime.UtcNow
                };
                _logger.LogError(ex, "[StorageController] File share diagnostics failed");
            }

            // Queue diagnostics
            diagnostics["Queue"] = new
            {
                Status = "Service Available",
                Queue = "customernotification",
                PortalPath = "Storage accounts → klmazureapp1 → Queues → customernotification",
                Note = "Message count requires receiving messages",
                LastChecked = DateTime.UtcNow
            };

            // System information
            diagnostics["System"] = new
            {
                Timestamp = DateTime.UtcNow,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                StorageAccount = "klmazureapp1",
                ApplicationName = "CLDV6212POE",
                Version = "1.0"
            };

            // Expected Azure Portal resources
            diagnostics["ExpectedPortalResources"] = new
            {
                StorageAccount = "klmazureapp1",
                Containers = new[] { "studentimages", "studentdocs" },
                Tables = new[] { "Customers", "Products", "StudentInfo" },
                Queues = new[] { "orderprocessing", "inventoryupdate", "imageprocessing", "paymentprocessing", "studentfiles", "customernotification" },
                FileShares = new[] { "contracts" }
            };

            ViewBag.Diagnostics = diagnostics;
            _logger.LogInformation("[StorageController] Comprehensive diagnostics completed successfully");

            return View();
        }

        // ===== TESTING ENDPOINTS =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestUpload()
        {
            try
            {
                _logger.LogInformation("[StorageController] === TESTING BLOB UPLOAD ===");

                // Create a simple test file in memory
                var testContent = $"Test file created at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}\n";
                testContent += "This is a test upload from CLDV6212POE web application.\n";
                testContent += $"Storage Account: klmazureapp1\n";
                testContent += $"Container: studentimages\n";
                testContent += $"Test ID: {Guid.NewGuid()}\n";

                var testBytes = System.Text.Encoding.UTF8.GetBytes(testContent);
                var testFileName = $"test-upload-{DateTime.UtcNow:yyyyMMddHHmmss}.txt";

                using var testStream = new MemoryStream(testBytes);

                _logger.LogInformation("[StorageController] Uploading test file: {TestFileName}", testFileName);
                var blobUrl = await _blobService.UploadFileAsync(testFileName, testStream, "text/plain");

                TempData["Success"] = $"Test upload successful! File: {testFileName}";
                TempData["TestDetails"] = $"Blob URL: {blobUrl}";
                TempData["PortalInstructions"] = "Check Azure Portal: Storage accounts → klmazureapp1 → Containers → studentimages";

                _logger.LogInformation("[StorageController] Test upload completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StorageController] Test upload failed");
                TempData["Error"] = $"Test upload failed: {ex.Message}";
            }

            return RedirectToAction(nameof(BlobStorage));
        }

        [HttpGet]
        public async Task<IActionResult> ContainerStatus()
        {
            try
            {
                var diagnostics = await _blobService.GetDiagnosticsAsync();

                return Json(new
                {
                    success = true,
                    containerName = "studentimages",
                    storageAccount = "klmazureapp1",
                    diagnostics = diagnostics,
                    timestamp = DateTime.UtcNow,
                    portalUrl = "https://portal.azure.com",
                    portalPath = "Storage accounts → klmazureapp1 → Containers → studentimages",
                    instructions = new
                    {
                        step1 = "Go to Azure Portal",
                        step2 = "Navigate to Storage accounts",
                        step3 = "Click on 'klmazureapp1'",
                        step4 = "Click on 'Containers'",
                        step5 = "Look for 'studentimages' container",
                        step6 = "Click on container to view contents"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StorageController] Container status check failed");
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // ===== HELPER METHODS =====
        private string GetContentTypeFromFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".rtf" => "application/rtf",
                ".odt" => "application/vnd.oasis.opendocument.text",
                _ => "application/octet-stream"
            };
        }
    }
}