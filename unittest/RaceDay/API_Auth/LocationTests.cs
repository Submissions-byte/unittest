using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay;
using RaceDay.Database;
using RaceDay.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API_Auth
{
    [TestClass]
    public class LocationTests
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

            var hasher = new PasswordHasher<Organiser>();
            var organiser = new Organiser
            {
                OrganiserID = 1,
                Name = "TestOrg",
                Password = string.Empty
            };
            organiser.Password = hasher.HashPassword(organiser, "Password");

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
        public async Task GetLocation()
        {
            await SeedEssentialEntitiesAsync();
            var response = await _client.GetAsync("api/locations");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
        [TestMethod]
        public async Task GetLocationByIDSuccess()
        {
            await SeedEssentialEntitiesAsync();
            var response = await _client.GetAsync("api/locations/1");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
        [TestMethod]
        public async Task GetLocationByIDFail()
        {
            await SeedEssentialEntitiesAsync();
            var response = await _client.GetAsync("api/locations/2");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task PutLocation()
        {
            await SeedEssentialEntitiesAsync();

            LoginDto unit = new LoginDto { Name = "TestOrg", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/login", unit);
            var location = new Location
            {
                LocationID = 1,
                Name = "Cape Town Beachfront",
                GPS_coordinates = "-29.8587, 31.0218",
                OrganiserID = 1,
                FinishLineLocation = "somewhere",
                StartingLineLocation = "here",
                RouteLength = 0
            };
            var response = await _client.PutAsJsonAsync("api/locations/1", location);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            response = await _client.GetAsync("api/locations/1");
            var update = JsonSerializer.Deserialize<Location>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.AreEqual(location.Name, update.Name);
            await _client.PostAsync("/api/auth/logout", null);
        }

        [TestMethod]
        public async Task PostLocation()
        {
            await SeedEssentialEntitiesAsync();

            OrganiserRegisterDto init = new OrganiserRegisterDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/login", unit);

            var location = new Location
            {
                LocationID = 2,
                Name = "Cape Town Beachfront",
                GPS_coordinates = "-30, 55",
                OrganiserID = 1,
                FinishLineLocation = "somewhere",
                StartingLineLocation = "here",
                RouteLength = 0
            };
            var response = await _client.PostAsJsonAsync("api/locations", location);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

            response = await _client.GetAsync("api/Locations/2");
            var update = JsonSerializer.Deserialize<Location>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.AreEqual(location.Name, update.Name);
            await _client.PostAsync("/api/auth/logout", null);
        }

        [TestMethod]
        public async Task DeleteLocation()
        {
            await SeedEssentialEntitiesAsync();

            OrganiserRegisterDto init = new OrganiserRegisterDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/login", unit);

            var response = await _client.DeleteAsync("api/locations/1");
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

            response = await _client.GetAsync("api/locations/1");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

            await _client.PostAsync("/api/auth/logout", null);
        }
    }
}
