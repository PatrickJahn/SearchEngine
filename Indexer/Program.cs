using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using Logging;

namespace Indexer
{
    internal abstract class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine($"Current Directory: {Directory.GetDirectoryRoot("/")}");
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Use the absolute path directly for the compressed file
            var compressedFilePath = Path.Combine(baseDirectory, "enron/mikro.tar.gz");
            var decompressedFilePath = "mails.tar"; 

            DecompressGzipFile(compressedFilePath, decompressedFilePath);

            try
            {
                // Check and delete any hidden files that may cause conflicts
                var hiddenFilePath = Path.Combine(Directory.GetCurrentDirectory(), "._maildir");
                if (File.Exists(hiddenFilePath))
                {
                    Console.WriteLine($"Hidden file detected: {hiddenFilePath}, deleting...");
                    File.Delete(hiddenFilePath);
                }

                // Ensure the maildir directory doesn't exist before extracting
                if (Directory.Exists("maildir"))
                {
                    Directory.Delete("maildir", true);  // Recursively delete if it exists
                }

                // Extract the contents of the tar file into the current directory
                TarFile.ExtractToDirectory(decompressedFilePath, ".", false);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error while handling files or directories: {ex.Message}");
            }

            // Proceed with other logic
            Renamer.Crawl(new DirectoryInfo("maildir"));
            new App().Run();
        }


        
        static void DecompressGzipFile(string compressedFilePath, string decompressedFilePath)
        {
            try
            {
                using var compressedFileStream = File.OpenRead(compressedFilePath);
                using var decompressedFileStream = File.Create(decompressedFilePath);
                using var gzipStream = new GZipStream(compressedFileStream, CompressionMode.Decompress);
                gzipStream.CopyTo(decompressedFileStream);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"File not found: {compressedFilePath}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during decompression: {ex.Message}");
                throw;
            }
        }

    }
}