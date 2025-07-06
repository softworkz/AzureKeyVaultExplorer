namespace Microsoft.Vault.Explorer
{
    using Microsoft.Azure.KeyVault.Models;

    public class SecretVersion : CustomVersion
    {
        public readonly SecretItem SecretItem;

        public SecretVersion(int index, SecretItem secretItem) : base(index, secretItem.Attributes.Created, secretItem.Attributes.Updated, Microsoft.Vault.Library.Utils.GetChangedBy(secretItem.Tags), secretItem.Identifier)
        {
            this.SecretItem = secretItem;
        }
    }
}