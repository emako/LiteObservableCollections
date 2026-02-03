[![NuGet](https://img.shields.io/nuget/v/LiteObservableCollections.svg)](https://nuget.org/packages/LiteObservableCollections) [![Actions](https://github.com/emako/LiteObservableCollections/actions/workflows/library.nuget.yml/badge.svg)](https://github.com/emako/LiteObservableCollections/actions/workflows/library.nuget.yml) 

# LiteObservableCollections

Lite version of ObservableCollections with fewer features but better performance.

## Features

- Lightweight observable list and collection implementations.
- Implements `INotifyCollectionChanged` and `INotifyPropertyChanged`.
- Supports batch addition via `AddRange`.
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
collection.AddRange(new[] { "C", "D" }); // AddRange triggers a single Add event with all new items
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

## Why AddRange?

The `AddRange` method allows you to efficiently add multiple items at once, reducing the number of notifications and improving performance in UI scenarios.

- `ObservableList<T>.AddRange` triggers a `Reset` event for maximum compatibility.
- `ObservableCollection<T>.AddRange` triggers a single `Add` event with all new items, which is more efficient for most UI frameworks.

## License

[MIT](LICENSE)

