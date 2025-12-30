using gAPI.AutoApi.Helpers;
using gAPI.AutoApi.Models;
using System.Linq;

namespace gAPI.AutoApi.Generators
{
    internal class ControllerGenerator : BaseGenerator
    {
        internal ControllerGenerator(ServiceContext dataModel, Service service)
        {
            DataModel = dataModel;
            Service = service;

            Directory = dataModel.Config.Controllers_Destination.Directory;
            Namespace = dataModel.Config.Controllers_Destination.Namespace;

            Name = service.Interface.ApiName + "Controller";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public Service Service { get; }
        public string HelperCode { get; private set; }

        public void GenerateCode()
        {
            Reg(Service.Interface);
            Reg("Microsoft.AspNetCore.Mvc");

            var methodCode = GenerateMethods();

            UnReg("System.Threading.Tasks");

            Code = GetNamespacesCode();
            Code += $"#nullable enable" + Environment.NewLine;
            Code += $"namespace {Namespace};" + Environment.NewLine;
            Code += $"" + Environment.NewLine;
            Code += $"[ApiController]" + Environment.NewLine;
            Code += $"[Route(\"{Service.Interface.ApiName.ToLower()}\")]" + Environment.NewLine;
            Code += $"public class {Name}(" + Environment.NewLine;
            Code += $"    {Service.Interface.Name} {Service.Name.ToLower()}," + Environment.NewLine;
            Code += $"    gAPI.Interfaces.IServerAuthenticationService serverAuthenticationService)" + Environment.NewLine;
            Code += $"    : ControllerBase" + Environment.NewLine;
            Code += $"{{" + Environment.NewLine;
            Code += methodCode;
            Code += $"}}";
        }

        private string GenerateMethods()
        {
            var code = string.Empty;

            var first = true;
            foreach (var method in Service.Interface.Methods)
            {
                if (first)
                    first = false;
                else
                    code += Environment.NewLine;

                var attr = "";
                if (method.IsCreate)
                {
                    Reg("Microsoft.AspNetCore.Mvc");
                    attr = $"[HttpPost(\"{method.Name.ToLower()}\")]";
                }
                else if (method.IsRead)
                {
                    Reg("Microsoft.AspNetCore.Mvc");
                    var routeArg = method.Arguments.First();
                    attr = $"[HttpGet(\"{method.Name.ToLower()}/{{{routeArg.Name.ToCamelCase()}}}\")]";
                }
                else if (method.IsUpdate)
                {
                    Reg("Microsoft.AspNetCore.Mvc");
                    attr = $"[HttpPut(\"{method.Name.ToLower()}\")]";
                }
                else if (method.IsDelete)
                {
                    Reg("Microsoft.AspNetCore.Mvc");
                    var routeArg = method.Arguments.First();
                    attr = $"[HttpDelete(\"{method.Name.ToLower()}/{{{routeArg.Name.ToCamelCase()}}}\")]";
                }
                else if (method.IsList)
                {
                    Reg("Microsoft.AspNetCore.Mvc");
                    attr = $"[HttpGet(\"{method.Name.ToLower()}\")]";
                }
                else if (method.IsListBy || method.IsListNotBy)
                {
                    Reg("Microsoft.AspNetCore.Mvc");
                    var routeArg = method.Arguments.First();
                    attr = $"[HttpGet(\"{method.Name.ToLower()}/{{{routeArg.Name.ToCamelCase()}}}\")]";
                }
                else if (method.IsUpdate)
                {
                    Reg("Microsoft.AspNetCore.Mvc");
                    attr = $"[HttpPut(\"{method.Name.ToLower()}\")]";
                }
                else if (method.IsFileUpdate)
                {
                    Reg("Microsoft.AspNetCore.Mvc");
                    var routeArg = method.Arguments.First();
                    attr = $"[HttpPut(\"{method.Name.ToLower()}/{{{routeArg.Name.ToCamelCase()}}}\")]";
                }
                else if (method.IsFileDelete)
                {
                    Reg("Microsoft.AspNetCore.Mvc");
                    var routeArg = method.Arguments.First();
                    attr = $"[HttpDelete(\"{method.Name.ToLower()}/{{{routeArg.Name.ToCamelCase()}}}\")]";
                }
                else
                {
                    if (method.Arguments.Length == 0)
                    {
                        Reg("Microsoft.AspNetCore.Mvc");
                        attr = $"[HttpGet(\"{method.Name.ToLower()}\")]";
                    }
                    else
                    {
                        Reg("Microsoft.AspNetCore.Mvc");
                        attr = $"[HttpPost(\"{method.Name.ToLower()}\")]";
                    }
                }

                var hasFile = method.Arguments.Any(a => a.IsIFormFile);

                RegRange(method.Arguments.SelectMany(b => b.ParameterType.Namespaces));
                var methodArguments = method.Arguments
                        .Select(arg =>
                        {
                            RegRange(arg.ParameterType.Namespaces);
                            if (arg.IsIFormFile)
                            {
                                return $"{arg.ParameterType.Name} {arg.Name}";
                            }
                            else if (method.IsFileUpdate || method.IsFileDelete)
                            {
                                return $"[FromRoute] {arg.ParameterType.Name} {arg.Name}";
                            }
                            else if (method.IsList || method.IsListBy || method.IsListNotBy)
                            {
                                if ((arg.Name == "skip" || arg.Name == "take" || arg.Name == "orderby"))
                                {
                                    return $"[FromQuery] {arg.ParameterType.Name} {arg.Name}";
                                }
                                else
                                {
                                    return $"[FromRoute] {arg.ParameterType.Name} {arg.Name}";
                                }
                            }
                            else if (method.IsRead || method.IsDelete)
                            {
                                return $"[FromRoute] {arg.ParameterType.Name} {arg.Name}";
                            }
                            else
                            {
                                return $"[FromForm] {arg.ParameterType.Name} {arg.Name}";
                            }
                        })
                        .ToList();

                methodArguments.Add("[FromHeader] Guid sessionId");

                var methodSignature = string.Join(", ", methodArguments);
                var methodCall = string.Join(", ", method.Arguments
                    .Select(a => $"{a.Name}"));

                RegRange(method.ResponseType.Namespaces);

                code += $"    {attr}" + Environment.NewLine;
                if (method.IsAsync)
                {
                    if (method.ResponseType.UnderlayingTypes.Length == 0)
                    {
                        code += $"    public async Task<ActionResult> {method.Name}({methodSignature})" + Environment.NewLine;
                    }
                    else
                    {
                        code += $"    public async Task<ActionResult<{method.ResponseType.UnderlayingTypes[0].Name}>> {method.Name}({methodSignature})" + Environment.NewLine;
                    }
                }
                else
                {
                    if (method.ResponseType.Name == "void")
                    {
                        code += $"    public async Task<ActionResult> {method.Name}({methodSignature})" + Environment.NewLine;
                    }
                    else
                    {
                        code += $"    public async Task<ActionResult<{method.ResponseType.Name}>> {method.Name}({methodSignature})" + Environment.NewLine;
                    }
                }

                code += $"    {{" + Environment.NewLine;
                code += $"        if (!ModelState.IsValid) return BadRequest(ModelState);" + Environment.NewLine;

                if (Service.Interface.IsAuthorized || method.IsAuthorize)
                    code += $"        if (!await serverAuthenticationService.InitializeAsync(sessionId)) return Unauthorized();" + Environment.NewLine;
                else
                    code += $"        await serverAuthenticationService.InitializeAsync(sessionId);" + Environment.NewLine;

                if (method.IsAsync)
                {
                    if (method.ResponseType.UnderlayingTypes.Length == 0)
                    {
                        code += $"        await {Service.Name.ToLower()}.{method.Name}({methodCall});" + Environment.NewLine;
                        code += $"        return Ok();" + Environment.NewLine;
                    }
                    else
                    {
                        code += $"        return Ok(await {Service.Name.ToLower()}.{method.Name}({methodCall}));" + Environment.NewLine;
                    }
                }
                else
                {
                    if (method.ResponseType.Name == "void")
                    {
                        code += $"        {Service.Name.ToLower()}.{method.Name}({methodCall});" + Environment.NewLine;
                        code += $"        return Ok();" + Environment.NewLine;
                    }
                    else
                    {
                        code += $"        return Ok({Service.Name.ToLower()}.{method.Name}({methodCall}));" + Environment.NewLine;
                    }
                }

                code += $"    }}" + Environment.NewLine;
            }

            return code;
        }
    }
}