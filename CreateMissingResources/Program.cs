using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Blobs;
using Azure.Data.Tables;

Console.WriteLine("=== CREATING MISSING AZURE RESOURCES ===");
Console.WriteLine("Storage Account: klmazureapp1\n");

var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
    ?? throw new InvalidOperationException("AZURE_STORAGE_CONNECTION_STRING environment variable is not set");

// Create missing table
Console.WriteLine("📋 Creating Table: StudentInfo");
await CreateTable(connectionString, "StudentInfo");

// Create missing queues
var missingQueues = new[] { "orderprocessing", "inventoryupdate", "imageprocessing", "paymentprocessing" };
Console.WriteLine("\n📬 Creating Missing Queues:");
foreach (var queueName in missingQueues)
{
    await CreateQueue(connectionString, queueName);
}

// Create missing container
Console.WriteLine("\n📦 Creating Container: studentdocs");
await CreateContainer(connectionString, "studentdocs");

Console.WriteLine("\n=== ALL RESOURCES CREATED ===");
Console.WriteLine("\nRun VerifyAzureResources to confirm all resources exist.\n");

static async Task CreateTable(string connectionString, string tableName)
{
    try
    {
        var tableServiceClient = new TableServiceClient(connectionString);
        var tableClient = tableServiceClient.GetTableClient(tableName);

        await tableClient.CreateIfNotExistsAsync();
        Console.WriteLine($"  ✅ Table '{tableName}' created successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ Failed to create table '{tableName}': {ex.Message}");
    }
}

static async Task CreateQueue(string connectionString, string queueName)
{
    try
    {
        var queueClient = new QueueClient(connectionString, queueName);
        await queueClient.CreateIfNotExistsAsync();
        Console.WriteLine($"  ✅ Queue '{queueName}' created successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ Failed to create queue '{queueName}': {ex.Message}");
    }
}

static async Task CreateContainer(string connectionString, string containerName)
{
    try
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await containerClient.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.None);
        Console.WriteLine($"  ✅ Container '{containerName}' created successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ Failed to create container '{containerName}': {ex.Message}");
    }
}
