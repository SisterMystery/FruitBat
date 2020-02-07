namespace FruitBat.Authentication
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using System.Collections.Specialized;
    using System.IO;
    using System.Collections.Generic;

    public static class VssAuthenticator
    {
        //============= Config [Edit these with your settings] =====================
        internal const string Project = "One"; 
        internal const string DevOpsCollection = "DefaultCollection";
        public const string AcisCloudvaultSourceId = "ccc3d054-67c0-4c59-8eb1-3b621504f184:63abd086-1335-438e-9acf-04676a747a04";
        internal const string AzureDevOpsHost = "https://msazure.vsrm.visualstudio.com/"; 
        internal const string AzureDevOpsBuildsHost = "https://msazure.visualstudio.com/"; 
        internal const string ApplicationClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1"; 
        internal const string ReplyUri = "urn:ietf:wg:oauth:2.0:oob";                     
        //==========================================================================

        internal const string azureDevOpsResourceId = "499b84ac-1321-427f-aa17-267ca6975798"; //Constant value to target Azure DevOps. Do not change  

        public static HttpClient AuthenticationClient = new HttpClient();
        public static IPlatformParameters AuthPromptBehavior = new PlatformParameters(PromptBehavior.Auto);

        static VssAuthenticator()
        {
                AuthenticationClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                AuthenticationClient.DefaultRequestHeaders.Add("User-Agent", "FruitBat");
                AuthenticationClient.DefaultRequestHeaders.Add("X-TFS-FedAuthRedirect", "Suppress");
        }

        public static AuthenticationHeaderValue GetBearerAuthHeader()
        {
            var authContext = GetAuthenticationContext();
            return GetBearerAuthHeader(authContext);
        }
        
        private static AuthenticationHeaderValue GetBearerAuthHeader(AuthenticationContext authContext)
        {
            var AuthenticationResponse = authContext.AcquireTokenAsync(
                azureDevOpsResourceId,
                ApplicationClientId,
                new Uri(ReplyUri),
                AuthPromptBehavior
            ).Result;

            return new AuthenticationHeaderValue("Bearer", AuthenticationResponse.AccessToken);
        }

        private static AuthenticationContext GetAuthenticationContext(string tenant = null)
        {
            AuthenticationContext context = null;
            if (tenant != null)
            {
                context = new AuthenticationContext("https://login.microsoftonline.com/" + tenant);
            }
            else
            {
                context = new AuthenticationContext("https://login.windows.net/common");
                if (context.TokenCache.Count > 0)
                {
                    string homeTenant = context.TokenCache.ReadItems().First().TenantId;
                    context = new AuthenticationContext("https://login.microsoftonline.com/" + homeTenant);
                }
            }
            return context;
        }
    }
}
