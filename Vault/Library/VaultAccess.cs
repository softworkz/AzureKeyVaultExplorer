// Copyright (c) Microsoft Corporation. All rights reserved. 
// Licensed under the MIT License. See License.txt in the project root for license information. 

namespace Microsoft.Vault.Library
{
    using Microsoft.Identity.Client;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Vault.Core;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Linq;

    [JsonObject]
    public abstract class VaultAccess
    {
        private static readonly SemaphoreSlim _authSemaphore = new SemaphoreSlim(1, 1);

        [JsonProperty]
        public readonly string ClientId; // Also known as ApplicationId, see Get-AzureRmADApplication

        [JsonIgnore]
        public readonly int Order;

        public VaultAccess(string clientId, int order)
        {
            Guid r;
            if (!Guid.TryParseExact(clientId, "D", out r))
            {
                throw new ArgumentException($"{clientId} must be a valid GUID in the following format: 00000000-0000-0000-0000-000000000000", nameof(clientId));
            }
            ClientId = clientId;
            Order = order;
        }

        protected abstract Task<AuthenticationResult> AcquireTokenInternalAsync(string[] scopes, string userAlias = "");

        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scopes, string userAlias = "")
        {
            await _authSemaphore.WaitAsync();
            try
            {
                try
                {
                    // First, try to get a token silently
                    return await AcquireTokenSilentAsync(scopes, userAlias);
                }
                catch (MsalUiRequiredException)
                {
                }

                // Silent token acquisition failed, fallback to interactive/credential flow
                return await AcquireTokenInternalAsync(scopes, userAlias);
            }
            finally
            {
                _authSemaphore.Release();
            }
        }

        protected abstract Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scopes, string userAlias = "");

        // Helper method to convert resource URL to scope
        public static string[] ConvertResourceToScopes(string resource)
        {
            if (string.IsNullOrEmpty(resource))
                return new string[0];

            // Convert ADAL resource URLs to MSAL scopes
            if (resource.EndsWith("/"))
                resource = resource.TrimEnd('/');

            return new[] { $"{resource}/.default" };
        }
    }

    [JsonObject]
    public class VaultAccessUserInteractive : VaultAccess
    {
        internal const string PowerShellApplicationId = "1950a258-227b-4e31-a9cf-717495945fc2";

        [JsonProperty]
        public readonly string DomainHint;

        [JsonProperty]
        public readonly string UserAliasType;

        private IPublicClientApplication _publicClientApp;

        public VaultAccessUserInteractive(string domainHint) : base(PowerShellApplicationId, 2)
        {
            DomainHint = string.IsNullOrEmpty(domainHint) ? "microsoft.com" : domainHint;
        }

        [JsonConstructor]
        public VaultAccessUserInteractive(string domainHint, string UserAlias) : base(PowerShellApplicationId, 2)
        {
            DomainHint = string.IsNullOrEmpty(domainHint) ? "microsoft.com" : domainHint;
            UserAliasType = string.IsNullOrEmpty(UserAlias) ? Environment.UserName : UserAlias;
        }

        private IPublicClientApplication GetPublicClientApp()
        {
            if (_publicClientApp == null)
            {
                var builder = PublicClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority($"https://login.microsoftonline.com/{DomainHint}")
                    .WithRedirectUri("http://localhost")
                    .WithDefaultRedirectUri();

                // Disable broker to avoid WAM issues
                try
                {
                    // Try the new way first (MSAL 4.61+)
                    _publicClientApp = builder.Build();
                }
                catch
                {
                    // Fallback for older MSAL versions
                    _publicClientApp = builder.Build();
                }

                // Configure token cache
                var tokenCache = new FileTokenCache(DomainHint);
                tokenCache.ConfigureTokenCache(_publicClientApp.UserTokenCache);
            }
            return _publicClientApp;
        }

        protected override async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scopes, string userAlias = "")
        {
            var app = GetPublicClientApp();
            var accounts = await app.GetAccountsAsync();

            if (!string.IsNullOrEmpty(userAlias))
            {
                var account = accounts.FirstOrDefault(a => a.Username.StartsWith(userAlias, StringComparison.OrdinalIgnoreCase));
                if (account != null)
                {
                    return await app.AcquireTokenSilent(scopes, account).ExecuteAsync();
                }
            }

            if (accounts.Any())
            {
                return await app.AcquireTokenSilent(scopes, accounts.First()).ExecuteAsync();
            }

            throw new MsalUiRequiredException("no_account", "No account found in cache");
        }

        protected override async Task<AuthenticationResult> AcquireTokenInternalAsync(string[] scopes, string userAlias = "")
        {
            if (false == Environment.UserInteractive)
            {
                throw new VaultAccessException($@"Current process PID: {Process.GetCurrentProcess().Id} is running in non user interactive mode. Username: {Environment.UserDomainName}\{Environment.UserName} Machine name: {Environment.MachineName}");
            }

            var app = GetPublicClientApp();
            var builder = app.AcquireTokenInteractive(scopes)
                .WithUseEmbeddedWebView(false);  // Use system browser instead of embedded

            // Attempt to login with provided user alias
            if (!string.IsNullOrEmpty(userAlias))
            {
                builder = builder.WithLoginHint($"{userAlias}@{DomainHint}");
            }

            return await builder.ExecuteAsync();
        }

        public override string ToString() => $"{nameof(VaultAccessUserInteractive)}";
    }

    [JsonObject]
    public class VaultAccessClientCredential : VaultAccess
    {
        [JsonProperty]
        public readonly string ClientSecret;

        private IConfidentialClientApplication _confidentialClientApp;

        [JsonConstructor]
        public VaultAccessClientCredential(string clientId, string clientSecret) : base(clientId, 1)
        {
            Guard.ArgumentNotNull(clientSecret, nameof(clientSecret));
            ClientSecret = clientSecret;
        }

        private IConfidentialClientApplication GetConfidentialClientApp()
        {
            if (_confidentialClientApp == null)
            {
                _confidentialClientApp = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithClientSecret(ClientSecret)
                    .WithAuthority("https://login.microsoftonline.com/common")
                    .Build();

                // Configure token cache
                var tokenCache = new MemoryTokenCache();
                tokenCache.ConfigureTokenCache(_confidentialClientApp.AppTokenCache);
            }
            return _confidentialClientApp;
        }

        protected override async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scopes, string userAlias = "")
        {
            var app = GetConfidentialClientApp();
            return await app.AcquireTokenForClient(scopes).ExecuteAsync();
        }

        protected override async Task<AuthenticationResult> AcquireTokenInternalAsync(string[] scopes, string userAlias = "")
        {
            var app = GetConfidentialClientApp();
            return await app.AcquireTokenForClient(scopes).ExecuteAsync();
        }

        public override string ToString() => $"{nameof(VaultAccessClientCredential)}";
    }

    [JsonObject]
    public class VaultAccessClientCertificate : VaultAccess
    {
        [JsonProperty]
        public readonly string CertificateThumbprint;

        private X509Certificate2 _certificate;
        private IConfidentialClientApplication _confidentialClientApp;

        [JsonConstructor]
        public VaultAccessClientCertificate(string clientId, string certificateThumbprint) : base(clientId, 0)
        {
            Guard.ArgumentIsSha1(certificateThumbprint, nameof(certificateThumbprint));
            CertificateThumbprint = certificateThumbprint;
            _certificate = null;
        }

        private IEnumerable<X509Store> EnumerateX509Stores()
        {
            yield return new X509Store(StoreName.My, StoreLocation.CurrentUser);
            yield return new X509Store(StoreName.My, StoreLocation.LocalMachine);
        }

        private void FindCertificate()
        {
            if (null != _certificate)
            {
                return;
            }

            foreach (var store in EnumerateX509Stores())
            {
                try
                {
                    store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                    var storeCerts = store.Certificates.Find(X509FindType.FindByThumbprint, CertificateThumbprint, false);
                    if ((storeCerts != null) && (storeCerts.Count > 0))
                    {
                        _certificate = storeCerts[0];
                        break;
                    }
                }
                finally
                {
                    store.Close();
                }
            }
        }

        [JsonIgnore]
        public X509Certificate2 Certificate
        {
            get
            {
                FindCertificate();
                if (_certificate == null)
                {
                    throw new CertificateNotFoundException($@"Certificate {CertificateThumbprint} is not installed in CurrentUser\My or in LocalMachine\My stores. Username: {Environment.UserDomainName}\{Environment.UserName} Machine name: {Environment.MachineName}");
                }

                return _certificate;
            }
        }

        private IConfidentialClientApplication GetConfidentialClientApp()
        {
            if (_confidentialClientApp == null)
            {
                _confidentialClientApp = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithCertificate(Certificate)
                    .WithAuthority("https://login.microsoftonline.com/common")
                    .Build();

                // Configure token cache
                var tokenCache = new MemoryTokenCache();
                tokenCache.ConfigureTokenCache(_confidentialClientApp.AppTokenCache);
            }
            return _confidentialClientApp;
        }

        protected override async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scopes, string userAlias = "")
        {
            var app = GetConfidentialClientApp();
            return await app.AcquireTokenForClient(scopes).ExecuteAsync();
        }

        protected override async Task<AuthenticationResult> AcquireTokenInternalAsync(string[] scopes, string userAlias = "")
        {
            var app = GetConfidentialClientApp();
            return await app.AcquireTokenForClient(scopes).ExecuteAsync();
        }

        public override string ToString() => $"{nameof(VaultAccessClientCertificate)}";
    }
}
