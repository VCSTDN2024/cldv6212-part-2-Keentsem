using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Files.Shares;

namespace StudentFunctionsAzure
{
    public class UploadToFileFunction
    {
        private readonly ILogger _logger;

        public UploadToFileFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadToFileFunction>();
        }

        [Function("UploadToFileShare")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing file share upload request");

            try
            {
                // Parse multipart form data
                var formData = await req.ReadFormDataAsync(_logger);

                if (formData == null || !formData.Files.Any())
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("No file uploaded");
                    return badResponse;
                }

                var file = formData.Files.First();
                var fileName = formData["fileShareName"] ?? file.FileName;

                // Get connection string from environment
                var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var shareClient = new ShareClient(connectionString, "contracts");

                // Ensure share exists
                await shareClient.CreateIfNotExistsAsync();

                // Get root directory
                var directoryClient = shareClient.GetRootDirectoryClient();

                // Upload file
                var fileClient = directoryClient.GetFileClient(fileName);
                using var stream = file.OpenReadStream();

                // Read stream into memory to get the actual length
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                await fileClient.CreateAsync(memoryStream.Length);
                await fileClient.UploadAsync(memoryStream);

                _logger.LogInformation($"Successfully uploaded file: {fileName}");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"File '{fileName}' uploaded successfully to file share");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading to file share: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }
}
