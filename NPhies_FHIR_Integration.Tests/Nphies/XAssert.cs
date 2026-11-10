namespace NPhies_FHIR_Integration.Tests.Nphies;

/// <summary>Small helpers for assertions MSTest 3.1 lacks (returning the asserted value, predicate checks).</summary>
internal static class XAssert
{
    public static T Single<T>(IEnumerable<T> source)
    {
        var list = source.ToList();
        Assert.AreEqual(1, list.Count, $"Expected exactly one {typeof(T).Name} but found {list.Count}.");
        return list[0];
    }

    public static T IsType<T>(object? value)
    {
        Assert.IsInstanceOfType(value, typeof(T));
        return (T)value!;
    }

    public static void Empty<T>(IEnumerable<T> source) =>
        Assert.AreEqual(0, source.Count(), "Expected an empty collection.");

    public static void NotEmpty<T>(IEnumerable<T> source) =>
        Assert.IsTrue(source.Any(), "Expected a non-empty collection.");

    public static void NotEmpty(string? value) =>
        Assert.IsFalse(string.IsNullOrEmpty(value), "Expected a non-empty string.");

    public static void Contains<T>(IEnumerable<T> source, Func<T, bool> predicate) =>
        Assert.IsTrue(source.Any(predicate), "No item matched the predicate.");

    public static void DoesNotContain<T>(IEnumerable<T> source, Func<T, bool> predicate) =>
        Assert.IsFalse(source.Any(predicate), "An item unexpectedly matched the predicate.");

    public static TException Throws<TException>(Action action) where TException : Exception =>
        Assert.ThrowsException<TException>(action);

    public static Task<TException> ThrowsAsync<TException>(Func<Task> action) where TException : Exception =>
        Assert.ThrowsExceptionAsync<TException>(action);
}
