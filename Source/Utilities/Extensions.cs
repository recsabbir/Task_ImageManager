using System.Runtime.CompilerServices;

namespace ImageDownloader.Utilities;

public static class Extensions
{
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        => collection == null || !collection.Any();
    public static bool IsNotNullOrEmpty<T>(this IEnumerable<T> collection)
        =>  collection != null && collection.Any();
}
