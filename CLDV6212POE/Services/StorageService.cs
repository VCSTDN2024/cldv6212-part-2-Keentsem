using Azure.Data.Tables;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CLDV6212POE.Services
{
    public interface ITableStorageService<T> where T : class, ITableEntity, new()
    {
        Task UpsertAsync(T entity);
        Task<T?> GetAsync(string partitionKey, string rowKey);
        Task<List<T>> GetAllAsync();
        Task<List<T>> QueryByPartitionAsync(string partitionKey);
    }

    public class TableStorageService<T> : ITableStorageService<T> where T : class, ITableEntity, new()
    {
        private readonly TableClient _table;

        public TableStorageService(TableClient tableClient)
        {
            _table = tableClient;
            _table.CreateIfNotExists();
        }

        public async Task UpsertAsync(T entity) =>
            await _table.UpsertEntityAsync(entity);

        public async Task<T?> GetAsync(string partitionKey, string rowKey)
        {
            try
            {
                var resp = await _table.GetEntityAsync<T>(partitionKey, rowKey);
                return resp.Value;
            }
            catch
            {
                return null;
            }
        }

        public Task<List<T>> GetAllAsync()
        {
            var results = _table.Query<T>().ToList();
            return Task.FromResult(results);
        }

        public Task<List<T>> QueryByPartitionAsync(string partitionKey)
        {
            var results = _table.Query<T>(x => x.PartitionKey == partitionKey).ToList();
            return Task.FromResult(results);
        }
    }
}
