using Minesweeper.Pesristance;
using Minesweeper.Structures;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;


namespace Minesweeper.Authentication
{
    public class AuthenticationUsersCache
    {
        // Ukládá uživatele do thread-safe slovníku s rychlým přístupem.
        private readonly ConcurrentDictionary<int, User> _users = new();

        // Factory pro vytváření nových scope, umožňuje přístup k DbContextu a dalším službám.
        private readonly IServiceScopeFactory _scopeFactory;

        // Semaphore pro asynchronní vzájemné vyloučení, aby byl refresh cache proveden bezpečně ve vícevláknovém prostředí.
        private static readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

        // Čas poslední aktualizace cache, inicializován na minimální možnou hodnotu.
        private DateTime _lastRefreshTime = new();

        // Konstanta určující, jak často by se měla cache obnovovat (v hodinách).
        private const int RefreshTimeHours = 12;

        // Konstruktor třídy, injektuje IServiceScopeFactory pro pozdější vytváření scope.
        public AuthenticationUsersCache(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        // Asynchronní metoda pro vyhledání všech uživatelů. Pokud je cache zastaralá, provede její obnovení.
        public async Task<User[]> FindAll()
        {
            // Zajištění, že v daný moment probíhá pouze jedna aktualizace cache.
            await _semaphoreSlim.WaitAsync();
            try
            {
                // Kontrola, zda již uplynula doba určená pro obnovení cache.
                if (DateTime.Compare(_lastRefreshTime.AddHours(RefreshTimeHours), DateTime.UtcNow) <= 0)
                {
                    // Vytvoření nového scope pro přístup k DbContextu.
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                    var users = await context.Users.ToArrayAsync();
                    if (users.Length != 0)
                    {
                        // Pokud byli nalezeni noví uživatelé, cache se vyčistí a naplní novými daty.
                        _users.Clear();
                        foreach (var ux in users)
                            _users.TryAdd(ux.Id, ux);

                        // Aktualizace času posledního obnovení cache.
                        _lastRefreshTime = DateTime.UtcNow;
                    }
                }
            }
            finally
            {
                // Uvolnění semaforu po dokončení operace.
                _semaphoreSlim.Release();
            }
            // Vrácení aktuálních dat z cache.
            return _users.Values.ToArray();
        }
    }

}
