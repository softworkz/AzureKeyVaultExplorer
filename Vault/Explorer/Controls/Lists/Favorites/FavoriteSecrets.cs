// Copyright (c) Microsoft Corporation. All rights reserved. 
// Licensed under the MIT License. See License.txt in the project root for license information. 

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Vault.Library;

namespace Microsoft.Vault.Explorer
{
    [JsonDictionary]
    public class FavoriteSecrets : Dictionary<string, FavoriteSecret>
    {
        public FavoriteSecrets() : base() { }

        [JsonConstructor]
        public FavoriteSecrets(IDictionary<string, FavoriteSecret> dictionary) : base(dictionary, StringComparer.CurrentCultureIgnoreCase)
        {
            foreach (string secretName in Keys)
            {
                if (false == Consts.ValidSecretNameRegex.IsMatch(secretName))
                {
                    throw new ArgumentException($"Invalid secret name {secretName}");
                }
            }
        }
    }
}
