using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HRS.Services
{
    public static class ApiService
    {
        private static readonly HttpClient _client = new HttpClient();
        private static readonly string BaseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"] ?? "http://127.0.0.1:5262/api/"; 

        public static async Task<T> GetAsync<T>(string endpoint)
        {
            var response = await _client.GetAsync(BaseUrl + endpoint);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(content);
        }

        public static async Task<T> PostAsync<T>(string endpoint, object data)
        {
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(BaseUrl + endpoint, content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseContent);
        }

        public static async Task PutAsync(string endpoint, object data)
        {
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync(BaseUrl + endpoint, content);
            response.EnsureSuccessStatusCode();
        }

        public static async Task DeleteAsync(string endpoint)
        {
            var response = await _client.DeleteAsync(BaseUrl + endpoint);
            response.EnsureSuccessStatusCode();
        }
    }
}
