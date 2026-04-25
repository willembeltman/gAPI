namespace gAPI.AutoPage.Interfaces;

public interface IContext
{
    ICrudlType[] Crudls { get; }
    IPageIndex[] PageIndexes { get; }
    IPage[] RootPages { get; }
    ISharedReferences SharedReferences { get; }
    ISharedReference ListDataSource { get; }
}
