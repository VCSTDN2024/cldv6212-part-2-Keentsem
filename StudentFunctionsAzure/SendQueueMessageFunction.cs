using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Queues;
using System.Text.Json;

namespace StudentFunctionsAzure
{
    public class SendQueueMessageFunction
    {
        private readonly ILogger _logger;

        public SendQueueMessageFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SendQueueMessageFunction>();
        }

        // Function to WRITE to queue
        [Function("SendQueueMessage")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing queue message WRITE request");

            try
            {
                // Read request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonSerializer.Deserialize<QueueMessageRequest>(requestBody);

                if (data == null || string.IsNullOrEmpty(data.Message))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Message is required");
                    return badResponse;
                }

                // Get connection string from environment
                var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

                // Use provided queue name or default to customernotification
                var queueName = data.QueueName ?? "customernotification";
                var queueClient = new QueueClient(connectionString, queueName);

                // Ensure queue exists
                await queueClient.CreateIfNotExistsAsync();

                // Send message
                await queueClient.SendMessageAsync(data.Message);

                _logger.LogInformation($"Successfully sent message to queue '{queueName}': {data.Message}");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"Message sent successfully to queue '{queueName}'");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending queue message: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        // Function to READ from queue
        [Function("ReceiveQueueMessage")]
        public async Task<HttpResponseData> ReceiveMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing queue message READ request");

            try
            {
                // Get queue name from query parameter or use default
                var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var queueName = queryParams["queueName"] ?? "customernotification";

                // Get connection string from environment
                var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var queueClient = new QueueClient(connectionString, queueName);

                // Ensure queue exists
                await queueClient.CreateIfNotExistsAsync();

                // Receive message (this retrieves and deletes the message)
                var receivedMessage = await queueClient.ReceiveMessageAsync();

                if (receivedMessage.Value != null)
                {
                    var message = receivedMessage.Value;

                    _logger.LogInformation($"Successfully received message from queue '{queueName}': {message.MessageText}");

                    // Delete the message after receiving
                    await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);

                    var response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(new
                    {
                        Success = true,
                        QueueName = queueName,
                        MessageId = message.MessageId,
                        MessageText = message.MessageText,
                        InsertedOn = message.InsertedOn,
                        DequeueCount = message.DequeueCount
                    });
                    return response;
                }
                else
                {
                    _logger.LogInformation($"No messages available in queue '{queueName}'");
                    var response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(new
                    {
                        Success = true,
                        QueueName = queueName,
                        Message = "No messages in queue"
                    });
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error receiving queue message: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }

    public class QueueMessageRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? QueueName { get; set; }  // Optional: defaults to customernotification
    }
}
