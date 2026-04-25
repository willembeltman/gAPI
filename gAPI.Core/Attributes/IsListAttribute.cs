using System;

namespace gAPI.Attributes;

/// <summary>
/// Markeert een methode als een "List"-operatie voor een entiteit.
/// Dit attribuut geeft aan dat de methode een verzameling van entiteiten retourneert.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class IsListAttribute : Attribute { }