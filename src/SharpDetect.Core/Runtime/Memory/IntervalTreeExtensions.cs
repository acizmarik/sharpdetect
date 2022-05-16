using IntervalTree;

namespace SharpDetect.Core.Runtime.Memory
{
    internal static class IntervalTreeExtensions
    {
        public static TValue? QuerySingle<TKey, TValue>(this IIntervalTree<TKey, TValue> tree, TKey key)
        {
            var result = tree.Query(key);
            if (result == null)
                return default;

            return result.FirstOrDefault();
        }
    }
}
