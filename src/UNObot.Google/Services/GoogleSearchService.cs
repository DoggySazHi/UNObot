using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UNObot.Google.Templates;

namespace UNObot.Google.Services
{
    public class SearchResult
    {
        public string Preview { get; }
        public string URL { get; }

        public SearchResult(string preview, string url)
        {
            Preview = preview;
            URL = url;
        }
    }
    
    public class GoogleSearchService
    {
        private const string SearchPrefixCore = "https://www.googleapis.com/customsearch/v1?key={0}&cx={1}&q=";
        private readonly string _searchPrefix;
        private static HttpClient _client;

        public GoogleSearchService(GoogleConfig config)
        {
            _searchPrefix = string.Format(SearchPrefixCore, config.Key, config.Context);
            _client = new HttpClient();
        }

        public async Task<SearchResult> Search(string query)
        {
            query = Uri.EscapeDataString(query);
            var request = await _client.GetAsync(_searchPrefix + query);
            var json = await request.Content.ReadAsStringAsync();
            var obj = JObject.Parse(json);
            var first = obj["items"]?[0];
            return first == null ? null : new SearchResult(first["snippet"]?.ToString(), first["formattedUrl"]?.ToString());
        }
        
        
    }
}