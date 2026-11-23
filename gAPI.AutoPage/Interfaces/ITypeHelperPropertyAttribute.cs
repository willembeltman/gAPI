namespace gAPI.AutoPage.Interfaces
{
    public interface ITypeHelperPropertyAttribute : ISharedReference
    {
        ITypeHelper Type { get; }

        string ToNameString();
        string ToString();
    }
}