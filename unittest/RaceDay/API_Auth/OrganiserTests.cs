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

namespace API_Auth
{
    [TestClass]
    public class OrganiserTests
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
    }
}
