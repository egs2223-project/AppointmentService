using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AppointmentService.Models
{
    public class RecurringOptions
    {
        [Key]
        [JsonIgnore]
        public long Id { get; set; }

        [Required]
        [JsonPropertyName("type")]
        public RecurringFrequencyType Type { get; set; }

        [Required]
        [JsonPropertyName("interval")]
        public int Interval { get; set; }

        [Required]
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum RecurringFrequencyType
        {
            Secondly,
            Minutely,
            Hourly,
            Daily,
            Weekly,
            Monthly,
            Yearly
        }

        //https://stackoverflow.com/a/75448004
    }
}
