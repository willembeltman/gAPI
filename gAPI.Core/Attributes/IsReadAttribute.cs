using System;

namespace gAPI.Attributes;

/// <summary>
/// Markeert een methode als een "Read"-operatie voor een entiteit.
/// Dit attribuut identificeert dat de methode een enkele instantie van het DTO ophaalt op basis van een uniek ID.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class IsReadAttribute : Attribute { }