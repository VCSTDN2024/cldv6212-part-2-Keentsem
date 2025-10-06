using Azure;
using Azure.Data.Tables;
using System;
using System.ComponentModel.DataAnnotations;

namespace CLDV6212POE.Models
{
    public class CustomerEntity : ITableEntity
    {
        // Required by ITableEntity
        public string PartitionKey { get; set; } = "CUSTOMER";
        public string RowKey { get; set; } = Guid.NewGuid().ToString("N");
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Your business properties that match the AddCustomer.cshtml form
        [Display(Name = "Customer Name")]
        public string Name { get; set; } = "";

        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = "";

        // Additional properties for completeness
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Phone { get; set; } = "";

        // Constructor to handle name splitting
        public CustomerEntity()
        {
        }

        // Helper method to split full name into first/last
        public void SetFullName(string fullName)
        {
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                FirstName = parts.Length > 0 ? parts[0] : "";
                LastName = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "";
                Name = fullName;
            }
        }
    }
}