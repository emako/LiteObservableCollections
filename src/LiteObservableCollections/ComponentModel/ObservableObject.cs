using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LiteObservableCollections.ComponentModel;

/// <summary>
/// A lightweight ObservableObject similar in behavior to CommunityToolkit.Mvvm's ObservableObject.
/// Provides change notifications via INotifyPropertyChanged and INotifyPropertyChanging and
/// includes a handy SetProperty helper to simplify property setters.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged, INotifyPropertyChanging
{
    /// <summary>
    /// Raised after a property value has changed.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raised before a property value is changing.
    /// </summary>
    public event PropertyChangingEventHandler? PropertyChanging;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed. If null, caller member name is used.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanging"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that is changing. If null, caller member name is used.</param>
    protected virtual void OnPropertyChanging([CallerMemberName] string? propertyName = null)
    {
        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the backing field and raises change notifications when the value actually changes.
    /// Returns true if the value was changed.
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        OnPropertyChanging(propertyName);
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Sets the backing field, invokes an optional onChanged callback, and raises change notifications when the value actually changes.
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, Action? onChanged, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        OnPropertyChanging(propertyName);
        field = value;
        try { onChanged?.Invoke(); }
        catch
        {
            // Don't swallow exceptions silently in production code; rethrow to surface.
            throw;
        }
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Explicitly raise PropertyChanged for a property name.
    /// </summary>
    public void RaisePropertyChanged(string propertyName)
        => OnPropertyChanged(propertyName);

    /// <summary>
    /// Explicitly raise PropertyChanging for a property name.
    /// </summary>
    public void RaisePropertyChanging(string propertyName)
        => OnPropertyChanging(propertyName);
}
