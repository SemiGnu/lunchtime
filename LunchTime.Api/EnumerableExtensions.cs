namespace LunchTime.Api;

public static class EnumerableExtensions
{
    public static IEnumerable<(T1, T2, T3)> Zip3<T1,T2,T3>(this IEnumerable<T1> first, IEnumerable<T2> second, IEnumerable<T3> third)
    {
        using var enumerator1 = first.GetEnumerator();
        using var enumerator2 = second.GetEnumerator();
        using var enumerator3 = third.GetEnumerator();
        while (enumerator1.MoveNext() && enumerator2.MoveNext() && enumerator3.MoveNext())
        {
            yield return (enumerator1.Current, enumerator2.Current, enumerator3.Current);
        }
    }
}