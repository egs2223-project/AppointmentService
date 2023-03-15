using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppointmentService.Models
{
    [JsonConverter(typeof(ParticipantJsonConverter))]
    public class Participant : IEquatable<Participant>
    {
        [Key]
        public long Id { get; set; }

        public Guid ParticipantId { get; set; }

        public Participant(Guid participantId)
        {
            ParticipantId = participantId;
        }

        public override bool Equals(object? obj)
        {
            return ParticipantId.Equals(obj);
        }

        public override int GetHashCode()
        {
            return ParticipantId.GetHashCode();
        }

        public bool Equals(Participant? other)
        {
            if (ReferenceEquals(null, other)) return false;
            return this.ParticipantId == other.ParticipantId;
        }
    }

    public class ParticipantJsonConverter : JsonConverter<Participant>
    {
        public override Participant Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return new(reader.GetGuid());
        }

        public override void Write(
            Utf8JsonWriter writer,
            Participant participant,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(participant.ParticipantId);
        }
    }
}
