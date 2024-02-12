using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Minesweeper.Interfaces;
using Minesweeper.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Minesweeper.Pesristance;
using Microsoft.OpenApi.Models;

namespace Minesweeper;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // Registrace slu�eb
        builder.Services.AddTransient<IGameService, GameService>();
        builder.Services.AddTransient<IGameFieldService, GameFieldService>();

        // Konfigurace Swagger
        builder.Services.AddSwaggerGen(c =>
        {
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "APIs", Version = "v1" });
        });

        // P�id�n� controller�
        builder.Services.AddControllers();

        // Konfigurace p�ipojen� k datab�zi
        var connectionString = builder.Configuration["PostgresSql:ConnectionString"];
        var dbPassword = builder.Configuration["PostgresSql:DbPassword"];
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString) { Password = dbPassword };
        builder.Services
            .AddDbContext<ApplicationContext>(options
            => options
            .UseNpgsql(connectionStringBuilder.ConnectionString));

        var app = builder.Build();

        // Vytvo�en� datab�ze
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var db = services.GetRequiredService<ApplicationContext>();
                db.Database.Migrate();
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Do�lo k chyb� p�i prov�d�n� migrace datab�ze.");
                Console.Error.WriteLine($"Do�lo k chyb� p�i prov�d�n� migrace datab�ze: {ex.Message}"); 
               // Environment.Exit(-1);
            }
        }

        // Konfigurace Swagger UI
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
            c.RoutePrefix = "swagger";
        });

        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
