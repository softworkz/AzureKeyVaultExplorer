namespace Microsoft.Vault.Explorer
{
    using System;
    using Newtonsoft.Json;

    [JsonObject]
    public class FavoriteSecret
    {
        [JsonProperty]
        public readonly DateTimeOffset CreationTime;

        public FavoriteSecret()
        {
            this.CreationTime = DateTimeOffset.UtcNow;
        }

        [JsonConstructor]
        public FavoriteSecret(DateTimeOffset creationTime)
        {
            this.CreationTime = creationTime;
        }
    }
}