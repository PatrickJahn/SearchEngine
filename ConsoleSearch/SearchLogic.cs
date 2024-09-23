using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Logging;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace ConsoleSearch
{
    public class SearchLogic
    {
        private readonly HttpClient _api = new() { BaseAddress = new Uri("http://word-service") };
        private readonly Dictionary<string, int> _mWords;

        public SearchLogic()
        {
            var response = _api.Send(new HttpRequestMessage(HttpMethod.Get, "Word"));
            var content = response.Content.ReadAsStringAsync().Result;
            _mWords = JsonSerializer.Deserialize<Dictionary<string, int>>(content);
            
        }

        public int GetIdOf(string word)
        {
            return _mWords.GetValueOrDefault(word, -1);
        }

        public Dictionary<int, int> GetDocuments(IEnumerable<int> wordIds)
        {
            using var activity = LoggingService._activitySource.StartActivity();

            var url = "Document/GetByWordIds?wordIds=" + string.Join("&wordIds=", wordIds);
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            var activityContext = activity?.Context ?? Activity.Current?.Context ?? default;
            var propagationContext = new PropagationContext(activityContext, Baggage.Current);
            var propagator = new TraceContextPropagator();
            
            propagator.Inject(propagationContext, request, (r, key, value) =>
            {
                r.Headers.Add(key,value);
            });
            
            var response = _api.Send(request);
            var content = response.Content.ReadAsStringAsync().Result;

            if (content.IsNullOrEmpty())
                return new Dictionary<int, int>();
            
            return JsonSerializer.Deserialize<Dictionary<int, int>>(content);
        }

        public List<string> GetDocumentDetails(IEnumerable<int> docIds)
        {
            using var activity = LoggingService._activitySource.StartActivity();

            var url = "Document/GetByDocIds?docIds=" + string.Join("&docIds=", docIds);
           var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            var activityContext = activity?.Context ?? Activity.Current?.Context ?? default;
            var propagationContext = new PropagationContext(activityContext, Baggage.Current);
            var propagator = new TraceContextPropagator();
            
            propagator.Inject(propagationContext, request, (r, key, value) =>
            {
                r.Headers.Add(key,value);
            });
            var response = _api.Send(request);
            var content = response.Content.ReadAsStringAsync().Result;

            return JsonSerializer.Deserialize<List<string>>(content);
        }
    }
}