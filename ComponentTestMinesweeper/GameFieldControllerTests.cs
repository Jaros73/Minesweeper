using ComponentTestMinesweeper.Utils;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace ComponentTestMinesweeper;

public class GameFieldControllerTests : IClassFixture<TestApplicationFactory>
{
    private const string Route = "GameField";
    private readonly HttpClient _httpClient;
    private readonly TestApplicationFactory _factory;

    public GameFieldControllerTests(TestApplicationFactory factory)
    {
        _factory = factory;
        _httpClient = factory.CreateClient();

        var authenticationString = "games-app:Karel*";
        var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticationString));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
    }

    [Fact]
    public async Task GetGameFields_SuccessfulAuthorization_ReturnsOk()
    {
        var validGameId = TestDataSeeder.Game_1_ID;

        var validCredentials = "games-app:Karel*";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(validCredentials)));

        var response = await _httpClient.GetAsync($"{Route}/{validGameId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetGameFields_UnsuccessfulAuthorization_ReturnsUnauthorized()
    {
        var validGameId = TestDataSeeder.Game_1_ID;

        // Nastavení neplatných autentizačních údajů pro neúspěšný test
        var invalidCredentials = "invalid-credentials";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(invalidCredentials)));

        var response = await _httpClient.GetAsync($"{Route}/{validGameId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RevealField_SuccessfulAuthorization_ReturnsOk()
    {
        // Arrange
        var validGameId = TestDataSeeder.Game_1_ID;
        var validFieldId = 1;

        // Nastavení platných autentizačních údajů pro úspěšný test
        var validCredentials = "games-app:Karel*";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(validCredentials)));

        // Act
        var response = await _httpClient.PostAsync($"{Route}/reveal/{validGameId}/{validFieldId}", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RevealField_FailedAuthorization_ReturnsUnauthorized()
    {
        // Arrange
        var validGameId = TestDataSeeder.Game_1_ID;
        var validFieldId = 1;

        // Nastavení neplatných autentizačních údajů pro neúspěšný test
        var invalidCredentials = "invalid-credentials";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(invalidCredentials)));

        var response = await _httpClient.PostAsync($"{Route}/reveal/{validGameId}/{validFieldId}", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
