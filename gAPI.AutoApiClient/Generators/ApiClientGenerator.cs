using gAPI.AutoApiClient.Configs;
using gAPI.AutoApiClient.Contexts;
using gAPI.AutoApiClient.Models;
using System.Linq;

namespace gAPI.AutoApiClient.Generators
{
    internal class ApiClientGenerator : BaseGenerator
    {
        public ApiClientGenerator(Interface service, ClientConfig config)
        {
            Interface = service;

            Directory = config.Clients_Destination.Directory;
            Namespace = config.Clients_Destination.Namespace;

            Name = Interface.ApiName + "Client";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public Interface Interface { get; }

        public void GenerateCode()
        {
            Reg(Interface);
            Reg("System.Net.Http.Json");
            Reg("System.Net.Http.Headers");
            Reg("Microsoft.AspNetCore.Http");
            Reg("System.Globalization");
            Reg("gAPI.Helpers");

            var methodCode = GenerateMethodCode();

            UnReg("System.Threading.Tasks");

            Code = GetNamespacesCode();
            Code += "#nullable enable" + Environment.NewLine;
            Code += $"namespace {Namespace};" + Environment.NewLine;
            Code += @"" + Environment.NewLine;
            //Code += @"#pragma warning disable CS0618" + Environment.NewLine;
            Code += $"public class {Name}(gAPI.Interfaces.IClientAuthenticationService clientAuthenticationService) : {Interface.Name}" + Environment.NewLine;
            Code += @"{
" + methodCode;
            Code += @"}";
        }


        private string GenerateMethodCode()
        {
            var code = string.Empty;
            foreach (var method in Interface.Methods)
            {
                var methodSignature = string.Join(", ", method.Arguments
                    .Select(arg => $"{arg.ParameterType.Name} {arg.Name}"));
                var methodCall = string.Join(", ", method.Arguments
                    .Select(arg => $"{arg.Name}"));

                RegRange(method.ResponseType.Namespaces);

                var args = method.Arguments;
                if (method.IsList || method.IsListBy)
                    args = args
                        .Where(a => a.Name != "skip" && a.Name != "take" && a.Name != "orderby")
                        .ToArray();

                if (method.IsAsync)
                {
                    if (method.ResponseType.UnderlayingTypes.Length == 0)
                    {
                        code += $"    public async Task {method.Name}({methodSignature}) " + Environment.NewLine;
                    }
                    else
                    {
                        code += $"    public async Task<{method.ResponseType.UnderlayingTypes[0].Name}> {method.Name}({methodSignature}) " + Environment.NewLine;
                    }
                }
                else
                {
                    code += $"    #warning For better performance please change the method `{Interface.Name}.{method.Name}({methodSignature})` to be async." + Environment.NewLine;
                    if (method.ResponseType.Name == "void")
                    {
                        code += $"    public void {method.Name}({methodSignature}) " + Environment.NewLine;
                    }
                    else
                    {
                        code += $"    public {method.ResponseType.Name} {method.Name}({methodSignature}) " + Environment.NewLine;
                    }
                }

                code += $"    {{" + Environment.NewLine;

                if (args.Length > 0)
                {
                    code += $"        using var content = new MultipartFormDataContent();" + Environment.NewLine;
                }
                foreach (var arg in args)
                {
                    RegRange(arg.ParameterType.Namespaces);
                    if (arg.IsIFormFile)
                    {
                        code = AddFile(code, arg);
                    }
                    else if (!method.IsFileUpdate && !method.IsFileDelete)
                    {
                        if (arg.ParameterTypeRapport.Dto != null)
                            code += SerializeToForm(arg.ParameterTypeRapport.Dto, arg.Name);
                        else
                            code = AddProperty(code, arg.IsNullable, arg.IsValueType, arg.ParameterType.IsArray, arg.Name);
                    }
                }

                if (method.IsAsync)
                {
                    if (method.Arguments.Length == 0)
                    {
                        code += $"        using var response = await clientAuthenticationService.GetAsync(\"/{Interface.ApiName}/{method.Name}\");" + Environment.NewLine;
                    }
                    else if (method.IsList)
                    {
                        var orderByQueryItem = method.Arguments
                            .Where(a => a.Name == "orderby")
                            .FirstOrDefault();

                        var queryItems = method.Arguments
                            .Where(a => a.Name == "skip" || a.Name == "take");

                        code += $"        var queryItems = new List<string>();" + Environment.NewLine;
                        foreach (var a in queryItems)
                        {
                            if (a.IsNullable)
                            {
                                code += $"        if ({a.Name} != null)" + Environment.NewLine;
                                code += $"            queryItems.Add($\"{a.Name}={{{a.Name}}}\");" + Environment.NewLine;
                            }
                            else
                            {
                                code += $"        queryItems.Add($\"{a.Name}={{{a.Name}}}\");" + Environment.NewLine;
                            }
                        }
                        if (orderByQueryItem != null)
                        {

                            if (orderByQueryItem.IsNullable)
                            {
                                code += $"        if ({orderByQueryItem.Name} != null)" + Environment.NewLine;
                                code += $"            queryItems.AddRange({orderByQueryItem.Name}.Select(a => $\"{orderByQueryItem.Name}={{a}}\"));" + Environment.NewLine;
                            }
                            else
                            {
                                code += $"        queryItems.AddRange({orderByQueryItem.Name}.Select(a => $\"{orderByQueryItem.Name}={{a}}\"));" + Environment.NewLine;
                            }
                        }

                        code += $"        var query = $\"{{(queryItems.Count > 0 ? \"?\" : \"\")}}{{string.Join(\"&\", queryItems)}}\";" + Environment.NewLine;

                        code += $"        using var response = await clientAuthenticationService.GetAsync($\"/{Interface.ApiName}/{method.Name}{{query}}\");" + Environment.NewLine;
                    }
                    else if (method.IsListBy)
                    {
                        var idArg = method.Arguments
                            .Where(a => a.Name != "skip" && a.Name != "take" && a.Name != "orderby")
                            .First();

                        var orderByQueryItem = method.Arguments
                            .Where(a => a.Name == "orderby")
                            .FirstOrDefault();

                        var queryItems = method.Arguments
                            .Where(a => a.Name == "skip" || a.Name == "take");

                        code += $"        var queryItems = new List<string>();" + Environment.NewLine;
                        foreach (var a in queryItems)
                        {
                            if (a.IsNullable)
                            {
                                code += $"        if ({a.Name} != null)" + Environment.NewLine;
                                code += $"            queryItems.Add($\"{a.Name}={{{a.Name}}}\");" + Environment.NewLine;
                            }
                            else
                            {
                                code += $"        queryItems.Add($\"{a.Name}={{{a.Name}}}\");" + Environment.NewLine;
                            }
                        }
                        if (orderByQueryItem != null)
                        {
                            if (orderByQueryItem.IsNullable)
                            {
                                code += $"        if ({orderByQueryItem.Name} != null)" + Environment.NewLine;
                                code += $"            queryItems.AddRange({orderByQueryItem.Name}.Select(a => $\"{orderByQueryItem.Name}={{a}}\"));" + Environment.NewLine;
                            }
                            else
                            {
                                code += $"        queryItems.AddRange({orderByQueryItem.Name}.Select(a => $\"{orderByQueryItem.Name}={{a}}\"));" + Environment.NewLine;
                            }
                        }

                        code += $"        var query = $\"{{(queryItems.Count > 0 ? \"?\" : \"\")}}{{string.Join(\"&\", queryItems)}}\";" + Environment.NewLine;

                        code += $"        using var response = await clientAuthenticationService.GetAsync($\"/{Interface.ApiName}/{method.Name}/{{{idArg.Name}}}{{query}}\");" + Environment.NewLine;
                    }
                    else if (method.IsRead)
                    {
                        var arg = method.Arguments.First();
                        code += $"        using var response = await clientAuthenticationService.GetAsync($\"/{Interface.ApiName}/{method.Name}/{{{arg.Name}}}\");" + Environment.NewLine;
                    }
                    else if (method.IsDelete)
                    {
                        var arg = method.Arguments.First();
                        code += $"        using var response = await clientAuthenticationService.DeleteAsync($\"/{Interface.ApiName}/{method.Name}/{{{arg.Name}}}\");" + Environment.NewLine;
                    }
                    else if (method.IsUpdate)
                    {
                        code += $"        using var response = await clientAuthenticationService.PutAsync($\"/{Interface.ApiName}/{method.Name}\", content);" + Environment.NewLine;
                    }
                    else if (method.IsFileUpdate)
                    {
                        var arg = method.Arguments.First();
                        code += $"        using var response = await clientAuthenticationService.PutAsync($\"/{Interface.ApiName}/{method.Name}/{{{arg.Name}}}\", content);" + Environment.NewLine;
                    }
                    else if (method.IsFileDelete)
                    {
                        var arg = method.Arguments.First();
                        code += $"        using var response = await clientAuthenticationService.DeleteAsync($\"/{Interface.ApiName}/{method.Name}/{{{arg.Name}}}\");" + Environment.NewLine;
                    }
                    else
                    {
                        code += $"        using var response = await clientAuthenticationService.PostAsync(\"/{Interface.ApiName}/{method.Name}\", content);" + Environment.NewLine;
                    }

                    if (method.ResponseType.UnderlayingTypes.Length == 0)
                    {
                        code += $"        response.EnsureSuccessStatusCode();" + Environment.NewLine;
                    }
                    else
                    {
                        if (method.ResponseType.IsNullable)
                        {
                            code += $"        var responseData = await response.Content.ReadFromJsonAsync<{method.ResponseType.UnderlayingTypes[0].Name}>()" + Environment.NewLine;
                            code += $"            ?? throw new Exception(\"Could not cast response data\");" + Environment.NewLine;
                        }
                        else
                        {
                            code += $"        var responseData = await response.Content.ReadFromJsonAsync<{method.ResponseType.UnderlayingTypes[0].Name}>();" + Environment.NewLine;
                        }
                        code += $"        await clientAuthenticationService.AfterReceivedResponseIsParsedAsync(responseData!);" + Environment.NewLine;
                        code += $"        return responseData;" + Environment.NewLine;
                    }
                }
                else
                {
                    if (method.Arguments.Length == 0)
                    {
                        code += $"        using var response = clientAuthenticationService.GetAsync(\"/{Interface.ApiName}/{method.Name}\").Result;" + Environment.NewLine;
                    }
                    else if (method.IsList)
                    {
                        var orderByQueryItem = method.Arguments
                            .Where(a => a.Name == "orderby")
                            .FirstOrDefault();

                        var queryItems = method.Arguments
                            .Where(a => a.Name == "skip" || a.Name == "take");

                        code += $"        var queryItems = new List<string>();" + Environment.NewLine;
                        foreach (var a in queryItems)
                        {
                            if (a.IsNullable)
                            {
                                code += $"        if ({a.Name} != null)" + Environment.NewLine;
                                code += $"            queryItems.Add($\"{a.Name}={{{a.Name}}}\");" + Environment.NewLine;
                            }
                            else
                            {
                                code += $"        queryItems.Add($\"{a.Name}={{{a.Name}}}\");" + Environment.NewLine;
                            }
                        }
                        if (orderByQueryItem != null)
                        {

                            if (orderByQueryItem.IsNullable)
                            {
                                code += $"        if ({orderByQueryItem.Name} != null)" + Environment.NewLine;
                                code += $"            queryItems.AddRange({orderByQueryItem.Name}.Select(a => $\"{orderByQueryItem.Name}={{a}}\"));" + Environment.NewLine;
                            }
                            else
                            {
                                code += $"        queryItems.AddRange({orderByQueryItem.Name}.Select(a => $\"{orderByQueryItem.Name}={{a}}\"));" + Environment.NewLine;
                            }
                        }

                        code += $"        var query = $\"{{(queryItems.Count > 0 ? \"?\" : \"\")}}{{string.Join(\"&\", queryItems)}}\";" + Environment.NewLine;

                        code += $"        using var response = clientAuthenticationService.GetAsync($\"/{Interface.ApiName}/{method.Name}{{query}}\").Result;" + Environment.NewLine;
                    }
                    else if (method.IsListBy)
                    {
                        var idArg = method.Arguments
                            .Where(a => a.Name != "skip" && a.Name != "take" && a.Name != "orderby")
                            .First();

                        var orderByQueryItem = method.Arguments
                            .Where(a => a.Name == "orderby")
                            .FirstOrDefault();

                        var queryItems = method.Arguments
                            .Where(a => a.Name == "skip" || a.Name == "take");

                        code += $"        var queryItems = new List<string>();" + Environment.NewLine;
                        foreach (var a in queryItems)
                        {
                            if (a.IsNullable)
                            {
                                code += $"        if ({a.Name} != null)" + Environment.NewLine;
                                code += $"            queryItems.Add($\"{a.Name}={{{a.Name}}}\");" + Environment.NewLine;
                            }
                            else
                            {
                                code += $"        queryItems.Add($\"{a.Name}={{{a.Name}}}\");" + Environment.NewLine;
                            }
                        }
                        if (orderByQueryItem != null)
                        {
                            if (orderByQueryItem.IsNullable)
                            {
                                code += $"        if ({orderByQueryItem.Name} != null)" + Environment.NewLine;
                                code += $"            queryItems.AddRange({orderByQueryItem.Name}.Select(a => $\"{orderByQueryItem.Name}={{a}}\"));" + Environment.NewLine;
                            }
                            else
                            {
                                code += $"        queryItems.AddRange({orderByQueryItem.Name}.Select(a => $\"{orderByQueryItem.Name}={{a}}\"));" + Environment.NewLine;
                            }
                        }

                        code += $"        var query = $\"{{(queryItems.Count > 0 ? \"?\" : \"\")}}{{string.Join(\"&\", queryItems)}}\";" + Environment.NewLine;

                        code += $"        using var response = clientAuthenticationService.GetAsync($\"/{Interface.ApiName}/{method.Name}/{{{idArg.Name}}}{{query}}\").Result;" + Environment.NewLine;
                    }
                    else if (method.IsRead)
                    {
                        var arg = method.Arguments.First();
                        code += $"        using var response = clientAuthenticationService.GetAsync($\"/{Interface.ApiName}/{method.Name}/{{{arg.Name}}}\").Result;" + Environment.NewLine;
                    }
                    else if (method.IsDelete)
                    {
                        var arg = method.Arguments.First();
                        code += $"        using var response = clientAuthenticationService.DeleteAsync($\"/{Interface.ApiName}/{method.Name}/{{{arg.Name}}}\").Result;" + Environment.NewLine;
                    }
                    else if (method.IsUpdate)
                    {
                        code += $"        using var response = clientAuthenticationService.PutAsync($\"/{Interface.ApiName}/{method.Name}\", content).Result;" + Environment.NewLine;
                    }
                    else if (method.IsFileUpdate)
                    {
                        var arg = method.Arguments.First();
                        code += $"        using var response = clientAuthenticationService.PutAsync($\"/{Interface.ApiName}/{method.Name}/{{{arg.Name}}}\", content).Result;" + Environment.NewLine;
                    }
                    else if (method.IsFileDelete)
                    {
                        var arg = method.Arguments.First();
                        code += $"        using var response = clientAuthenticationService.DeleteAsync($\"/{Interface.ApiName}/{method.Name}/{{{arg.Name}}}\").Result;" + Environment.NewLine;
                    }
                    else
                    {
                        code += $"        using var response = clientAuthenticationService.PostAsync(\"/{Interface.ApiName}/{method.Name}\", content).Result;" + Environment.NewLine;
                    }

                    if (method.ResponseType.Name == "void")
                    {
                        code += $"        response.EnsureSuccessStatusCode();" + Environment.NewLine;
                    }
                    else
                    {
                        if (method.ResponseType.IsNullable)
                        {
                            code += $"        var responseData = response.Content.ReadFromJsonAsync<{method.ResponseType.Name}>().Result" + Environment.NewLine;
                            code += $"            ?? throw new Exception(\"Could not cast response data\");" + Environment.NewLine;
                        }
                        else
                        {
                            code += $"        var responseData = response.Content.ReadFromJsonAsync<{method.ResponseType.Name}>().Result;" + Environment.NewLine;
                        }
                        code += $"        clientAuthenticationService.AfterReceivedResponseIsParsedAsync(responseData!).GetAwaiter().GetResult();" + Environment.NewLine;
                        code += $"        return responseData;" + Environment.NewLine;
                    }
                }

                code += $"    }}" + Environment.NewLine;
            }
            return code;
        }

        private string AddFile(string code, InterfaceMethodArgument arg)
        {
            code += $@"        if ({arg.Name} != null)
        {{
            var fileContent = new StreamContent({arg.Name}.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue({arg.Name}.ContentType);
            content.Add(fileContent, ""{arg.Name}"", {arg.Name}.FileName);
        }}" + Environment.NewLine;
            return code;
        }

        private static string SerializeToForm(Dto dto, string root = null)
        {
            var code = string.Empty;
            foreach (var prop in dto.Properties)
            {
                if (!prop.IsReadOnly && !prop.IsForeignName)
                {
                    var propertyName = root == null ? prop.Name : $"{root}.{prop.Name}";
                    if (prop.TypeRapport.Dto != null)
                    {
                        SerializeToForm(prop.TypeRapport.Dto, propertyName);
                    }
                    else
                    {
                        code = AddProperty(code, prop.IsNullable, prop.TypeRapport.IsValueType, prop.Type.IsArray, propertyName);
                    }
                }
            }
            return code;
        }

        private static string AddProperty(string code, bool isNulleble, bool isValueType, bool isArray, string propertyName)
        {
            if (isArray)
            {
                if (isNulleble)
                {
                    if (isValueType)
                    {
                        code += $@"        if ({propertyName} != null)
            foreach(var item in {propertyName}) 
                content.Add(new StringContent(item.Value.ToString(CultureInfo.InvariantCulture)), ""{propertyName}"");" + Environment.NewLine;
                    }
                    else
                    {
                        code += $@"        if ({propertyName} != null)
            foreach(var item in {propertyName}) 
                content.Add(new StringContent(item.ToString(CultureInfo.InvariantCulture)), ""{propertyName}"");" + Environment.NewLine;
                    }
                }
                else
                {
                    code += $@"        foreach(var item in {propertyName}) 
            content.Add(new StringContent(item.ToString(CultureInfo.InvariantCulture)), ""{propertyName}"");" + Environment.NewLine;
                }

            }
            else if (isNulleble)
            {
                if (isValueType)
                {
                    code += $@"        if ({propertyName} != null)
            content.Add(new StringContent({propertyName}.Value.ToString(CultureInfo.InvariantCulture)), ""{propertyName}"");" + Environment.NewLine;
                }
                else
                {
                    code += $@"        if ({propertyName} != null)
            content.Add(new StringContent({propertyName}.ToString(CultureInfo.InvariantCulture)), ""{propertyName}"");" + Environment.NewLine;
                }
            }
            else
            {
                code += $@"        content.Add(new StringContent({propertyName}.ToString(CultureInfo.InvariantCulture)), ""{propertyName}"");" + Environment.NewLine;
            }

            return code;
        }
    }
}