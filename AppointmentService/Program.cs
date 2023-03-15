using AppointmentService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Linq;
using System.Reflection;
using static AppointmentService.Models.Appointment;

namespace AppointmentService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.MapType<TimeSpan>(() => new OpenApiSchema { Type = "string", Example = new OpenApiString("00:20:00") });
                c.MapType<Participant>(() => new OpenApiSchema { Type = "string", Example = new OpenApiString(Guid.Empty.ToString()) });

                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });

            if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
            {
                builder.Services.AddDbContext<AppointmentDb>(opt =>
                {
                    opt.UseInMemoryDatabase("Appointments");
                });
            }
            else
            {
                builder.Services.AddDbContext<AppointmentDb>(options =>
                {
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure();
                    });
                });
            }

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapAppointmentRoutes();

            app.Run();
        }
    }

    public static class AppointmentEndpoints
    {
        public static void MapAppointmentRoutes(this IEndpointRouteBuilder app)
        {
            app.MapGet   ("/v1/appointments", GetAppointmentSearch);
            app.MapPost  ("/v1/appointments", PostAppointment);
            app.MapGet   ("/v1/appointments/{appointment_id}", GetAppointment);
            app.MapPut   ("/v1/appointments/{appointment_id}", PutAppointment);
            app.MapDelete("/v1/appointments/{appointment_id}", DeleteAppointment);
        }

        /// <summary>
        /// This endpoint searches for appointments using several search criteria
        /// </summary>
        /// <remarks>
        /// This endpoint supports pagination
        /// </remarks>
        /// <param name="participant_id">A participant id</param>
        /// <param name="location">A location</param>
        /// <param name="num_participants">The number of participants</param>
        /// <param name="expected_duration">The expected duration</param>
        /// <param name="from">Starting after</param>
        /// <param name="to">Ending before</param>
        /// <param name="status">The sheduling status</param>
        /// <param name="limit">The maximum number of elements to return</param>
        /// <param name="offset">The offset of elements to return</param>
        /// <response code="200">Success</response>
        /// <response code="409">This appointment conflicts with another already scheduled by one or more participants</response>
        public static async Task<ICollection<Appointment>> GetAppointmentSearch(AppointmentDb db, Guid? participant_id, string? location, int? num_participants, 
            TimeSpan? expected_duration, DateTime? from, DateTime? to, AppointmentStatus? status, int limit = 50, int offset = 0)
        {
            if (from == null) from = DateTime.MinValue;
            if (to == null) to = DateTime.MaxValue;

            var appointments = db.Appointments.Where(a => a.DateTime >= from && a.DateTime <= to);

            if (participant_id != null) appointments = appointments.Where(a => a.Participants.Any(b => b.ParticipantId == participant_id));
            if (location != null) appointments = appointments.Where(a => a.Location == location);
            if (num_participants != null) appointments = appointments.Where(a => a.NumParticipants == num_participants);
            if (expected_duration != null) appointments = appointments.Where(a => a.ExpectedDuration == expected_duration);
            if (status != null) appointments = appointments.Where(a => a.Status == status);

            return await appointments.Include(a => a.RecurringFrequency).Include(a => a.Participants).OrderBy(a => a.DateTime).Skip(offset).Take(limit).ToListAsync();
        }

        /// <summary>
        /// This endpoint creates a new appointment
        /// </summary>
        /// <remarks>
        /// The id, num_participants and ical_data fields are ignored and computed automatically by the API.
        /// 
        /// When succesfull returns the created Appointment object.
        /// In case of conflict returns a list of conflicting Appointment objects.
        /// </remarks>
        /// <param name="appointment">The new Appointment object</param>
        /// <response code="201">Created</response>
        /// <response code="409">This appointment conflicts with another already scheduled by one or more participants</response>
        [ProducesResponseType(typeof(Appointment), 201)]
        [ProducesResponseType(typeof(List<Appointment>), 409)]
        public static async Task<IResult> PostAppointment(Appointment appointment, AppointmentDb db)
        {
            appointment.Id = Guid.Empty;
            if (appointment.Participants is not null)
            {
                IEnumerable<Guid> ids = appointment.Participants.Select(h => h.ParticipantId);
                DateTime appointmentStart = appointment.DateTime;
                DateTime appointmentEnd = appointment.DateTime + appointment.ExpectedDuration;

                var conflicts = (await db.Appointments.Include(a => a.Participants)
                .Where(a => (a.DateTime >= appointmentStart && a.DateTime < appointmentEnd)
                    || (a.DateTime.AddMilliseconds(EF.Functions.DateDiffMillisecond(TimeSpan.Zero, a.ExpectedDuration)) > appointmentStart
                    && a.DateTime.AddMilliseconds(EF.Functions.DateDiffMillisecond(TimeSpan.Zero, a.ExpectedDuration)) <= appointmentEnd))
                .Where(a => a.Participants.Any(b => ids.Contains(b.ParticipantId)))
                .Where(a => a.Status != AppointmentStatus.Cancelled).ToListAsync());

                if (conflicts is not null && conflicts.Count > 0) return Results.Conflict(conflicts);
            }

            iCal.UpdateiCalData(appointment);

            if (appointment.Participants == null) appointment.NumParticipants = 0;
            else appointment.NumParticipants = appointment.Participants.Count;

            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();

            return Results.Created($"/appointments/{appointment.Id}", appointment);
        }

        /// <summary>
        /// This endpoint gets a single appointment
        /// </summary>
        /// <param name="appointment_id">The id of the appointment to be deleted</param>
        /// <response code="200">Updated</response>
        /// <response code="404">Appointment not found</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public static async Task<IResult> GetAppointment(Guid appointment_id, AppointmentDb db)
        {
            return await db.Appointments.Where(a => a.Id == appointment_id).Include(a => a.RecurringFrequency).Include(a => a.Participants).SingleOrDefaultAsync()
                       is Appointment appointment ? Results.Ok(appointment) : Results.NotFound();
        }

        /// <summary>
        /// This endpoint updates a single appointment
        /// </summary>
        /// <remarks>
        /// The id, num_participants and ical_data fields are ignored and computed automatically by the API 
        /// </remarks>
        /// <param name="appointment_id">The id of the appointment to be deleted</param>
        /// <param name="inAppointment">The updated Appointment object</param>
        /// <response code="200">Updated</response>
        /// <response code="404">Appointment not found</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public static async Task<IResult> PutAppointment(Guid appointment_id, Appointment inAppointment, AppointmentDb db)
        {
            var appointment = await db.Appointments.Where(a => a.Id == appointment_id).Include(a => a.RecurringFrequency).Include(a => a.Participants).SingleOrDefaultAsync();

            if (appointment is null) return Results.NotFound();

            appointment.Description = inAppointment.Description;
            appointment.DateTime = inAppointment.DateTime;
            appointment.Status = inAppointment.Status;
            appointment.Location = inAppointment.Location;
            appointment.ExpectedDuration = inAppointment.ExpectedDuration;
            appointment.Recurring = inAppointment.Recurring;
            appointment.Participants = inAppointment.Participants;

            if (inAppointment.Recurring) appointment.RecurringFrequency = inAppointment.RecurringFrequency;
            else appointment.RecurringFrequency = null;

            if(inAppointment.Participants == null) appointment.NumParticipants = 0;
            else appointment.NumParticipants = inAppointment.Participants.Count;

            iCal.UpdateiCalData(appointment);

            await db.SaveChangesAsync();

            return Results.Ok(appointment);
        }

        /// <summary>
        /// This endpoint deletes a single appointment
        /// </summary>
        /// <param name="appointment_id">The id of the appointment to be deleted</param>
        /// <response code="200">Deleted</response>
        /// <response code="404">Appointment not found</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public static async Task<IResult> DeleteAppointment(Guid appointment_id, AppointmentDb db)
        {
            var appointment = await db.Appointments.Where(a => a.Id == appointment_id).Include(a => a.RecurringFrequency).Include(a => a.Participants).SingleOrDefaultAsync();

            if (appointment is null) return Results.NotFound();

            if (appointment.Participants != null)
            {
                foreach (var participant in appointment.Participants)
                {
                    db.Participants.Remove(participant);
                }
            }

            if (appointment.RecurringFrequency != null) db.RecurringOptions.Remove(appointment.RecurringFrequency);

            db.Appointments.Remove(appointment);
            await db.SaveChangesAsync();

            return Results.Ok(appointment);
        }
    }
}