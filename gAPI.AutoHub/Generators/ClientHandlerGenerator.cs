using gAPI.AutoHub.Models;
using System.Linq;
using System;

namespace gAPI.AutoHub.Generators
{
    internal class ClientHandlerGenerator : BaseGenerator
    {
        internal ClientHandlerGenerator(ServiceContext dataModel, Interface @interface)
        {
            DataModel = dataModel;
            Interface = @interface;

            Directory = dataModel.Config.HubServices_Destination.Directory;
            Namespace = dataModel.Config.HubServices_Destination.Namespace;

            Name = Interface.ApiName + "Handler";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public Interface Interface { get; }
        public string HelperCode { get; private set; }

        public void GenerateCode()
        {
            Reg(Interface);
            Reg("Microsoft.AspNetCore.SignalR");
            var methodsCode = GenerateMethods();
            Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace};

public class {Name}(
    IClientProxy clientProxy)
    : {Interface.Name}
{{
{methodsCode}}}
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
                    throw new Exception("IFormFile is not supported by SignalR");

                var isTask = method.ResponseType.Name != "void" || method.ResponseType.Name != "Task";
                if (isTask == false || method.ResponseType.UnderlayingTypes.Length > 0)
                    throw new Exception("Only one-way calls are allowed, there is no return value, use the API directly for this");

                RegRange(method.Arguments.SelectMany(b => b.ParameterType.Namespaces));
                var methodArguments = method.Arguments
                        .Select(arg =>
                        {
                            RegRange(arg.ParameterType.Namespaces);
                            return $"{arg.ParameterType.Name} {arg.Name}";
                        })
                        .ToList();

                var methodSignature = string.Join(", ", methodArguments);
                var methodCall = string.Join(", ", method.Arguments.Select(a => $"{a.Name}"));

                RegRange(method.ResponseType.Namespaces);

                if (method.IsAsync)
                {
                    code += $"    public async Task {method.Name}({methodSignature})" + Environment.NewLine;
                    code += $"        => await clientProxy.SendAsync(\"{Interface.ApiName}.{method.Name}\", {methodCall});" + Environment.NewLine;
                }
                else
                {
                    code += $"    #warning For better performance please change the method `{Interface.Name}.{method.Name}({methodSignature})` to be async." + Environment.NewLine;
                    code += $"    public void {method.Name}({methodSignature})" + Environment.NewLine;
                    code += $"        => clientProxy.SendAsync(\"{Interface.ApiName}.{method.Name}\", {methodCall}).Result;" + Environment.NewLine;
                }
            }

            return code;
        }
    }
}