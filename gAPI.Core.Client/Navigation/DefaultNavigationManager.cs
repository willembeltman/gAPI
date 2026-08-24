using gAPI.Core.Client.Interfaces;
using Microsoft.AspNetCore.Components;

namespace gAPI.Core.Client.Navigation;

public class DefaultNavigationManager(
    NavigationManager navigation)
    : IUriNavigationManager
{
    string IUriNavigationManager.GetPathAndQuery()
        => navigation.ToBaseRelativePath(navigation.Uri);
}
