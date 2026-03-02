namespace IsOnline.Checkers;

/// <summary>
/// Shared utilities used by all connectivity checker classes.
/// </summary>
internal static class CheckerHelpers
{
    /// <summary>
    /// Resolves to <c>true</c> as soon as any task in the collection returns <c>true</c>.
    /// Uses <c>Task.WhenAny</c> to short-circuit on the first success.
    /// </summary>
    internal static async Task<bool> AnySucceeds(IEnumerable<Task<bool>> tasks)
    {
        var remaining = tasks.ToList();
        while (remaining.Count > 0)
        {
            var finished = await Task.WhenAny(remaining).ConfigureAwait(false);
            remaining.Remove(finished);
            if (await finished.ConfigureAwait(false)) return true;
        }

        return false;
    }
}
