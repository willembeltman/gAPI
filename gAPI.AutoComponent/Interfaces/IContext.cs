namespace gAPI.AutoComponent.Interfaces;

public interface IContext
{
    ICrudType[] Cruds { get; }
    IPageIndex[] PageIndexes { get; }
    IPage[] RootPages { get; }
    ISharedReferences SharedReferences { get; }
}
