using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record McsBotDeathRequestData : IRequestData
    {
        [JsonPropertyName("BotProfileId")]
        public required MongoId BotProfileId { get; set; }
    }
}
