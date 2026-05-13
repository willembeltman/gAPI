using gAPI.CodeGen.Frontend.Helpers;
using gAPI.CodeGen.Frontend.Models.Configs;
using gAPI.CodeGen.Frontend.Models.ServiceModels;
using gAPI.Core.Attributes;
using System.Reflection;

namespace gAPI.CodeGen.Frontend.Generators.Razor.Pages.Page;

public class PageGenerator : BaseGenerator
{
    public PageGenerator(
        InterfaceMethod interfaceMethod,
        FrontendConfig clientConfig,
        ImportsGenerator imports)
    {
        ClientConfig = clientConfig;
        Imports = imports;
        Interface = interfaceMethod.Interface;
        Method = interfaceMethod;
        ResponseType = interfaceMethod.ResponseType;

        IsTaskT = ResponseType.IsTaskT;
        if (IsTaskT)
            ResponseType = ResponseType.UnderlayingTypes[0];

        InnerResponseType = ResponseType;

        IsBaseResponse = InnerResponseType.IsBaseResponse;

        IsBaseResponseT = InnerResponseType.IsBaseResponseT;
        if (IsBaseResponseT)
            InnerResponseType = InnerResponseType.UnderlayingTypes[0];

        IsBaseListResponseT = InnerResponseType.IsBaseListResponseT;
        if (IsBaseListResponseT)
            InnerResponseType = InnerResponseType.UnderlayingTypes[0];

        if (interfaceMethod.IsPageRoute == null)
            throw new Exception($"No route for page {Method.Name}");
        Route = interfaceMethod.IsPageRoute;
        Title = interfaceMethod.IsPageTitle;
        SubmitText = interfaceMethod.IsPageSubmitText ?? "Submit";

        IsAuthorized = Method.IsAuthorized;
        IsNotAuthorized = Method.IsNotAuthorized;

        var split = Route.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var pathsplit = split.Take(split.Length - 1);

        Name = split.Last().ToNameCase();
        FileName = $"{Name}.razor";

        Namespace = string.Join(".", [clientConfig.PagesNamespace, .. pathsplit]);
        RoutePath = $"/{string.Join("/", pathsplit)}";

        var fullDirectory = clientConfig.PagesDirectory.FullName;
        foreach (var path in pathsplit)
        {
            fullDirectory = Path.Combine(fullDirectory, path);
        }
        Directory = new DirectoryInfo(fullDirectory);
    }

    public FrontendConfig ClientConfig { get; }
    public ImportsGenerator Imports { get; }
    public Interface Interface { get; }
    public InterfaceMethod Method { get; }
    public TypeHelper ResponseType { get; }
    public TypeHelper InnerResponseType { get; }
    public bool IsAuthorized { get; }
    public bool IsNotAuthorized { get; }
    public bool IsTaskT { get; }
    public bool IsBaseResponse { get; }
    public bool IsBaseResponseT { get; }
    public bool IsBaseListResponseT { get; }
    public string Route { get; }
    public string? Title { get; }
    public string SubmitText { get; }
    public string RoutePath { get; }

    public void GenerateCode()
    {
        var args = Method.Arguments.Where(a => a.ParameterType.Name != "CancellationToken").ToArray();

        foreach (var arg in args)
        {
            Imports.Reg(arg.ParameterType);
            Imports.Reg(Method.ResponseTypeDigger);
        }

        Imports.Reg(Interface);

        var space = Method.IsAuthorized || Method.IsNotAuthorized ? "        " : "";

        var requestForm = string.Join(
            "",
            args.Select(arg =>
                GenerateInputForType($"{space}    ", arg.Name!.ToNameCase(), arg.Title!, arg.ParameterInfo.ParameterType, arg.IsPassword)));


        Code = $@"{GetRazorNamespacesCode()}@page ""{Route}""
@implements IAsyncDisposable
@inject {Interface.Name} {Interface.Title.ToNameCase()}{(IsBaseResponse || IsBaseResponseT || IsBaseListResponseT ? $@"
@inject NavigationManager NavigationManager" : "")}

<PageTitle>{Title}</PageTitle>
<h3>{Title}</h3>{(Method.IsAuthorized && !Method.IsNotAuthorized ? $@"

<AuthorizeView>
    <Authorized Context=""_"">" : "")}{(!Method.IsAuthorized && Method.IsNotAuthorized ? $@"

<AuthorizeView>
    <Authorized>
        <RedirectToHome />
    </Authorized>
    <NotAuthorized Context=""_"">" : "")}
{(args.Length == 0 ? $@"

{space}<div>
{space}    Waiting for server...{(IsBaseResponse || IsBaseResponseT ? $@"
{space}    <ErrorView Response=""Response"" />" : "")}
{space}</div>" : $@"
{space}<EditForm Model=""this"" OnValidSubmit=""HandleValidSubmit"">
{space}    <DataAnnotationsValidator />
{space}    <ValidationSummary />{requestForm}{(IsBaseResponse || IsBaseResponseT ? $@"
{space}    <ErrorView Response=""Response"" />" : "")}
{space}    <button type=""submit"" class=""btn btn-primary"">{SubmitText}</button>
{space}</EditForm>")}{(

    IsBaseResponse
    ? $@"

{space}@if (Response != null && Response.Success)
{space}{{
{space}    <h3>Success</h3>
{space}}}"
    : (
        IsBaseResponseT
        ? $@"

{space}@if (Response != null && Response.Response != null)
{space}{{
{space}    <div class=""mb-3"">
{space}        <h4>Response</h4>{GenerateDisplayForType($"{space}        ", "Response.Response", InnerResponseType.Type)}
{space}    </div>
{space}}}"
        : (
            IsBaseListResponseT
            ? $@"

{space}@if (Response != null)
{space}{{
{space}    <span>Not jet implemented</span>
{space}}}"
            : $@"

{space}@if (Response != null)
{space}{{
{space}    <div class=""mb-3"">
{space}        <h4>Response</h4>{GenerateDisplayForType($"{space}        ", "Response", ResponseType.Type)}
{space}    </div>
{space}}}"
        )
    )

    )}{(Method.IsAuthorized && !Method.IsNotAuthorized ? $@"

    </Authorized>
    <NotAuthorized>
        <RedirectToLogin />
    </NotAuthorized>
</AuthorizeView>" : "")}{(!Method.IsAuthorized && Method.IsNotAuthorized ? $@"

    </NotAuthorized>
</AuthorizeView>" : "")}

@code {{
    private CancellationTokenSource Cts = new();{string.Join("", args.Select(arg => $@"
    private {arg.ParameterType.Name} {arg.Name!.ToNameCase()} {{ get; set; }}{(arg.ParameterType.IsNullable || arg.ParameterType.IsValueType ? "" : (arg.ParameterType.IsString ? " = string.Empty;" : " = new();"))}"))}{(!ResponseType.IsTask && !ResponseType.IsVoid ? $@"
    private {ResponseType.Name}? Response {{ get; set; }}" : "")}{(args.Length == 0 ? $@"

    protected override async Task OnInitializedAsync()
    {{
        await HandleValidSubmit();
    }}" : "")}

    private async Task HandleValidSubmit()
    {{
        {(!ResponseType.IsTask && !ResponseType.IsVoid ? "Response = await " : "")}{Interface.Title.ToNameCase()}.{Method.Name}({string.Join(", ",
            Method.Arguments.Any(a => a.ParameterType.Name == "CancellationToken")
            ? [.. args.Select(a => a.Name!.ToNameCase()), "Cts.Token"]
            : args.Select(a => a.Name!.ToNameCase()))});{(IsBaseResponse || IsBaseResponseT ? $@"
        if (!string.IsNullOrWhiteSpace(Response.RedirectPath))
        {{
            NavigationManager.NavigateTo(Response.RedirectPath);
        }}" : "")}
    }}

    async ValueTask IAsyncDisposable.DisposeAsync()
    {{
        await Cts.CancelAsync();
        Cts.Dispose();
    }}
}}";

        Save(false);
    }

    private static string GenerateInputForType(string space, string propertyName, string name, Type type, bool isPassword)
    {
        // For simplicity: only support primitive/string/enum types + one-level DTO properties
        if (type == typeof(string))
            return $@"
{space}<div class=""mb-3"">
{space}    <label>{name}</label>
{space}    <InputText @bind-Value=""{propertyName}""{(isPassword ? $@" type=""password""" : "")} class=""form-control"" />
{space}</div>";

        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(double) || type == typeof(decimal) || type == typeof(float))
            return $@"
{space}<div class=""mb-3"">
{space}    <label>{name}</label>
{space}    <InputNumber @bind-Value=""{propertyName}"" class=""form-control"" />
{space}</div>";

        if (type == typeof(bool))
            return $@"
{space}<div class=""mb-3 form-check"">
{space}    <InputCheckbox @bind-Value=""{propertyName}"" class=""form-check-input"" id=""{propertyName}"" />
{space}    <label class=""form-check-label"" for=""{propertyName}"">{name}</label>
{space}</div>";

        if (type.IsEnum)
            return $@"
{space}<div class=""mb-3"">
{space}    <label>{name}</label>
{space}    <InputSelect @bind-Value=""{propertyName}"" class=""form-select"">
{space}        @foreach (var val in Enum.GetValues(typeof({type.Name})).Cast<{type.Name}>())
{space}        {{
{space}            <option value=""@val"">@val</option>
{space}        }}
{space}    </InputSelect>
{space}</div>";

        return $@"
{space}<fieldset class=""mb-3"">
{space}    <legend>{name}</legend>{string.Join("", type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(prop => $@"
{GenerateInputForType($"{space}    ", $"{propertyName}.{prop.Name}", prop.GetCustomAttribute<TitleAttribute>()?.Name ?? prop.Name, prop.PropertyType, prop.GetCustomAttribute<IsPasswordAttribute>() != null)}"))}
{space}</fieldset>";
    }

    private static string GenerateDisplayForType(string space, string name, Type type)
    {
        return $@"
{space}<dl>{string.Join("", type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(prop =>
{
    var properties = prop.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    if (properties.Length > 0 && !prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string))
    {
        return $@"
{space}    <dt>{prop.Name}</dt>
{space}    <dd>{GenerateDisplayForType($"{space}        ", $"{name}.{prop.Name}", prop.PropertyType)}
{space}    </dd>";
    }
    return $@"
{space}    <dt>{prop.Name}</dt>
{space}    <dd>@{name}.{prop.Name}</dd>";
}))}
{space}</dl>";
    }

    private static string GetTypeName(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return $"{GetTypeName(type.GetGenericArguments()[0])}?";
        return type.Name;
    }
}