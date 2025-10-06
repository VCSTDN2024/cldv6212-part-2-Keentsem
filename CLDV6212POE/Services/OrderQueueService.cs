using Azure.Storage.Queues;
using System.Threading.Tasks;

namespace CLDV6212POE.Services
{
    public class OrderQueueService
    {
        private readonly QueueClient _queueClient;

        public OrderQueueService(string connectionString, string queueName)
        {
            _queueClient = new QueueClient(connectionString, queueName);

            try
            {
                // Use async version properly
                _queueClient.CreateIfNotExistsAsync().GetAwaiter().GetResult();
                Console.WriteLine($"[OrderQueueService] ✅ Queue '{queueName}' ready");
                Console.WriteLine($"[OrderQueueService] Queue URI: {_queueClient.Uri}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OrderQueueService] ❌ Error creating queue '{queueName}': {ex.Message}");
                Console.WriteLine($"[OrderQueueService] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task SendMessageAsync(string message)
        {
            try
            {
                Console.WriteLine($"[OrderQueueService] === SENDING MESSAGE ===");
                Console.WriteLine($"[OrderQueueService] Queue URI: {_queueClient.Uri}");
                Console.WriteLine($"[OrderQueueService] Queue Name: {_queueClient.Name}");
                Console.WriteLine($"[OrderQueueService] Account Name: {_queueClient.AccountName}");
                Console.WriteLine($"[OrderQueueService] Message: {message}");

                var response = await _queueClient.SendMessageAsync(message);

                Console.WriteLine($"[OrderQueueService] Message sent successfully!");
                Console.WriteLine($"[OrderQueueService] Message ID: {response.Value.MessageId}");
                Console.WriteLine($"[OrderQueueService] Insertion Time: {response.Value.InsertionTime}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OrderQueueService] Send message failed: {ex.Message}");
                Console.WriteLine($"[OrderQueueService] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<string?> ReceiveMessageAsync()
        {
            try
            {
                var response = await _queueClient.ReceiveMessageAsync();
                if (response.Value != null)
                {
                    var msg = response.Value.MessageText;
                    Console.WriteLine($"[OrderQueueService] Received message: {msg}");

                    // Delete the message after receiving it
                    await _queueClient.DeleteMessageAsync(response.Value.MessageId, response.Value.PopReceipt);
                    return msg;
                }

                Console.WriteLine($"[OrderQueueService] No messages in queue");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OrderQueueService] Receive message failed: {ex.Message}");
                throw;
            }
        }
    }
}