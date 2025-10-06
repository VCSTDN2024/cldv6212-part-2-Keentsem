# CLDV6212POE - Azure Cloud Storage Web Application

A comprehensive ASP.NET Core MVC web application demonstrating Azure Cloud Storage services integration, including Blob Storage, Table Storage, Queue Storage, and File Share services.

## 📋 Project Overview

This application is built for the CLDV6212 course and showcases integration with various Azure Storage services. It provides a complete solution for managing customer data, products, images, contracts, and message queuing through Azure's cloud infrastructure.

## 🚀 Features

- **Azure Blob Storage**: Image upload and management for student images
- **Azure Table Storage**: Customer and product entity management
- **Azure Queue Storage**: Message queue processing for orders and notifications
- **Azure File Share**: Contract file storage and retrieval
- **Azure Functions**: Serverless backend processing capabilities
- **MVC Architecture**: Clean separation of concerns with Controllers, Models, and Views

## 🛠️ Technologies Used

- **Framework**: ASP.NET Core 9.0 (MVC)
- **Language**: C# (.NET 9.0)
- **Cloud Platform**: Microsoft Azure
- **Azure SDKs**:
  - Azure.Storage.Blobs v12.19.0
  - Azure.Data.Tables v12.11.0
  - Azure.Storage.Queues v12.15.0
  - Azure.Storage.Files.Shares v12.14.0
- **ORM**: Entity Framework Core v7.0.5

## 📁 Project Structure

```
CLDV6212POE/
├── Controllers/
│   ├── HomeController.cs          # Main application controller
│   ├── StorageController.cs       # Blob storage operations
│   ├── TableStorageController.cs  # Table storage CRUD operations
│   ├── AzureFunctionsController.cs # Azure Functions integration
│   └── DiagnosticsController.cs   # System diagnostics
├── Models/
│   ├── CustomerEntity.cs          # Customer data model
│   ├── ProductEntity.cs           # Product data model
│   └── BlobInfo.cs                # Blob metadata model
├── Services/
│   ├── BlobImageService.cs        # Blob storage service
│   ├── FileContractService.cs     # File share service
│   ├── OrderQueueService.cs       # Order queue service
│   └── StudentFilesQueueService.cs # Student files queue service
├── Views/                         # MVC Views
└── Program.cs                     # Application entry point
```

## ⚙️ Configuration

### Azure Storage Account Setup

1. Create an Azure Storage Account named `klmazureapp1`
2. Create the following resources in your storage account:

**Blob Containers:**
- `studentimages` - For storing student profile images
- `studentdocs` - For storing student documents

**Tables:**
- `Customers` - Customer information storage
- `Products` - Product catalog storage
- `StudentInfo` - Student data storage

**Queues:**
- `orderprocessing` - Order processing queue
- `customernotification` - Customer notification queue
- `studentfiles` - Student file processing queue
- `inventoryupdate` - Inventory update queue
- `imageprocessing` - Image processing queue
- `paymentprocessing` - Payment processing queue

**File Shares:**
- `contracts` - Contract document storage

### Application Configuration

Update `appsettings.json` with your Azure Storage connection string:

```json
{
  "ConnectionStrings": {
    "AzureStorage": "DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT_NAME;AccountKey=YOUR_ACCOUNT_KEY;EndpointSuffix=core.windows.net"
  },
  "AzureStorage": {
    "BlobContainerName": "studentimages",
    "DocumentContainerName": "studentdocs",
    "CustomerTable": "Customers",
    "ProductTable": "Products",
    "StudentTable": "StudentInfo",
    "OrderQueue": "orderprocessing",
    "NotificationQueue": "customernotification",
    "StudentQueue": "studentfiles",
    "ContractFileShare": "contracts"
  }
}
```

## 🔧 Installation & Setup

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 (recommended) or Visual Studio Code
- Azure Storage Account
- Azure Functions Core Tools (for serverless functions)

### Steps

1. **Clone the repository:**
   ```bash
   git clone https://github.com/VCSTDN2024/cldv6212-part-2-Keentsem.git
   cd CLDV6212POE
   ```

2. **Restore NuGet packages:**
   ```bash
   dotnet restore
   ```

3. **Update configuration:**
   - Edit `appsettings.json` with your Azure Storage connection string
   - Ensure all Azure resources are created as specified above

4. **Build the project:**
   ```bash
   dotnet build
   ```

5. **Run the application:**
   ```bash
   dotnet run --project CLDV6212POE/CLDV6212POE.csproj
   ```

6. **Access the application:**
   - Open your browser and navigate to `https://localhost:5001` or `http://localhost:5000`

## 📝 Usage

### Main Features

1. **Home Page**: Landing page with navigation to all features
2. **Image Upload**: Upload student images to Azure Blob Storage
3. **Customer Management**: Add, view, and manage customer records in Table Storage
4. **Product Catalog**: Manage product inventory using Azure Tables
5. **Queue Messages**: Send and process messages through Azure Queues
6. **File Management**: Upload and retrieve contract files from Azure File Share
7. **Diagnostics**: View system health and Azure service connectivity

### API Endpoints

- `GET /` - Home page
- `GET/POST /Storage/UploadImage` - Upload images to blob storage
- `GET/POST /TableStorage/AddCustomer` - Add customer to table storage
- `GET/POST /TableStorage/AddProduct` - Add product to table storage
- `POST /AzureFunctions/*` - Azure Functions integration endpoints

## 🧪 Azure Functions

The solution includes a separate Azure Functions project (`StudentFunctionsAzure`) with the following functions:

- **UploadToBlobFunction**: Upload files to blob storage
- **SaveToStudentTable**: Save student data to table storage
- **SendQueueMessageFunction**: Send messages to queues
- **UploadToFileFunction**: Upload files to file share

## 🔍 Diagnostics & Logging

The application includes comprehensive logging:

- **Startup diagnostics**: Validates all Azure services on application start
- **Service health checks**: Tests connectivity to all Azure resources
- **Error logging**: Detailed error messages and stack traces
- **Configuration validation**: Ensures all required settings are present

View diagnostics at: `/Diagnostics/Index`

## 📦 Dependencies

All dependencies are managed via NuGet:

```xml
<PackageReference Include="Azure.Data.Tables" Version="12.11.0" />
<PackageReference Include="Azure.Storage.Blobs" Version="12.19.0" />
<PackageReference Include="Azure.Storage.Queues" Version="12.15.0" />
<PackageReference Include="Azure.Storage.Files.Shares" Version="12.14.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="7.0.5" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="7.0.0" />
```

## 🐛 Troubleshooting

### Common Issues

1. **Connection String Errors**:
   - Verify your Azure Storage connection string in `appsettings.json`
   - Ensure the storage account exists and is accessible

2. **Resource Not Found**:
   - Verify all containers, tables, queues, and file shares exist in your storage account
   - Check spelling and naming conventions

3. **Service Initialization Failures**:
   - Check the application logs on startup
   - Ensure all required Azure resources are created
   - Verify network connectivity to Azure

### Logs Location

Logs are written to:
- Console output during development
- Debug output in Visual Studio
- Application Insights (if configured)

## 👥 Author

**Student**: Keentsem
**Course**: CLDV6212
**Institution**: The IIE Varsity College

## 📄 License

This project is created for educational purposes as part of the CLDV6212 course.

## 🔗 Useful Links

- [Azure Portal](https://portal.azure.com)
- [Azure Storage Documentation](https://docs.microsoft.com/azure/storage/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/)
- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)

## 🤝 Contributing

This is an academic project. For course-related questions, please contact your instructor.

## ⚠️ Important Notes

- **Security**: Never commit your Azure connection strings to source control
- **Costs**: Monitor your Azure resource usage to avoid unexpected charges
- **Development**: HTTPS redirection is temporarily disabled for Azure Functions testing
- **Storage Account**: Default account name is `klmazureapp1`

---

**Last Updated**: January 2025
**Version**: 1.0.0
