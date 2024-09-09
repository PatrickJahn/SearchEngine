using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ConsoleSearch
{
    public class App
    {
        public void Run()
        {
            SearchLogic mSearchLogic = new SearchLogic();
            Console.WriteLine("Console Search");
            
            while (true)
            {
                Console.WriteLine("enter search terms - q for quit [default: hello]");
                string input = Console.ReadLine() ?? "hello"; // Search for hello by default
                if (input.Equals("q")) break;

                var wordIds = new List<int>();
                var searchTerms = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                foreach (var t in searchTerms)
                {
                    int id = mSearchLogic.GetIdOf(t);
                    if (id != -1)
                    {
                        wordIds.Add(id);
                    }
                    else
                    {
                        Console.WriteLine(t + " will be ignored");
                    }
                }

                DateTime start = DateTime.Now;

                var docIds = mSearchLogic.GetDocuments(wordIds);

                // get details for the first 10             
                var top10 = new List<int>();
                foreach (var p in docIds.Take(10))
                {
                    top10.Add(p.Key);
                }

                TimeSpan used = DateTime.Now - start;

                int idx = 0;
                foreach (var doc in mSearchLogic.GetDocumentDetails(top10))
                {
                    Console.WriteLine("" + (idx+1) + ": " + doc + " -- contains " + docIds[docIds.Keys.ToArray()[idx]] + " search terms");
                    idx++;
                }
                Console.WriteLine("Documents: " + docIds.Count + ". Time: " + used.TotalMilliseconds);
                
                Thread.Sleep(1000);
            }
        }
    }
}
