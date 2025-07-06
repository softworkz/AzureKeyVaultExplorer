// Copyright (c) Microsoft Corporation. All rights reserved. 
// Licensed under the MIT License. See License.txt in the project root for license information. 

using Microsoft.Azure.KeyVault;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Vault.Library
{
    /// <summary>
    /// Simple wrapper around KeyVaultClient
    /// </summary>
    internal class KeyVaultClientEx : KeyVaultClient
    {
        public readonly string VaultName;
        public readonly string VaultUri;

        public KeyVaultClientEx(string vaultName, AuthenticationCallback authenticationCallback) : base(authenticationCallback)
        {
            Utils.GuardVaultName(vaultName);
            VaultName = vaultName;
            VaultUri = string.Format(Consts.AzureVaultUriFormat, VaultName);
        }

        private string ToIdentifier(string endpoint, string name, string version) => $"{VaultUri}/{endpoint}/{name}" + (string.IsNullOrEmpty(version) ? "" : $"/{version}");

        public string ToSecretIdentifier(string secretName, string version = null) => ToIdentifier(Consts.SecretsEndpoint, secretName, version);

        public string ToCertificateIdentifier(string certificateName, string version = null) => ToIdentifier(Consts.CertificatesEndpoint, certificateName, version);

        public override string ToString() => VaultUri;
    }
}
