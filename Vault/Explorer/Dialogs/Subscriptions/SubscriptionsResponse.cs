namespace Microsoft.Vault.Explorer
{
    using Newtonsoft.Json;

    [JsonObject]
    public class SubscriptionsResponse
    {
        [JsonProperty(PropertyName = "value")]
        public Subscription[] Subscriptions { get; set; }
    }
}