using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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
        private const string SearchPrefix = "https://www.googleapis.com/customsearch/v1?key=AIzaSyDteTSAAAWYlVdHEuq-RGhO-_tdWUsmCPo&cx=95f69753b34f1c16a&q=";

        public async Task<SearchResult> Search(string query)
        {
            query = Uri.EscapeDataString(query);
            var request = (HttpWebRequest) WebRequest.Create(SearchPrefix + query);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using var response = (HttpWebResponse)await request.GetResponseAsync();
            await using var stream = response.GetResponseStream();
            using var reader = new StreamReader(stream!);
            var json = await reader.ReadToEndAsync();
            var obj = JObject.Parse(json);
            var first = obj["items"]?[0];
            return first == null ? null : new SearchResult(first["snippet"]?.ToString(), first["formattedUrl"]?.ToString());
        }
    }
}