using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Minesweeper.Interfaces;
using Minesweeper.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Minesweeper.Pesristance;
using Microsoft.OpenApi.Models;
using NodaTime;
using Microsoft.AspNetCore.Authentication;
using System.Net;
using Minesweeper.Authentication;
using Minesweeper.Structures;

namespace Minesweeper;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // Registrace slu�eb
        builder.Services.AddTransient<IGameService, GameService>();
        builder.Services.AddTransient<IGameFieldService, GameFieldService>();
        builder.Services.AddSingleton<IClock>(NodaTime.SystemClock.Instance);
        builder.Services.AddSingleton<AuthenticationUsersCache>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);

        // Konfigurace Swagger
        builder.Services.AddSwaggerGen(c =>
        {
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "APIs", Version = "v1" });
            c.AddSecurityDefinition("Basic", new OpenApiSecurityScheme()
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Basic",
                In = ParameterLocation.Header,
                Description = "Basic Authorization header"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Basic"
                        },
                        Name = "Basic",
                        In = ParameterLocation.Header,
                    },
                    new List<string>()
                }
            });
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

                //  Dopln�n� u�ivatele "games-app" pokud neexistuje - Authentication pro test
                if (!db.Users.Any(u => u.UserName == "games-app"))
                {
                    db.Users.Add(new User { UserName = "games-app", Password = "Karel*" });
                    db.SaveChanges();
                    Console.WriteLine("U�ivatel 'games-app' byl �sp�n� p�id�n do datab�ze.");
                }
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Do�lo k chyb� p�i prov�d�n� migrace datab�ze.");
                Console.Error.WriteLine($"Do�lo k chyb� p�i prov�d�n� migrace datab�ze: {ex.Message}");
                // Environment.Exit(-1);
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
}