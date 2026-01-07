using gAPI.AutoSse.Models;
using System;
using System.Linq;

namespace gAPI.AutoSse.Generators
{
    internal class SseServiceGenerator : BaseGenerator
    {
        internal SseServiceGenerator(ServiceContext dataModel, Interface @interface)
        {
            DataModel = dataModel;
            Interface = @interface;

            Directory = dataModel.Config.SseServices_Destination.Directory;
            Namespace = dataModel.Config.SseServices_Destination.Namespace;

            Name = Interface.ApiName;
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public Interface Interface { get; }

        public void GenerateCode()
        {
            Reg("Newtonsoft.Json");
            Reg(Interface);
            Reg(DataModel.FabricClient);
            Reg(DataModel.UserId);
            Reg(DataModel.SessionId);
            Reg(DataModel.SseServiceId);
            Reg(DataModel.SseServiceId);
            Reg(DataModel.SseServiceMethodId);
            var methodsCode = GenerateMethods();

            var dtos = string.Join(Environment.NewLine, Interface.Methods.Select(m =>
            {
                return @$"
    public class {Interface.ApiName}_{m.Name}
    {{{string.Join(Environment.NewLine, m.Arguments.Select(p =>
            {
                RegRange(p.ParameterType.Namespaces);
                return @$"
        public {p.ParameterType.Name} {p.Name} {{ get; set; }}";
            }))}
    }}
";
            }));

            Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace};

public class {Name}(
    {DataModel.FabricClient} FabricClient,
    {DataModel.UserId}? UserId = null,
    {DataModel.SessionId}? SessionId = null)
    : {Interface}
{{
    private readonly {DataModel.SseServiceId} ServiceId = new {DataModel.SseServiceId}(""{Interface}"");
{methodsCode}
{dtos}
}}
";
        }

        private string GenerateMethods()
        {
            var code = string.Empty;

            var first = true;
            foreach (var method in Interface.Methods)
            {
                if (first)
                    first = false;
                else
                    code += Environment.NewLine;

                var hasFile = method.Arguments.Any(a => a.IsIFormFile);
                if (hasFile)
                    throw new Exception(
                        $"`{method.ResponseType.Name} {Interface.Name}.{method.Name}` " +
                        "IFormFile is not supported by SignalR");

                var isTask = method.ResponseType.Name != "Void" || method.ResponseType.Name != "Task";
                if (isTask == false || method.ResponseType.UnderlayingTypes.Length > 0)
                    throw new Exception(
                        $"`{method.ResponseType.Name} {Interface.Name}.{method.Name}` has a error: " +
                        "Only one-way calls (void / Task) are allowed, there is no return value, use the API directly for this");

                RegRange(method.Arguments.SelectMany(b => b.ParameterType.Namespaces));
                //var methodArguments = method.Arguments
                //        .Select(arg =>
                //        {
                //            RegRange(arg.ParameterType.Namespaces);
                //            return $"{arg.ParameterType.Name} {arg.Name}";
                //        })
                //        .ToList();

                //var methodSignature = string.Join(", ", methodArguments);
                //var methodCall = string.Join(", ", method.Arguments.Select(a => $"{a.Name}"));

                RegRange(method.ResponseType.Namespaces);

                if (method.IsAsync)
                {
                    if (method.ResponseType.Name != "Task")
                        throw new Exception(
                        $"`{method.ResponseType.Name} {Interface.Name}.{method.Name}` has a error: " +
                        "Only one-way calls (void / Task) are allowed, there is no return value, use the API directly for this");

                    code += $@"
    public async Task {method.Name}({string.Join($",{Environment.NewLine}", method.Arguments.Select(a => @$"
            {a.ParameterType.Name} {a.Name}"))})
    {{
        var payload = new {Interface.ApiName}_{method.Name}
        {{{string.Join($",{Environment.NewLine}", method.Arguments.Select(a => @$"
            {a.Name} = {a.Name}"))}
        }};
        var serviceMethodId = new {DataModel.SseServiceMethodId}(""{method.Name}"");
        var json = JsonConvert.SerializeObject(payload);
        FabricClient.Publish(ServiceId, serviceMethodId, UserId, SessionId, json);
    }}";


                    //code += $"    public async Task {method.Name}({methodSignature})" + Environment.NewLine;
                    //code += $"        => await clientProxy.SendAsync(\"{Interface.ApiName}.{method.Name}\", {methodCall});" + Environment.NewLine;
                }
                else
                {
                    if (method.ResponseType.Name != "Void")
                        throw new Exception(
                        $"`{method.ResponseType.Name} {Interface.Name}.{method.Name}` has a error: " +
                        "Only one-way calls (void / Task) are allowed, there is no return value, use the API directly for this");

                    code += $@"
    public void {method.Name}({string.Join($",{Environment.NewLine}", method.Arguments.Select(a => @$"
            {a.ParameterType.Name} {a.Name}"))})
    {{
        var payload = new {Interface.ApiName}_{method.Name}
        {{{string.Join($",{Environment.NewLine}", method.Arguments.Select(a => @$"
            {a.Name} = {a.Name}"))}
        }};
        var serviceMethodId = new {DataModel.SseServiceMethodId}(""{method.Name}"");
        var json = JsonConvert.SerializeObject(payload);
        FabricClient.Publish(ServiceId, serviceMethodId, UserId, SessionId, json);
    }}";

                    //code += $"    #warning For better performance please change the method `{Interface.Name}.{method.Name}({methodSignature})` to be async." + Environment.NewLine;
                    //code += $"    public void {method.Name}({methodSignature})" + Environment.NewLine;
                    //code += $"        => clientProxy.SendAsync(\"{Interface.ApiName}.{method.Name}\", {methodCall}).GetAwaiter().GetResult();" + Environment.NewLine;
                }
            }


            return code;
        }
    }
}