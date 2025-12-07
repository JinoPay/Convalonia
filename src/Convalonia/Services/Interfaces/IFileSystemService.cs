using System.Collections.Generic;
using System.Threading.Tasks;

namespace Convalonia.Services;

/// <summary>
/// Interface for file system operations
/// </summary>
public interface IFileSystemService
{
    Task<string> ReadFileAsync(string filePath);
    Task WriteFileAsync(string filePath, string content);
    Task<List<string>> ListFilesAsync(string directoryPath, string pattern = "*");
    Task<List<FileMatch>> SearchInFilesAsync(string directoryPath, string searchPattern);
    Task DeleteFileAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);
}
