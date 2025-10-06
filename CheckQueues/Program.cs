using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

Console.WriteLine("=== QUEUE MESSAGE CHECKER ===\n");

var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
    ?? throw new InvalidOperationException("AZURE_STORAGE_CONNECTION_STRING environment variable is not set");

// Check customernotification queue
await CheckQueue("customernotification", connectionString);
Console.WriteLine();

// Check studentfiles queue
await CheckQueue("studentfiles", connectionString);

Console.WriteLine("\n=== CHECK COMPLETE ===");

static async Task CheckQueue(string queueName, string connectionString)
{
    try
    {
        Console.WriteLine($"Checking queue: {queueName}");
        Console.WriteLine(new string('-', 50));

        var queueClient = new QueueClient(connectionString, queueName);

        // Get queue properties
        var properties = await queueClient.GetPropertiesAsync();
        var messageCount = properties.Value.ApproximateMessagesCount;

        Console.WriteLine($"✅ Queue exists: {queueName}");
        Console.WriteLine($"📊 Approximate message count: {messageCount}");

        if (messageCount > 0)
        {
            Console.WriteLine($"\n📬 Peeking at up to 5 messages:");

            // Peek messages (doesn't remove them)
            var messages = await queueClient.PeekMessagesAsync(maxMessages: 5);

            int msgNum = 1;
            foreach (var message in messages.Value)
            {
                Console.WriteLine($"\n  Message #{msgNum}:");
                Console.WriteLine($"  ID: {message.MessageId}");
                Console.WriteLine($"  Inserted: {message.InsertedOn}");
                Console.WriteLine($"  Content: {message.MessageText}");
                msgNum++;
            }
        }
        else
        {
            Console.WriteLine("❌ NO MESSAGES FOUND IN QUEUE");
            Console.WriteLine("   This queue is empty.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error checking queue '{queueName}': {ex.Message}");
    }
}
