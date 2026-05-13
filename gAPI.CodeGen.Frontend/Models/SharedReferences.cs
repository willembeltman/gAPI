using gAPI.CodeGen.Frontend.Models.Configs;
using gAPI.Core.Attributes;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace gAPI.CodeGen.Frontend.Models;

public class SharedReferences
{
    public SharedReferences(FrontendConfig config)
    {
        var allTypes =
            config.Assemblies.SelectMany(a => a.GetTypes()).ToArray();

        FormFile = allTypes
            .Where(t =>
                t.IsClass &&
                t.GetCustomAttribute<IsFormFileAttribute>() != null
            )
            .Select(a => new SharedReference(a))
            .First();
        IsFormFileExtension = allTypes
            .Where(t =>
                t.IsClass &&
                t.GetCustomAttribute<IsFormFileExtensionAttribute>() != null
            )
            .Select(a => new SharedReference(a))
            .First();

        LoaderView = allTypes
            .Where(a => a.Name == "LoaderView")
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new Exception("cannot find LoaderView");

        ErrorView = allTypes
            .Where(a => a.Name == "ErrorView")
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new Exception("cannot find ErrorView");

        RedirectToHome = allTypes
            .Where(a => a.Name == "RedirectToHome")
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new Exception("cannot find RedirectToHome");

        RedirectToLogin = allTypes
            .Where(a => a.Name == "RedirectToLogin")
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new Exception("cannot find RedirectToLogin");

        BaseResponse = new SharedReference("gAPI.Core.Dtos.BaseResponse");
        BaseResponseT = new SharedReference("gAPI.Core.Dtos.BaseResponseT");
        BaseListResponseT = new SharedReference("gAPI.Core.Dtos.BaseListResponseT");
        StateChangedHandler = new SharedReference("gAPI.Delegates.StateChangedHandler");
        IClientAuthenticatedHttpClient = new SharedReference("gAPI.Interfaces.IClientAuthenticatedHttpClient");
        ItemDataSource = new SharedReference("gAPI.Core.Client.ItemDataSource");
        ListDataSource = new SharedReference("gAPI.Core.Client.ListDataSource");


        int CountHierarchy(Type? t)
        {
            var count = 0;
            while (t != null && t != typeof(object))
            {
                if (t.GetCustomAttributes(typeof(IsStateDtoAttribute), inherit: false).Any())
                    return count;

                count++;
                t = t.BaseType;
            }
            return -1;
        }

        var state = allTypes
            .Where(t =>
                t.IsClass &&
                t.GetCustomAttributes(typeof(IsStateDtoAttribute), inherit: true).Any()
            )
            .Select(t => new { Type = t, Count = CountHierarchy(t) })
            .OrderByDescending(a => a.Count)
            .Select(a => a.Type)
            .ToArray();

        State = state
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new InvalidOperationException(
                "The `State` dto is missing or does not have the required " +
                "`gAPI.Core.Attributes.IsStateDtoAttribute`. " +
                "Ensure your shared project defines a `State` dto and that it is annotated with " +
                "`[IsStateDtoAttribute]`.");

    }

    public SharedReference FormFile { get; }
    public SharedReference IsFormFileExtension { get; }
    public SharedReference BaseResponse { get; }
    public SharedReference BaseResponseT { get; }
    public SharedReference BaseListResponseT { get; }
    public SharedReference LoaderView { get; }
    public SharedReference ErrorView { get; }
    public SharedReference RedirectToHome { get; }
    public SharedReference RedirectToLogin { get; }
    public SharedReference State { get; }
    public SharedReference StateChangedHandler { get; }
    public SharedReference IClientAuthenticatedHttpClient { get; }
    public SharedReference ItemDataSource { get; }
    public SharedReference ListDataSource { get; }
}
