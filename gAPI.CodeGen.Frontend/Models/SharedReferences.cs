using gAPI.Attributes;
using gAPI.CodeGen.Frontend.Models.Configs;
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
        IsFormFileExtention = allTypes
            .Where(t =>
                t.IsClass &&
                t.GetCustomAttribute<IsFormFileExtentionAttribute>() != null
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

        BaseResponse = new SharedReference("gAPI.Dtos.BaseResponse");
        BaseResponseT = new SharedReference("gAPI.Dtos.BaseResponseT");
        BaseListResponseT = new SharedReference("gAPI.Dtos.BaseListResponseT");
        StateChangedHandler = new SharedReference("gAPI.Delegates.StateChangedHandler");

        State = allTypes
            .Where(t =>
                t.IsClass &&
                t.GetCustomAttributes(typeof(IsStateDtoAttribute), inherit: true).Length > 0
            )
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new InvalidOperationException(
                "The `State` dto is missing or does not have the required " +
                "`gAPI.Attributes.IsStateDtoAttribute`. " +
                "Ensure your shared project defines a `State` dto and that it is annotated with " +
                "`[IsStateDtoAttribute]`.");

    }

    public SharedReference FormFile { get; }
    public SharedReference IsFormFileExtention { get; }
    public SharedReference BaseResponse { get; }
    public SharedReference BaseResponseT { get; }
    public SharedReference BaseListResponseT { get; }
    public SharedReference LoaderView { get; }
    public SharedReference ErrorView { get; }
    public SharedReference RedirectToHome { get; }
    public SharedReference RedirectToLogin { get; }
    public SharedReference State { get; }
    public SharedReference StateChangedHandler { get; }
}
