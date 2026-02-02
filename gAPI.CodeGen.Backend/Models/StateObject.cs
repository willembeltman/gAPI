using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend.Models;

public class StateObject
{
    public StateObject(Entity entity, List<StateObject> allStateObjects, StateObject? parentObject = null, EntityProperty? entityProperty = null, Entity[]? alreadyRead = null)
    {
        Parent = parentObject;
        PropertyOfParent = entityProperty;
        Entity = entity;
        alreadyRead ??= [entity];

        ForeignProperties = [];
        KeyProperties = [];
        Properties = [];
        ForeignLists = [];

        var list = entity.Properties
            .Where(a => a.IsState || a.IsKey)
            .OrderByDescending(a => a.IsKey ? 1 : 0);
        foreach (var prop in list)
        {
            if (prop.NavigationListProperty != null)
            {
                var subEntity = prop.NavigationListProperty.ParentEntity;
                if (alreadyRead.Contains(subEntity))
                    continue;

                // Navigatie state property
                var newStateObject = new StateObject(subEntity, allStateObjects, this, prop, [.. alreadyRead, subEntity]);
                allStateObjects.Add(newStateObject);
                ForeignLists.Add(newStateObject);
            }
            else if (prop.NavigationDbSet?.Entity != null)
            {
                var subEntity = prop.NavigationDbSet.Entity;
                if (alreadyRead.Contains(subEntity))
                    continue;

                // Navigatie state property
                var newStateObject = new StateObject(subEntity, allStateObjects, this, prop, [.. alreadyRead, subEntity]);
                allStateObjects.Add(newStateObject);
                ForeignProperties.Add(newStateObject);
            }
            else
            {
                if (prop.IsKey)
                {
                    // Normale property
                    KeyProperties.Add(new StateObjectProperty(this, prop));
                }
                else
                {
                    // Normale property
                    Properties.Add(new StateObjectProperty(this, prop));
                }
            }
        }
    }

    public StateObject? Parent { get; }
    public EntityProperty? PropertyOfParent { get; }
    public Entity Entity { get; }
    public List<StateObjectProperty> KeyProperties { get; }
    public List<StateObject> ForeignProperties { get; }
    public List<StateObjectProperty> Properties { get; }
    public List<StateObject> ForeignLists { get; }
    public string ClassName => Entity.FullName;
}