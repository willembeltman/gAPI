
using gAPI.CodeGen.Frontend.Configs;
using gAPI.CodeGen.Frontend.Models.ServiceModels;
using System.Reflection;
using System.Text;

namespace gAPI.CodeGen.Frontend.Generators.Razor.Pages
{
    public class PageGenerator
    {
        public PageGenerator(InterfaceMethod interfaceMethod, FrontendConfig clientConfig, ImportsGenerator imports)
        {
            ClientConfig = clientConfig;
            Imports = imports;
            Interface = interfaceMethod.Interface;
            Client = Interface.Client;
            Method = interfaceMethod;
            Arguments = Method.Arguments;
            ResponseType = interfaceMethod.ResponseRealType;
        }

        public FrontendConfig ClientConfig { get; }
        public ImportsGenerator Imports { get; }
        public Interface Interface { get; }
        public Client? Client { get; }
        public InterfaceMethod Method { get; }
        public InterfaceMethodArgument[] Arguments { get; }
        public Type ResponseType { get; }

        public string Namespace => $"{ClientConfig.PagesNamespace}.{Interface.Name}";

        public string FileName => $"{Method.Name}.razor";

        public string GenerateCode()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"@page \"/{Interface.Name.ToLower()}/{Method.Name.ToLower()}\"");
            sb.AppendLine();

            // Parameters section
            sb.AppendLine("@code {");

            // Generate properties for each parameter, supporting primitives and complex DTOs
            foreach (var arg in Arguments)
            {
                string typeName = GetTypeName(arg.ParameterInfo.ParameterType);
                string propName = arg.Name!;

                sb.AppendLine($"    private {typeName} {propName} {{ get; set; }} = default!;");
            }

            // Property for response
            if (ResponseType != null && ResponseType != typeof(void))
            {
                string respTypeName = GetTypeName(ResponseType);
                sb.AppendLine($"    private {respTypeName}? Response {{ get; set; }}");
            }

            // Method to call service method
            sb.AppendLine();
            sb.AppendLine("    private async Task CallServiceMethod()");
            sb.AppendLine("    {");
            sb.Append("        ");

            if (ResponseType != null && ResponseType != typeof(void))
                sb.Append("Response = await ");

            // Compose service call with arguments
            sb.Append($"{Client!.Name}.{Method.Name}(");
            sb.Append(string.Join(", ", Arguments.Select(a => a.Name)));
            sb.AppendLine(");");

            sb.AppendLine("    }");

            sb.AppendLine("}");

            // Form UI generation
            sb.AppendLine();
            sb.AppendLine("<h3>Parameters</h3>");
            sb.AppendLine("<EditForm OnValidSubmit=\"CallServiceMethod\">");

            foreach (var arg in Arguments)
            {
                sb.AppendLine(GenerateInputForType(arg.Name!, arg.ParameterInfo.ParameterType));
            }

            sb.AppendLine("    <button type=\"submit\" class=\"btn btn-primary\">Call</button>");
            sb.AppendLine("</EditForm>");

            // Response UI
            if (ResponseType != null && ResponseType != typeof(void))
            {
                sb.AppendLine();
                sb.AppendLine("<h3>Response</h3>");
                sb.AppendLine(GenerateDisplayForType("Response", ResponseType));
            }

            return sb.ToString();
        }

        private string GenerateInputForType(string name, Type type)
        {
            // For simplicity: only support primitive/string/enum types + one-level DTO properties
            if (type == typeof(string))
                return $@"    <div class=""mb-3"">
        <label>{name}</label>
        <InputText @bind-Value=""{name}"" class=""form-control"" />
    </div>";

            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(double) || type == typeof(decimal) || type == typeof(float))
                return $@"    <div class=""mb-3"">
        <label>{name}</label>
        <InputNumber @bind-Value=""{name}"" class=""form-control"" />
    </div>";

            if (type == typeof(bool))
                return $@"    <div class=""mb-3 form-check"">
        <InputCheckbox @bind-Value=""{name}"" class=""form-check-input"" id=""{name}"" />
        <label class=""form-check-label"" for=""{name}"">{name}</label>
    </div>";

            if (type.IsEnum)
                return $@"    <div class=""mb-3"">
        <label>{name}</label>
        <InputSelect @bind-Value=""{name}"" class=""form-select"">
            @foreach (var val in Enum.GetValues(typeof({type.Name})).Cast<{type.Name}>())
            {{
                <option value=""@val"">@val</option>
            }}
        </InputSelect>
    </div>";

            // For complex types: flatten properties one level (could recurse)
            var sb = new StringBuilder();
            sb.AppendLine($@"    <fieldset class=""mb-3""><legend>{name}</legend>");
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                string propName = $"{name}.{prop.Name}";
                sb.AppendLine(GenerateInputForType(propName, prop.PropertyType));
            }
            sb.AppendLine("</fieldset>");
            return sb.ToString();
        }

        private string GenerateDisplayForType(string name, Type type)
        {
            // Display all public properties
            var sb = new StringBuilder();
            sb.AppendLine("<dl>");
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                sb.AppendLine($"  <dt>{prop.Name}</dt>");
                sb.AppendLine($"  <dd>@{name}.{prop.Name}</dd>");
            }
            sb.AppendLine("</dl>");
            return sb.ToString();
        }

        private string GetTypeName(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return $"{GetTypeName(type.GetGenericArguments()[0])}?";
            return type.Name;
        }
    }
}
//using gAPI.CodeGen.Frontend.Configs;
//using gAPI.CodeGen.Frontend.Models.Interfaces;
//using System.Reflection;

//namespace gAPI.CodeGen.Frontend.Generators.Razor.Pages
//{
//    public class PageGenerator
//    {
//        public PageGenerator(FrontendConfig clientConfig, InterfaceMethod interfaceMethod)
//        {
//            ClientConfig = clientConfig;
//            Interface = interfaceMethod.Interface;
//            Client = Interface.Client; // Imitates the Service
//            Method = interfaceMethod;
//            Arguments = Method.Arguments;
//            ResponseType = interfaceMethod.ResponseRealType; 
//        }

//        public FrontendConfig ClientConfig { get; }
//        public InterfaceMethod Method { get; }
//        public Interface Interface { get; }
//        public Client? Client { get; }
//        public InterfaceMethodArgument[] Arguments { get; }
//        public Type ResponseType { get; }

//        internal void GenerateCode()
//        {
//            foreach (var arg in Arguments)
//            {
//                ParameterInfo info = arg.ParameterInfo;
//                string name = arg.Name;
//            }
//            throw new NotImplementedException();
//        }
//    }
//}