namespace Microsoft.Vault.Explorer
{
    using Microsoft.Azure.KeyVault.Models;

    public class CertificateVersion : CustomVersion
    {
        public readonly CertificateItem CertificateItem;

        public CertificateVersion(int index, CertificateItem certificateItem) : base(index, certificateItem.Attributes.Created, certificateItem.Attributes.Updated, Microsoft.Vault.Library.Utils.GetChangedBy(certificateItem.Tags), certificateItem.Identifier)
        {
            this.CertificateItem = certificateItem;
        }
    }
}