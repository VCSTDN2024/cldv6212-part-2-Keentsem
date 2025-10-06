using CLDV6212POE.Models;
using CLDV6212POE.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CLDV6212POE.Controllers
{
    public class TableStorageController : Controller
    {
        private readonly ITableStorageService<CustomerEntity> _customers;
        private readonly ITableStorageService<ProductEntity> _products;
        private readonly ILogger<TableStorageController> _logger;

        public TableStorageController(
            ITableStorageService<CustomerEntity> customers,
            ITableStorageService<ProductEntity> products,
            ILogger<TableStorageController> logger)
        {
            _customers = customers;
            _products = products;
            _logger = logger;
        }

        // ===== Index/Landing Page =====

        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("[TableStorageController] Loading Table Storage Index page");
            return RedirectToAction(nameof(Customers));
        }

        // ===== Customers =====

        [HttpGet]
        public async Task<IActionResult> Customers()
        {
            try
            {
                _logger.LogInformation("[TableStorageController] Loading customers from Azure Table Storage");

                var list = await _customers.GetAllAsync();

                _logger.LogInformation("[TableStorageController] Successfully loaded {Count} customers", list.Count);

                return View(list); // Views/TableStorage/Customers.cshtml
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TableStorageController] Error loading customers");
                TempData["Error"] = $"Error loading customers: {ex.Message}";
                return View(new List<CustomerEntity>());
            }
        }

        [HttpGet]
        public IActionResult AddCustomer()
        {
            _logger.LogInformation("[TableStorageController] Loading Add Customer form");
            return View(new CustomerEntity());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCustomer(CustomerEntity model)
        {
            try
            {
                _logger.LogInformation("[TableStorageController] === ADDING CUSTOMER ===");
                _logger.LogInformation("[TableStorageController] Name: {FirstName} {LastName}", model.FirstName, model.LastName);
                _logger.LogInformation("[TableStorageController] Email: {Email}", model.Email);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[TableStorageController] Model validation failed");
                    foreach (var error in ModelState)
                    {
                        _logger.LogWarning("[TableStorageController] Validation error for {Field}: {Errors}", 
                            error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                    }
                    return View(model);
                }

                // Ensure keys are set properly
                if (string.IsNullOrWhiteSpace(model.PartitionKey))
                    model.PartitionKey = "CUSTOMER";

                if (string.IsNullOrWhiteSpace(model.RowKey))
                    model.RowKey = Guid.NewGuid().ToString("N");

                // Set the full name from first/last
                model.Name = $"{model.FirstName} {model.LastName}".Trim();

                _logger.LogInformation("[TableStorageController] Saving customer with PartitionKey: {PartitionKey}, RowKey: {RowKey}",
                    model.PartitionKey, model.RowKey);

                await _customers.UpsertAsync(model);

                _logger.LogInformation("[TableStorageController] === CUSTOMER SAVED SUCCESSFULLY ===");
                _logger.LogInformation("[TableStorageController] Check Azure Portal: Storage accounts → klmazureapp1 → Tables → Customers");

                TempData["Msg"] = $"Customer '{model.Name}' saved to Azure Table Storage successfully!";
                TempData["Success"] = $"Customer ID: {model.RowKey}";

                return RedirectToAction(nameof(Customers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TableStorageController] === CUSTOMER SAVE FAILED ===");
                _logger.LogError("[TableStorageController] Customer: {FirstName} {LastName}", model.FirstName, model.LastName);

                TempData["Error"] = $"Failed to save customer: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                _logger.LogInformation("[TableStorageController] API: Loading customers from Azure Table Storage");

                var customers = await _customers.GetAllAsync();

                _logger.LogInformation("[TableStorageController] API: Successfully loaded {Count} customers", customers.Count);

                return Json(new
                {
                    success = true,
                    data = customers,
                    count = customers.Count,
                    message = $"Found {customers.Count} customers in Azure Table Storage",
                    timestamp = DateTime.UtcNow,
                    portalUrl = "https://portal.azure.com -> Storage accounts -> klmazureapp1 -> Tables -> Customers"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TableStorageController] API: Error loading customers");
                
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // ===== Products =====

        [HttpGet]
        public async Task<IActionResult> Products()
        {
            try
            {
                _logger.LogInformation("[TableStorageController] Loading products from Azure Table Storage");

                var list = await _products.GetAllAsync();

                _logger.LogInformation("[TableStorageController] Successfully loaded {Count} products", list.Count);

                return View(list); // Views/TableStorage/Products.cshtml
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TableStorageController] Error loading products");
                TempData["Error"] = $"Error loading products: {ex.Message}";
                return View(new List<ProductEntity>());
            }
        }

        [HttpGet]
        public IActionResult AddProduct()
        {
            _logger.LogInformation("[TableStorageController] Loading Add Product form");
            return View(new ProductEntity());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(ProductEntity model)
        {
            try
            {
                _logger.LogInformation("[TableStorageController] === ADDING PRODUCT ===");
                _logger.LogInformation("[TableStorageController] SKU: {Sku}", model.Sku);
                _logger.LogInformation("[TableStorageController] Name: {Name}", model.Name);
                _logger.LogInformation("[TableStorageController] Price: {Price}", model.Price);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[TableStorageController] Product model validation failed");
                    return View(model);
                }

                // Ensure keys are set properly
                if (string.IsNullOrWhiteSpace(model.PartitionKey))
                    model.PartitionKey = "PRODUCT";

                if (string.IsNullOrWhiteSpace(model.RowKey))
                    model.RowKey = Guid.NewGuid().ToString("N");

                _logger.LogInformation("[TableStorageController] Saving product with PartitionKey: {PartitionKey}, RowKey: {RowKey}",
                    model.PartitionKey, model.RowKey);

                await _products.UpsertAsync(model);

                _logger.LogInformation("[TableStorageController] === PRODUCT SAVED SUCCESSFULLY ===");
                _logger.LogInformation("[TableStorageController] Check Azure Portal: Storage accounts → klmazureapp1 → Tables → Products");

                TempData["Msg"] = $"Product '{model.Name}' saved to Azure Table Storage successfully!";
                TempData["Success"] = $"Product ID: {model.RowKey}";

                return RedirectToAction(nameof(Products));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TableStorageController] === PRODUCT SAVE FAILED ===");
                _logger.LogError("[TableStorageController] Product: {Name} ({Sku})", model.Name, model.Sku);

                TempData["Error"] = $"Failed to save product: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                _logger.LogInformation("[TableStorageController] API: Loading products from Azure Table Storage");

                var products = await _products.GetAllAsync();

                _logger.LogInformation("[TableStorageController] API: Successfully loaded {Count} products", products.Count);

                return Json(new
                {
                    success = true,
                    data = products,
                    count = products.Count,
                    message = $"Found {products.Count} products in Azure Table Storage",
                    timestamp = DateTime.UtcNow,
                    portalUrl = "https://portal.azure.com -> Storage accounts -> klmazureapp1 -> Tables -> Products"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TableStorageController] API: Error loading products");
                
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // ===== Diagnostic Actions =====

        [HttpGet]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                _logger.LogInformation("[TableStorageController] Testing Table Storage connections");

                var customerCount = (await _customers.GetAllAsync()).Count;
                var productCount = (await _products.GetAllAsync()).Count;

                var result = new
                {
                    Success = true,
                    Timestamp = DateTime.UtcNow,
                    CustomerCount = customerCount,
                    ProductCount = productCount,
                    Message = "Table Storage connections working properly",
                    PortalInstructions = new
                    {
                        Step1 = "Go to https://portal.azure.com",
                        Step2 = "Navigate to Storage accounts → klmazureapp1",
                        Step3 = "Click on 'Tables'",
                        Step4 = "Look for 'Customers' and 'Products' tables",
                        Step5 = "Click on each table to view stored data"
                    }
                };

                _logger.LogInformation("[TableStorageController] Connection test successful - Customers: {CustomerCount}, Products: {ProductCount}",
                    customerCount, productCount);

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TableStorageController] Connection test failed");

                return Json(new
                {
                    Success = false,
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}