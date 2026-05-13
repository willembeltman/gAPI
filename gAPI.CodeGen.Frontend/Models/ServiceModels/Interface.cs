using gAPI.AutoComponent.Interfaces;
using gAPI.CodeGen.Frontend.Helpers;
using gAPI.Core.Attributes;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace gAPI.CodeGen.Frontend.Models.ServiceModels;

public class Interface : ISharedReference
{
    public Interface(ServiceContext dataModel, Type type, IEnumerable<Type> allTypes)
    {
        Type = type;

        Name = Type.Name;
        FullName = Type.FullName;
        Namespace = Type.Namespace;

        Title = Name;
        Title = ServiceNameHelper.RemoveInterfacePrefix(Title);
        var generateApiAttr = type
            .GetCustomAttribute<GenerateApiAttribute>();
        if (generateApiAttr != null)
        {
            Title = generateApiAttr.Name ?? Title;
        }
        Title = ServiceNameHelper.RemoveServiceName(Title);

        var nameAttr = type
            .GetCustomAttribute<TitleAttribute>();
        if (nameAttr != null)
        {
            Name = nameAttr.Name ?? Name;
        }

        IsAuthorized = type
            .GetCustomAttribute<IsAuthorizedAttribute>() != null;

        IsNotAuthorized = type
            .GetCustomAttribute<IsNotAuthorizedAttribute>() != null;

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
    public string Title { get; }
    public bool IsAuthorized { get; }
    public bool IsNotAuthorized { get; }
    public bool IsHidden { get; }
    public InterfaceMethod[] Methods { get; }
    public Client[] Clients { get; }
    public Client? Client => Clients.FirstOrDefault();
}