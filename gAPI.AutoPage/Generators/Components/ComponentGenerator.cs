using gAPI.AutoPage.Helpers;
using gAPI.AutoPage.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoPage.Generators.Components;

public class ComponentGenerator : BaseGenerator
{
    public ComponentGenerator(
        IContext context,
        ICrudlMethod method,
        IBaseGenerator imports,
        string directory,
        string @namespace)
    {
        Context = context;
        Imports = imports;
        Method = method;
        Interface = method.Interface;
        ResponseType = method.Type.UnderlayingTypes[0];

        InnerResponseType = ResponseType;

        IsBaseResponse = InnerResponseType.IsBaseResponse;

        IsBaseResponseT = InnerResponseType.IsBaseResponseT;
        if (IsBaseResponseT)
            InnerResponseType = InnerResponseType.UnderlayingTypes[0];

        IsBaseListResponseT = InnerResponseType.IsBaseListResponseT;
        if (IsBaseListResponseT)
            InnerResponseType = InnerResponseType.UnderlayingTypes[0];

        Title = method.IsComponentTitle ?? Name;
        SubmitText = method.IsComponentSubmitText ?? "Submit";
        ResponseText = method.IsComponentResponseText ?? "Response";

        IsAuthorized = Method.IsAuthorized;
        IsNotAuthorized = Method.IsNotAuthorized;
        ListDataSource = Context.ListDataSource!;

        Name = $"{Interface.Name}{method.Name}";
        FileName = $"{Name}.razor";

        Namespace = @namespace;
        Directory = directory;

    }

    public IContext Context { get; }
    public IBaseGenerator Imports { get; }
    public ICrudlMethod Method { get; }
    public ISharedReference Interface { get; }
    public ITypeHelper ResponseType { get; }
    public ITypeHelper InnerResponseType { get; }
    public bool IsAuthorized { get; }
    public bool IsNotAuthorized { get; }
    public bool IsBaseResponse { get; }
    public bool IsBaseResponseT { get; }
    public bool IsBaseListResponseT { get; }
    public string Title { get; }
    public string SubmitText { get; }
    public string ResponseText { get; }
    public ISharedReference ListDataSource { get; }

    public List<string> Injects = [];
    public List<string> Properties = [];
    public List<string> DataSources = [];
    public List<string> Disposes = [];

    public void GenerateCode()
    {
        var args = Method.Arguments.Where(a => a.Type.Name != "CancellationToken").ToArray();

        Imports.Reg("Microsoft.AspNetCore.Components");
        Imports.Reg("Microsoft.AspNetCore.Components.Forms");
        Imports.Reg("Microsoft.AspNetCore.Components.Web");
        Imports.Reg("gAPI.Core.Dtos");
        Imports.Reg("Microsoft.JSInterop");

        Imports.Reg(Method.Type);

        foreach (var arg in args)
        {
            Imports.Reg(arg.Type);
        }

        Imports.Reg(Interface);

        var space = Method.IsAuthorized || Method.IsNotAuthorized ? "        " : "";

        var requestForm = string.Join(
            "",
            args.Select(arg =>
                GenerateInputForType($"{space}    ", arg.Name!.ToNameCase(), arg)));

        Code = $@"@implements IAsyncDisposable
@inject {Interface.Name} {Interface.Name.ToCamelCase()}{(IsBaseResponse || IsBaseResponseT || IsBaseListResponseT ? $@"
@inject NavigationManager NavigationManager" : "")}{string.Join("", Injects)}
@inject IJSRuntime JS

<ComponentTitle>{Title}</ComponentTitle>
<h2>{Title}</h3>{(Method.IsAuthorized && !Method.IsNotAuthorized ? $@"

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
{space}        {GenerateDisplayForType($"{space}        ", "Response.Response", ResponseText, InnerResponseType)}
{space}    </div>
{space}}}"
        : (
            IsBaseListResponseT
            ? $@"

{space}@if (Response != null)
{space}{{
{space}    <span>Not jet implemented</span>
{space}}}"
            : (
                ResponseType.IsTask || ResponseType.IsVoid
                ? ""
                : $@"

{space}@if (Response != null)
{space}{{
{space}    <div class=""mb-3"">
{space}        {GenerateDisplayForType($"{space}        ", "Response", ResponseText, ResponseType)}
{space}    </div>
{space}}}"
            )
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

@code {{{(DataSources.Any() ? $@"
    private CancellationTokenSource? Cts;" : $@"
    private CancellationTokenSource Cts = new();")}{string.Join("", args.Select(arg => $@"
    private {arg.Type.Name} {arg.Name!.ToNameCase()} {{ get; set; }}{(arg.Type.IsNullable || arg.Type.IsValueType ? "" : (arg.Type.IsString ? " = string.Empty;" : " = new();"))}"))}{(!ResponseType.IsTask && !ResponseType.IsVoid ? $@"
    private {ResponseType.Name}{(ResponseType.IsNullable ? "" : "?")} Response {{ get; set; }}" : "")}{(args.Length == 0 ? $@"

    protected override async Task OnInitializedAsync()
    {{
        await HandleValidSubmit();
    }}" : "")}
{string.Join("", Properties)}
{(DataSources.Any() ? $@"

    protected override async Task OnInitializedAsync()
    {{
        if (Cts != null)
        {{
            await Cts.CancelAsync();
            Cts.Dispose();
        }}

        Cts = new CancellationTokenSource();{string.Join("", DataSources)}
    }}" : "")}

    private async Task HandleValidSubmit()
    {{
        {(!ResponseType.IsTask && !ResponseType.IsVoid ? $"Response = " : "")} await {Interface.Name.ToCamelCase()}.{Method.Name}({string.Join(", ",
            Method.Arguments.Any(a => a.Type.Name == "CancellationToken")
            ? [.. args.Select(a => a.Name!.ToNameCase()), "Cts.Token"]
            : args.Select(a => a.Name!.ToNameCase()))});{(IsBaseResponse || IsBaseResponseT ? $@"
        if (!string.IsNullOrWhiteSpace(Response.RedirectPath))
        {{
            NavigationManager.NavigateTo(Response.RedirectPath);
        }}" : "")}
    }}
{(DataSources.Any() ? $@"
    async ValueTask IAsyncDisposable.DisposeAsync()
    {{
        await Cts?.CancelAsync();
        Cts?.Dispose();{string.Join("", Disposes)}
    }}" : $@"
    async ValueTask IAsyncDisposable.DisposeAsync()
    {{
        await Cts.CancelAsync();
        Cts.Dispose();{string.Join("", Disposes)}
    }}")}
}}";
    }
    private string GenerateInputForType(string space, string propertyName, ITypeHelperProperty prop)
    {
        var type = prop.Type;
        var title = prop.Title;
        var isPassword = prop.IsPassword;

        var friendlyPropertyName = propertyName.Replace(".", "-");

        if (prop.IsForeignKeyType != null)
        {
            var crudl = Context.Crudls.FirstOrDefault(a => a.ResponseTypeDigger?.Type.FullName == prop.IsForeignKeyType.FullName);
            if (crudl != null)
            {
                var dto = crudl.ResponseTypeDigger;
                var interfaceMethod = crudl.Methods.FirstOrDefault(a => a.CrudlMethodType == Enums.CrudlMethodTypeEnum.List);
                if (interfaceMethod != null && dto != null)
                {
                    var name = crudl.Name.ToNameCase();
                    var dropdownName = $"{name}DropDown";
                    var dropdownComponent = Context.SharedReferences.AllComponents
                        .FirstOrDefault(a => a.Name == dropdownName);
                    if (dropdownComponent == null)
                    {
                        var autoDropDownName = $"{crudl.Name.ToNameCase()}DropDown";
                        dropdownComponent = Context.SharedReferences.AllComponents
                            .FirstOrDefault(a => a.Name == autoDropDownName);
                    }
                    if (dropdownComponent != null)
                    {
                        // Todo dropdown toevoegen
                        var dsName = name.ToMultiple();
                        string bindAttr = type.IsNullable ? "bind-NullableValue" : "bind-Value";
                        string bindTypeAttr = type.IsNullable ? "bindtype_NullableValue" : "bindtype_Value";
                        string keyType = crudl.KeyProperty.TypeSimpleName;
                        bool keyTypeNullable = type.IsNullable;
                        string keyName = crudl.KeyProperty.Name;

                        var serviceName = interfaceMethod.Interface.Name.ToCamelCase();
                        var interfaceName = interfaceMethod.Interface.Name;


                        // Ja dit mag:
                        Imports.Reg(dropdownComponent);
                        Imports.Reg(ListDataSource);
                        Imports.Reg(interfaceMethod.Interface);
                        Imports.Reg(dto.Type);

                        Injects.Add($@"
@inject {interfaceName} {serviceName}");

                        Properties.Add($@"
    private {ListDataSource.Name}<{name}, {keyType}>? {dsName};
    private string? {name}Name;");

                        DataSources.Add($@"

        {dsName} = new {ListDataSource.Name}<{name}, {keyType}>(
            JS,
            StateHasChanged,
            GetPrimaryKey: {name.ToLower()} => {name.ToLower()}.{keyName},
            List: {serviceName}.List,
            SetForeignKey: null,
            AfterSaveAction: null,
            AfterCancelAction: null,
            Create: null,
            Read: null,
            Update: null,
            Delete: null
        );

        await {dsName}.InitialiseAsync();");

                        Disposes.Add($@"

        if ({dsName} != null)
            await {dsName}.DisposeAsync();");

                        return $@"
        <div class=""mb-3"">
            <label for=""{friendlyPropertyName}"" class=""form-label"">{title}</label>
            <{dropdownComponent.Name} @{bindAttr}=""{propertyName}"" {bindTypeAttr}=""{keyType}{(keyTypeNullable ? "?" : "")}""
                @bind-ForeignName=""{name}Name"" bindtype_ForeignName=""string?""
                DataSource=""@{dsName}"" id=""{friendlyPropertyName}"" />
        </div>";
                    }
                }
            }
        }

        if (type.IsGuid)
            return "";

        Imports.Reg(type);

        // For simplicity: only support primitive/string/enum types + one-level DTO properties
        if (type.IsString)
            return $@"
{space}<div class=""mb-3"">
{space}    <label for=""{friendlyPropertyName}"">{title}</label>
{space}    <InputText id=""{friendlyPropertyName}"" @bind-Value=""{propertyName}"" bindtype_Value=""{type.Name}"" class=""form-control""{(isPassword ? $@" type=""password""" : " autocomplete")} />
{space}</div>";

        if (type.IsNumber)
            return $@"
{space}<div class=""mb-3"">
{space}    <label for=""{friendlyPropertyName}"">{title}</label>
{space}    <InputNumber id=""{friendlyPropertyName}"" @bind-Value=""{propertyName}"" bindtype_Value=""{type.Name}"" class=""form-control"" autocomplete />
{space}</div>";

        if (type.IsCheckbox)
            return $@"
{space}<div class=""mb-3 form-check"">
{space}    <InputCheckbox id=""{friendlyPropertyName}"" @bind-Value=""{propertyName}"" bindtype_Value=""{type.Name}"" class=""form-check-input"" autocomplete />
{space}    <label for=""{friendlyPropertyName}"" class=""form-check-label"">{title}</label>
{space}</div>";

        if (type.IsEnum)
            return $@"
{space}<div class=""mb-3"">
{space}    <label for=""{friendlyPropertyName}"">{title}</label>
{space}    <InputSelect id=""{friendlyPropertyName}"" @bind-Value=""{propertyName}"" bindtype_Value=""{type.Name}"" class=""form-select"">
{space}        @foreach (var val in Enum.GetValues(typeof({type.Name})).Cast<{type.Name}>())
{space}        {{
{space}            <option value=""@val"">@val</option>
{space}        }}
{space}    </InputSelect>
{space}</div>";

        return $@"
{space}<fieldset class=""mb-3"">
{space}    <legend>{title}</legend>{string.Join("", type.GetProperties().Select(prop => $@"
{GenerateInputForType($"{space}    ", $"{propertyName}.{prop.Name}", prop)}"))}
{space}</fieldset>";
    }

    private static string GenerateDisplayForType(string space, string name, string title, ITypeHelper type)
    {
        if (type.IsString || type.IsNumber || type.IsCheckbox || type.IsDateTime || type.IsEnum || type.IsPrimitive || type.IsValueType)
        {
            if (type.GetProperties().Any())
            {
                return $@"
{space}<h3>{title}</h3>
{space}<p>@{name}</p>";
            }
            return $@"
{space}<dl>
{space}    <dt>{title}</dt>
{space}    <dd>@{name}</dd>
{space}</dl>";
        }

        return $@"
{space}<dl>{string.Join("", type.GetProperties().Select((Func<ITypeHelperProperty, string>)(prop =>
        {
            if (prop.Type.GetProperties().Length > 0 &&
                !prop.Type.IsPrimitive &&
                !prop.Type.IsString)
            {
                return $@"
{space}    <dt>{prop.Name}</dt>
{space}    <dd>{GenerateDisplayForType($"{space}        ", $"{name}.{prop.Name}", prop.Title, (ITypeHelper)prop.Type)}
{space}    </dd>";
            }

            return $@"
{space}    <dt>{prop.Name}</dt>
{space}    <dd>@{name}.{prop.Name}</dd>";
        })))}
{space}</dl>";
    }

    private static string GetTypeName(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return $"{GetTypeName(type.GetGenericArguments()[0])}?";
        return type.Name;
    }
}
