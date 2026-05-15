namespace Gubbins.Entities;

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash = 0;
        Includes = new Type[]{};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext<T1>
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash =  typeof(T1).GetHashCode();
        Includes = new Type[]{typeof(T1)};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext<T1, T2>
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash =  typeof(T1).GetHashCode() | typeof(T2).GetHashCode();
        Includes = new Type[]{typeof(T1), typeof(T2)};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext<T1, T2, T3>
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash =  typeof(T1).GetHashCode() | typeof(T2).GetHashCode() | typeof(T3).GetHashCode();
        Includes = new Type[]{typeof(T1), typeof(T2), typeof(T3)};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext<T1, T2, T3, T4>
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash =  typeof(T1).GetHashCode() | typeof(T2).GetHashCode() | typeof(T3).GetHashCode() | typeof(T4).GetHashCode();
        Includes = new Type[]{typeof(T1), typeof(T2), typeof(T3), typeof(T4)};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext<T1, T2, T3, T4, T5>
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash =  typeof(T1).GetHashCode() | typeof(T2).GetHashCode() | typeof(T3).GetHashCode() | typeof(T4).GetHashCode() | typeof(T5).GetHashCode();
        Includes = new Type[]{typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext<T1, T2, T3, T4, T5, T6>
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash =  typeof(T1).GetHashCode() | typeof(T2).GetHashCode() | typeof(T3).GetHashCode() | typeof(T4).GetHashCode() | typeof(T5).GetHashCode() | typeof(T6).GetHashCode();
        Includes = new Type[]{typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6)};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext<T1, T2, T3, T4, T5, T6, T7>
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash =  typeof(T1).GetHashCode() | typeof(T2).GetHashCode() | typeof(T3).GetHashCode() | typeof(T4).GetHashCode() | typeof(T5).GetHashCode() | typeof(T6).GetHashCode() | typeof(T7).GetHashCode();
        Includes = new Type[]{typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7)};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8>
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash =  typeof(T1).GetHashCode() | typeof(T2).GetHashCode() | typeof(T3).GetHashCode() | typeof(T4).GetHashCode() | typeof(T5).GetHashCode() | typeof(T6).GetHashCode() | typeof(T7).GetHashCode() | typeof(T8).GetHashCode();
        Includes = new Type[]{typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8)};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9>
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash =  typeof(T1).GetHashCode() | typeof(T2).GetHashCode() | typeof(T3).GetHashCode() | typeof(T4).GetHashCode() | typeof(T5).GetHashCode() | typeof(T6).GetHashCode() | typeof(T7).GetHashCode() | typeof(T8).GetHashCode() | typeof(T9).GetHashCode();
        Includes = new Type[]{typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9)};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash =  typeof(T1).GetHashCode() | typeof(T2).GetHashCode() | typeof(T3).GetHashCode() | typeof(T4).GetHashCode() | typeof(T5).GetHashCode() | typeof(T6).GetHashCode() | typeof(T7).GetHashCode() | typeof(T8).GetHashCode() | typeof(T9).GetHashCode() | typeof(T10).GetHashCode();
        Includes = new Type[]{typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10)};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash =  typeof(T1).GetHashCode() | typeof(T2).GetHashCode() | typeof(T3).GetHashCode() | typeof(T4).GetHashCode() | typeof(T5).GetHashCode() | typeof(T6).GetHashCode() | typeof(T7).GetHashCode() | typeof(T8).GetHashCode() | typeof(T9).GetHashCode() | typeof(T10).GetHashCode() | typeof(T11).GetHashCode();
        Includes = new Type[]{typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11)};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash =  typeof(T1).GetHashCode() | typeof(T2).GetHashCode() | typeof(T3).GetHashCode() | typeof(T4).GetHashCode() | typeof(T5).GetHashCode() | typeof(T6).GetHashCode() | typeof(T7).GetHashCode() | typeof(T8).GetHashCode() | typeof(T9).GetHashCode() | typeof(T10).GetHashCode() | typeof(T11).GetHashCode() | typeof(T12).GetHashCode();
        Includes = new Type[]{typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12)};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash =  typeof(T1).GetHashCode() | typeof(T2).GetHashCode() | typeof(T3).GetHashCode() | typeof(T4).GetHashCode() | typeof(T5).GetHashCode() | typeof(T6).GetHashCode() | typeof(T7).GetHashCode() | typeof(T8).GetHashCode() | typeof(T9).GetHashCode() | typeof(T10).GetHashCode() | typeof(T11).GetHashCode() | typeof(T12).GetHashCode() | typeof(T13).GetHashCode();
        Includes = new Type[]{typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13)};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash =  typeof(T1).GetHashCode() | typeof(T2).GetHashCode() | typeof(T3).GetHashCode() | typeof(T4).GetHashCode() | typeof(T5).GetHashCode() | typeof(T6).GetHashCode() | typeof(T7).GetHashCode() | typeof(T8).GetHashCode() | typeof(T9).GetHashCode() | typeof(T10).GetHashCode() | typeof(T11).GetHashCode() | typeof(T12).GetHashCode() | typeof(T13).GetHashCode() | typeof(T14).GetHashCode();
        Includes = new Type[]{typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14)};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash =  typeof(T1).GetHashCode() | typeof(T2).GetHashCode() | typeof(T3).GetHashCode() | typeof(T4).GetHashCode() | typeof(T5).GetHashCode() | typeof(T6).GetHashCode() | typeof(T7).GetHashCode() | typeof(T8).GetHashCode() | typeof(T9).GetHashCode() | typeof(T10).GetHashCode() | typeof(T11).GetHashCode() | typeof(T12).GetHashCode() | typeof(T13).GetHashCode() | typeof(T14).GetHashCode() | typeof(T15).GetHashCode();
        Includes = new Type[]{typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15)};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}

/// <summary>
/// Represents an entity query handle.
/// </summary>
public readonly struct EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
{
    /// <summary>
    /// Global components hash.
    /// </summary>
    public readonly int Hash;
    internal readonly Type[] Includes;
    internal readonly Type[] Excludes;
    private EntityQueryContext(int hash, Type[] includes, Type[] excludes)
    {
        Hash = hash;
        Includes = includes;
        Excludes = excludes;
    }
    public EntityQueryContext()
    {
        Hash =  typeof(T1).GetHashCode() | typeof(T2).GetHashCode() | typeof(T3).GetHashCode() | typeof(T4).GetHashCode() | typeof(T5).GetHashCode() | typeof(T6).GetHashCode() | typeof(T7).GetHashCode() | typeof(T8).GetHashCode() | typeof(T9).GetHashCode() | typeof(T10).GetHashCode() | typeof(T11).GetHashCode() | typeof(T12).GetHashCode() | typeof(T13).GetHashCode() | typeof(T14).GetHashCode() | typeof(T15).GetHashCode() | typeof(T16).GetHashCode();
        Includes = new Type[]{typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16)};
        Excludes = Array.Empty<Type>();
    }
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Include<I1>() => new (Hash | typeof(I1).GetHashCode(), [..Includes, typeof(I1)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Include<I1, I2>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode(), [..Includes, typeof(I1), typeof(I2)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Include<I1, I2, I3>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Include<I1, I2, I3, I4>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Include<I1, I2, I3, I4, I5>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Include<I1, I2, I3, I4, I5, I6>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Include<I1, I2, I3, I4, I5, I6, I7>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Include<I1, I2, I3, I4, I5, I6, I7, I8>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15)], Excludes);
    /// <summary>
    /// Includes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Include<I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16>() => new (Hash | typeof(I1).GetHashCode() | typeof(I2).GetHashCode() | typeof(I3).GetHashCode() | typeof(I4).GetHashCode() | typeof(I5).GetHashCode() | typeof(I6).GetHashCode() | typeof(I7).GetHashCode() | typeof(I8).GetHashCode() | typeof(I9).GetHashCode() | typeof(I10).GetHashCode() | typeof(I11).GetHashCode() | typeof(I12).GetHashCode() | typeof(I13).GetHashCode() | typeof(I14).GetHashCode() | typeof(I15).GetHashCode() | typeof(I16).GetHashCode(), [..Includes, typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5), typeof(I6), typeof(I7), typeof(I8), typeof(I9), typeof(I10), typeof(I11), typeof(I12), typeof(I13), typeof(I14), typeof(I15), typeof(I16)], Excludes);

    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Exclude<E1>() => new (Hash, Includes, [..Excludes, typeof(E1)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Exclude<E1, E2>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Exclude<E1, E2, E3>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Exclude<E1, E2, E3, E4>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Exclude<E1, E2, E3, E4, E5>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Exclude<E1, E2, E3, E4, E5, E6>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Exclude<E1, E2, E3, E4, E5, E6, E7>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Exclude<E1, E2, E3, E4, E5, E6, E7, E8>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15)]);
    /// <summary>
    /// Excludes the specified components.
    /// </summary>
    public EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Exclude<E1, E2, E3, E4, E5, E6, E7, E8, E9, E10, E11, E12, E13, E14, E15, E16>() => new (Hash, Includes, [..Excludes, typeof(E1), typeof(E2), typeof(E3), typeof(E4), typeof(E5), typeof(E6), typeof(E7), typeof(E8), typeof(E9), typeof(E10), typeof(E11), typeof(E12), typeof(E13), typeof(E14), typeof(E15), typeof(E16)]);
}
