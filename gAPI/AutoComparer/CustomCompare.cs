namespace gAPI.AutoComparer;

//public abstract class CustomCompare<T>
//{
//    public abstract Task<bool> AfterComparing(T input, T output);
//}
public abstract class CustomCompare<TIn, TOut>
{
    public abstract bool IsEqual(TIn input, TOut output);
}