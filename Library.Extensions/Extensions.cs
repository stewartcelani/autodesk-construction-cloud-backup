namespace Library.Extensions;

public static class Extensions
{
    public static IEnumerable<T> FlattenRecursive<T>(this IEnumerable<T> rootEnumerable,
        Func<T, IEnumerable<T>> selector)
    {
        IEnumerable<T> recursiveFlatten = rootEnumerable.ToList();
        if (recursiveFlatten.Any() == false) return recursiveFlatten;

        IEnumerable<T> descendants = recursiveFlatten
            .SelectMany(selector)
            .FlattenRecursive(selector);

        return recursiveFlatten.Concat(descendants);
    }
}