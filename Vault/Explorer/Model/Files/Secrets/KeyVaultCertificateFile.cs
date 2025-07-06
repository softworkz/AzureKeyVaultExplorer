namespace Microsoft.Vault.Explorer
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