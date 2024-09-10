using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace Indexer
{
    public class App
    {
        public void Run()
        {
            var api = new HttpClient() { BaseAddress = new Uri("http://word-service") };
           var d =  api.Send(new HttpRequestMessage(HttpMethod.Delete, "DbManagement"));
            var dd = api.Send(new HttpRequestMessage(HttpMethod.Post, "DbManagement"));    
                Console.WriteLine("DONE2 " + JsonSerializer.Serialize(dd));

                
            var crawler = new Crawler();

            var directoryArray = new DirectoryInfo("maildir").GetDirectories();
            var directories = new List<DirectoryInfo>(directoryArray).OrderBy(d => d.Name).AsEnumerable();
            
            DateTime start = DateTime.Now;
            foreach (var directory in directories)
            {
                crawler.IndexFilesIn(directory, new List<string> { ".txt"});
            }
            
            TimeSpan used = DateTime.Now - start;
            Console.WriteLine("DONE! used " + used.TotalMilliseconds);
        }
    }
}
