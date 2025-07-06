namespace Microsoft.Vault.Explorer.Model.Files.Secrets
{
    using Microsoft.Azure.KeyVault.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents .kv-certificate file
    /// </summary>
    [JsonObject]
    public class KeyVaultCertificateFile : KeyVaultFile<CertificateBundle>
    {
        public KeyVaultCertificateFile() : base() { }
        public KeyVaultCertificateFile(CertificateBundle cb) : base(cb) { }
    }
}