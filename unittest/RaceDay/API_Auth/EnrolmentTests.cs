using System.Net;
using System.Net.Http.Json;
using RaceDay;
using RaceDay.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Database;
using System.Text.Json;

namespace API_Auth
{
    [TestClass]
    public class EnrolmentTests
    {
        private WebApplicationFactory<Program> _factory = null!;
        private HttpClient _client = null!;

        [TestInitialize]
        public void Setup()
        {
            var dbName = Guid.NewGuid().ToString(); // one new empty database per test

            // Start the real API in memory for testing
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    // Replace the SQL Server database with an in-memory one (fake db)
                    builder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<DbContextOptions<RaceDayDBContext>>();
                        services.RemoveAll<IDbContextOptionsConfiguration<RaceDayDBContext>>();
                        services.AddDbContext<RaceDayDBContext>(o => o.UseInMemoryDatabase(dbName));
                    });
                });

            // HttpClient that talks to the in-memory API. It stores cookies like a browser does.
            _client = _factory.CreateClient();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        private async Task SeedEssentialEntitiesAsync()
        {
            var scope = _factory.Services.CreateScope();

            RaceDayDBContext context = scope.ServiceProvider.GetRequiredService<RaceDayDBContext>();

            var organiser = new Organiser
            {
                OrganiserID = 1,
                Name = "TestOrg",
                Password = "hash"
            };

            // 2. Participant (Independent parent)
            var participant = new Participant
            {
                ParticipantID = 1,
                Name = "Nona Dispargement",
                PhoneNumber = "0910910919",
                Age = 25,
                Password = "hash",
                AvatarUrl = string.Empty
            };

            // 3. Location (Depends on Organiser)
            var location = new Location
            {
                LocationID = 1,
                Name = "Durban Beachfront",
                GPS_coordinates = "-29.8587, 31.0218",
                OrganiserID = 1,
                FinishLineLocation = "somewhere",
                StartingLineLocation = "here",
                RouteLength = 0
            };

            // 4. Event (Depends on Organiser and Location)
            var raceEvent = new Event
            {
                EventID = 1,
                Title = "Marathon",
                EventDate = new DateOnly(2026, 11, 15),
                StartTime = new TimeOnly(6, 0),
                EndTime = new TimeOnly(12, 0),
                EventType = 0,
                LocationID = 1,
                OrganiserID = 1,
                BannerUrl = "banner/example.com",
                Description = "pretty good"
            };

            // 5. Category (Depends on Event)
            var category = new Category
            {
                CategoryID = 1,
                Name = "10km",
                Distance = "10km",
                EventID = 1,
                AgeRange = "U18"
            };

            // 6. Enrolment (Depends on Participant, Event, and Category)
            var enrolment = new Enrolment
            {
                EnrolmentID = 1,
                ParticipantID = 1,
                EventID = 1,
                CategoryID = 1,
                EnrolmentDate = DateOnly.FromDateTime(DateTime.Today)
            };

            // 7. Results (Depends on Enrolment)
            var result = new Results
            {
                ResultID = 1,
                EnrolmentID = 1,
                FinishingTime = new TimeOnly(0, 45, 30),
                FinishingPosition = 1
            };

            // Add to context in dependency order
            context.Organiser.Add(organiser);
            context.Participant.Add(participant);
            context.Location.Add(location);
        
        context.Event.Add(raceEvent);
            context.Category.Add(category);
            context.Enrolment.Add(enrolment);
            context.Results.Add(result);

            await context.SaveChangesAsync();
        }

        
        [TestMethod]
        public async Task GetEnrolment()
        {
            await SeedEssentialEntitiesAsync();
            OrganiserRegisterDto init = new OrganiserRegisterDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/login", unit);
            var response = await _client.GetAsync("api/Enrolments");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            await _client.PostAsync("/api/auth/logout", null);
        }

        [TestMethod]
        public async Task GetMyEventEnrolment()
        {
            await SeedEssentialEntitiesAsync();
            OrganiserRegisterDto init = new OrganiserRegisterDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/login", unit);
            var response = await _client.GetAsync("api/Enrolments/my");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            await _client.PostAsync("/api/auth/logout", null);
        }

        [TestMethod]
        public async Task GetEnrolmentByIdSuccess()
        {
            await SeedEssentialEntitiesAsync();
            OrganiserRegisterDto init = new OrganiserRegisterDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/login", unit);
            var response = await _client.GetAsync("api/Enrolments/1");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            await _client.PostAsync("/api/auth/logout", null);
        }

        [TestMethod]
        public async Task GetEnrolmentByIdFailure()
        {
            await SeedEssentialEntitiesAsync();
            ParticipantRegisterDto init = new ParticipantRegisterDto { Name = "John Doe", Age = 25, Password = "Password", PhoneNumber = "0950960977", AvatarUrl = "example.com" };
            await _client.PostAsJsonAsync("/api/auth/participants/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/participants/login", unit);
            var response = await _client.GetAsync("api/Enrolments/1");
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
            await _client.PostAsync("/api/auth/logout", null);
        }

        [TestMethod]
        public async Task PutEnrolmentSuccess()
        {
            await SeedEssentialEntitiesAsync();
            OrganiserRegisterDto init = new OrganiserRegisterDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/login", unit);

            var enrolment = new Enrolment
            {
                EnrolmentID = 1,
                ParticipantID = 1,
                EventID = 1,
                CategoryID = 1,
                EnrolmentDate = new DateOnly(2026, 11, 15)
            };

            var response = await _client.PutAsJsonAsync("api/Enrolments/1",enrolment);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            response = await _client.GetAsync("api/Enrolments/1");
            var body= JsonSerializer.Deserialize<Enrolment>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive=true});
            Assert.AreEqual(body.EnrolmentDate, new DateOnly(2026, 11, 15));

            await _client.PostAsync("/api/auth/logout", null);
        }

        [TestMethod]
        public async Task PostEnrolmentSuccess()
        {
            await SeedEssentialEntitiesAsync();
            OrganiserRegisterDto init = new OrganiserRegisterDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/login", unit);

            var enrolment = new Enrolment
            {
                EnrolmentID = 2,
                ParticipantID = 1,
                EventID = 1,
                CategoryID = 1,
                EnrolmentDate = new DateOnly(2026, 11, 15)
            };

            var response = await _client.PostAsJsonAsync("api/Enrolments", enrolment);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            response = await _client.GetAsync("api/Enrolments/2");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var body = JsonSerializer.Deserialize<Enrolment>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.AreEqual(body.EnrolmentDate, new DateOnly(2026, 11, 15));

            await _client.PostAsync("/api/auth/logout", null);
        }

        [TestMethod]
        public async Task PutEnrolmentFailure()
        {
            await SeedEssentialEntitiesAsync();
            ParticipantRegisterDto init = new ParticipantRegisterDto { Name = "John Doe", Age = 25, Password = "Password", PhoneNumber = "0950960977", AvatarUrl = "example.com" };
            await _client.PostAsJsonAsync("/api/auth/participants/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/participants/login", unit);
            var enrolment = new Enrolment
            {
                EnrolmentID = 1,
                ParticipantID = 1,
                EventID = 1,
                CategoryID = 1,
                EnrolmentDate = new DateOnly(2026, 11, 15)
            };

            var response = await _client.PutAsJsonAsync("api/Enrolments/1", enrolment);
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
            
            await _client.PostAsync("/api/auth/logout", null);
        }

        [TestMethod]
        public async Task PostEnrolmentFailure()
        {
            await SeedEssentialEntitiesAsync();
            ParticipantRegisterDto init = new ParticipantRegisterDto { Name = "John Doe", Age = 25, Password = "Password", PhoneNumber = "0950960977", AvatarUrl = "example.com" };
            await _client.PostAsJsonAsync("/api/auth/participants/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/participants/login", unit);

            var enrolment = new Enrolment
            {
                EnrolmentID = 2,
                ParticipantID = 1,
                EventID = 1,
                CategoryID = 1,
                EnrolmentDate = new DateOnly(2026, 11, 15)
            };

            var response = await _client.PostAsJsonAsync("api/Enrolments", enrolment);
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
            
            await _client.PostAsync("/api/auth/logout", null);
        }
    }
    }

