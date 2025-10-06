using Azure.Storage.Queues;
using System.Threading.Tasks;

namespace CLDV6212POE.Services
{
    public class StudentFilesQueueService
    {
        private readonly QueueClient _queueClient;

        public StudentFilesQueueService(string connectionString, string queueName)
        {
            _queueClient = new QueueClient(connectionString, queueName);

            try
            {
                // Use async version properly
                _queueClient.CreateIfNotExistsAsync().GetAwaiter().GetResult();
                Console.WriteLine($"[StudentFilesQueueService] ✅ Queue '{queueName}' ready");
                Console.WriteLine($"[StudentFilesQueueService] Queue URI: {_queueClient.Uri}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StudentFilesQueueService] ❌ Error creating queue '{queueName}': {ex.Message}");
                Console.WriteLine($"[StudentFilesQueueService] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task SendMessageAsync(string message)
        {
            try
            {
                Console.WriteLine($"[StudentFilesQueueService] Sending message: {message}");
                await _queueClient.SendMessageAsync(message);
                Console.WriteLine($"[StudentFilesQueueService] Message sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StudentFilesQueueService] Send message failed: {ex.Message}");
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
                    Console.WriteLine($"[StudentFilesQueueService] Received message: {msg}");

                    // Delete the message after receiving it
                    await _queueClient.DeleteMessageAsync(response.Value.MessageId, response.Value.PopReceipt);
                    return msg;
                }

                Console.WriteLine($"[StudentFilesQueueService] No messages in queue");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StudentFilesQueueService] Receive message failed: {ex.Message}");
                throw;
            }
        }
    }
}
