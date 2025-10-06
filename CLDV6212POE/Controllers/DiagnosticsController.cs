using Microsoft.AspNetCore.Mvc;
using CLDV6212POE.Services;
using CLDV6212POE.Models;
using Azure.Data.Tables;

namespace CLDV6212POE.Controllers
{
    public class DiagnosticsController : Controller
    {
        private readonly ITableStorageService<CustomerEntity> _customers;
        private readonly OrderQueueService _queueService;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(
            ITableStorageService<CustomerEntity> customers,
            OrderQueueService queueService,
            ILogger<DiagnosticsController> logger)
        {
            _customers = customers;
            _queueService = queueService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> TestAzureConnections()
        {
            var results = new List<string>();

            try
            {
                // Test customer table
                _logger.LogInformation("Testing customer table connection...");
                var customers = await _customers.GetAllAsync();
                results.Add($"✅ Customer Table: Connected - Found {customers.Count} customers");
                
                // Test adding a customer
                var testCustomer = new CustomerEntity
                {
                    PartitionKey = "TEST",
                    RowKey = Guid.NewGuid().ToString("N"),
                    FirstName = "Test",
                    LastName = "Customer",
                    Email = "test@example.com",
                    Phone = "123-456-7890"
                };
                
                await _customers.UpsertAsync(testCustomer);
                results.Add($"✅ Customer Save: Success - Test customer saved with ID {testCustomer.RowKey}");
                
                // Verify it was saved
                var savedCustomer = await _customers.GetAsync("TEST", testCustomer.RowKey);
                if (savedCustomer != null)
                {
                    results.Add($"✅ Customer Retrieve: Success - Test customer retrieved");
                }
                else
                {
                    results.Add($"❌ Customer Retrieve: Failed - Could not retrieve saved customer");
                }
            }
            catch (Exception ex)
            {
                results.Add($"❌ Customer Table: Error - {ex.Message}");
                _logger.LogError(ex, "Customer table test failed");
            }

            try
            {
                // Test queue
                _logger.LogInformation("Testing queue connection...");
                await _queueService.SendMessageAsync("Test message from diagnostics");
                results.Add($"✅ Queue Send: Success - Test message sent");
                
                var message = await _queueService.ReceiveMessageAsync();
                if (message != null)
                {
                    results.Add($"✅ Queue Receive: Success - Message: {message}");
                }
                else
                {
                    results.Add($"⚠️ Queue Receive: No messages in queue");
                }
            }
            catch (Exception ex)
            {
                results.Add($"❌ Queue: Error - {ex.Message}");
                _logger.LogError(ex, "Queue test failed");
            }

            return Json(new
            {
                timestamp = DateTime.UtcNow,
                results = results,
                portalInstructions = new
                {
                    step1 = "Go to https://portal.azure.com",
                    step2 = "Navigate to Storage accounts → klmazureapp1",
                    step3 = "Check Tables → Customers for test data",
                    step4 = "Check Queues → customernotification for messages"
                }
            });
        }
    }
}
