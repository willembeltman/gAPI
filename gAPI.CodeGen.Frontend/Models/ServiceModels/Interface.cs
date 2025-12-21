using gAPI.Attributes;
using gAPI.AutoComponent.Interfaces;
using gAPI.CodeGen.Frontend.Contexts;
using gAPI.CodeGen.Frontend.Helpers;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace gAPI.CodeGen.Frontend.Models.ServiceModels
{
    public class Interface : ISharedReference
    {
        public Interface(ServiceContext dataModel, Type type, IEnumerable<Type> allTypes)
        {
            Type = type;

            Name = Type.Name;
            FullName = Type.FullName;
            Namespace = Type.Namespace;

            ApiName = Name;
            ApiName = ServiceNameHelper.RemoveInterfacePrefix(ApiName);


            var apiNameAttr = type
                .GetCustomAttribute<ApiNameAttribute>();
            if (apiNameAttr != null)
            {
                ApiName = apiNameAttr.Name ?? ApiName;
            }
            ApiName = ServiceNameHelper.RemoveServiceName(ApiName);

            IsAuthorized = type
                .GetCustomAttribute<IsAuthorizedAttribute>() != null;

            IsHidden = type
                .GetCustomAttribute<IsHiddenAttribute>() != null;

            Methods = Type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<IsHiddenAttribute>() == null)
                .Select(methodInfo => new InterfaceMethod(dataModel, this, methodInfo))
                .ToArray();

            Clients = allTypes
                .Where(t =>
                    t.IsClass &&
                    t.GetInterface(type.FullName!) != null
                )
                .Select(a => new Client(this, a))
                .ToArray();

            //Client = allTypes
            //    .Where(a =>
            //        a.TypeKind == TypeKind.Class &&
            //        a.Interfaces.Any(@interface => @interface.ToDisplayString() == type.ToDisplayString()))
            //    .Select(a => new Client(this, a))
            //    .SingleOrDefault();
        }

        public Type Type { get; }
        public string Name { get; }
        public string? FullName { get; }
        public string? Namespace { get; }
        public string ApiName { get; }
        public bool IsAuthorized { get; }
        public bool IsHidden { get; }
        public InterfaceMethod[] Methods { get; }
        public Client[] Clients { get; }
        public Client? Client => Clients.FirstOrDefault();
    }
}