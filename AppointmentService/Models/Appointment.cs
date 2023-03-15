using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AppointmentService.Models
{
    public class Appointment
    {
        [Key]
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [Required]
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [Required]
        [JsonPropertyName("datetime")]
        public DateTime DateTime { get; set; }
        
        [JsonPropertyName("ical_data")]
        public string ICalData { get; set; }

        [Required]
        [JsonPropertyName("status")]
        public AppointmentStatus Status { get; set; }

        [Required]
        [JsonPropertyName("num_participants")]
        public int NumParticipants { get; set; }

        [Required]
        [JsonPropertyName("location")]
        public string Location { get; set; }

        [Required]
        [JsonPropertyName("expected_duration")]
        public TimeSpan ExpectedDuration { get; set; }

        [Required]
        [JsonPropertyName("recurring")]
        public bool Recurring { get; set; }

        [JsonPropertyName("recurring_frequency")]
        public RecurringOptions? RecurringFrequency { get; set; }

        [JsonPropertyName("participant_ids")]
        public List<Participant>? Participants { get; set; }

        //public Appointment(long id, string description, DateTime dateTime, string iCalData, AppointmentStatus status, int numParticipants, string location, TimeSpan expectedDuration, bool recurring, RecurringOptions recurringFrequency)
        //{
        //    Id = id;
        //    Description = description;
        //    DateTime = dateTime;
        //    ICalData = iCalData;
        //    Status = status;
        //    NumParticipants = numParticipants;
        //    Location = location;
        //    ExpectedDuration = expectedDuration;
        //    Recurring = recurring;
        //    RecurringFrequency = recurringFrequency;
        //}

        //public Appointment() { }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum AppointmentStatus
        {
            Scheduled,
            Completed,
            Cancelled
        }
    }
}
