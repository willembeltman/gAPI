using gAPI.AutoApiClient.Models;
using System;
using System.Linq;

namespace gAPI.AutoApiClient.Generators;

public class ApiClientGeneratorOld : BaseGenerator
{
    public ApiClientGeneratorOld(Generator context, Interface @interface)
    {
        Interface = @interface;
        Context = context;

        Directory = "";
        Namespace = @interface.Namespace;

        Name = Interface.CleanName;
        FileName = $"{Name}.g.cs";
    }

    public Interface Interface { get; }
    public Generator Context { get; }

    public SharedReference BaseResponse => Context.SharedReferences.BaseResponse;
    public SharedReference BaseResponseT => Context.SharedReferences.BaseResponseT;

    public SharedReference IClientAuthenticatedHttpClient => Context.SharedReferences.IClientAuthenticatedHttpClient;

    public override void GenerateCode()
    {
        Reg(Interface);
        Reg("Microsoft.Extensions.Logging");
        Reg("System.Net.Http.Json");
        Reg("System.Net.Http.Headers");
        Reg("Microsoft.AspNetCore.Http");
        Reg("System.Globalization");
        Reg("gAPI.Core.Extensions");
        Reg("gAPI.Core.Dtos");
        Reg(BaseResponse);
        Reg(BaseResponseT);

        var synchroniousMethod = Interface.Methods.FirstOrDefault(method => method.IsAsync == false);
        if (synchroniousMethod != null)
            throw new Exception($"We don't do synchonious code anymore, please change to async. {Name}.{synchroniousMethod.Name}");

        foreach (var method in Interface.Methods)
        {
            RegRange(method.Arguments.SelectMany(b => b.ParameterType.Namespaces));
            RegRange(method.ResponseType.Namespaces);

            var cancellationToken = method.Arguments
                .FirstOrDefault(a => a.ParameterType.Name == "CancellationToken");
            if (cancellationToken != null)
            {
                Reg(cancellationToken.ParameterType);
            }
        }

        var oldCode = string.Join("", Interface.Methods.Select(method =>
        {
            var methodSignature = string.Join(", ", method.Arguments
                .Select(arg => $"{arg.ParameterType.Name} {arg.Name}"));
            var methodCall = string.Join(", ", method.Arguments
                .Select(arg => $"{arg.Name}"));

            var args = method.Arguments;
            if (method.IsList || method.IsListBy)
                args = [.. args.Where(a => a.Name != "skip" && a.Name != "take" && a.Name != "orderby")];

            var cancellationToken = args
                .FirstOrDefault(a => a.ParameterType.Name == "CancellationToken");
            if (cancellationToken != null)
            {
                args = [.. args.Where(a => a.ParameterType.Name != "CancellationToken")];
            }

            var orderByQueryItem = method.Arguments
                .Where(a => a.Name == "orderby")
                .FirstOrDefault();

            var skipTakeQueryItems = method.Arguments
                .Where(a => a.Name == "skip" || a.Name == "take");

            var underlaying = method.ResponseType.UnderlayingTypes.FirstOrDefault();

            var methodType =
                underlaying == null
                ? $"Task"
                : $"Task<{method.ResponseType.UnderlayingTypes[0].Name}>";

            var responseType =
                underlaying == null
                ? "BaseResponse"
                : (underlaying.IsBaseResponse || underlaying.IsBaseResponseT || underlaying.IsBaseListResponseT)
                    ? underlaying.Name
                    : $"BaseResponseT<{underlaying.Name}>";

            return $@"
    public async {methodType} {method.Name}({methodSignature})
    {{{(args.Length > 0 ? $@"
        using var content = new MultipartFormDataContent();" : "")}{string.Join("", args.Select(arg =>
                    arg.IsIFormFile
                    ? $@"
        if ({arg.Name} != null)
        {{
            var fileContent = new StreamContent({arg.Name}.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue({arg.Name}.ContentType);
            content.Add(fileContent, ""{arg.Name}"", {arg.Name}.FileName);
        }}"
                    : method.IsFileUpdate == false && method.IsFileDelete == false
                        ?
                            arg.ParameterType.IsProperty == false
                            ? SerializeToForm(arg.ParameterType, arg.Name)
                            : AddProperty("", arg.IsNullable, arg.IsValueType, arg.ParameterType.IsArray, arg.ParameterType.IsDateTime, arg.ParameterType.IsDateTimeOffset, arg.Name)
                        : ""))}{(args.Length > 0 ? $@"
" : "")}{(
                method.IsList || method.IsListBy || method.IsListNotBy
                ? $@"
        var queryItems = new List<string>();{string.Join("", skipTakeQueryItems.Select(a => $@"
        if ({a.Name} != null)
            queryItems.Add($""{a.Name}={{{a.Name}}}"");"))}"
                : $@"")}
{(
                orderByQueryItem != null
                ? $@"
        if ({orderByQueryItem.Name} != null)
            queryItems.AddRange({orderByQueryItem.Name}.Select(a => $""{orderByQueryItem.Name}={{a}}""));
"
                : $@"")}{GetResponse(method, args, cancellationToken)}
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadFromJsonAsync<{responseType}>({(cancellationToken != null ? $"{cancellationToken.Name}" : "Cts.Token")})
            ?? throw new Exception(""Could not cast response data"");{(
                underlaying == null
                ? ""
                :
                    underlaying.IsBaseResponse ||
                    underlaying.IsBaseResponseT ||
                    underlaying.IsBaseListResponseT
                    ? $@"
        return responseData;"
                    :
                        underlaying.IsNullable || underlaying.Name == "bool"
                        ? @"
        return responseData.Response;" :
                            underlaying.IsValueType
                            ? @"
        return responseData.Response!;"
                        : @"
        return responseData.Response
            ?? throw new Exception(""Could not cast response data"");")}
    }}";
        }));

        Code = $@"{GetNamespacesCode()}
// <auto-generated />
#nullable enable
namespace {Namespace};

public class {Name}(
    {IClientAuthenticatedHttpClient.FullName} clientAuthenticationService) 
    : {Interface.Name}
    , IDisposable
{{
    private readonly CancellationTokenSource Cts = new();{oldCode}
    public void Dispose()
    {{
        Cts.Dispose();
    }}
}}";
    }

    private string GetResponse(InterfaceMethod method, InterfaceMethodArgument[] args, InterfaceMethodArgument? cancellationToken)
    {
        var code = "";
        if (args.Length == 0)
        {
            code += $"        using var response = await clientAuthenticationService.GetAsync(\"/{Interface.CleanName}/{method.Name}\"{(cancellationToken != null ? $", {cancellationToken.Name}" : ", Cts.Token")});";
        }
        else if (method.IsList)
        {
            code += $"        var query = $\"{{(queryItems.Count > 0 ? \"?\" : \"\")}}{{string.Join(\"&\", queryItems)}}\";\r\n";
            code += $"        using var response = await clientAuthenticationService.GetAsync($\"/{Interface.CleanName}/{method.Name}{{query}}\"{(cancellationToken != null ? $", {cancellationToken.Name}" : ", Cts.Token")});";
        }
        else if (method.IsListBy || method.IsListNotBy)
        {
            var idArg = method.Arguments
                .First(a => a.Name != "skip" && a.Name != "take" && a.Name != "orderby");
            code += $"        var query = $\"{{(queryItems.Count > 0 ? \"?\" : \"\")}}{{string.Join(\"&\", queryItems)}}\";\r\n";
            code += $"        using var response = await clientAuthenticationService.GetAsync($\"/{Interface.CleanName}/{method.Name}/{{{idArg.Name}}}{{query}}\"{(cancellationToken != null ? $", {cancellationToken.Name}" : ", Cts.Token")});";
        }
        else if (method.IsRead)
        {
            var arg = method.Arguments.First();
            code += $"        using var response = await clientAuthenticationService.GetAsync($\"/{Interface.CleanName}/{method.Name}/{{{arg.Name}}}\"{(cancellationToken != null ? $", {cancellationToken.Name}" : ", Cts.Token")});";
        }
        else if (method.IsDelete)
        {
            var arg = method.Arguments.First();
            code += $"        using var response = await clientAuthenticationService.DeleteAsync($\"/{Interface.CleanName}/{method.Name}/{{{arg.Name}}}\"{(cancellationToken != null ? $", {cancellationToken.Name}" : ", Cts.Token")});";
        }
        else if (method.IsUpdate)
        {
            code += $"        using var response = await clientAuthenticationService.PutAsync($\"/{Interface.CleanName}/{method.Name}\", content{(cancellationToken != null ? $", {cancellationToken.Name}" : ", Cts.Token")});";
        }
        else if (method.IsFileUpdate)
        {
            var arg = method.Arguments.First();
            code += $"        using var response = await clientAuthenticationService.PutAsync($\"/{Interface.CleanName}/{method.Name}/{{{arg.Name}}}\", content{(cancellationToken != null ? $", {cancellationToken.Name}" : ", Cts.Token")});";
        }
        else if (method.IsFileDelete)
        {
            var arg = method.Arguments.First();
            code += $"        using var response = await clientAuthenticationService.DeleteAsync($\"/{Interface.CleanName}/{method.Name}/{{{arg.Name}}}\"{(cancellationToken != null ? $", {cancellationToken.Name}" : ", Cts.Token")});";
        }
        else
        {
            code += $"        using var response = await clientAuthenticationService.PostAsync(\"/{Interface.CleanName}/{method.Name}\", content{(cancellationToken != null ? $", {cancellationToken.Name}" : ", Cts.Token")});";
        }
        return code;
    }

    private static string SerializeToForm(TypeHelper dto, string? root = null)
    {
        var code = string.Empty;
        foreach (var prop in dto.GetProperties())
        {
            if (!prop.IsReadOnly && !prop.IsForeignName && prop.PropertyType.IsCancellationToken == false)
            {
                var propertyName = root == null ? prop.Name : $"{root}.{prop.Name}";
                if (prop.PropertyType.IsProperty == false)
                {
                    SerializeToForm(prop.PropertyType, propertyName);
                }
                else
                {
                    code = AddProperty(code, prop.IsNullable, prop.PropertyType.IsValueType, prop.PropertyType.IsArray, prop.PropertyType.IsDateTime, prop.PropertyType.IsDateTimeOffset, propertyName);
                }
            }
        }
        return code;
    }

    private static string AddProperty(string code, bool isNulleble, bool isValueType, bool isArray, bool isDateTime, bool isDateTimeOffset, string propertyName)
    {
        string toString;
        if (isDateTimeOffset)
        {
            toString = ".ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)";
        }
        else if (isDateTime)
        {
            toString = ".ToUniversalTime().ToString(\"yyyy-MM-ddTHH:mm:ss.fffffffZ\", CultureInfo.InvariantCulture)";
        }
        else
        {
            if (isValueType)
                toString = ".ToString()";
            else
                toString = ".ToString(CultureInfo.InvariantCulture)";
        }

        if (isArray)
        {
            if (isNulleble)
            {
                if (isValueType)
                {
                    return $@"
        if ({propertyName} != null)
            foreach(var item in {propertyName}) 
                content.Add(new StringContent(item.Value{toString}), ""{propertyName}"");";
                }
                else
                {
                    code += $@"
        if ({propertyName} != null)
            foreach(var item in {propertyName}) 
                content.Add(new StringContent(item{toString}), ""{propertyName}"");";
                }
            }
            else
            {
                code += $@"
        foreach(var item in {propertyName}) 
            content.Add(new StringContent(item{toString}), ""{propertyName}"");";
            }
        }
        else if (isNulleble)
        {
            if (isValueType)
            {
                code += $@"
        if ({propertyName} != null)
            content.Add(new StringContent({propertyName}.Value{toString}), ""{propertyName}"");";
            }
            else
            {
                code += $@"
        if ({propertyName} != null)
            content.Add(new StringContent({propertyName}{toString}), ""{propertyName}"");";
            }
        }
        else
        {
            code += $@"
        content.Add(new StringContent({propertyName}{toString}), ""{propertyName}"");";
        }

        return code;
    }
}