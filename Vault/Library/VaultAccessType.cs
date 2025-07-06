// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Vault.Library
{
    using Microsoft.Vault.Core;
    using Newtonsoft.Json;

    public enum VaultAccessTypeEnum
    {
        ReadOnly,
        ReadWrite,
    }

    [JsonObject(IsReference = true)]
    public class VaultAccessType
    {
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Objects)]
        public readonly VaultAccess[] ReadOnly;

        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Objects)]
        public readonly VaultAccess[] ReadWrite;

        [JsonConstructor]
        public VaultAccessType(VaultAccess[] readOnly, VaultAccess[] readWrite)
        {
            Guard.ArgumentNotNull(readOnly, nameof(readOnly));
            Guard.ArgumentNotNull(readWrite, nameof(readWrite));
            this.ReadOnly = readOnly;
            this.ReadWrite = readWrite;
        }
    }
}