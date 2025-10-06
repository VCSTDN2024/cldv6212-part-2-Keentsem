using Microsoft.EntityFrameworkCore;
using CLDV6212POE.Models;

namespace CLDV6212POE.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Add DbSets for your models here
        public DbSet<ProductEntity> Products { get; set; }
    }
}
