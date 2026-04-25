using System;

namespace gAPI.Attributes;

/// <summary>
/// Markeert een methode als een "Delete"-operatie voor een entiteit.
/// Dit attribuut specificeert het type (DTO) waarop deze delete-operatie betrekking heeft.
/// </summary>
/// <param name="deleteType">
/// Het <see cref="Type"/> van het DTO dat verwijderd wordt.
/// </param>
[AttributeUsage(AttributeTargets.Method)]
public class IsDeleteAttribute(Type deleteType) : Attribute
{
    public Type DeleteType { get; } = deleteType;
}