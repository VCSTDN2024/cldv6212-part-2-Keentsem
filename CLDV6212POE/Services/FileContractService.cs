using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System.IO;
using System.Threading.Tasks;

namespace CLDV6212POE.Services
{
    public class FileContractService
    {
        private readonly ShareClient _shareClient;

        public FileContractService(string connectionString, string shareName)
        {
            // Connect to the file share
            _shareClient = new ShareClient(connectionString, shareName);
            _shareClient.CreateIfNotExists(); // Ensure share exists

            Console.WriteLine($"[FileContractService] Using file share: {shareName}");
        }

        // Upload a file (e.g. PDF contract)
        public async Task UploadFileAsync(string directoryName, string fileName, Stream fileStream)
        {
            try
            {
                var directoryClient = _shareClient.GetDirectoryClient(directoryName);
                await directoryClient.CreateIfNotExistsAsync();

                var fileClient = directoryClient.GetFileClient(fileName);

                Console.WriteLine($"[FileContractService] Uploading file: {fileName} to directory: {directoryName}");

                await fileClient.CreateAsync(fileStream.Length);
                fileStream.Position = 0; // Reset stream position
                await fileClient.UploadAsync(fileStream);

                Console.WriteLine($"[FileContractService] Successfully uploaded: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileContractService] Upload failed for {fileName}: {ex.Message}");
                throw;
            }
        }

        // Download a file
        public async Task<Stream?> DownloadFileAsync(string directoryName, string fileName)
        {
            try
            {
                var directoryClient = _shareClient.GetDirectoryClient(directoryName);
                var fileClient = directoryClient.GetFileClient(fileName);

                if (await fileClient.ExistsAsync())
                {
                    var download = await fileClient.DownloadAsync();
                    return download.Value.Content;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileContractService] Download failed for {fileName}: {ex.Message}");
                throw;
            }
        }

        // List all files in a directory
        public async Task<IEnumerable<string>> ListFilesAsync(string directoryName)
        {
            try
            {
                var directoryClient = _shareClient.GetDirectoryClient(directoryName);
                var fileNames = new List<string>();

                await foreach (ShareFileItem item in directoryClient.GetFilesAndDirectoriesAsync())
                {
                    if (!item.IsDirectory)
                        fileNames.Add(item.Name);
                }

                Console.WriteLine($"[FileContractService] Found {fileNames.Count} files in directory: {directoryName}");
                return fileNames;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileContractService] List files failed for directory {directoryName}: {ex.Message}");
                throw;
            }
        }
    }
}