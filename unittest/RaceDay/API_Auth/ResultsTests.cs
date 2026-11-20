using Microsoft.AspNetCore.Mvc;
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
    public class ResultsTests
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
        public async Task PostResultsSuccess()
        {
            await SeedEssentialEntitiesAsync();
            OrganiserRegisterDto init = new OrganiserRegisterDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/login", unit);
            
            Results result = new Results { EnrolmentID = 2, FinishingTime = new TimeOnly(0, 45, 39), FinishingPosition = 2 };
            var response = await _client.PostAsJsonAsync("api/Results", result);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        }

        [TestMethod]
        public async Task PublicGetResultsSuccess()
        {
            ParticipantRegisterDto init = new ParticipantRegisterDto { Name = "John Doe", Age = 25, Password = "Password", PhoneNumber = "0950960977", AvatarUrl = "example.com" };
            await _client.PostAsJsonAsync("/api/auth/participants/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/participants/login", unit);

            var response = await _client.GetAsync("api/Results");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            await _client.PostAsync("/api/auth/logout", null);
        }

        [TestMethod]
        public async Task GetSpecificResultsAuthFailure()
        {
            ParticipantRegisterDto init = new ParticipantRegisterDto { Name = "John Doe", Age = 25, Password = "Password", PhoneNumber = "0950960977", AvatarUrl = "example.com" };
            await _client.PostAsJsonAsync("/api/auth/participants/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/participants/login", unit);

            var response = await _client.GetAsync("api/Results/1");
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
            await _client.PostAsync("/api/auth/logout", null);
        }

        [TestMethod]
        public async Task GetSpecificResultsLogicFailure()
        {
            OrganiserRegisterDto init = new OrganiserRegisterDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/login", unit);

            var response = await _client.GetAsync("api/Results/2");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            await _client.PostAsync("/api/auth/logout", null);
        }

        [TestMethod]
        public async Task GetSpecificResultsSuccess()
        {
            await SeedEssentialEntitiesAsync();
            OrganiserRegisterDto init = new OrganiserRegisterDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/login", unit);

            var response = await _client.GetAsync("api/Results/1");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            await _client.PostAsync("/api/auth/logout", null);
        }

        [TestMethod]
        public async Task PutResults()
        {
            await SeedEssentialEntitiesAsync();
            OrganiserRegisterDto init = new OrganiserRegisterDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/login", unit);

            Results result = new Results { ResultID=1, EnrolmentID = 2, FinishingTime = new TimeOnly(0, 45, 39), FinishingPosition = 3 };
            await _client.PutAsJsonAsync("api/Results/1", result);


            var response = await _client.GetAsync("api/Results/1");
            var update = JsonSerializer.Deserialize<Results>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive=true});

            Assert.AreEqual(result.FinishingPosition, update.FinishingPosition);
            await _client.PostAsync("/api/auth/logout", null);
        }

        [TestMethod]
        public async Task DeleteSuccess()
        {
            await SeedEssentialEntitiesAsync();
            OrganiserRegisterDto init = new OrganiserRegisterDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/login", unit);

            var response = await _client.DeleteAsync("api/Results/1");
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
            await _client.PostAsync("/api/auth/logout", null);
        }

        [TestMethod]
        public async Task GetEventResults()
        {
            await SeedEssentialEntitiesAsync();
            OrganiserRegisterDto init = new OrganiserRegisterDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/register", init);
            LoginDto unit = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/organisers/login", unit);

            var response = await _client.GetAsync("api/Results/1");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            await _client.PostAsync("/api/auth/logout", null);
        }


        [TestMethod]
        public async Task GetMyResultsFail()
        {
            await SeedEssentialEntitiesAsync();

            ParticipantRegisterDto init2 = new ParticipantRegisterDto { Name = "John Doe", Age = 25, Password = "Password", PhoneNumber = "0950960977", AvatarUrl = "example.com" };
            await _client.PostAsJsonAsync("/api/auth/participants/register", init2);
            LoginDto unit2 = new LoginDto { Name = "John Doe", Password = "Password" };
            await _client.PostAsJsonAsync("/api/auth/participants/login", unit2);

            var response = await _client.GetAsync("api/Results/my");
            //var result2 = JsonSerializer.Deserialize<List<Results>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            await _client.PostAsync("/api/auth/logout", null);
        }

        //[TestMethod]
        //public async Task GetMyResults()
        //{
        //    await SeedEssentialEntitiesAsync();

        //    ParticipantRegisterDto init2 = new ParticipantRegisterDto { Name = "John Doe", Age = 25, Password = "Password", PhoneNumber = "0950960977", AvatarUrl = "example.com" };
        //    await _client.PostAsJsonAsync("/api/auth/participants/register", init2);
        //    LoginDto unit2 = new LoginDto { Name = "John Doe", Password = "Password" };
        //    await _client.PostAsJsonAsync("/api/auth/participants/login", unit2);
        //    await _client.PostAsync("/api/auth/logout", null);

        //    OrganiserRegisterDto init = new OrganiserRegisterDto { Name = "John D", Password = "Password" };
        //    await _client.PostAsJsonAsync("/api/auth/organisers/register", init);
        //    LoginDto unit = new LoginDto { Name = "John D", Password = "Password" };
        //    await _client.PostAsJsonAsync("/api/auth/organisers/login", unit);

        //    var enrolment = new Enrolment
        //    {
        //        EnrolmentID = 2,
        //        ParticipantID = 2,
        //        EventID = 1,
        //        CategoryID = 1,
        //        EnrolmentDate = DateOnly.FromDateTime(DateTime.Today)
        //    };

        //    await _client.PostAsJsonAsync("api/Enrolments", enrolment);

        //    Results result = new Results { EnrolmentID = 2, FinishingTime = new TimeOnly(0, 45, 39), FinishingPosition = 2 };
        //    await _client.PostAsJsonAsync("api/Results", result);
        //    await _client.PostAsync("/api/auth/logout", null);

        //    await _client.PostAsJsonAsync("/api/auth/participants/login", unit2);

        //    var response = await _client.GetAsync("api/Results/my");
        //    var result2 = JsonSerializer.Deserialize<List<Results>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        //    Assert.AreEqual(result2.Count, 1);
        //    await _client.PostAsync("/api/auth/logout", null);
        //}
    }
}
