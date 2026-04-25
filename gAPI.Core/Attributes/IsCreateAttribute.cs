using System;

namespace gAPI.Attributes;


/// <summary>
/// Markeert een methode als een "Create"-operatie voor een entiteit.
/// Dit attribuut geeft aan dat de methode gebruikt kan worden om een nieuwe instantie van het DTO aan te maken.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class IsCreateAttribute : Attribute { }