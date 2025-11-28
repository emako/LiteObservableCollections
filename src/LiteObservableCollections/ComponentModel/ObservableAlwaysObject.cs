using System.Runtime.CompilerServices;

namespace LiteObservableCollections.ComponentModel;

/// <summary>
/// An <see cref="ObservableObject"/> variant that always notifies property changes,
/// even if the new value is equal to the old value (force-refresh behavior).
/// This is useful in scenarios where UI or observers need to react regardless of equality.
/// </summary>
public abstract class ObservableAlwaysObject : ObservableObject
{
    /// <summary>
    /// Sets the backing field and raises <see cref="ObservableObject.PropertyChanging"/>
    /// and <see cref="ObservableObject.PropertyChanged"/> events unconditionally.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value to assign.</param>
    /// <param name="propertyName">
    /// The name of the property being set. Automatically supplied by the compiler if omitted.
    /// </param>
    /// <returns>Always returns true after assignment and notification.</returns>
    protected bool SetPropertyAlways<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        OnPropertyChanging(propertyName);
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Sets the backing field, invokes an optional callback, and raises
    /// <see cref="ObservableObject.PropertyChanging"/> and <see cref="ObservableObject.PropertyChanged"/>
    /// events unconditionally.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value to assign.</param>
    /// <param name="onChanged">An optional <see cref="Action"/> to invoke after assignment and before notification.</param>
    /// <param name="propertyName">
    /// The name of the property being set. Automatically supplied by the compiler if omitted.
    /// </param>
    /// <returns>Always returns true after assignment, callback invocation, and notification.</returns>
    /// <remarks>
    /// Exceptions thrown from <paramref name="onChanged"/> are rethrown to avoid silently swallowing errors.
    /// </remarks>
    protected bool SetPropertyAlways<T>(ref T field, T value, Action? onChanged, [CallerMemberName] string? propertyName = null)
    {
        OnPropertyChanging(propertyName);
        field = value;
        try
        {
            onChanged?.Invoke();
        }
        catch
        {
            // Don't swallow exceptions silently in production code; rethrow to surface.
            throw;
        }
        OnPropertyChanged(propertyName);
        return true;
    }
}
