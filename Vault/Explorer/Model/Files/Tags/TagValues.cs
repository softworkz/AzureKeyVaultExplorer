namespace Microsoft.Vault.Explorer.Model.Files.Tags
{
    using System;

    public class TagValues
    {
        public String tagvalue;
        public override String ToString()
        {
            return this.tagvalue;
        }
        public TagValues(string tag) { this.tagvalue = tag; }
        public TagValues() : base() { }
    }
}