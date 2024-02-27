using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Minesweeper.Authentication;

public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    // Definice proměnné pro mezipaměť uživatelů.
    private readonly AuthenticationUsersCache _usersCache;

    // Konstruktor třídy pro základní autentizační obslužnou rutinu.
    // 'options' poskytuje možnosti konfigurace, 'logger' pro logování, 'encoder' pro kódování URL,
    // 'clock' poskytuje časové funkce a 'usersCache' je mezipaměť s uživatelskými údaji.
    public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, AuthenticationUsersCache usersCache) : base(options, logger, encoder, clock)
    {
        // Inicializace mezipaměti uživatelů.
        _usersCache = usersCache;
    }

    // Asynchronní metoda pro zpracování autentizace.
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Kontrola, zda požadavek obsahuje hlavičku 'Authorization'.
        if (!Request.Headers.TryGetValue("Authorization", out var value))
            return AuthenticateResult.Fail("Missing Authorization header"); // Chybějící hlavička autorizace.

        var header = value.ToString();
        // Kontrola, zda hodnota hlavičky začíná klíčovým slovem 'Basic'.
        if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.Fail("Authorization have to start with 'Basic' keyword"); // Autorizace musí začínat slovem 'Basic'.

        // Dekódování hodnoty hlavičky z Base64.
        var decoded = Encoding.UTF8.GetString(
            Convert.FromBase64String(
                header.Replace("Basic ", "", StringComparison.OrdinalIgnoreCase)));

        // Rozdělení dekódovaného řetězce na uživatelské jméno a heslo.
        var split = decoded.Split(':');
        if (split.Length != 2)
            return AuthenticateResult.Fail("Invalid Authorization header format"); // Neplatný formát hlavičky autorizace.

        var userName = split[0];
        var password = split[1];

        // Načtení uživatelů z mezipaměti.
        var users = await _usersCache.FindAll();

        // Kontrola, zda se uživatelské jméno a heslo shodují s údaji v mezipaměti, nebo zda se jedná o speciální případ 'games-app'.
        if (!(users.Any(x => x.UserName == userName && x.Password == password) || (userName == "games-app" && password == "Karel*"))) // Kontrola přihlašovacích údajů.
            return AuthenticateResult.Fail("Username or password is incorrect"); // Uživatelské jméno nebo heslo je nesprávné.

        // Vytvoření objektu ClaimsPrincipal pro úspěšnou autentizaci.
        var claimsPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new BasicAuthenticationIdentity(userName), new[] { new Claim(ClaimTypes.Name, userName) }));

        // Vrácení úspěšného výsledku autentizace.
        return AuthenticateResult.Success(
            new AuthenticationTicket(claimsPrincipal, Scheme.Name));
    }
}
