//Author: zFlarex <https://zflarex.pw>

using System;
using System.IO;
using System.Net;
using System.Web;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using System.Net.Http;
using Jurassic.Library;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using InstagramBooster.API.Utils;
using System.Collections.Specialized;

namespace InstagramBooster.API
{
    public delegate void OnSuccessEventHandler(Registration registeredAccount);
    public delegate void OnWarningEventHandler(string warningMessage);
    public delegate void OnFailureEventHandler(string errorMessage);

    public class Registration
    {
        private string _instagramUsername;
        private string _instagramPassword;
        private string _instagramEmail;

        private CookieContainer _cookieContainer = new CookieContainer();
        private HttpClient _registrationClient;
        private WebProxy _registrationProxy;

        public string Username { get { return _instagramUsername; } set { _instagramUsername = value; } }
        public string Password { get { return _instagramPassword; } set { _instagramPassword = value; } }
        public string Email    { get { return _instagramEmail; } set { _instagramEmail = value; } }
        public string Proxy    { get { return string.Format("{0}:{1}", _registrationProxy.Address.Host, _registrationProxy.Address.Port);  } }

        public event OnSuccessEventHandler OnSuccess;
        public event OnWarningEventHandler OnWarning;
        public event OnFailureEventHandler OnFailure;

        public Registration(string proxyHost, int proxyPort)
        {
            _instagramUsername = Helper.RandomString(10, 15);
            _instagramPassword = Helper.RandomString(10, 15);
            _instagramEmail = Helper.RandomString(15, 20) + "@gmail.co.uk";

            _registrationProxy = new WebProxy(proxyHost, proxyPort);
            _registrationClient = new HttpClient(new HttpClientHandler() { Proxy = _registrationProxy, CookieContainer = _cookieContainer });

            //_registrationClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            _registrationClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            _registrationClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.82 Safari/537.36 OPR/39.0.2256.48");
            //_registrationClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, lzma, sdch");
            _registrationClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.8");
        }

        public Registration()
        {
            _instagramUsername = Helper.RandomString(10, 15);
            _instagramPassword = Helper.RandomString(10, 15);
            _instagramEmail = Helper.RandomString(15, 20) + "@gmail.co.uk";

            _registrationClient = new HttpClient(new HttpClientHandler() { CookieContainer = _cookieContainer });

            //_registrationClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            _registrationClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            _registrationClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.82 Safari/537.36 OPR/39.0.2256.48");
            //_registrationClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, lzma, sdch");
            _registrationClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.8");
        }

        public async void Create()
        {
            try
            {
                HttpResponseMessage response = await _registrationClient.GetAsync("https://www.instagram.com");

                var responseCookies = _cookieContainer.GetCookies(new Uri("https://www.instagram.com")).Cast<Cookie>().ToList();

                _registrationClient.DefaultRequestHeaders.Add("Origin", "https://www.instagram.com");
                _registrationClient.DefaultRequestHeaders.Add("Referer", "https://www.instagram.com/");
                _registrationClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                _registrationClient.DefaultRequestHeaders.Add("X-Instagram-AJAX", "1");
                _registrationClient.DefaultRequestHeaders.Add("X-CSRFToken", responseCookies[0].Value);

                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(await response.Content.ReadAsStringAsync());

                var javascriptEngine = new Jurassic.ScriptEngine();
                object evalResult = javascriptEngine.Evaluate("(function() { var window = {}; " + htmlDocument.DocumentNode.SelectNodes("//script[@type='text/javascript']")[4].InnerHtml + " return window._sharedData; })()");

                JObject scriptJson = JObject.Parse(JSONObject.Stringify(javascriptEngine, evalResult));

                _cookieContainer.Add(new Cookie("ig_pr", scriptJson["display_properties_server_guess"]["pixel_ratio"].ToString()) { Domain = "instagram.com" });
                _cookieContainer.Add(new Cookie("ig_vw", scriptJson["display_properties_server_guess"]["viewport_width"].ToString()) { Domain = "instagram.com" });

                NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);

                queryString["first_name"] = _instagramUsername;
                queryString["username"] = _instagramUsername;
                queryString["password"] = _instagramPassword;
                queryString["email"] = _instagramEmail;

                StringContent queryStringContent = new StringContent(queryString.ToString());
                queryStringContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                HttpResponseMessage creationResponse = await _registrationClient.PostAsync("https://www.instagram.com/accounts/web_create_ajax/", queryStringContent);

                JObject responseJson = JObject.Parse(await creationResponse.Content.ReadAsStringAsync());

                if (responseJson["account_created"].ToObject<bool>())
                {
                    if (OnSuccess != null)
                    {
                        OnSuccess.BeginInvoke(this, null, null);
                    }
                }
                else
                {
                    if (responseJson["errors"] != null)
                    {
                        StringBuilder stringBuilder = new StringBuilder();

                        foreach (JProperty error in responseJson["errors"])
                        {
                            stringBuilder.Append(error.Value[0] + " ");
                        }

                        if (OnWarning != null)
                        {
                            OnWarning.BeginInvoke(stringBuilder.ToString(), null, null);
                        }
                    }
                }
            }
            catch (Exception ioException) when (ioException is IOException)
            {

            }
            catch (Exception requestException) when (requestException is HttpRequestException)
            {
                if(requestException.InnerException is WebException)
                {
                    if (((WebException)requestException.InnerException).Status == WebExceptionStatus.ConnectFailure)
                    {
                        if (OnFailure != null)
                        {
                            OnFailure.BeginInvoke(string.Format("'{0}' failed to connect to Instagram.", Proxy), null, null);
                        }
                    }
                }

            }
            catch(Exception ex)
            {
                //ignore the other exceptions
            }
        }
    }
}
