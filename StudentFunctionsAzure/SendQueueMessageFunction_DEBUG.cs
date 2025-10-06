using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Queues;
using System.Text.Json;

namespace StudentFunctionsAzure
{
    // TEMPORARY DEBUG VERSION - Use this to troubleshoot queue issues
    // Rename to SendQueueMessageFunction.cs after fixing the original
    public class SendQueueMessageFunction_DEBUG
    {
        private readonly ILogger _logger;

        public SendQueueMessageFunction_DEBUG(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SendQueueMessageFunction_DEBUG>();
        }

        [Function("SendQueueMessageDebug")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequestData req)
        {
            _logger.LogInformation("=== QUEUE MESSAGE DEBUG - START ===");

            try
            {
                // Log request details
                _logger.LogInformation($"Request Method: {req.Method}");
                _logger.LogInformation($"Request URL: {req.Url}");

                // Read request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation($"Request Body: {requestBody}");

                var data = JsonSerializer.Deserialize<QueueMessageRequest>(requestBody);

                if (data == null || string.IsNullOrEmpty(data.Message))
                {
                    _logger.LogWarning("Invalid request: Message is null or empty");
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Message is required");
                    return badResponse;
                }

                _logger.LogInformation($"Parsed Message: '{data.Message}'");

                // Get connection string from environment
                var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("CRITICAL: AzureWebJobsStorage environment variable is NULL or EMPTY!");
                    var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                    await errorResponse.WriteStringAsync("Connection string not configured");
                    return errorResponse;
                }

                // Log connection string details (masked)
                var accountNameMatch = System.Text.RegularExpressions.Regex.Match(connectionString, "AccountName=([^;]+)");
                var accountName = accountNameMatch.Success ? accountNameMatch.Groups[1].Value : "UNKNOWN";
                _logger.LogInformation($"Storage Account: {accountName}");
                _logger.LogInformation($"Connection String Length: {connectionString.Length} characters");

                // Create queue client
                var queueName = "studentfiles";
                _logger.LogInformation($"Creating QueueClient for queue: '{queueName}'");

                QueueClient queueClient;
                try
                {
                    queueClient = new QueueClient(connectionString, queueName);
                    _logger.LogInformation($"QueueClient created successfully");
                    _logger.LogInformation($"Queue URI: {queueClient.Uri}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"FAILED to create QueueClient: {ex.Message}");
                    _logger.LogError($"Exception Type: {ex.GetType().Name}");
                    _logger.LogError($"Stack Trace: {ex.StackTrace}");
                    throw;
                }

                // Ensure queue exists
                _logger.LogInformation("Attempting to create queue if not exists...");
                try
                {
                    var createResponse = await queueClient.CreateIfNotExistsAsync();
                    if (createResponse != null)
                    {
                        _logger.LogInformation($"Queue '{queueName}' created (was new)");
                    }
                    else
                    {
                        _logger.LogInformation($"Queue '{queueName}' already exists");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"FAILED to create/verify queue: {ex.Message}");
                    _logger.LogError($"Exception Type: {ex.GetType().Name}");

                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                    }
                    throw;
                }

                // Send message
                _logger.LogInformation($"Sending message: '{data.Message}'");
                try
                {
                    var sendResponse = await queueClient.SendMessageAsync(data.Message);
                    _logger.LogInformation($"✓ Message sent successfully!");
                    _logger.LogInformation($"  Message ID: {sendResponse.Value.MessageId}");
                    _logger.LogInformation($"  Pop Receipt: {sendResponse.Value.PopReceipt}");
                    _logger.LogInformation($"  Insertion Time: {sendResponse.Value.InsertionTime}");
                    _logger.LogInformation($"  Expiration Time: {sendResponse.Value.ExpirationTime}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"FAILED to send message: {ex.Message}");
                    _logger.LogError($"Exception Type: {ex.GetType().Name}");

                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                    }
                    throw;
                }

                _logger.LogInformation("=== QUEUE MESSAGE DEBUG - SUCCESS ===");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"Message sent successfully to queue '{queueName}'. Check Azure Portal: Storage accounts → {accountName} → Queues → {queueName}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("=== QUEUE MESSAGE DEBUG - FAILED ===");
                _logger.LogError($"Error Type: {ex.GetType().Name}");
                _logger.LogError($"Error Message: {ex.Message}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner Exception Type: {ex.InnerException.GetType().Name}");
                    _logger.LogError($"Inner Exception Message: {ex.InnerException.Message}");
                }

                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }
}
