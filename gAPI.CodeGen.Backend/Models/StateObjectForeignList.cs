using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend.Models
{
    public class StateObjectForeignList
    {
        public StateObjectForeignList(StateObject newStateObject, EntityProperty prop)
        {
            NewStateObject = newStateObject;
            Prop = prop;
        }

        public StateObject NewStateObject { get; }
        public EntityProperty Prop { get; }
    }
}