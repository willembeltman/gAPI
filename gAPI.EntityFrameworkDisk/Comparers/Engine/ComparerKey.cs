namespace gAPI.EntityFrameworkDisk.Comparers.Engine;

internal struct ComparerKey
{
    public ComparerKey(Type inType, Type outType)
    {
        InType = inType;
        OutType = outType;
    }

    public Type InType { get; }
    public Type OutType { get; }
}
