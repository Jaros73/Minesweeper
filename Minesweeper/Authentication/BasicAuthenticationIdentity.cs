using System.Security.Principal;

namespace Minesweeper.Authentication;

public class BasicAuthenticationIdentity : IIdentity
{
        public BasicAuthenticationIdentity(string? name)
        {
            AuthenticationType = "Basic";
            IsAuthenticated = true;
            Name = name;
        }

    /// <inheritdoc/>
    public string? AuthenticationType { get; set; }

    /// <inheritdoc/>
    public bool IsAuthenticated { get; set; }

    /// <inheritdoc/>
    public string? Name { get; set; }

}
