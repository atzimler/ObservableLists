using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;

namespace ATZ.ObservableLists
{
    public class ObservableList<T> 
        : IReadOnlyList<T>, IList, IList<T>, INotifyCollectionChanged
        //        : IList<T>, IList, IReadOnlyList<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly Queue<NotifyCollectionChangedEventArgs> _changes = new Queue<NotifyCollectionChangedEventArgs>();
        private readonly EqualityComparer<T> _equalityComparer = EqualityComparer<T>.Default;
        private readonly List<T> _items = new List<T>();
        private NotifyCollectionChangedEventArgs _originalRequest;

        object IList.this[int index]
        {
            get => _items[index];
            set => SetAt(index, AssertArgumentIsOfTypeT(value));
        }

        public T this[int index]
        {
            get => _items[index];
            set => SetAt(index, value);
        }

        public int Count => _items.Count;
        public bool IsFixedSize => ((IList)_items).IsFixedSize;
        public bool IsReadOnly => ((ICollection<T>)_items).IsReadOnly;
        public bool IsSynchronized => ((ICollection)_items).IsSynchronized;
        public NotifyCollectionChangedEventArgs OriginalRequest => _originalRequest;
        public object SyncRoot => ((ICollection)_items).SyncRoot;

        public event NotifyCollectionChangedEventHandler CollectionChanged = delegate {  };
        public event EventHandler<ItemUpdatedEventArgs> ItemUpdated = delegate { }; 

        private NotifyCollectionChangedEventArgs ApplyChange(NotifyCollectionChangedEventArgs e)
        {
            if (IsObsoleteRequest(e))
            {
                return null;
            }

            ApplyReset(e);
            ApplyReplace(e);
            ApplyRemoveAt(e);
            return ApplyInsertAt(e);
        }

        private NotifyCollectionChangedEventArgs ApplyInsertAt(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewStartingIndex == -1)
            {
                e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems[0], _items.Count);
            }
            
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Move)
            {
                _items.Insert(e.NewStartingIndex, (T)e.NewItems[0]);
            }

            return e;
        }

        private void ApplyRemoveAt(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Move || e.Action == NotifyCollectionChangedAction.Remove)
            {
                _items.RemoveAt(e.OldStartingIndex);
            }
        }

        private void ApplyReplace(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                _items[e.OldStartingIndex] = (T)e.NewItems[0];
            }
        }

        private void ApplyReset(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _items.Clear();
            }
        }

        private T AssertArgumentIsOfTypeT(object item)
        {
            try
            {
                return (T)item;
            }
            catch (InvalidCastException)
            {
                // ReSharper disable once NotResolvedInText => Behaving exactly as the .NET Framework.
                throw new ArgumentException(
                    $@"The value ""{Convert.ToString(item, CultureInfo.InvariantCulture)}"" is not of type ""{typeof(T)}"" and cannot be used in this generic collection.", 
                    "value");
            }
        }

        private bool IsObsoleteRequest(NotifyCollectionChangedEventArgs e)
            => !OldItemIsValid(e) || !NewPositionIsValidForInsert(e) || !NewPositionIsValidForMove(e);
        
        private bool NewPositionIsValidForInsert(NotifyCollectionChangedEventArgs e)
            => e.Action != NotifyCollectionChangedAction.Add || e.NewStartingIndex <= _items.Count;

        private bool NewPositionIsValidForMove(NotifyCollectionChangedEventArgs e)
            => e.Action != NotifyCollectionChangedAction.Move || e.NewStartingIndex < _items.Count;
        
        private bool OldItemHasNotChanged(NotifyCollectionChangedEventArgs e)
            => e.OldStartingIndex < _items.Count && _equalityComparer.Equals(_items[e.OldStartingIndex], (T)e.OldItems[0]);

        private bool OldItemIsValid(NotifyCollectionChangedEventArgs e)
            => e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Reset || OldItemHasNotChanged(e); 

        
        private void ProcessChange()
        {
            var requestedChange = _changes.Dequeue();
            var appliedChange = ApplyChange(requestedChange);
            if (appliedChange != null)
            {
                OnCollectionChanged(appliedChange);
            }
        }
        
        private void ProcessChanges(NotifyCollectionChangedEventArgs e)
        {
            _changes.Enqueue(e);
            if (_originalRequest != null)
            {
                return;
            }

            try
            {
                _originalRequest = e;
                while (_changes.Count > 0)
                {
                    ProcessChange();
                }
            }
            finally
            {
                _originalRequest = null;
            }
        }
        
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged(this, e);
        }

        protected virtual void OnItemUpdated(ItemUpdatedEventArgs e)
        {
            ItemUpdated(this, e);
        }

        private void SetAt(int index, T value) 
            => ProcessChanges(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, _items[index], index));
        
        int IList.Add(object item)
        {
            Add(AssertArgumentIsOfTypeT(item));

            return Count - 1;
        }

        public void Add(T item) => ProcessChanges(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));

        public void Clear() => ProcessChanges(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

        bool IList.Contains(object item) => ((IList)_items).Contains(item);
        public bool Contains(T item) => _items.Contains(item);

        public void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        public void CopyTo(T[] array, int index) => _items.CopyTo(array, index);

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

        int IList.IndexOf(object item) => ((IList)_items).IndexOf(item);
        public int IndexOf(T item) => _items.IndexOf(item);

        void IList.Insert(int index, object item) => Insert(index, AssertArgumentIsOfTypeT(item));
        public void Insert(int index, T item) => ProcessChanges(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item , index));

        public void ItemUpdate(T item)
        {
            ItemUpdateAt(_items.IndexOf(item));
        }
        
        public void ItemUpdateAt(int index)
        {
            if (index < 0 || _items.Count <= index)
            {
                return;
            }
            
            OnItemUpdated(new ItemUpdatedEventArgs(index));
        }
        
        public void Move(int oldIndex, int newIndex)
        {
            if (oldIndex == newIndex)
            {
                return;
            }
            
            ProcessChanges(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, _items[oldIndex], newIndex, oldIndex));   
        }
        
        void IList.Remove(object item)
        {
            if (item is T x)
            {
                Remove(x);
            }
        }
        
        public bool Remove(T item)
        {
            var index = _items.IndexOf(item);
            if (index != -1)
            {
                RemoveAt(index);
            }

            return index != -1;
        }

        public void RemoveAt(int index) => ProcessChanges(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, _items[index], index)); 
        
//        public event PropertyChangedEventHandler PropertyChanged;
    }
}