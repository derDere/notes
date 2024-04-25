using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Notes {
  public class ManagedList<T> : List<T> {

    public enum FixedSizeRemoveModes {
      Front,
      Back
    }

    public static T[] Empty = new T[0];

    public static int CountUniques<T>(IEnumerable<T> items) {
      List<T> l = new List<T>();
      foreach (T itm in items) {
        if (!l.Contains(itm)) {
          l.Add(itm);
        }
      }
      return l.Count;
    }

    public static ManagedList<T> Fixed(int size) {
      ManagedList<T> l = new ManagedList<T>();
      l.Capacity = size;
      return l;
    }

    public class ListChangedEventArgs : HandledEventArgs {
      public T[] NewItems { get; private set; }
      public T[] RemovedItems { get; private set; }
      public int NewItemCount { get; private set; }
      public int RemovedItemCount { get; private set; }
      public int NewCount { get; private set; }
      public int OldCount { get; private set; }
      public bool SizeChanged { get; private set; }

      public ListChangedEventArgs(T[] newItems, T[] removedItems, int newItemCount, int removedItemCount, int newCount, int oldCount, bool sizeChanged) {
        this.NewItems = newItems;
        this.RemovedItems = removedItems;
        this.NewItemCount = newItemCount;
        this.RemovedItemCount = removedItemCount;
        this.NewCount = newCount;
        this.OldCount = oldCount;
        this.SizeChanged = sizeChanged;
      }
    }

    public class ListValidateItemEventArgs : HandledEventArgs {
      public T Item { get; private set; }
      public bool IsValid { get; set; }
      public ListValidateItemEventArgs(T item) {
        this.Item = item;
        this.IsValid = true;
      }
    }

    public delegate void ListChangedEventHandler(object sender, ListChangedEventArgs e);
    public event ListChangedEventHandler ListChanged;
    protected virtual bool OnListChanged(ListChangedEventArgs e) => true;
    private void RaiseListChangedEvent(ListChangedEventArgs e) {
      if (OnListChanged(e)) {
        if (!e.Handled) {
          ListChanged?.Invoke(this, e);
        }
      }
    }

    public delegate void ListItemValidCheckHandler(object sender, ListValidateItemEventArgs e);
    public event ListItemValidCheckHandler ListItemValidCheck;
    protected virtual bool OnListItemValidCheck(ListValidateItemEventArgs e) => true;
    private bool RaiseListItemValidCheck(T item) {
      ListValidateItemEventArgs e = new ListValidateItemEventArgs(item);
      if (OnListItemValidCheck(e)) {
        ListItemValidCheck?.Invoke(this, e);
      }
      return e.IsValid;
    }

    public bool Unique { get; private set; } = false;
    public int? FixedSize { get; private set; } = null;
    public FixedSizeRemoveModes FixedSizeRemoveMode { get; set; } = FixedSizeRemoveModes.Back;
    public int UniqueCount {
      get {
        return CountUniques(this);
      }
    }

    public void MakeUnique() {
      Unique = true;
      List<T> singles = new List<T>();
      List<int> indices = new List<int>();
      for (int i = 0; i < this.Count; i++) {
        T item = this[i];
        if (singles.Contains(item)) {
          indices.Add(i);
        }
        else {
          singles.Add(item);
        }
      }
      indices.Reverse();
      int removedItemCount = 0;
      int oldCount = this.Count;
      foreach (int i in indices) {
        base.RemoveAt(i);
        removedItemCount++;
      }
      ListChangedEventArgs e = new ListChangedEventArgs(
        Empty,
        Empty,
        0,
        removedItemCount,
        this.Count,
        oldCount,
        this.Count != oldCount
      );
      RaiseListChangedEvent(e);
    }

    public void AllowDuplicates() {
      Unique = false;
    }

    public void FixateToSize(int size) {
      FixedSize = size;
      int oldCount = this.Count;
      T[] removedItems = RemoveFixedSizeOverflow();
      Capacity = size;
      if (removedItems.Length > 0) {
        ListChangedEventArgs e = new ListChangedEventArgs(
          Empty,
          removedItems,
          0,
          removedItems.Length,
          0,
          oldCount,
          oldCount != this.Count
        );
        RaiseListChangedEvent(e);
      }
    }

    public void FreeSize() {
      FixedSize = null;
    }

    private T[] RemoveFixedSizeOverflow() {
      if (!FixedSize.HasValue)
        return Empty;
      List<T> removedItems = new List<T>();
      while (this.Count > FixedSize.Value) {
        if (FixedSizeRemoveMode == FixedSizeRemoveModes.Front) {
          removedItems.Add(this[0]);
          base.RemoveAt(0);
        }
        else {
          removedItems.Add(this[this.Count - 1]);
          base.RemoveAt(this.Count - 1);
        }
      }
      return removedItems.ToArray();
    }

    public new void Add(T itm) {
      bool hasSpace = !FixedSize.HasValue || FixedSize.Value > this.Count;
      if (!hasSpace && FixedSizeRemoveMode == FixedSizeRemoveModes.Back) {
        return;
      }
      if (!Unique || !this.Contains(itm)) {
        if (RaiseListItemValidCheck(itm)) {
          int oldCount = this.Count;
          base.Add(itm);
          T[] removedItems = RemoveFixedSizeOverflow();
          ListChangedEventArgs e = new ListChangedEventArgs(
            new T[] { itm },
            removedItems,
            1,
            removedItems.Length,
            this.Count,
            oldCount,
            oldCount != this.Count
          );
          RaiseListChangedEvent(e);
        }
      }
    }

    public new void Remove(T itm) {
      if (this.Contains(itm)) {
        int oldCount = this.Count;
        base.Remove(itm);
        ListChangedEventArgs e = new ListChangedEventArgs(
          Empty,
          new T[] { itm },
          0,
          1,
          this.Count,
          oldCount,
          oldCount != this.Count
        );
        RaiseListChangedEvent(e);
      }
    }

    public new void Clear() {
      if (this.Count > 0) {
        T[] removedItems = this.ToArray();
        int oldCount = this.Count;
        ListChangedEventArgs e = new ListChangedEventArgs(
          Empty,
          removedItems,
          0,
          removedItems.Length,
          0,
          oldCount,
          oldCount != 0
        );
        RaiseListChangedEvent(e);
      }
    }

    public new void Insert(int index, T itm) {
      bool hasSpace = !FixedSize.HasValue || FixedSize.Value > this.Count;
      if (
        !hasSpace && (
          (FixedSizeRemoveMode == FixedSizeRemoveModes.Back && index >= (this.Count - 1)) ||
          (FixedSizeRemoveMode == FixedSizeRemoveModes.Front && index <= 0)
        )
      ) {
        return;
      }
      if (!Unique || !this.Contains(itm)) {
        if (RaiseListItemValidCheck(itm)) {
          int oldCount = this.Count;
          base.Insert(index, itm);
          T[] removedItems = RemoveFixedSizeOverflow();
          ListChangedEventArgs e = new ListChangedEventArgs(
            new T[] { itm },
            removedItems,
            1,
            removedItems.Length,
            this.Count,
            oldCount,
            oldCount != this.Count
          );
          RaiseListChangedEvent(e);
        }
      }
    }

    public new void InsertRange(int index, IEnumerable<T> collection) {
      int oldCount = this.Count;
      T[] newItems = collection.Where(itm => {
        if (!Unique || !this.Contains(itm)) {
          if (RaiseListItemValidCheck(itm)) {
            return true;
          }
        }
        return false;
      }).ToArray();
      base.InsertRange(index, newItems);
      T[] removedItems = RemoveFixedSizeOverflow();
      ListChangedEventArgs e = new ListChangedEventArgs(
        newItems,
        removedItems,
        newItems.Length,
        removedItems.Length,
        this.Count,
        oldCount,
        oldCount != this.Count
      );
      RaiseListChangedEvent(e);
    }

    public new void RemoveAt(int index) {
      if (index >= 0 && index < this.Count) {
        int oldCount = this.Count;
        T itm = this[index];
        base.RemoveAt(index);
        ListChangedEventArgs e = new ListChangedEventArgs(
          Empty,
          new T[] { itm },
          0,
          1,
          this.Count,
          oldCount,
          oldCount != this.Count
        );
        RaiseListChangedEvent(e);
      }
    }

    public new void AddRange(IEnumerable<T> collection) {
      int oldCount = this.Count;
      T[] newItems = collection.Where(itm => {
        if (!Unique || !this.Contains(itm)) {
          if (RaiseListItemValidCheck(itm)) {
            return true;
          }
        }
        return false;
      }).ToArray();
      base.AddRange(newItems);
      T[] removedItems = RemoveFixedSizeOverflow();
      ListChangedEventArgs e = new ListChangedEventArgs(
        newItems,
        removedItems,
        newItems.Length,
        removedItems.Length,
        this.Count,
        oldCount,
        oldCount != this.Count
      );
      RaiseListChangedEvent(e);
    }

    public new void RemoveRange(int index, int count) {
      int oldCount = this.Count;
      T[] removedItems = base.GetRange(index, count).ToArray();
      base.RemoveRange(index, count);
      ListChangedEventArgs e = new ListChangedEventArgs(
        Empty,
        removedItems,
        0,
        removedItems.Length,
        this.Count,
        oldCount,
        oldCount != this.Count
      );
      RaiseListChangedEvent(e);
    }

    public new int RemoveAll(Predicate<T> predicate) {
      int oldCount = this.Count;
      T[] removedItems = this.Where(itm => predicate(itm)).ToArray();
      int r = base.RemoveAll(predicate);
      ListChangedEventArgs e = new ListChangedEventArgs(
        Empty,
        removedItems,
        0,
        removedItems.Length,
        this.Count,
        oldCount,
        oldCount != this.Count
      );
      RaiseListChangedEvent(e);
      return r;
    }

    public new void Reverse() {
      base.Reverse();
      RaiseListChangedEvent(new ListChangedEventArgs(Empty, Empty, 0, 0, this.Count, this.Count, false));
    }

    public new void Reverse(int index, int count) {
      base.Reverse(index, count);
      RaiseListChangedEvent(new ListChangedEventArgs(Empty, Empty, 0, 0, this.Count, this.Count, false));
    }

    public new void Sort() {
      base.Sort();
      RaiseListChangedEvent(new ListChangedEventArgs(Empty, Empty, 0, 0, this.Count, this.Count, false));
    }
    public new void Sort(Comparison<T> comparison) {
      base.Sort(comparison);
      RaiseListChangedEvent(new ListChangedEventArgs(Empty, Empty, 0, 0, this.Count, this.Count, false));
    }
    public new void Sort(IComparer<T> comparer) {
      base.Sort(comparer);
      RaiseListChangedEvent(new ListChangedEventArgs(Empty, Empty, 0, 0, this.Count, this.Count, false));
    }
    public new void Sort(int index, int count, IComparer<T> comparer) {
      base.Sort(index, count, comparer);
      RaiseListChangedEvent(new ListChangedEventArgs(Empty, Empty, 0, 0, this.Count, this.Count, false));
    }

    public new void TrimExcess() {
      int oldCount = this.Count;
      T[] removedItems = this.ToArray();
      base.TrimExcess();
      removedItems = removedItems.Where(itm => !this.Contains(itm)).ToArray();
      ListChangedEventArgs e = new ListChangedEventArgs(
        Empty,
        removedItems,
        0,
        removedItems.Length,
        this.Count,
        oldCount,
        oldCount != this.Count
      );
      RaiseListChangedEvent(e);
    }
  }
}
