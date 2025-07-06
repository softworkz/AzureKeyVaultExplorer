﻿namespace Microsoft.Vault.Explorer.Model.Files.Secrets
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.Vault.Explorer.Common;
    using Newtonsoft.Json;

    [JsonObject]
    public abstract class KeyVaultFile<T> where T : class
    {
        [JsonProperty]
        public readonly string CreatedBy;

        [JsonProperty]
        public readonly DateTimeOffset CreationTime;

        [JsonProperty]
        public readonly byte[] Data;

        [JsonConstructor]
        public KeyVaultFile() { }

        protected KeyVaultFile(T obj)
        {
            this.CreatedBy = $"{Environment.UserDomainName}\\{Environment.UserName}";
            this.CreationTime = DateTimeOffset.UtcNow;
            this.Data = ProtectedData.Protect(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, Formatting.Indented)), null, DataProtectionScope.CurrentUser);
        }

        private string GetValueForDeserialization() => Encoding.UTF8.GetString(ProtectedData.Unprotect(this.Data, null, DataProtectionScope.CurrentUser));

        public T Deserialize() => JsonConvert.DeserializeObject<T>(this.GetValueForDeserialization());

        public string Serialize()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"// --------------------------------------------------------------------------------------");
            sb.AppendLine($"// {Utils.AppName} encrypted {typeof(T).Name}");
            sb.AppendLine($"// Do not edit manually!!!");
            sb.AppendLine($"// This file can be opened only by the user who saved the file");
            sb.AppendLine($"// --------------------------------------------------------------------------------------");
            sb.AppendLine();
            sb.Append(JsonConvert.SerializeObject(this, Formatting.Indented));
            return sb.ToString();
        }
    }
}