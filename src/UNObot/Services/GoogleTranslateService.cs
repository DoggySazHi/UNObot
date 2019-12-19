﻿using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace UNObot.Services
{
    public class GoogleTranslateService
    {
        private static GoogleTranslateService Instance;
        public static GoogleTranslateService GetSingleton()
        {
            if (Instance == null)
                Instance = new GoogleTranslateService();
            return Instance;
        }
        public string Translate(string text, string fromCulture, string toCulture)
        {
            fromCulture = fromCulture.ToLower();
            toCulture = toCulture.ToLower();

            // normalize the culture in case something like en-us was passed 
            // retrieve only en since Google doesn't support sub-locales
            string[] tokens = fromCulture.Split('-');
            if (tokens.Length > 1)
                fromCulture = tokens[0];

            // normalize ToCulture
            tokens = toCulture.Split('-');
            if (tokens.Length > 1)
                toCulture = tokens[0];

            string url = $@"http://translate.google.com/translate_a/t?client=j&text={HttpUtility.UrlEncode(text)}&hl=en&sl={fromCulture}&tl={toCulture}";

            // Retrieve Translation with HTTP GET call
            string html;
            try
            {
                using WebClient web = new WebClient();

                // MUST add a known browser user agent or else response encoding doen't return UTF-8 (WTF Google?)
                web.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0");
                web.Headers.Add(HttpRequestHeader.AcceptCharset, "UTF-8");

                // Make sure we have response encoding to UTF-8
                web.Encoding = Encoding.UTF8;
                html = web.DownloadString(url);
            }
            catch (Exception ex)
            {
                return $"Error: {ex.GetBaseException().Message}";
            }

            // Extract out trans":"...[Extracted]...","from the JSON string
            string result = Regex.Match(html, "trans\":(\".*?\"),\"", RegexOptions.IgnoreCase).Groups[1].Value;

            if (string.IsNullOrEmpty(html))
            {
                return $"Error: No response was returned.";
            }

            return html;
        }
    }
}