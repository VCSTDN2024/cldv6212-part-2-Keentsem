using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using System.Text.Json;

namespace StudentFunctionsAzure
{
    public class SaveToStudentTable
    {
        private readonly ILogger _logger;

        public SaveToStudentTable(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SaveToStudentTable>();
        }

        [Function("SaveToTable")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing request to save student to table storage");

            try
            {
                // Read request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonSerializer.Deserialize<StudentTableEntity>(requestBody);

                if (data == null || string.IsNullOrEmpty(data.PartitionKey) || string.IsNullOrEmpty(data.RowKey))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid request data. PartitionKey and RowKey are required.");
                    return badResponse;
                }

                // Get connection string from environment
                var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var tableClient = new TableClient(connectionString, "StudentInfo");

                // Ensure table exists
                await tableClient.CreateIfNotExistsAsync();

                // Add entity to table
                await tableClient.AddEntityAsync(data);

                _logger.LogInformation($"Successfully added student {data.RowKey} to StudentInfo table");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync("Student added successfully to table storage");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving to table: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }

    public class StudentTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }
    }
}
