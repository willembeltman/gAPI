using System;

namespace gAPI.Attributes
{
    /// <summary>
    /// Markeert een methode als een "Read"-operatie voor een entiteit.
    /// Dit attribuut identificeert dat de methode een enkele instantie van het DTO ophaalt op basis van een uniek ID.
    /// </summary>
    public class IsReadAttribute : Attribute { }
}