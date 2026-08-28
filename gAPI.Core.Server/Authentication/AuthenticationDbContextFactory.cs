using Microsoft.EntityFrameworkCore;

namespace gAPI.Core.Server.Authentication;

public class AuthenticationDbContextFactory<TContext> : IDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly Func<TContext> _factory;

    public AuthenticationDbContextFactory(Func<TContext> factory)
    {
        _factory = factory;
    }

    public TContext CreateDbContext()
        => _factory();
}
