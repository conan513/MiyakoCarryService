using System.Runtime.Serialization;
using EFT;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public class McsBotDeath
    {
        [DataMember(Name = "BotProfileId")]
        public MongoID BotProfileId;
    }
}
