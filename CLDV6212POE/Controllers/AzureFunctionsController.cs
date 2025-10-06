using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using CLDV6212POE.Services;

namespace CLDV6212POE.Controllers
{
    public class AzureFunctionsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureFunctionsController> _logger;
        private readonly OrderQueueService _orderQueueService;

        public AzureFunctionsController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AzureFunctionsController> logger,
            OrderQueueService orderQueueService)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _orderQueueService = orderQueueService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddToTable(string studentId, string studentName)
        {
            try
            {
                var functionUrl = _configuration["AzureFunctionUrls:SaveToTable"];
                if (string.IsNullOrEmpty(functionUrl))
                {
                    ViewBag.TableMessage = "❌ Error: Azure Function URL not configured for order processing";
                    return View("Index");
                }

                var payload = new
                {
                    PartitionKey = "CustomerOrders",
                    RowKey = studentId,
                    Name = studentName
                };

                var httpClient = _httpClientFactory.CreateClient();
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await httpClient.PostAsync(functionUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ViewBag.TableMessage = $"✅ Order {studentId} successfully registered for customer {studentName}! Ready for processing.";
                    _logger.LogInformation($"ABC Retail: Order {studentId} for customer {studentName} registered successfully");
                }
                else
                {
                    ViewBag.TableMessage = $"❌ Failed to register order: {responseContent}";
                    _logger.LogError($"ABC Retail: Failed to register order {studentId}: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                ViewBag.TableMessage = $"❌ System Error: Unable to process order registration - {ex.Message}";
                _logger.LogError(ex, "ABC Retail: Exception while registering order");
            }

            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UploadBlob(IFormFile blobFile, string blobName)
        {
            try
            {
                if (blobFile == null || blobFile.Length == 0)
                {
                    ViewBag.BlobMessage = "❌ Error: No product image selected for upload";
                    return View("Index");
                }

                var functionUrl = _configuration["AzureFunctionUrls:UploadToBlob"];
                if (string.IsNullOrEmpty(functionUrl))
                {
                    ViewBag.BlobMessage = "❌ Error: Azure Function URL not configured for image uploads";
                    return View("Index");
                }

                var httpClient = _httpClientFactory.CreateClient();
                using var formContent = new MultipartFormDataContent();

                // Add file
                var fileStream = blobFile.OpenReadStream();
                var streamContent = new StreamContent(fileStream);
                formContent.Add(streamContent, "file", blobFile.FileName);

                // Add blob name
                var finalBlobName = blobName ?? blobFile.FileName;
                formContent.Add(new StringContent(finalBlobName), "blobName");

                var response = await httpClient.PostAsync(functionUrl, formContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ViewBag.BlobMessage = $"✅ Product image '{finalBlobName}' uploaded successfully! Now available for customer viewing during shopping.";
                    _logger.LogInformation($"ABC Retail: Product image {finalBlobName} uploaded successfully to blob storage");
                }
                else
                {
                    ViewBag.BlobMessage = $"❌ Failed to upload product image: {responseContent}";
                    _logger.LogError($"ABC Retail: Failed to upload product image {finalBlobName}: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                ViewBag.BlobMessage = $"❌ System Error: Unable to upload product image - {ex.Message}";
                _logger.LogError(ex, "ABC Retail: Exception while uploading product image");
            }

            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UploadFileShare(IFormFile fileShareDoc, string fileShareName)
        {
            try
            {
                if (fileShareDoc == null || fileShareDoc.Length == 0)
                {
                    ViewBag.FileMessage = "❌ Error: No contract or document selected for upload";
                    return View("Index");
                }

                var functionUrl = _configuration["AzureFunctionUrls:UploadToFileShare"];
                if (string.IsNullOrEmpty(functionUrl))
                {
                    ViewBag.FileMessage = "❌ Error: Azure Function URL not configured for document management";
                    return View("Index");
                }

                var httpClient = _httpClientFactory.CreateClient();
                using var formContent = new MultipartFormDataContent();

                // Add file
                var fileStream = fileShareDoc.OpenReadStream();
                var streamContent = new StreamContent(fileStream);
                formContent.Add(streamContent, "file", fileShareDoc.FileName);

                // Add file share name
                var finalFileName = fileShareName ?? fileShareDoc.FileName;
                formContent.Add(new StringContent(finalFileName), "fileShareName");

                var response = await httpClient.PostAsync(functionUrl, formContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ViewBag.FileMessage = $"✅ Contract '{finalFileName}' uploaded successfully! Securely stored in Azure File Share for business operations.";
                    _logger.LogInformation($"ABC Retail: Contract/document {finalFileName} uploaded successfully to file share");
                }
                else
                {
                    ViewBag.FileMessage = $"❌ Failed to upload contract: {responseContent}";
                    _logger.LogError($"ABC Retail: Failed to upload contract {finalFileName}: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                ViewBag.FileMessage = $"❌ System Error: Unable to upload contract - {ex.Message}";
                _logger.LogError(ex, "ABC Retail: Exception while uploading contract to file share");
            }

            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> SendQueueMessage(string queueMessage, string queueName = "customernotification")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(queueMessage))
                {
                    ViewBag.QueueMessage = "❌ Error: Order notification message cannot be empty";
                    return View("Index");
                }

                var functionUrl = _configuration["AzureFunctionUrls:SendQueueMessage"];
                if (string.IsNullOrEmpty(functionUrl))
                {
                    ViewBag.QueueMessage = "❌ Error: Azure Function URL not configured for queue messaging";
                    return View("Index");
                }

                var payload = new
                {
                    Message = queueMessage,
                    QueueName = queueName
                };

                var httpClient = _httpClientFactory.CreateClient();
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await httpClient.PostAsync(functionUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ViewBag.QueueMessage = $"✅ Order notification sent successfully! Message queued for reliable delivery to customer.";
                    _logger.LogInformation($"ABC Retail: Order notification sent via Azure Queue: {queueMessage}");
                }
                else
                {
                    ViewBag.QueueMessage = $"❌ Failed to send order notification: {responseContent}";
                    _logger.LogError($"ABC Retail: Failed to send order notification: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                ViewBag.QueueMessage = $"❌ System Error: Unable to send order notification - {ex.Message}";
                _logger.LogError(ex, "ABC Retail: Exception while sending order notification via Azure Function");
            }

            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveQueueMessage(string queueName = "customernotification")
        {
            try
            {
                var functionUrl = _configuration["AzureFunctionUrls:ReceiveQueueMessage"];
                if (string.IsNullOrEmpty(functionUrl))
                {
                    ViewBag.ReceivedMessage = "❌ Error: Azure Function URL not configured for queue retrieval";
                    return View("Index");
                }

                var httpClient = _httpClientFactory.CreateClient();
                var url = $"{functionUrl}?queueName={queueName}";

                var response = await httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Try to parse JSON response
                    try
                    {
                        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        if (jsonResponse.TryGetProperty("MessageText", out var messageText))
                        {
                            ViewBag.ReceivedMessage = $"✅ Order Notification Retrieved: {messageText.GetString()}";
                        }
                        else if (jsonResponse.TryGetProperty("Message", out var msg))
                        {
                            ViewBag.ReceivedMessage = $"ℹ️ {msg.GetString()}";
                        }
                        else
                        {
                            ViewBag.ReceivedMessage = $"✅ Message Retrieved: {responseContent}";
                        }
                    }
                    catch
                    {
                        ViewBag.ReceivedMessage = $"✅ Message Retrieved: {responseContent}";
                    }
                    _logger.LogInformation($"ABC Retail: Order notification retrieved successfully from Azure Queue");
                }
                else
                {
                    ViewBag.ReceivedMessage = $"❌ Failed to retrieve notification: {responseContent}";
                    _logger.LogError($"ABC Retail: Failed to retrieve queue message: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                ViewBag.ReceivedMessage = $"❌ System Error: Unable to retrieve queue message - {ex.Message}";
                _logger.LogError(ex, "ABC Retail: Exception while receiving queue message via Azure Function");
            }

            return View("Index");
        }
    }
}
