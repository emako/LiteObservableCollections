using LiteObservableCollections.EventListeners.Internals;
using System.Collections;
using System.ComponentModel;
using System.Linq.Expressions;

namespace LiteObservableCollections.EventListeners.WeakEvents;

/// <summary>
/// Weak event listener for receiving INotifyPropertyChanged.PropertyChanged events.
/// </summary>
public sealed class PropertyChangedWeakEventListener :
    ObservableWeakEventListener<PropertyChangedEventHandler, PropertyChangedEventArgs>,
    IEnumerable<KeyValuePair<string, List<PropertyChangedEventHandler>>>
{
    private readonly AnonymousPropertyChangedEventHandlerBag _bag;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="source">INotifyPropertyChanged object</param>
    public PropertyChangedWeakEventListener(INotifyPropertyChanged source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        _bag = new AnonymousPropertyChangedEventHandlerBag(source);
        Initialize(
            h => new PropertyChangedEventHandler(h ?? throw new ArgumentNullException(nameof(h))),
            h => source.PropertyChanged += h,
            h => source.PropertyChanged -= h,
            (sender, e) => _bag.ExecuteHandler(e ?? throw new ArgumentNullException(nameof(e))));
    }

    /// <summary>
    /// Constructor. Registers one handler at the same time as creating the listener instance.
    /// </summary>
    /// <param name="source">INotifyPropertyChanged object</param>
    /// <param name="handler">PropertyChanged event handler</param>
    public PropertyChangedWeakEventListener(INotifyPropertyChanged source,
        PropertyChangedEventHandler handler)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        _bag = new AnonymousPropertyChangedEventHandlerBag(source, handler);
        Initialize(
            h => new PropertyChangedEventHandler(h ?? throw new ArgumentNullException(nameof(h))),
            h => source.PropertyChanged += h,
            h => source.PropertyChanged -= h,
            (sender, e) => _bag.ExecuteHandler(e ?? throw new ArgumentNullException(nameof(e))));
    }

    IEnumerator<KeyValuePair<string, List<PropertyChangedEventHandler>>>
        IEnumerable<KeyValuePair<string, List<PropertyChangedEventHandler>>>.GetEnumerator()
    {
        ThrowExceptionIfDisposed();
        return
            ((IEnumerable<KeyValuePair<string, List<PropertyChangedEventHandler>>>)_bag)
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        ThrowExceptionIfDisposed();
        return ((IEnumerable<KeyValuePair<string, List<PropertyChangedEventHandler>>>)_bag).GetEnumerator();
    }

    /// <summary>
    /// Adds a new handler to this listener instance.
    /// </summary>
    /// <param name="handler">PropertyChanged event handler</param>
    public void RegisterHandler(PropertyChangedEventHandler handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        ThrowExceptionIfDisposed();
        _bag.RegisterHandler(handler);
    }

    /// <summary>
    /// Adds a handler filtered by property name to this listener instance.
    /// </summary>
    /// <param name="propertyName">Name of PropertyChangedEventArgs.PropertyName to register the handler for</param>
    /// <param name="handler">PropertyChanged event handler for the property specified by propertyName</param>
    public void RegisterHandler(string propertyName, PropertyChangedEventHandler handler)
    {
        if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        ThrowExceptionIfDisposed();
        _bag.RegisterHandler(propertyName, handler);
    }

    /// <summary>
    /// Adds a handler filtered by property name to this listener instance.
    /// </summary>
    /// <param name="propertyExpression">Lambda expression in the form () => property</param>
    /// <param name="handler">PropertyChanged event handler for the property specified by propertyExpression</param>
    public void RegisterHandler<T>(Expression<Func<T>> propertyExpression,
        PropertyChangedEventHandler handler)
    {
        if (propertyExpression == null) throw new ArgumentNullException(nameof(propertyExpression));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        ThrowExceptionIfDisposed();
        _bag.RegisterHandler(propertyExpression, handler);
    }

    public void Add(PropertyChangedEventHandler handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        ThrowExceptionIfDisposed();
        _bag.Add(handler);
    }

    public void Add(string propertyName, PropertyChangedEventHandler handler)
    {
        if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        ThrowExceptionIfDisposed();
        _bag.Add(propertyName, handler);
    }

    public void Add(string propertyName, params PropertyChangedEventHandler[] handlers)
    {
        if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
        if (handlers == null) throw new ArgumentNullException(nameof(handlers));

        ThrowExceptionIfDisposed();
        _bag.Add(propertyName, handlers);
    }

    public void Add<T>(Expression<Func<T>> propertyExpression,
        PropertyChangedEventHandler handler)
    {
        if (propertyExpression == null) throw new ArgumentNullException(nameof(propertyExpression));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        ThrowExceptionIfDisposed();
        _bag.Add(propertyExpression, handler);
    }

    public void Add<T>(Expression<Func<T>> propertyExpression,
        params PropertyChangedEventHandler[] handlers)
    {
        if (propertyExpression == null) throw new ArgumentNullException(nameof(propertyExpression));
        if (handlers == null) throw new ArgumentNullException(nameof(handlers));

        ThrowExceptionIfDisposed();
        _bag.Add(propertyExpression, handlers);
    }
}
