using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;

namespace StudentFunctionsAzure
{
    public class UploadToBlobFunction
    {
        private readonly ILogger _logger;

        public UploadToBlobFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadToBlobFunction>();
        }

        [Function("UploadToBlob")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing blob upload request");

            try
            {
                // Log request details
                var contentType = req.Headers.TryGetValues("Content-Type", out var ctValues) ? ctValues.FirstOrDefault() : null;
                var contentLength = req.Headers.TryGetValues("Content-Length", out var clValues) ? clValues.FirstOrDefault() : null;

                _logger.LogInformation($"Content-Type: {contentType}");
                _logger.LogInformation($"Content-Length: {contentLength}");

                // Parse multipart form data
                var formData = await req.ReadFormDataAsync(_logger);

                _logger.LogInformation($"FormData parsed: Files count = {formData?.Files?.Count ?? 0}");

                if (formData == null || !formData.Files.Any())
                {
                    _logger.LogWarning("No file uploaded - formData is null or has no files");
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("No file uploaded");
                    return badResponse;
                }

                var file = formData.Files.First();
                var blobName = formData["blobName"] ?? file.FileName;

                // Get connection string from environment
                var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var blobServiceClient = new BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient("studentimages");

                // Ensure container exists
                await containerClient.CreateIfNotExistsAsync();

                // Upload blob
                var blobClient = containerClient.GetBlobClient(blobName);
                await blobClient.UploadAsync(file.OpenReadStream(), overwrite: true);

                _logger.LogInformation($"Successfully uploaded blob: {blobName}");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"File '{blobName}' uploaded successfully to blob storage");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading blob: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }

    // Helper extension methods
    public static class HttpRequestDataExtensions
    {
        public static async Task<FormData> ReadFormDataAsync(this HttpRequestData req, ILogger logger)
        {
            var formData = new FormData();

            try
            {
                var contentType = req.Headers.TryGetValues("Content-Type", out var values) ? values.FirstOrDefault() : null;
                logger.LogInformation($"[DEBUG] Content-Type: {contentType}");

                if (contentType == null || !contentType.Contains("multipart/form-data"))
                {
                    logger.LogWarning($"[DEBUG] Not multipart/form-data, returning empty formData");
                    return formData;
                }

                var boundaryParts = contentType.Split("boundary=");
                if (boundaryParts.Length < 2)
                {
                    logger.LogError("[DEBUG] No boundary found in Content-Type");
                    return formData;
                }

                var boundary = boundaryParts[1].Trim().Trim('"');
                logger.LogInformation($"[DEBUG] Boundary: {boundary}");

                var reader = new MultipartReader(boundary, req.Body);

                int sectionCount = 0;
                var section = await reader.ReadNextSectionAsync();
                while (section != null)
                {
                    sectionCount++;
                    logger.LogInformation($"[DEBUG] Section {sectionCount} found");

                    var contentDisposition = section.Headers.ContainsKey("Content-Disposition")
                        ? section.Headers["Content-Disposition"].FirstOrDefault()
                        : null;

                    logger.LogInformation($"[DEBUG] Content-Disposition: {contentDisposition}");

                    if (contentDisposition != null)
                    {
                        if (contentDisposition.Contains("filename="))
                        {
                            var fileNameParts = contentDisposition.Split("filename=");
                            if (fileNameParts.Length > 1)
                            {
                                // Extract filename and clean it - split on semicolon to handle filename* attribute
                                var fileName = fileNameParts[1].Split(';')[0].Trim('"', ' ');
                                logger.LogInformation($"[DEBUG] Found file: {fileName}");
                                var memoryStream = new MemoryStream();
                                await section.Body.CopyToAsync(memoryStream);
                                memoryStream.Position = 0;
                                logger.LogInformation($"[DEBUG] File size: {memoryStream.Length} bytes");
                                formData.Files.Add(new FormFile(memoryStream, fileName));
                            }
                        }
                        else if (contentDisposition.Contains("name="))
                        {
                            var nameParts = contentDisposition.Split("name=");
                            if (nameParts.Length > 1)
                            {
                                var name = nameParts[1].Split(';')[0].Trim('"', ' ');
                                using var streamReader = new StreamReader(section.Body);
                                var value = await streamReader.ReadToEndAsync();
                                logger.LogInformation($"[DEBUG] Found field: {name} = {value}");
                                formData[name] = value;
                            }
                        }
                    }

                    section = await reader.ReadNextSectionAsync();
                }

                logger.LogInformation($"[DEBUG] Total sections processed: {sectionCount}, Files found: {formData.Files.Count}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DEBUG] Exception in ReadFormDataAsync");
            }

            return formData;
        }
    }

    public class FormData
    {
        public List<FormFile> Files { get; set; } = new();
        private Dictionary<string, string> _values = new();

        public string? this[string key]
        {
            get => _values.ContainsKey(key) ? _values[key] : null;
            set { if (value != null) _values[key] = value; }
        }
    }

    public class FormFile
    {
        private readonly MemoryStream _stream;
        public string FileName { get; }

        public FormFile(MemoryStream stream, string fileName)
        {
            _stream = stream;
            FileName = fileName;
        }

        public Stream OpenReadStream() => _stream;
    }

    public class MultipartReader
    {
        private readonly string _boundary;
        private readonly Stream _stream;
        private bool _finished = false;

        public MultipartReader(string boundary, Stream stream)
        {
            _boundary = boundary;
            _stream = stream;
        }

        public async Task<MultipartSection?> ReadNextSectionAsync()
        {
            if (_finished) return null;

            var reader = new StreamReader(_stream, leaveOpen: true);
            string? line;

            // Use a loop instead of recursion to find the boundary
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.Contains($"--{_boundary}--"))
                {
                    _finished = true;
                    return null;
                }

                if (line.Contains($"--{_boundary}"))
                {
                    break; // Found the boundary, proceed to read headers
                }
            }

            if (line == null)
            {
                _finished = true;
                return null;
            }

            var headers = new Dictionary<string, List<string>>();
            while ((line = await reader.ReadLineAsync()) != null && !string.IsNullOrWhiteSpace(line))
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = line.Substring(0, colonIndex);
                    var value = line.Substring(colonIndex + 1).Trim();
                    if (!headers.ContainsKey(key))
                        headers[key] = new List<string>();
                    headers[key].Add(value);
                }
            }

            var bodyStream = new MemoryStream();
            var buffer = new byte[4096];
            int bytesRead;

            while ((bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                var chunk = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                if (chunk.Contains($"--{_boundary}"))
                {
                    var boundaryIndex = chunk.IndexOf($"--{_boundary}");
                    if (boundaryIndex > 0)
                    {
                        await bodyStream.WriteAsync(buffer, 0, boundaryIndex);
                    }
                    break;
                }
                await bodyStream.WriteAsync(buffer, 0, bytesRead);
            }

            bodyStream.Position = 0;
            return new MultipartSection { Headers = headers, Body = bodyStream };
        }
    }

    public class MultipartSection
    {
        public Dictionary<string, List<string>> Headers { get; set; } = new();
        public Stream Body { get; set; } = Stream.Null;
    }
}
