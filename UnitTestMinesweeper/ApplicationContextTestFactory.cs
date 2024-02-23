using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Minesweeper.Pesristance;

namespace UnitTestMinesweeper;

public class ApplicationContextTestFactory
{
    public static ApplicationContext CreateContext()
    {
        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        var builder = new DbContextOptionsBuilder<ApplicationContext>();
        builder.UseInMemoryDatabase("InMemoryDbForTesting").UseInternalServiceProvider(serviceProvider);

        var context = new ApplicationContext(builder.Options);
        return context;
    }
}
