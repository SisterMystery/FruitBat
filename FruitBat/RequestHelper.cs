namespace FruitBat.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Authentication;
    using System.Net.Http.Headers;
    using System.Net.Http;
    using System.Threading.Tasks;

    public static class RequestHelper
    {
        public static HttpClient RequestClient = new HttpClient();
        
        static RequestHelper()
        {
                RequestClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                RequestClient.DefaultRequestHeaders.Add("User-Agent", "FruitBat");
                RequestClient.DefaultRequestHeaders.Add("X-TFS-FedAuthRedirect", "Suppress");
        }

        public static async Task<string> SendRequestAsync(AuthenticationHeaderValue authHeader, Uri uriPath)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = uriPath
            };
            request.Headers.Authorization = authHeader;
        
            HttpResponseMessage response = await RequestClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException();
            } 
            else
            {
                throw new HttpRequestException(response.ToString());
            }
        }

        public static async Task<string> SendRequestAsync(Uri uriPath)
        {
            var bearerAuthHeader = VssAuthenticator.GetBearerAuthHeader();

            return await SendRequestAsync(bearerAuthHeader, uriPath);
        }

        public static async Task<string> TestRequestAsync(string uriPath)
        {
            return await SendRequestAsync(new Uri(uriPath));
        }

        public static string ToQueryString(this IDictionary<string, string> queryArguments)
        {
            if(queryArguments == null || queryArguments.Count == 0)
            {
                return string.Empty;
            }

            var formattedQueryArgs = queryArguments.
                Where(keyValue => !string.IsNullOrWhiteSpace(keyValue.Value)).
                Select(keyValue => $"{keyValue.Key}={keyValue.Value}");

            return $"{String.Join("&", formattedQueryArgs)}";
        }
        
        public static Uri BuildRequestUri(string host, string apiSubPath, IDictionary<string,string> queryParameters = null)
        {
            return new UriBuilder(host)
            {
                Path = apiSubPath,
                Query = queryParameters.ToQueryString()
            }.Uri;
        }

        
        public static async Task<IEnumerable<string>> ParalellRequest(params Uri[] requesetAddresses)
        {
            var requests = requesetAddresses.Select(async address => await SendRequestAsync(address));
            return await Task.WhenAll(requests);
        }
    }
}
