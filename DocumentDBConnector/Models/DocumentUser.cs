using Newtonsoft.Json;

namespace DocumentDBConnector.Models
{
    public class DocumentUser
    {
            [JsonProperty(PropertyName = "userid")]
            public string UserId { get; set; }

  
    }
}