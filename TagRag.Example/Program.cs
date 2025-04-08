using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serina.TagMemory.Interfaces;
using Serina.TagMemory.SqlBuilder;
using TagRag.Example.Db;
using TagRag.Example.Plugin;
using TagRag.Example.Services;

namespace TagRag.Example
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)

                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // Add configuration sources if needed
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    // Configure logging if needed


                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging();
                    services.AddHttpClient();




                    services.AddSingleton(provider =>
                    {
                        // Create and open a single SQLite in-memory connection
                        var connection = new SqliteConnection("DataSource=:memory:");
                        connection.Open(); // 🔥 Must stay open!

                        // Configure EF Core to use this connection
                        var options = new DbContextOptionsBuilder<ConversationContext>()
                            .UseSqlite(connection)
                            .Options;

                        var db = new ConversationContext(options);
                        db.Database.EnsureCreated(); // Create schema

                        return db; // ✅ DO NOT dispose here!
                    });



                    services.AddScoped<IDbConnector, InMemoryDbConnector>();

                    services.AddScoped<ISchemaScanner, SchemaScanner>(); 


                    services.AddHostedService<ConversationService>();
                });
    }
}
