using System;

namespace gAPI.AutoComparer;

public class ComparerInstance<TIn, TOut>
{
    public ComparerInstance(
        string code,
        Func<TIn, TOut, bool> isDirtyDelegate)
    {
        Code = code;
        IsEqualDelegate = isDirtyDelegate;
    }

    public string Code { get; }
    private Func<TIn, TOut, bool> IsEqualDelegate;

    public bool IsEqual(TIn source, TOut destination)
        => IsEqualDelegate(source, destination);
}
