namespace Service_bus.Volumes;

/// <summary>
/// File helper
/// </summary>
public static class FileHelper
{
    private const int MaxFileSize = 10000; // 10 KB, above this value another file should be created

    private const string DirectoryName = "./data";

    private const string LogFilePrefixName = "log_"; // data/log_1, data_log_2, ...

    private static readonly SemaphoreSlim _Semaphore = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Read data from log files.
    /// </summary>
    public static IEnumerable<(string, Task<string>)> ReadDataAsync()
    {
        string[] dataFiles = GetDataFiles();
        foreach (string fileName in dataFiles)
        {
            yield return (fileName, ReadFileAsync(fileName));
        }
    }

    /// <summary>
    /// Write data to log files.
    /// </summary>
    /// <param name="data">A string representing a serialized data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task</returns>
    public static async Task WriteDataAsync(string? data, CancellationToken cancellationToken)
    {
        if (data == null)
        {
            return;
        }

        // Note: cancellationToken should not be used inside this method to cancel writing data to log file as it will lead to inconsistency
        await _Semaphore.WaitAsync();

        int dataSize = System.Text.ASCIIEncoding.Unicode.GetByteCount(data);

        int lastIndexFile = GetLastIndexFile();
        if (lastIndexFile == -1)
        {
            // no file exist
            lastIndexFile = 0;
        }

        string fileName = DirectoryName + "/" + LogFilePrefixName + lastIndexFile;

        // Check if there is enough space for the new event
        FileInfo fi = new FileInfo(fileName);
        if (fi.Exists && fi.Length + dataSize > MaxFileSize)
        {
            // New file should be created to handle new events
            lastIndexFile++;
            fileName = DirectoryName + "/" + LogFilePrefixName + lastIndexFile;
        }
        await AppendToFileAsync(fileName, data);
        _Semaphore.Release(1);
    }

    /// <summary>
    /// Read data from a file.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>A Task<string></returns>
    public static Task<string> ReadFileAsync(string fileName)
    {
        if (File.Exists(fileName))
        {
            // Read entire text file content as one string
            string content = File.ReadAllText(fileName);
            return Task.FromResult(content);
        }

        throw new FileNotFoundException($"File {fileName} not found");
    }

    private static string[] GetDataFiles()
    {
        return System.IO.Directory
                        .GetFiles(DirectoryName)
                        .Where(file => file.Contains(LogFilePrefixName))
                        .ToArray();
    }

    private static int GetLastIndexFile()
    {
        string[] files = GetDataFiles();

        if (files == null || files.Length == 0)
        {
            return -1;
        }

        return files.Select(file => file.Split('_')[1])
                    .Select(stringIndex =>
                    {
                        if (int.TryParse(stringIndex, out int n))
                        {
                            return n;
                        }
                        return -1;
                    })
                    .Where(index => index >= 0)
                    .Max();
    }

    private static async Task AppendToFileAsync(string path, string data)
    {

        if (!File.Exists(path))
        {
            // Create a file to write to.
            using (StreamWriter sw = File.CreateText(path))
            {
                await sw.WriteAsync(data);
            }
        }
        else
        {
            using (StreamWriter sw = File.AppendText(path))
            {
                await sw.WriteAsync(data);
            }
        }
    }
}