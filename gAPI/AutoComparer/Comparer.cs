using gAPI.AutoComparer.Engine;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;

namespace gAPI.AutoComparer;

public static class Comparer
{
    private static readonly ConcurrentDictionary<Engine.ComparerKey, object> DifferentComparerFactorys =
        new ConcurrentDictionary<ComparerKey, object>();

    public static IServiceProvider? ServiceProvider { get; private set; }

    public static void SetServiceProvider(IServiceProvider provider)
        => ServiceProvider = provider;

    public static bool IsEqualTo<TIn, TOut>(this TIn source, TOut destination)
        => IsEqual(source, destination);

    public static bool IsEqual<TIn, TOut>(TIn source, TOut destination)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));

        var mapperInstance = GetInstance<TIn, TOut>();

        var result = false;
        IServiceScope? serviceScope = null;
        try
        {
            CustomCompare<TIn, TOut>? customComparer = null;
            if (ServiceProvider != null)
            {
                serviceScope = ServiceProvider.CreateScope();
                customComparer = serviceScope?.ServiceProvider
                    .GetService<CustomCompare<TIn, TOut>>();
            }

            if (serviceScope != null && customComparer != null)
            {
                result = customComparer.IsEqual(source, destination);
            }
            else
            {
                result = mapperInstance.IsEqual(source, destination);
            }
        }
        finally
        {
            serviceScope?.Dispose();
        }
        return result;
    }

    public static ComparerInstance<TIn, TOut> GetInstance<TIn, TOut>()
    {
        var key = new Engine.ComparerKey(typeof(TIn), typeof(TOut));
        if (!DifferentComparerFactorys.TryGetValue(key, out var entityFactory))
        {
            entityFactory = Engine.ComparerFactory<TIn, TOut>.CreateInstance();
            DifferentComparerFactorys[key] = entityFactory;
        }
        return (ComparerInstance<TIn, TOut>)entityFactory;
    }
}
