using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MULTI___DATABASE_MANAGEMENT_PORTAL.Services
{
    public class CookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CookieService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // --- Single value cookie methods ---

        // Get a cookie value by key
        public string GetCookie(string key)
        {
            return _httpContextAccessor.HttpContext?.Request.Cookies[key];
        }

        // Set a cookie value with optional expiration in days
        public void SetCookie(string key, string value, int? expireDays = null)
        {
            var options = new CookieOptions();
            options.Expires = expireDays.HasValue
                ? DateTimeOffset.UtcNow.AddDays(expireDays.Value)
                : DateTimeOffset.UtcNow.AddDays(7); // Default 7 days expiry

            _httpContextAccessor.HttpContext?.Response.Cookies.Append(key, value, options);
        }

        // Delete a cookie by key
        public void DeleteCookie(string key)
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete(key);
        }

        // --- Multi key-value pairs in a single cookie ---

        // Save or update multiple key-value pairs in a single cookie as JSON
        public void SetKeyValueInCookie(string cookieName, Dictionary<string, string> keyValues, int? expireDays = null)
        {
            var existing = GetDictionaryFromCookie(cookieName) ?? new Dictionary<string, string>();

            foreach (var kvp in keyValues)
            {
                existing[kvp.Key] = kvp.Value;
            }


            var json = JsonConvert.SerializeObject(existing);
            SetCookie(cookieName, json, expireDays);
        }

        // Get a specific value from a JSON dictionary stored in a cookie by key
        public string GetValueFromCookie(string cookieName, string key)
        {
            var dict = GetDictionaryFromCookie(cookieName);
            if (dict != null && dict.TryGetValue(key, out string value))
                return value;
            return null;
        }

        // Get all key-value pairs stored in a JSON cookie as a Dictionary
        public Dictionary<string, string> GetDictionaryFromCookie(string cookieName)
        {
            var json = GetCookie(cookieName);
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
            catch
            {
                // If parsing fails, return null to indicate no usable data
                return null;
            }
        }
    }
}