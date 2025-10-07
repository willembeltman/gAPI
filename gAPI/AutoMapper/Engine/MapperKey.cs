using System;

namespace gAPI.AutoMapper.Engine
{
    internal struct MapperKey
    {
        public MapperKey(Type inType, Type outType)
        {
            InType = inType;
            OutType = outType;
        }

        public Type InType { get; }
        public Type OutType { get; }
    }
}