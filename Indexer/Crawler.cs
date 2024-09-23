using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Indexer
{
    public class Crawler
    {
        private readonly char[] _sep = " \\\n\t\"$'!,?;.:-_**+=)([]{}<>/@&%€#".ToCharArray();

        private readonly Dictionary<string, int> _words = new ();
        private readonly Dictionary<string, int> _documents = new ();

        private readonly HttpClient _api = new() { BaseAddress = new Uri("http://word-service") };
        
        //Return a dictionary containing all words (as the key)in the file
        // [f] and the value is the number of occurrences of the key in file.
        private ISet<string> ExtractWordsInFile(FileInfo f)
        {
            ISet<string> res = new HashSet<string>();
            var content = File.ReadAllLines(f.FullName);
            foreach (var line in content)
            {
                foreach (var aWord in line.Split(_sep, StringSplitOptions.RemoveEmptyEntries))
                {
                    res.Add(aWord);
                }
            }

            return res;
        }

        private ISet<int> GetWordIdFromWords(ISet<string> src)
        {
            ISet<int> res = new HashSet<int>();

            foreach ( var p in src)
            {
                res.Add(_words[p]);
            }
            return res;
        }

        // Return a dictionary of all the words (the key) in the files contained
        // in the directory [dir]. Only files with an extension in
        // [extensions] is read. The value part of the return value is
        // the number of occurrences of the key.
        public void IndexFilesIn(DirectoryInfo dir, List<string> extensions)
        {
            Console.WriteLine("Crawling " + dir.FullName);
            
            foreach (var file in dir.EnumerateFiles())
            {
                if (!extensions.Contains(file.Extension)) continue;
                _documents.Add(file.FullName, _documents.Count + 1);

                using (var activity = LoggingService._activitySource.StartActivity("Index Documnet"))
                {
                    var documentMessage = new HttpRequestMessage(HttpMethod.Post, "Document?id=" + _documents[file.FullName]  + "&url=" + Uri.EscapeDataString(file.FullName));
                  
                    var activityContext = activity?.Context ?? Activity.Current?.Context ?? default;
                    LoggingService.Log.Information(JsonSerializer.Serialize(activityContext));
                    var propagationContext = new PropagationContext(activityContext, Baggage.Current);
                    var propagator = new TraceContextPropagator();
        
                    propagator.Inject(propagationContext, documentMessage, (r, key, value) =>
                    {
                        r.Headers.Add(key,value);
                    });
                    
                    _api.Send(documentMessage);

                };

           
                var newWords = new Dictionary<string, int>();
                var wordsInFile = ExtractWordsInFile(file);
                foreach (var aWord in wordsInFile)
                {
                    if (_words.ContainsKey(aWord)) continue;
                    _words.Add(aWord, _words.Count + 1);
                    newWords.Add(aWord, _words[aWord]);
                }

        
                var wordMessage = new HttpRequestMessage(HttpMethod.Post, "Word");
                    wordMessage.Content = JsonContent.Create(newWords);
                    //LoggingService.AddActivityInfoToHttpRequest(wordMessage, activity);
                    _api.Send(wordMessage);
                

          
                    var occurrenceMessage = new HttpRequestMessage(HttpMethod.Post,
                        "Occurrence?docId=" + _documents[file.FullName]);
                    occurrenceMessage.Content = JsonContent.Create(GetWordIdFromWords(wordsInFile));

                    _api.Send(occurrenceMessage);

                

            }

            foreach (var d in dir.EnumerateDirectories())
            {
                IndexFilesIn(d, extensions);
            }
        }
    }
}
