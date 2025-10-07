using System;

namespace gAPI.Attributes
{
    /// <summary>
    /// Markeert een methode als een "Update"-operatie voor een entiteit.
    /// Dit attribuut geeft aan dat de methode bestaande data kan aanpassen.
    /// </summary>
    public class IsUpdateAttribute : Attribute { }
}