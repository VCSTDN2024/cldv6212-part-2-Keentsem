using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CLDV6212POE.Services;
using CLDV6212POE.Models;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using System;

namespace CLDV6212POE
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add MVC controllers + views
            builder.Services.AddControllersWithViews();

            // Add HttpClient for Azure Functions communication
            builder.Services.AddHttpClient();

            // Enhanced logging
            builder.Services.AddLogging(config =>
            {
                config.AddConsole();
                config.AddDebug();
                config.SetMinimumLevel(LogLevel.Information);
            });

            // Load configuration with null checks
            var configuration = builder.Configuration;
            var azureConfig = configuration.GetSection("AzureStorage");
            var connectionString = configuration.GetConnectionString("AzureStorage") 
                ?? throw new InvalidOperationException("AzureStorage connection string not configured");

            // Validate configuration (removed BuildServiceProvider call)
            ValidateConfiguration(connectionString, azureConfig, null);

            // Register TableServiceClient oncetar
            builder.Services.AddSingleton(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<TableServiceClient>>();
                try
                {
                    var serviceClient = new TableServiceClient(connectionString);
                    logger.LogInformation("TableServiceClient created successfully for account: {AccountName}",
                        serviceClient.AccountName);
                    return serviceClient;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create TableServiceClient");
                    throw;
                }
            });

            // Register typed table storage services with error handling
            builder.Services.AddScoped<ITableStorageService<CustomerEntity>>(sp =>
            {
                var serviceClient = sp.GetRequiredService<TableServiceClient>();
                var logger = sp.GetRequiredService<ILogger<TableStorageService<CustomerEntity>>>();
                var tableName = azureConfig["CustomerTable"] ?? "Customers";

                try
                {
                    var tableClient = serviceClient.GetTableClient(tableName);
                    tableClient.CreateIfNotExists();
                    logger.LogInformation("Customer table service initialized: {TableName}", tableName);
                    return new TableStorageService<CustomerEntity>(tableClient);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to initialize customer table service: {TableName}", tableName);
                    throw;
                }
            });

            builder.Services.AddScoped<ITableStorageService<ProductEntity>>(sp =>
            {
                var serviceClient = sp.GetRequiredService<TableServiceClient>();
                var logger = sp.GetRequiredService<ILogger<TableStorageService<ProductEntity>>>();
                var tableName = azureConfig["ProductTable"] ?? "Products";

                try
                {
                    var tableClient = serviceClient.GetTableClient(tableName);
                    tableClient.CreateIfNotExists();
                    logger.LogInformation("Product table service initialized: {TableName}", tableName);
                    return new TableStorageService<ProductEntity>(tableClient);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to initialize product table service: {TableName}", tableName);
                    throw;
                }
            });

            // FIXED: Register Blob service with proper logger injection
            builder.Services.AddSingleton(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<BlobImageService>>();
                var containerName = azureConfig["BlobContainerName"] ?? "studentimages";

                try
                {
                    logger.LogInformation("=== INITIALIZING BLOB SERVICE ===");
                    logger.LogInformation("Container: {ContainerName}", containerName);
                    logger.LogInformation("Connection string configured: {HasConnection}", !string.IsNullOrEmpty(connectionString));

                    var service = new BlobImageService(connectionString, containerName, logger);
                    logger.LogInformation("BlobImageService initialized successfully");
                    return service;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "CRITICAL: Failed to initialize BlobImageService for container: {ContainerName}", containerName);
                    logger.LogError("Error details: {ErrorMessage}", ex.Message);
                    if (ex.InnerException != null)
                    {
                        logger.LogError("Inner exception: {InnerError}", ex.InnerException.Message);
                    }
                    throw;
                }
            });

            // FIXED: Register File service with better error handling
            builder.Services.AddSingleton(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<FileContractService>>();
                var shareName = azureConfig["ContractFileShare"] ?? "contracts";

                try
                {
                    logger.LogInformation("Initializing FileContractService for share: {ShareName}", shareName);
                    var service = new FileContractService(connectionString, shareName);
                    logger.LogInformation("FileContractService initialized successfully");
                    return service;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to initialize FileContractService for share: {ShareName}", shareName);
                    throw;
                }
            });

            // Register CustomerNotification Queue Service
            builder.Services.AddSingleton<OrderQueueService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<OrderQueueService>>();
                var queueName = azureConfig["NotificationQueue"] ?? "customernotification";

                try
                {
                    logger.LogInformation("Initializing OrderQueueService (CustomerNotification) for queue: {QueueName}", queueName);
                    var service = new OrderQueueService(connectionString, queueName);
                    logger.LogInformation("OrderQueueService (CustomerNotification) initialized successfully");
                    return service;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to initialize OrderQueueService for queue: {QueueName}", queueName);
                    throw;
                }
            });

            // Register StudentFiles Queue Service
            builder.Services.AddSingleton<StudentFilesQueueService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<StudentFilesQueueService>>();
                var queueName = azureConfig["StudentQueue"] ?? "studentfiles";

                try
                {
                    logger.LogInformation("Initializing StudentFilesQueueService for queue: {QueueName}", queueName);
                    var service = new StudentFilesQueueService(connectionString, queueName);
                    logger.LogInformation("StudentFilesQueueService initialized successfully");
                    return service;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to initialize StudentFilesQueueService for queue: {QueueName}", queueName);
                    throw;
                }
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();

                // Log startup diagnostics in development
                LogStartupDiagnostics(app.Services, connectionString, azureConfig);
            }

            // Temporarily disabled for Azure Functions testing (mixed HTTP/HTTPS)
            // app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // ADDED: Test services during startup
            TestServicesOnStartup(app.Services);

            // Log application start
            var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
            startupLogger.LogInformation("=== CLDV6212POE APPLICATION STARTED ===");
            startupLogger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
            startupLogger.LogInformation("Storage Account: klmazureapp1");
            startupLogger.LogInformation("Expected Portal Resources:");
            startupLogger.LogInformation("  - Container: studentimages");
            startupLogger.LogInformation("  - Tables: Customers, Products, StudentInfo");
            startupLogger.LogInformation("  - Queues: orderprocessing, customernotification");
            startupLogger.LogInformation("  - File Share: contracts");

            app.Run();
        }

        private static void ValidateConfiguration(string? connectionString, IConfigurationSection azureConfig, ILogger<Program>? logger = null)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(connectionString))
                errors.Add("Azure Storage connection string is missing");

            if (string.IsNullOrEmpty(azureConfig["BlobContainerName"]))
                errors.Add("BlobContainerName configuration is missing");

            if (string.IsNullOrEmpty(azureConfig["CustomerTable"]))
                errors.Add("CustomerTable configuration is missing");

            if (string.IsNullOrEmpty(azureConfig["ProductTable"]))
                errors.Add("ProductTable configuration is missing");

            if (string.IsNullOrEmpty(azureConfig["OrderQueue"]))
                errors.Add("OrderQueue configuration is missing");

            if (string.IsNullOrEmpty(azureConfig["ContractFileShare"]))
                errors.Add("ContractFileShare configuration is missing");

            if (errors.Any())
            {
                var errorMessage = "Configuration validation failed:\n" + string.Join("\n", errors);
                logger?.LogCritical(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            logger?.LogInformation("Configuration validation passed");
        }

        private static void TestServicesOnStartup(IServiceProvider services)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("=== TESTING SERVICES ON STARTUP ===");

            try
            {
                // Test blob service
                var blobService = services.GetRequiredService<BlobImageService>();
                logger.LogInformation("✅ BlobImageService: OK");

                // Test file service
                var fileService = services.GetRequiredService<FileContractService>();
                logger.LogInformation("✅ FileContractService: OK");

                // Test queue service
                var queueService = services.GetRequiredService<OrderQueueService>();
                logger.LogInformation("✅ OrderQueueService: OK");

                // Test table services
                var customerService = services.GetRequiredService<ITableStorageService<CustomerEntity>>();
                var productService = services.GetRequiredService<ITableStorageService<ProductEntity>>();
                logger.LogInformation("✅ Table Services: OK");

                logger.LogInformation("=== ALL SERVICES INITIALIZED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Service initialization test failed: {Error}", ex.Message);
            }
        }

        private static void LogStartupDiagnostics(IServiceProvider services, string connectionString, IConfigurationSection azureConfig)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("=== AZURE STORAGE CONFIGURATION DIAGNOSTICS ===");

            // Parse connection string to get account name
            var accountName = ExtractAccountName(connectionString);
            logger.LogInformation("Storage Account: {AccountName}", accountName ?? "Unknown");

            logger.LogInformation("Configured Services:");
            logger.LogInformation("  - Blob Container: {Container}", azureConfig["BlobContainerName"]);
            logger.LogInformation("  - Document Container: {Container}", azureConfig["DocumentContainerName"]);
            logger.LogInformation("  - Customer Table: {Table}", azureConfig["CustomerTable"]);
            logger.LogInformation("  - Product Table: {Table}", azureConfig["ProductTable"]);
            logger.LogInformation("  - Student Table: {Table}", azureConfig["StudentTable"]);
            logger.LogInformation("  - Order Queue: {Queue}", azureConfig["OrderQueue"]);
            logger.LogInformation("  - Contract File Share: {Share}", azureConfig["ContractFileShare"]);

            logger.LogInformation("Expected Azure Portal Services:");
            logger.LogInformation("  - Containers: 'studentimages', 'studentdocs'");
            logger.LogInformation("  - Tables: 'Customers', 'Products', 'StudentInfo'");
            logger.LogInformation("  - Queues: 'orderprocessing', 'inventoryupdate', 'imageprocessing', 'paymentprocessing', 'studentfiles', 'customernotification'");
            logger.LogInformation("  - File Share: 'contracts'");

            logger.LogInformation("Portal URL: https://portal.azure.com");
            logger.LogInformation("Navigate to: Storage accounts → {AccountName}", accountName);

            logger.LogInformation("=== END DIAGNOSTICS ===");
        }

        private static string? ExtractAccountName(string connectionString)
        {
            try
            {
                var parts = connectionString.Split(';');
                var accountPart = parts.FirstOrDefault(p => p.StartsWith("AccountName="));
                return accountPart?.Split('=')[1];
            }
            catch
            {
                return null;
            }
        }
    }
}