using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Ical.Net;
using AppointmentService.Models;

namespace AppointmentService
{
    public static class iCal
    {
        public static void UpdateiCalData(Appointment appointment)
        {
            if(appointment.Status == Appointment.AppointmentStatus.Cancelled)
            {
                var calendar = Calendar.Load(appointment.ICalData);
                
                foreach (var ev in calendar.Events)
                {
                    ev.Status = "CANCELLED";
                }

                calendar.Method = "CANCEL";

                var serializer = new CalendarSerializer();
                appointment.ICalData = serializer.SerializeToString(calendar);
            }
            else
            {
                DateTime start = appointment.DateTime;
                DateTime end = appointment.DateTime + appointment.ExpectedDuration;
                string description = appointment.Description;

                FrequencyType frequencyType;
                if (appointment.Recurring && appointment.RecurringFrequency != null)
                {
                    switch(appointment.RecurringFrequency.Type)
                    {
                        case RecurringOptions.RecurringFrequencyType.Secondly:
                            frequencyType = FrequencyType.Secondly;
                            break;
                        case RecurringOptions.RecurringFrequencyType.Minutely:
                            frequencyType = FrequencyType.Minutely;
                            break;
                        case RecurringOptions.RecurringFrequencyType.Hourly:
                            frequencyType = FrequencyType.Hourly;
                            break;
                        case RecurringOptions.RecurringFrequencyType.Daily:
                            frequencyType = FrequencyType.Daily;
                            break;
                        case RecurringOptions.RecurringFrequencyType.Weekly:
                            frequencyType = FrequencyType.Weekly;
                            break;
                        case RecurringOptions.RecurringFrequencyType.Monthly:
                            frequencyType = FrequencyType.Monthly;
                            break;
                        case RecurringOptions.RecurringFrequencyType.Yearly:
                            frequencyType = FrequencyType.Yearly;
                            break;
                        default:
                            frequencyType = FrequencyType.None;
                            break;
                    }
                }
                else frequencyType = FrequencyType.None;

                int interval = appointment.RecurringFrequency == null ? 1 : appointment.RecurringFrequency.Interval;
                int repeat = appointment.RecurringFrequency == null ? int.MinValue : appointment.RecurringFrequency.Count;

                appointment.ICalData = GenerateiCalFile(start, end, description, frequencyType, interval, repeat);
            }
        }

        private static string GenerateiCalFile(DateTime start, DateTime end, string description, FrequencyType frequencyType = FrequencyType.None, int interval = 1, int repeat = int.MinValue)
        {
            CalendarEvent calendarEvent;

            if (frequencyType != FrequencyType.None)
            {
                var recurenceRule = new RecurrencePattern(frequencyType, interval) { Count = repeat };

                calendarEvent = new CalendarEvent
                {
                    Start = new CalDateTime(start),
                    End = new CalDateTime(end),
                    RecurrenceRules = new List<RecurrencePattern> { recurenceRule },
                };
            }
            else
            {
                calendarEvent = new CalendarEvent
                {
                    Start = new CalDateTime(start),
                    End = new CalDateTime(end),
                };
            }

            calendarEvent.Summary = description;

            var calendar = new Calendar();
            calendar.Events.Add(calendarEvent);

            var serializer = new CalendarSerializer();
            return serializer.SerializeToString(calendar);
        }
    }
}
