[![NuGet](https://img.shields.io/nuget/v/LiteObservableCollections.svg)](https://nuget.org/packages/LiteObservableCollections) [![Actions](https://github.com/emako/LiteObservableCollections/actions/workflows/library.nuget.yml/badge.svg)](https://github.com/emako/LiteObservableCollections/actions/workflows/library.nuget.yml) 

# LiteObservableCollections

Lite version of ObservableCollections with fewer features but better performance.

## Features

- Lightweight observable list and collection implementations.
- Implements `INotifyCollectionChanged` and `INotifyPropertyChanged`.
- Supports batch addition via `AddRange`.
- Built-in `ItemPropertyChanged` on `ObservableList<T>` / `ObservableCollection<T>` (also via `IObservableList<T>` / `IObservableCollection<T>`). Items are only listened to while a handler is attached.
- **ObservableViewList**: reactive view over `ObservableList<T>` with filter, sort, and optional projection; filter/sort are persisted across source updates.

## Usage

### ObservableList<T>

```csharp
using LiteObservableCollections;

var list = new ObservableList<int>();
list.CollectionChanged += (s, e) => Console.WriteLine($"Action: {e.Action}");
list.Add(1);
list.Add(2);
list.AddRange(new[] { 3, 4, 5 }); // AddRange triggers a Reset event
Console.WriteLine(string.Join(", ", list)); // Output: 1, 2, 3, 4, 5
```

### ObservableCollection<T>

```csharp
using LiteObservableCollections;

var collection = new ObservableCollection<string>();
collection.CollectionChanged += (s, e) => Console.WriteLine($"Action: {e.Action}");
collection.Add("A");
collection.Add("B");
collection.AddRange(new[] { "C", "D" }); // AddRange triggers a Reset event
Console.WriteLine(string.Join(", ", collection)); // Output: A, B, C, D
```

### Move Support

Both types support moving items:

```csharp
list.Move(0, 2); // Moves the first item to index 2
collection.Move(1, 0); // Moves the second item to the first position
```

### ObservableViewList\<T\>

A reactive view over an `ObservableList<T>` that supports filtering and sorting. Changes in the source list are reflected in the view. Filter and sort are persisted: `Refresh()` (e.g. when the source changes) re-applies them.

**View without projection** (same element type):

```csharp
var source = new ObservableList<string> { "Banana", "Apple", "Cherry" };
using var view = new ObservableViewList<string>(source);

// Filter: only items containing "a"
view.AttachFilter(s => s.Contains('a'));
// view: Banana, Apple

// Reset filter
view.ResetFilter();

// Sort by default comparer (or AttachSort(comparison))
view.AttachSort();
// view: Apple, Banana, Cherry

// Restore source order
view.ResetSort();
```

**View with projection** (`ObservableViewList<TSource, TResult>`):

```csharp
var source = new ObservableList<int> { 1, 2, 3 };
using var view = new ObservableViewList<int, string>(source, x => $"Item {x}");
// view: "Item 1", "Item 2", "Item 3"

view.AttachFilter(x => x >= 2);
// view: "Item 2", "Item 3"
```

- `AttachFilter(predicate)` / `ResetFilter()` — filter operates on source elements (before projection).
- `AttachSort()` / `AttachSort(comparer)` / `AttachSort(comparison)` / `ResetSort()` — sort operates on view elements. Sort is re-applied on each `Refresh()`.
- `ObservableViewList` implements `IDisposable`; dispose to unsubscribe from the source.

### EventListeners

`ObservableList<T>` raises collection-level notifications (`CollectionChanged` / list `PropertyChanged`). To react to a property change on an item, the item must implement `INotifyPropertyChanged`; you can then use `ItemPropertyChanged` on `ObservableList<T>` / `ObservableCollection<T>` (also via `IObservableList<T>` / `IObservableCollection<T>`), or `CollectionItemPropertyChangedListener<T>` for arbitrary sources. You can inherit from `ObservableObject` to implement the standard notification pattern:

```csharp
using LiteObservableCollections.ComponentModel;

public sealed class Person : ObservableObject
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}
```

Use `PropertyChangedEventListener` to register handlers for all properties or a specific property. The listener is `IDisposable`; dispose it when the subscription is no longer needed.

```csharp
using LiteObservableCollections.EventListeners;

var person = new Person();
using var listener = new PropertyChangedEventListener(person);

listener.RegisterHandler((_, e) =>
    Console.WriteLine($"{e.PropertyName} changed"));

listener.RegisterHandler(nameof(Person.Name), (_, _) =>
    Console.WriteLine("Name changed"));

person.Name = "Ada";
```

You can also use a property expression to avoid string literals:

```csharp
listener.RegisterHandler(() => person.Name, (_, _) =>
    Console.WriteLine("Name changed"));
```

To observe property changes from items in an `ObservableList<T>` or `ObservableCollection<T>`, subscribe to `ItemPropertyChanged` directly. Reference-type items that implement `INotifyPropertyChanged` are observed; other items are ignored:

```csharp
using LiteObservableCollections;

var people = new ObservableList<Person>();

people.ItemPropertyChanged += (sender, e) =>
    Console.WriteLine($"{e.Item.Name}.{e.PropertyName} changed");

var person = new Person();
people.Add(person);
person.Name = "Ada";
```

`ItemPropertyChanged` is raised only when an item's property changes. The event `sender` is the collection. Like `CollectionChanged`, it respects `IsNotifyEnabled` and is marshalled through `EventDispatcher` when set; a marshalled event is dropped if the item was removed before delivery, even if the same instance was added again in the meantime. Item subscriptions stay synchronized with the collection even while notifications are disabled. To react to add, remove, replace, or reset operations, use `CollectionChanged` or `CollectionChangedEventListener`.

Items are subscribed when the first handler is attached and unsubscribed when the last handler is removed, so a collection without `ItemPropertyChanged` handlers does not listen to its items at all. While a handler is attached, each observed item references the collection through its `PropertyChanged` event, so items that outlive the collection keep it alive. Remove your handlers when they are no longer needed.

Like the collections themselves, `ItemPropertyChanged` is not synchronized with mutations. Handlers may be added and removed concurrently with each other, but not while another thread mutates the collection; synchronize those with your own lock, or use the concurrent collections. If subscribing to an item's `PropertyChanged` throws during a mutation, the mutation is still applied and its notifications are raised before the exception propagates.

The event is declared on `IItemPropertyObservable<T>`, which `IObservableList<T>` and `IObservableCollection<T>` inherit. Other collection types (such as `ObservableViewList` or the concurrent collections) do not implement it. The event arguments type is `ItemPropertyChangedEventArgs<T>` in the `LiteObservableCollections` namespace.

`CollectionItemPropertyChangedListener<T>` provides item property notifications for any source that implements both `IEnumerable<T>` and `INotifyCollectionChanged`; the event `sender` is the listener. It raises synchronously on the thread that changed the item and is not affected by the source's `IsNotifyEnabled` or `EventDispatcher`. For `ObservableList<T>` and `ObservableCollection<T>` it shares the collection's item subscriptions, which stay synchronized even while the collection's notifications are disabled. For other sources it maintains subscriptions from the source's collection-change events across add, remove, replace, reset, and duplicate references; mutations for which the source suppresses `CollectionChanged` cannot be observed. Dispose the listener to release its item subscriptions.

`CollectionChangedEventListener` provides the same pattern for collection events and can filter handlers by `NotifyCollectionChangedAction`:

```csharp
using System.Collections.Specialized;
using LiteObservableCollections;
using LiteObservableCollections.EventListeners;

var list = new ObservableList<Person>();
using var listener = new CollectionChangedEventListener(list);

listener.RegisterHandler(NotifyCollectionChangedAction.Add, (_, e) =>
    Console.WriteLine($"Added {e.NewItems?.Count} item(s)"));

list.Add(new Person());
```

For subscriptions whose lifetime is shorter than the event source, use `PropertyChangedWeakEventListener` or `CollectionChangedWeakEventListener` from `LiteObservableCollections.EventListeners.WeakEvents`. They retain the listener weakly, helping prevent the source from keeping the listener alive. Keep a strong reference to the listener for as long as it should receive events, and still call `Dispose()` when deterministic unsubscription is needed.

```csharp
using LiteObservableCollections.EventListeners.WeakEvents;

var listener = new PropertyChangedWeakEventListener(person);
listener.RegisterHandler(nameof(Person.Name), (_, _) =>
    Console.WriteLine("Name changed"));
```

> `ObservableViewList` refreshes itself when its source collection changes. An item's `PropertyChanged` event does not automatically reapply the view's filter or sort; handle the item event and call `view.Refresh()` when needed.

## Migrating from 5.x to 6.0

6.0 contains the following breaking changes:

- `CollectionItemPropertyChangedEventArgs<T>` has been removed. Use `ItemPropertyChangedEventArgs<T>` in the `LiteObservableCollections` namespace instead; it keeps the `Item` and `PropertyChangedEventArgs` members and adds `PropertyName`.
- `CollectionItemPropertyChangedListener<T>.ItemPropertyChanged` is now an `EventHandler<ItemPropertyChangedEventArgs<T>>`. Handlers that spell out the old argument type must be updated; lambdas without explicit parameter types compile unchanged. Code compiled against 5.x must be recompiled.
- `IObservableList<T>` and `IObservableCollection<T>` now inherit `IItemPropertyObservable<T>`. Custom implementations of these interfaces must implement the `ItemPropertyChanged` event.
- The `where T : class, INotifyPropertyChanged` constraint on `CollectionItemPropertyChangedListener<T>` has been removed; items that are not reference types implementing `INotifyPropertyChanged` are ignored.

Behavior changes:

- `Remove(item)` now reports the instance stored in the collection in `CollectionChanged.OldItems`, instead of the argument passed in. This is only observable when items are equal by `Equals` but are different instances, and it matches `System.Collections.ObjectModel.ObservableCollection<T>`.
- When attached to `ObservableList<T>` or `ObservableCollection<T>`, `CollectionItemPropertyChangedListener<T>` no longer depends on `CollectionChanged`, so it also tracks items added while the collection's `IsNotifyEnabled` is false.
- `ObservableList<T>` and `ObservableCollection<T>` reuse cached `PropertyChangedEventArgs` instances for `Count` and `Item[]`, and a cached `NotifyCollectionChangedEventArgs` instance for `Reset`. Do not rely on receiving a distinct event-args instance per notification.

## Why AddRange?

The `AddRange` method allows you to efficiently add multiple items at once, reducing the number of notifications and improving performance in UI scenarios.

- By default, `AddRange` and `RemoveRange` on both `ObservableList<T>` and `ObservableCollection<T>` trigger a single `Reset` event (`RemoveRange` only does so when at least one item was removed). This is the most compatible choice: WPF's `CollectionView`, for example, does not support multi-item `Add` events.
- Set `IsNotifyOnEachInRange` to true to raise one `Add` or `Remove` event per item instead, for example to animate individual insertions and deletions.

## License

[MIT](LICENSE)

