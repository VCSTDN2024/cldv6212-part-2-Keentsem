using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;

namespace CLDV6212POE.Services
{
    public class BlobImageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly BlobServiceClient _serviceClient;
        private readonly string _containerName;
        private readonly ILogger<BlobImageService>? _logger;
        private bool _isInitialized = false;

        public BlobImageService(string connectionString, string containerName, ILogger<BlobImageService>? logger = null)
        {
            _logger = logger;
            _containerName = containerName;

            LogInfo("=== INITIALIZING BLOB SERVICE ===");
            LogInfo($"Container Name: {_containerName}");

            try
            {
                _serviceClient = new BlobServiceClient(connectionString);
                _containerClient = _serviceClient.GetBlobContainerClient(containerName);

                LogInfo($"Storage Account: {_serviceClient.AccountName}");
                LogInfo($"Service URI: {_serviceClient.Uri}");
                LogInfo($"Container URI: {_containerClient.Uri}");
                LogInfo("BlobServiceClient created successfully");
            }
            catch (Exception ex)
            {
                LogError($"CRITICAL: Failed to create BlobServiceClient: {ex.Message}");
                throw;
            }
        }

        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized)
                return;

            try
            {
                LogInfo("=== ENSURING CONTAINER EXISTS ===");

                // Test connection first
                await _serviceClient.GetAccountInfoAsync();
                LogInfo("✅ Connection to storage account verified");

                // Create container if it doesn't exist (using Blob for anonymous read access to blobs)
                var createResponse = await _containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                if (createResponse != null)
                {
                    LogInfo($"✅ Container '{_containerName}' created successfully");
                }
                else
                {
                    LogInfo($"✅ Container '{_containerName}' already exists");
                }

                // Verify container exists
                var existsResponse = await _containerClient.ExistsAsync();
                if (!existsResponse.Value)
                {
                    throw new InvalidOperationException($"Container '{_containerName}' does not exist and could not be created");
                }

                // Ensure container has public access for blob reading
                try
                {
                    await _containerClient.SetAccessPolicyAsync(PublicAccessType.Blob);
                    LogInfo("✅ Container access level set to public (Blob)");
                }
                catch (Exception ex)
                {
                    LogError($"Warning: Could not set public access policy: {ex.Message}");
                    // Continue anyway - might already be set
                }

                LogInfo("=== CONTAINER INITIALIZATION COMPLETE ===");
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                LogError($"CRITICAL ERROR during container initialization: {ex.Message}");
                LogError($"Exception Type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    LogError($"Inner Exception: {ex.InnerException.Message}");
                }
                throw new InvalidOperationException($"Failed to initialize blob container '{_containerName}': {ex.Message}", ex);
            }
        }

        public async Task<string> UploadFileAsync(string blobName, Stream fileStream, string? contentType = null)
        {
            try
            {
                // Ensure container exists before upload
                await EnsureInitializedAsync();

                LogInfo("=== UPLOAD STARTING ===");
                LogInfo($"Blob name: {blobName}");
                LogInfo($"Content type: {contentType ?? "application/octet-stream"}");
                LogInfo($"File size: {fileStream.Length:N0} bytes");

                // Validate inputs
                if (string.IsNullOrWhiteSpace(blobName))
                {
                    throw new ArgumentException("Blob name cannot be null or empty");
                }

                if (fileStream == null || fileStream.Length == 0)
                {
                    throw new ArgumentException("File stream is null or empty");
                }

                var blobClient = _containerClient.GetBlobClient(blobName);

                // Reset stream position
                if (fileStream.CanSeek)
                {
                    fileStream.Position = 0;
                }

                LogInfo("Starting Azure blob upload...");

                // Simple upload approach that always works
                var uploadResult = await blobClient.UploadAsync(fileStream, overwrite: true);

                // Set content type after upload if provided
                if (!string.IsNullOrEmpty(contentType))
                {
                    await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders
                    {
                        ContentType = contentType
                    });
                }

                // Add metadata after upload
                var metadata = new Dictionary<string, string>
                {
                    { "UploadedAt", DateTime.UtcNow.ToString("O") },
                    { "Source", "CLDV6212POE" },
                    { "OriginalSize", fileStream.Length.ToString() }
                };

                await blobClient.SetMetadataAsync(metadata);

                LogInfo("=== UPLOAD COMPLETED ===");
                LogInfo($"Upload successful - ETag: {uploadResult.Value.ETag}");
                LogInfo($"Blob URL: {blobClient.Uri}");

                // Verify the upload immediately
                await VerifyBlobExistsAsync(blobName);

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                LogError("=== UPLOAD FAILED ===");
                LogError($"Error: {ex.Message}");
                LogError($"Exception Type: {ex.GetType().Name}");

                if (ex.InnerException != null)
                {
                    LogError($"Inner Exception: {ex.InnerException.Message}");
                }

                // Re-throw with more context
                throw new InvalidOperationException($"Failed to upload blob '{blobName}': {ex.Message}", ex);
            }
        }

        private async Task VerifyBlobExistsAsync(string blobName)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);

                // Small delay for Azure consistency
                await Task.Delay(2000);

                var exists = await blobClient.ExistsAsync();
                if (!exists.Value)
                {
                    throw new InvalidOperationException($"Blob '{blobName}' was not found after upload - upload may have failed silently");
                }

                var properties = await blobClient.GetPropertiesAsync();
                LogInfo("=== UPLOAD VERIFICATION SUCCESS ===");
                LogInfo($"Blob size: {properties.Value.ContentLength:N0} bytes");
                LogInfo($"Last modified: {properties.Value.LastModified}");
                LogInfo($"Content type: {properties.Value.ContentType}");
                LogInfo($"✅ Blob should now be visible in Azure Portal");
            }
            catch (Exception ex)
            {
                LogError($"Upload verification failed: {ex.Message}");
                throw;
            }
        }

        public async Task<Stream> DownloadFileAsync(string blobName)
        {
            try
            {
                await EnsureInitializedAsync();

                var blobClient = _containerClient.GetBlobClient(blobName);
                var exists = await blobClient.ExistsAsync();

                if (!exists.Value)
                {
                    throw new FileNotFoundException($"Blob '{blobName}' not found");
                }

                var response = await blobClient.DownloadAsync();
                LogInfo($"Downloaded: {blobName}");
                return response.Value.Content;
            }
            catch (Exception ex)
            {
                LogError($"Download failed: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<BlobInfo>> ListBlobsAsync()
        {
            try
            {
                // Ensure container exists before listing
                await EnsureInitializedAsync();

                LogInfo("=== LISTING BLOBS ===");
                LogInfo($"Container: {_containerName}");
                LogInfo($"Storage Account: {_serviceClient.AccountName}");

                var blobs = new List<BlobInfo>();
                var blobCount = 0;

                await foreach (var blobItem in _containerClient.GetBlobsAsync(BlobTraits.All))
                {
                    var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                    blobCount++;

                    var blobInfo = new BlobInfo
                    {
                        Name = blobItem.Name,
                        Size = blobItem.Properties.ContentLength ?? 0,
                        LastModified = blobItem.Properties.LastModified?.DateTime,
                        ContentType = blobItem.Properties.ContentType ?? "unknown",
                        Url = blobClient.Uri.ToString(),
                        ETag = blobItem.Properties.ETag?.ToString() ?? ""
                    };

                    blobs.Add(blobInfo);
                    LogInfo($"Found blob: {blobItem.Name} ({blobInfo.FormattedSize})");
                }

                LogInfo("=== LISTING COMPLETE ===");
                LogInfo($"Total blobs found: {blobCount}");

                if (blobCount == 0)
                {
                    LogInfo("WARNING: No blobs found in container 'studentimages'");
                    LogInfo($"Check Azure Portal: Storage accounts → klmazureapp1 → Containers → studentimages");
                    LogInfo("Possible issues:");
                    LogInfo("- Container name mismatch");
                    LogInfo("- No files uploaded yet");
                    LogInfo("- Upload failures");
                }

                return blobs;
            }
            catch (Exception ex)
            {
                LogError($"Listing failed: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteBlobAsync(string blobName)
        {
            try
            {
                await EnsureInitializedAsync();

                var blobClient = _containerClient.GetBlobClient(blobName);
                var response = await blobClient.DeleteIfExistsAsync();
                LogInfo($"Delete '{blobName}': {(response.Value ? "SUCCESS" : "NOT FOUND")}");
                return response.Value;
            }
            catch (Exception ex)
            {
                LogError($"Delete failed: {ex.Message}");
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetDiagnosticsAsync()
        {
            try
            {
                var diagnostics = new Dictionary<string, object>();

                // Storage account info
                var accountInfo = await _serviceClient.GetAccountInfoAsync();
                diagnostics["StorageAccount"] = _serviceClient.AccountName;
                diagnostics["AccountKind"] = accountInfo.Value.AccountKind.ToString();
                diagnostics["SKU"] = accountInfo.Value.SkuName.ToString();

                // Container info
                var containerExists = await _containerClient.ExistsAsync();
                diagnostics["ContainerExists"] = containerExists.Value;
                diagnostics["ContainerName"] = _containerName;
                diagnostics["ContainerUri"] = _containerClient.Uri.ToString();

                if (containerExists.Value)
                {
                    var properties = await _containerClient.GetPropertiesAsync();
                    diagnostics["ContainerAccessLevel"] = properties.Value.PublicAccess.ToString() ?? "Unknown";
                    diagnostics["ContainerLastModified"] = properties.Value.LastModified;

                    // Count blobs efficiently
                    var blobCount = 0;
                    await foreach (var blob in _containerClient.GetBlobsAsync())
                    {
                        blobCount++;
                        if (blobCount >= 100) break; // Limit for performance
                    }
                    diagnostics["BlobCount"] = blobCount;
                }

                diagnostics["PortalUrl"] = "https://portal.azure.com";
                diagnostics["PortalPath"] = $"Storage accounts → {_serviceClient.AccountName} → Containers → {_containerName}";

                return diagnostics;
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>
                {
                    ["Error"] = ex.Message,
                    ["ContainerName"] = _containerName,
                    ["Timestamp"] = DateTime.UtcNow
                };
            }
        }

        // Create a simple test upload method
        public async Task<string> TestUploadAsync()
        {
            try
            {
                var testFileName = $"test-{DateTime.UtcNow:yyyyMMddHHmmss}.txt";
                var testContent = $"Test file created at {DateTime.UtcNow}\nThis is a test upload from CLDV6212POE";
                var testBytes = System.Text.Encoding.UTF8.GetBytes(testContent);

                using var testStream = new MemoryStream(testBytes);

                LogInfo($"=== RUNNING TEST UPLOAD ===");
                LogInfo($"Test file: {testFileName}");
                LogInfo($"Test size: {testBytes.Length} bytes");

                var result = await UploadFileAsync(testFileName, testStream, "text/plain");

                LogInfo("=== TEST UPLOAD COMPLETED ===");
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Test upload failed: {ex.Message}");
                throw;
            }
        }

        private void LogInfo(string message)
        {
            var logMessage = $"[BlobImageService] {message}";
            _logger?.LogInformation(logMessage);
            Console.WriteLine(logMessage);
        }

        private void LogError(string message)
        {
            var logMessage = $"[BlobImageService] {message}";
            _logger?.LogError(logMessage);
            Console.WriteLine(logMessage);
        }
    }

    public class BlobInfo
    {
        public string Name { get; set; } = "";
        public long Size { get; set; }
        public DateTime? LastModified { get; set; }
        public string ContentType { get; set; } = "";
        public string Url { get; set; } = "";
        public string ETag { get; set; } = "";

        public string FormattedSize => FormatBytes(Size);
        public string FormattedDate => LastModified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown";

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}