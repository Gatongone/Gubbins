namespace Gubbins.Entities;
/// <summary>
/// Component filter, using a Bloom filter as a quick pre-filter for containing sets.
/// </summary>
public readonly struct ComponentFilter
{
    internal readonly Type[]      Includes;
    internal readonly Type[]      Excludes;

    private ComponentFilter(Type[]? includes, Type[]? excludes)
    {
        Includes = includes ?? [];
        Excludes = excludes ?? [];
    }

    public ComponentFilter() : this([], []) { }

    /// <summary> Returns a new filter that includes the specified component types. This method creates a new filter instance with the added component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include(params Type[] types)
    {
        return new ComponentFilter(Includes.Concat(types).ToArray(), Excludes);
    }

    /// <summary>Returns a new filter that includes the specified component type. This method creates a new filter instance with the added component type, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include<T>() where T : unmanaged
        => Include(typeof(T));

    /// <summary>Returns a new filter that includes the specified component types. This method creates a new filter instance with the added component type, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include<TI1, TI2>() where TI1 : unmanaged where TI2 : unmanaged
        => Include(typeof(TI1), typeof(TI2));

    /// <summary>Returns a new filter that includes the specified component types. This method creates a new filter instance with the added component type, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include<TI1, TI2, TI3>() where TI1 : unmanaged where TI2 : unmanaged where TI3 : unmanaged
        => Include(typeof(TI1), typeof(TI2), typeof(TI3));

    /// <summary>Returns a new filter that includes the specified component types. This method creates a new filter instance with the added component type, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include<TI1, TI2, TI3, TI4>() where TI1 : unmanaged where TI2 : unmanaged where TI3 : unmanaged where TI4 : unmanaged
        => Include(typeof(TI1), typeof(TI2), typeof(TI3), typeof(TI4));

    /// <summary>Returns a new filter that includes the specified component types. This method creates a new filter instance with the added component type, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include<TI1, TI2, TI3, TI4, TI5>() where TI1 : unmanaged where TI2 : unmanaged where TI3 : unmanaged where TI4 : unmanaged where TI5 : unmanaged
        => Include(typeof(TI1), typeof(TI2), typeof(TI3), typeof(TI4), typeof(TI5));

    /// <summary>Returns a new filter that includes the specified component types. This method creates a new filter instance with the added component type, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include<TI1, TI2, TI3, TI4, TI5, TI6>() where TI1 : unmanaged where TI2 : unmanaged where TI3 : unmanaged where TI4 : unmanaged where TI5 : unmanaged where TI6 : unmanaged
        => Include(typeof(TI1), typeof(TI2), typeof(TI3), typeof(TI4), typeof(TI5), typeof(TI6));

    /// <summary>Returns a new filter that includes the specified component types. This method creates a new filter instance with the added component type, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include<TI1, TI2, TI3, TI4, TI5, TI6, TI7>() where TI1 : unmanaged where TI2 : unmanaged where TI3 : unmanaged where TI4 : unmanaged where TI5 : unmanaged where TI6 : unmanaged where TI7 : unmanaged
        => Include(typeof(TI1), typeof(TI2), typeof(TI3), typeof(TI4), typeof(TI5), typeof(TI6), typeof(TI7));

    /// <summary>Returns a new filter that includes the specified component types. This method creates a new filter instance with the added component type, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include<TI1, TI2, TI3, TI4, TI5, TI6, TI7, TI8>() where TI1 : unmanaged where TI2 : unmanaged where TI3 : unmanaged where TI4 : unmanaged where TI5 : unmanaged where TI6 : unmanaged where TI7 : unmanaged where TI8 : unmanaged
        => Include(typeof(TI1), typeof(TI2), typeof(TI3), typeof(TI4), typeof(TI5), typeof(TI6), typeof(TI7), typeof(TI8));

    /// <summary>Returns a new filter that includes the specified component types. This method creates a new filter instance with the added component type, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include<TI1, TI2, TI3, TI4, TI5, TI6, TI7, TI8, TI9>() where TI1 : unmanaged where TI2 : unmanaged where TI3 : unmanaged where TI4 : unmanaged where TI5 : unmanaged where TI6 : unmanaged where TI7 : unmanaged where TI8 : unmanaged where TI9 : unmanaged
        => Include(typeof(TI1), typeof(TI2), typeof(TI3), typeof(TI4), typeof(TI5), typeof(TI6), typeof(TI7), typeof(TI8), typeof(TI9));

    /// <summary>Returns a new filter that includes the specified component types. This method creates a new filter instance with the added component type, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include<TI1, TI2, TI3, TI4, TI5, TI6, TI7, TI8, TI9, TI10>() where TI1 : unmanaged where TI2 : unmanaged where TI3 : unmanaged where TI4 : unmanaged where TI5 : unmanaged where TI6 : unmanaged where TI7 : unmanaged where TI8 : unmanaged where TI9 : unmanaged where TI10 : unmanaged
        => Include(typeof(TI1), typeof(TI2), typeof(TI3), typeof(TI4), typeof(TI5), typeof(TI6), typeof(TI7), typeof(TI8), typeof(TI9), typeof(TI10));

    /// <summary>Returns a new filter that includes the specified component types. This method creates a new filter instance with the added component type, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include<TI1, TI2, TI3, TI4, TI5, TI6, TI7, TI8, TI9, TI10, TI11>() where TI1 : unmanaged where TI2 : unmanaged where TI3 : unmanaged where TI4 : unmanaged where TI5 : unmanaged where TI6 : unmanaged where TI7 : unmanaged where TI8 : unmanaged where TI9 : unmanaged where TI10 : unmanaged where TI11 : unmanaged
        => Include(typeof(TI1), typeof(TI2), typeof(TI3), typeof(TI4), typeof(TI5), typeof(TI6), typeof(TI7), typeof(TI8), typeof(TI9), typeof(TI10), typeof(TI11));

    /// <summary>Returns a new filter that includes the specified component types. This method creates a new filter instance with the added component type, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include<TI1, TI2, TI3, TI4, TI5, TI6, TI7, TI8, TI9, TI10, TI11, TI12>() where TI1 : unmanaged where TI2 : unmanaged where TI3 : unmanaged where TI4 : unmanaged where TI5 : unmanaged where TI6 : unmanaged where TI7 : unmanaged where TI8 : unmanaged where TI9 : unmanaged where TI10 : unmanaged where TI11 : unmanaged where TI12 : unmanaged
        => Include(typeof(TI1), typeof(TI2), typeof(TI3), typeof(TI4), typeof(TI5), typeof(TI6), typeof(TI7), typeof(TI8), typeof(TI9), typeof(TI10), typeof(TI11), typeof(TI12));

    /// <summary>Returns a new filter that includes the specified component types. This method creates a new filter instance with the added component type, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include<TI1, TI2, TI3, TI4, TI5, TI6, TI7, TI8, TI9, TI10, TI11, TI12, TI13>() where TI1 : unmanaged where TI2 : unmanaged where TI3 : unmanaged where TI4 : unmanaged where TI5 : unmanaged where TI6 : unmanaged where TI7 : unmanaged where TI8 : unmanaged where TI9 : unmanaged where TI10 : unmanaged where TI11 : unmanaged where TI12 : unmanaged where TI13 : unmanaged
        => Include(typeof(TI1), typeof(TI2), typeof(TI3), typeof(TI4), typeof(TI5), typeof(TI6), typeof(TI7), typeof(TI8), typeof(TI9), typeof(TI10), typeof(TI11), typeof(TI12), typeof(TI13));

    /// <summary>Returns a new filter that includes the specified component types. This method creates a new filter instance with the added component type, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include<TI1, TI2, TI3, TI4, TI5, TI6, TI7, TI8, TI9, TI10, TI11, TI12, TI13, TI14>() where TI1 : unmanaged where TI2 : unmanaged where TI3 : unmanaged where TI4 : unmanaged where TI5 : unmanaged where TI6 : unmanaged where TI7 : unmanaged where TI8 : unmanaged where TI9 : unmanaged where TI10 : unmanaged where TI11 : unmanaged where TI12 : unmanaged where TI13 : unmanaged where TI14 : unmanaged
        => Include(typeof(TI1), typeof(TI2), typeof(TI3), typeof(TI4), typeof(TI5), typeof(TI6), typeof(TI7), typeof(TI8), typeof(TI9), typeof(TI10), typeof(TI11), typeof(TI12), typeof(TI13), typeof(TI14));

    /// <summary>Returns a new filter that includes the specified component types. This method creates a new filter instance with the added component type, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include<TI1, TI2, TI3, TI4, TI5, TI6, TI7, TI8, TI9, TI10, TI11, TI12, TI13, TI14, TI15>() where TI1 : unmanaged where TI2 : unmanaged where TI3 : unmanaged where TI4 : unmanaged where TI5 : unmanaged where TI6 : unmanaged where TI7 : unmanaged where TI8 : unmanaged where TI9 : unmanaged where TI10 : unmanaged where TI11 : unmanaged where TI12 : unmanaged where TI13 : unmanaged where TI14 : unmanaged where TI15 : unmanaged
        => Include(typeof(TI1), typeof(TI2), typeof(TI3), typeof(TI4), typeof(TI5), typeof(TI6), typeof(TI7), typeof(TI8), typeof(TI9), typeof(TI10), typeof(TI11), typeof(TI12), typeof(TI13), typeof(TI14), typeof(TI15));

    /// <summary>Returns a new filter that includes the specified component types. This method creates a new filter instance with the added component type, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Include<TI1, TI2, TI3, TI4, TI5, TI6, TI7, TI8, TI9, TI10, TI11, TI12, TI13, TI14, TI15, TI16>() where TI1 : unmanaged where TI2 : unmanaged where TI3 : unmanaged where TI4 : unmanaged where TI5 : unmanaged where TI6 : unmanaged where TI7 : unmanaged where TI8 : unmanaged where TI9 : unmanaged where TI10 : unmanaged where TI11 : unmanaged where TI12 : unmanaged where TI13 : unmanaged where TI14 : unmanaged where TI15 : unmanaged where TI16 : unmanaged
        => Include(typeof(TI1), typeof(TI2), typeof(TI3), typeof(TI4), typeof(TI5), typeof(TI6), typeof(TI7), typeof(TI8), typeof(TI9), typeof(TI10), typeof(TI11), typeof(TI12), typeof(TI13), typeof(TI14), typeof(TI15), typeof(TI16));

    /// <summary>Returns a new filter that excludes the specified component types. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude(params Type[] types)
        => new(Includes, Excludes.Concat(types).ToArray());

    /// <summary>Returns a new filter that excludes the specified component type. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude<TE>() where TE : unmanaged
        => Exclude(typeof(TE));

    /// <summary>Returns a new filter that excludes the specified component types. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude<TE1, TE2>() where TE1 : unmanaged where TE2 : unmanaged
        => Exclude(typeof(TE1), typeof(TE2));

    /// <summary>Returns a new filter that excludes the specified component types. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude<TE1, TE2, TE3>() where TE1 : unmanaged where TE2 : unmanaged where TE3 : unmanaged
        => Exclude(typeof(TE1), typeof(TE2), typeof(TE3));

    /// <summary>Returns a new filter that excludes the specified component types. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude<TE1, TE2, TE3, TE4>() where TE1 : unmanaged where TE2 : unmanaged where TE3 : unmanaged where TE4 : unmanaged
        => Exclude(typeof(TE1), typeof(TE2), typeof(TE3), typeof(TE4));

    /// <summary>Returns a new filter that excludes the specified component types. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude<TE1, TE2, TE3, TE4, TE5>() where TE1 : unmanaged where TE2 : unmanaged where TE3 : unmanaged where TE4 : unmanaged where TE5 : unmanaged
        => Exclude(typeof(TE1), typeof(TE2), typeof(TE3), typeof(TE4), typeof(TE5));

    /// <summary>Returns a new filter that excludes the specified component types. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude<TE1, TE2, TE3, TE4, TE5, TE6>() where TE1 : unmanaged where TE2 : unmanaged where TE3 : unmanaged where TE4 : unmanaged where TE5 : unmanaged where TE6 : unmanaged
        => Exclude(typeof(TE1), typeof(TE2), typeof(TE3), typeof(TE4), typeof(TE5), typeof(TE6));

    /// <summary>Returns a new filter that excludes the specified component types. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude<TE1, TE2, TE3, TE4, TE5, TE6, TE7>() where TE1 : unmanaged where TE2 : unmanaged where TE3 : unmanaged where TE4 : unmanaged where TE5 : unmanaged where TE6 : unmanaged where TE7 : unmanaged
        => Exclude(typeof(TE1), typeof(TE2), typeof(TE3), typeof(TE4), typeof(TE5), typeof(TE6), typeof(TE7));

    /// <summary>Returns a new filter that excludes the specified component types. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude<TE1, TE2, TE3, TE4, TE5, TE6, TE7, TE8>() where TE1 : unmanaged where TE2 : unmanaged where TE3 : unmanaged where TE4 : unmanaged where TE5 : unmanaged where TE6 : unmanaged where TE7 : unmanaged where TE8 : unmanaged
        => Exclude(typeof(TE1), typeof(TE2), typeof(TE3), typeof(TE4), typeof(TE5), typeof(TE6), typeof(TE7), typeof(TE8));

    /// <summary>Returns a new filter that excludes the specified component types. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude<TE1, TE2, TE3, TE4, TE5, TE6, TE7, TE8, TE9>() where TE1 : unmanaged where TE2 : unmanaged where TE3 : unmanaged where TE4 : unmanaged where TE5 : unmanaged where TE6 : unmanaged where TE7 : unmanaged where TE8 : unmanaged where TE9 : unmanaged
        => Exclude(typeof(TE1), typeof(TE2), typeof(TE3), typeof(TE4), typeof(TE5), typeof(TE6), typeof(TE7), typeof(TE8), typeof(TE9));

    /// <summary>Returns a new filter that excludes the specified component types. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude<TE1, TE2, TE3, TE4, TE5, TE6, TE7, TE8, TE9, TE10>() where TE1 : unmanaged where TE2 : unmanaged where TE3 : unmanaged where TE4 : unmanaged where TE5 : unmanaged where TE6 : unmanaged where TE7 : unmanaged where TE8 : unmanaged where TE9 : unmanaged where TE10 : unmanaged
        => Exclude(typeof(TE1), typeof(TE2), typeof(TE3), typeof(TE4), typeof(TE5), typeof(TE6), typeof(TE7), typeof(TE8), typeof(TE9), typeof(TE10));

    /// <summary>Returns a new filter that excludes the specified component types. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude<TE1, TE2, TE3, TE4, TE5, TE6, TE7, TE8, TE9, TE10, TE11>() where TE1 : unmanaged where TE2 : unmanaged where TE3 : unmanaged where TE4 : unmanaged where TE5 : unmanaged where TE6 : unmanaged where TE7 : unmanaged where TE8 : unmanaged where TE9 : unmanaged where TE10 : unmanaged where TE11 : unmanaged
        => Exclude(typeof(TE1), typeof(TE2), typeof(TE3), typeof(TE4), typeof(TE5), typeof(TE6), typeof(TE7), typeof(TE8), typeof(TE9), typeof(TE10), typeof(TE11));

    /// <summary>Returns a new filter that excludes the specified component types. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude<TE1, TE2, TE3, TE4, TE5, TE6, TE7, TE8, TE9, TE10, TE11, TE12>() where TE1 : unmanaged where TE2 : unmanaged where TE3 : unmanaged where TE4 : unmanaged where TE5 : unmanaged where TE6 : unmanaged where TE7 : unmanaged where TE8 : unmanaged where TE9 : unmanaged where TE10 : unmanaged where TE11 : unmanaged where TE12 : unmanaged
        => Exclude(typeof(TE1), typeof(TE2), typeof(TE3), typeof(TE4), typeof(TE5), typeof(TE6), typeof(TE7), typeof(TE8), typeof(TE9), typeof(TE10), typeof(TE11), typeof(TE12));

    /// <summary>Returns a new filter that excludes the specified component types. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude<TE1, TE2, TE3, TE4, TE5, TE6, TE7, TE8, TE9, TE10, TE11, TE12, TE13>() where TE1 : unmanaged where TE2 : unmanaged where TE3 : unmanaged where TE4 : unmanaged where TE5 : unmanaged where TE6 : unmanaged where TE7 : unmanaged where TE8 : unmanaged where TE9 : unmanaged where TE10 : unmanaged where TE11 : unmanaged where TE12 : unmanaged where TE13 : unmanaged
        => Exclude(typeof(TE1), typeof(TE2), typeof(TE3), typeof(TE4), typeof(TE5), typeof(TE6), typeof(TE7), typeof(TE8), typeof(TE9), typeof(TE10), typeof(TE11), typeof(TE12), typeof(TE13));

    /// <summary>Returns a new filter that excludes the specified component types. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude<TE1, TE2, TE3, TE4, TE5, TE6, TE7, TE8, TE9, TE10, TE11, TE12, TE13, TE14>() where TE1 : unmanaged where TE2 : unmanaged where TE3 : unmanaged where TE4 : unmanaged where TE5 : unmanaged where TE6 : unmanaged where TE7 : unmanaged where TE8 : unmanaged where TE9 : unmanaged where TE10 : unmanaged where TE11 : unmanaged where TE12 : unmanaged where TE13 : unmanaged where TE14 : unmanaged
        => Exclude(typeof(TE1), typeof(TE2), typeof(TE3), typeof(TE4), typeof(TE5), typeof(TE6), typeof(TE7), typeof(TE8), typeof(TE9), typeof(TE10), typeof(TE11), typeof(TE12), typeof(TE13), typeof(TE14));

    /// <summary>Returns a new filter that excludes the specified component types. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude<TE1, TE2, TE3, TE4, TE5, TE6, TE7, TE8, TE9, TE10, TE11, TE12, TE13, TE14, TE15>() where TE1 : unmanaged where TE2 : unmanaged where TE3 : unmanaged where TE4 : unmanaged where TE5 : unmanaged where TE6 : unmanaged where TE7 : unmanaged where TE8 : unmanaged where TE9 : unmanaged where TE10 : unmanaged where TE11 : unmanaged where TE12 : unmanaged where TE13 : unmanaged where TE14 : unmanaged where TE15 : unmanaged
        => Exclude(typeof(TE1), typeof(TE2), typeof(TE3), typeof(TE4), typeof(TE5), typeof(TE6), typeof(TE7), typeof(TE8), typeof(TE9), typeof(TE10), typeof(TE11), typeof(TE12), typeof(TE13), typeof(TE14), typeof(TE15));

    /// <summary>Returns a new filter that excludes the specified component types. This method creates a new filter instance with the added excluded component types, allowing for method chaining to build complex filters.</summary>
    public ComponentFilter Exclude<TE1, TE2, TE3, TE4, TE5, TE6, TE7, TE8, TE9, TE10, TE11, TE12, TE13, TE14, TE15, TE16>() where TE1 : unmanaged where TE2 : unmanaged where TE3 : unmanaged where TE4 : unmanaged where TE5 : unmanaged where TE6 : unmanaged where TE7 : unmanaged where TE8 : unmanaged where TE9 : unmanaged where TE10 : unmanaged where TE11 : unmanaged where TE12 : unmanaged where TE13 : unmanaged where TE14 : unmanaged where TE15 : unmanaged where TE16 : unmanaged
        => Exclude(typeof(TE1), typeof(TE2), typeof(TE3), typeof(TE4), typeof(TE5), typeof(TE6), typeof(TE7), typeof(TE8), typeof(TE9), typeof(TE10), typeof(TE11), typeof(TE12), typeof(TE13), typeof(TE14), typeof(TE15), typeof(TE16));
}