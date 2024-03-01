using ComponentTestMinesweeper.Mocks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Minesweeper;
using Minesweeper.Interfaces;
using Minesweeper.Pesristance;

namespace ComponentTestMinesweeper.Utils;

public class TestApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Nastaví prostředí aplikace na "Test" pro testovací účely
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Hledá a odstraňuje konfiguraci DbContextOptions pro ApplicationContext, pokud existuje
            var descriptor = services.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<ApplicationContext>));
            if (descriptor is not null) services.Remove(descriptor);

            // Přidává ApplicationContext s použitím in-memory databáze pro testování bez potřeby skutečné databáze
            services.AddDbContext<ApplicationContext>(options => options.UseInMemoryDatabase($"InMemoryDbForTesting"));

            // Přidává NullLoggerFactory pro potlačení logování a zrychlení testů
            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();

            // Pokud již existuje konfigurace DbContextOptions pro ApplicationContext, odstraní všechny instance INotificationService
            if (services.Any(x => x.ServiceType == typeof(DbContextOptions<ApplicationContext>)))
                services.RemoveAll(typeof(INotificationService));

            // Přidává MockNotificationService jako INotificationService pro simulaci notifikačních služeb během testů
            services.AddTransient<INotificationService, MockNotificationService>();

            // Vytváří ServiceProvider a používá jej k inicializaci testovacích dat
            var sp = services.BuildServiceProvider();
            TestDataSeeder.SeedData(sp);
        });
    }
}
