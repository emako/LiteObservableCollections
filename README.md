
# LiteObservableCollections

Lite version of ObservableCollections with fewer features but better performance.

## Features

- Lightweight observable list and collection implementations.
- Implements `INotifyCollectionChanged` and `INotifyPropertyChanged`.
- Supports batch addition via `AddRange`.

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

## Why AddRange?

The `AddRange` method allows you to efficiently add multiple items at once, reducing the number of notifications and improving performance in UI scenarios.

- `ObservableList<T>.AddRange` triggers a `Reset` event for maximum compatibility.
- `ObservableCollection<T>.AddRange` triggers a single `Add` event with all new items, which is more efficient for most UI frameworks.

## License

[MIT](LICENSE)

