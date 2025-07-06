// Copyright (c) Microsoft Corporation. All rights reserved. 
// Licensed under the MIT License. See License.txt in the project root for license information. 

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Vault.Explorer
{
    /// <summary>
    /// Represents .kv-secret file
    /// </summary>
    [JsonObject]
    public class KeyVaultSecretFile : KeyVaultFile<SecretBundle>
    {
        public KeyVaultSecretFile() : base() { }
        public KeyVaultSecretFile(SecretBundle secret) : base(secret) { }
    }
}
