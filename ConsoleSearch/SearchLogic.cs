using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;

namespace ConsoleSearch
{
    public class SearchLogic
    {
        private readonly HttpClient _api = new() { BaseAddress = new Uri("http://localhost:5088/api") };
        private readonly Dictionary<string, int> _mWords;

        public SearchLogic()
        {
            const string url = "Document";
            var response = _api.Send(new HttpRequestMessage(HttpMethod.Get, url));
            var content = response.Content.ReadAsStringAsync().Result;

            _mWords = JsonConvert.DeserializeObject<Dictionary<string, int>>(content);
        }

        public int GetIdOf(string word)
        {
            return _mWords.GetValueOrDefault(word, -1);
        }

        public Dictionary<int, int> GetDocuments(IEnumerable<int> wordIds)
        {
            var url = "Document/GetByWordIds?wordIds=" + string.Join("&wordIds=", wordIds);
            var response = _api.Send(new HttpRequestMessage(HttpMethod.Get, url));
            var content = response.Content.ReadAsStringAsync().Result;

            return JsonConvert.DeserializeObject<Dictionary<int, int>>(content);
        }

        public List<string> GetDocumentDetails(IEnumerable<int> docIds)
        {
            var url = "Document/GetByDocIds?docIds=" + string.Join("&docIds=", docIds);
            var response = _api.Send(new HttpRequestMessage(HttpMethod.Get, url));
            var content = response.Content.ReadAsStringAsync().Result;

            return JsonConvert.DeserializeObject<List<string>>(content);
        }
    }
}