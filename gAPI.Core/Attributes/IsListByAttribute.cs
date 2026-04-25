using System;

namespace gAPI.Attributes;

/// <summary>
/// Markeert een methode als een gefilterde "List"-operatie op basis van een foreign key.
/// Dit attribuut definieert zowel de naam van de foreign key als het type waar deze naar verwijst.
/// </summary>
/// <param name="foreignKeyName">
/// De naam van de property op het DTO die als foreign key fungeert.
/// </param>
/// <param name="foreignType">
/// Het <see cref="Type"/> waar de foreign key naar verwijst.
/// </param>
[AttributeUsage(AttributeTargets.Method)]
public class IsListByAttribute(string foreignKeyName, Type foreignType) : Attribute
{
    public string ForeignKeyName { get; } = foreignKeyName;
    public Type ForeignType { get; } = foreignType;
}