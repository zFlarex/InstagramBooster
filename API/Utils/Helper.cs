using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace InstagramBooster.API.Utils
{
    class Helper
    {
        private static CryptoRandom cRandom = new CryptoRandom();
        private const string characterSet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static string RandomString(int minimumLength, int maximumLength)
        {
            return new string(Enumerable.Repeat(characterSet, cRandom.Next(minimumLength, maximumLength))
              .Select(s => s[cRandom.Next(s.Length)]).ToArray());
        }

        public static async Task<bool> IsProxyActive(HttpClient httpClient)
        {
            HttpResponseMessage response = await httpClient.GetAsync("https://api.ipify.org?format=json");
            JObject json = JObject.Parse(await response.Content.ReadAsStringAsync());

            if(json["ip"] != null)
            {
                return json["ip"].ToString() != "127.0.0.1";
            }

            return false;
        }
    }
}
