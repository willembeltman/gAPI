using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend.Models
{
    public class StateObjectProperty
    {
        public StateObjectProperty(StateObject parent, EntityProperty prop)
        {
            Parent = parent;
            Property = prop;
        }

        public StateObject Parent { get; }
        public EntityProperty Property { get; }
    }
}