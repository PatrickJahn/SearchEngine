using System;
using System.Collections.Generic;
using System.IO;

namespace Indexer
{
    public class Renamer
    {
        private static void RenameFile(FileSystemInfo f)
        {
            if  (f.FullName.EndsWith(".txt")) return;
            if (f.Name.StartsWith('.')) return;
            var ending = f.FullName.EndsWith(".") ? "txt" : ".txt";
            File.Move(f.FullName, f.FullName + ending, true);
        }

        public static void Crawl(DirectoryInfo dir)
        {
            Console.WriteLine("Crawling " + dir.FullName);
            
            foreach (var file in dir.EnumerateFiles())
                RenameFile(file);

            foreach (var d in dir.EnumerateDirectories())
                Crawl(d);
        }
    }
}
