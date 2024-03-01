using System.Net.Http.Headers;
using System.Text;
using Minesweeper.Api;
using ComponentTestMinesweeper.Utils;
using Minesweeper.Pesristance;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Net;
using Minesweeper.Interfaces;
using Moq;

namespace ComponentTestMinesweeper;

public class GameControllerTests : IClassFixture<TestApplicationFactory>
{
    private const string Route = "Game";
    private readonly HttpClient _httpClient;
    private readonly TestApplicationFactory _factory;

    public GameControllerTests(TestApplicationFactory factory)
    {
       _factory = factory;
        _httpClient = factory.CreateClient();

        var authenticationString = "games-app:Karel*";
        var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticationString));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
    }
    [Fact]
    public async Task Get_Correct_Ok()
    {
        // Arrange
        var validId = TestDataSeeder.Game_2_ID;

        // Act
        var response = await _httpClient.GetAsync($"{Route}/{validId}");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // Ověření, že stavový kód je 200 (OK)

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrEmpty(responseBody)); // Ověření, že tělo odpovědi není prázdné
    }

    [Fact]
    public async Task GetActive_Correct_Ok()
    {
        // Provedení HTTP GET požadavku na endpoint /active
        var response = await _httpClient.GetAsync($"{Route}/active");

        Assert.NotNull(response);
        Assert.NotEmpty(response.ToString());
    }

    [Fact]
    public async Task Create_Correct_ReturnsOk()
    {
        var mockNotificationService = new Mock<INotificationService>();
        mockNotificationService.Setup(x => x.SendNotification(It.IsAny<string>()))
                               .Returns(Task.FromResult(true));

        var context = _factory.Services.GetRequiredService<ApplicationContext>();

        // Příprava vstupních dat
        var gameInputDto = new GameInputDto
        {
            Name = "Hra",
            MinesCount = 10
        };

        // Volání testované metody
        var response = await _httpClient.PostAsJsonAsync(Route, gameInputDto);

        // Ověření výsledku
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        var game = await context.Games.SingleOrDefaultAsync(x => x.Name == gameInputDto.Name && x.MinesCount == gameInputDto.MinesCount);
        Assert.NotNull(game);

        // Ověření, že byla odeslána notifikace o vytvoření hry
      //  mockNotificationService.Verify(x => x.SendNotification("HRA_VYTVOŘENA"), Times.Once);

        Assert.True(response.Headers.Location?.AbsoluteUri.Contains(Route));
    }

    [Fact]
    public async Task Delete_WhenCalledWithValidId_ReturnsOk()
    {
        // Arrange
        var validId = TestDataSeeder.Game_3_ID;

        // Act
        var response = await _httpClient.DeleteAsync($"{Route}/{validId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetGame_SuccessfulAuthorization_ReturnsOk()
    {
        var validId = TestDataSeeder.Game_1_ID;

        var validCredentials = "games-app:Karel*";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(validCredentials)));

        var response = await _httpClient.GetAsync($"/Game/{validId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetGame_UnsuccessfulAuthorization_ReturnsUnauthorized()
    {
        var validId = TestDataSeeder.Game_1_ID;

        var invalidCredentials = "Username:Password";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(invalidCredentials)));

        var response = await _httpClient.GetAsync($"/Game/{validId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
