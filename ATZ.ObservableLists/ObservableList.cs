using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;

namespace ATZ.ObservableLists
{
    /// <summary>
    /// List allowing observation of changes via INotifyCollectionChanged. During the collection changed event handler
    /// code is allowed to make requests for further changes. These requests will be processed in sequence
    /// after the original handler for the first change request executed fully, but only if the conditions are still valid
    /// for the change. If values of items to be updated have been changed, item has been moved away or positions became invalid
    /// the change request made is ignored and the next change request will be processed until the list settles into a state
    /// where no further change requests are present.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    // ReSharper disable once InheritdocConsiderUsage => Additional explanation needed for the class because implements different functionality.
    public class ObservableList<T> : IReadOnlyList<T>, IList, IList<T>, INotifyCollectionChanged, INotifyPropertyChanged
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

        /// <inheritdoc cref="IList&lt;T&gt;" />
        public T this[int index]
        {
            get => _items[index];
            set => SetAt(index, value);
        }

        /// <inheritdoc cref="IList&lt;T&gt;" />
        public int Count => _items.Count;
        
        /// <inheritdoc />
        public bool IsFixedSize => ((IList)_items).IsFixedSize;
        
        /// <inheritdoc cref="ICollection&lt;T&gt;" />
        public bool IsReadOnly => ((ICollection<T>)_items).IsReadOnly;
        
        /// <inheritdoc />
        public bool IsSynchronized => ((ICollection)_items).IsSynchronized;
        
        /// <summary>
        /// The original request starting the chain of events to be processed.
        /// </summary>
        /// <remarks>
        /// This property can be used to distinguish between change process cycles.
        /// </remarks>
        public NotifyCollectionChangedEventArgs OriginalRequest => _originalRequest;
        
        /// <summary>
        /// Gets an object that can be used to synchronize access to the ICollection.
        /// </summary>
        public object SyncRoot => ((ICollection)_items).SyncRoot;

        /// <inheritdoc />
        public event NotifyCollectionChangedEventHandler CollectionChanged = delegate {  };
        
        /// <summary>
        /// Occurs when an item is declared to have changed state via call to ItemUpdate or ItemUpdateAt functions.
        /// </summary>
        public event EventHandler<ItemUpdatedEventArgs> ItemUpdated = delegate { };

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        
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

        private static T AssertArgumentIsOfTypeT(object item)
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
                var originalCount = _items.Count;
                
                while (_changes.Count > 0)
                {
                    ProcessChange();
                }

                ProcessCountPropertyChanged(originalCount);
            }
            finally
            {
                _originalRequest = null;
            }
        }

        private void ProcessCountPropertyChanged(int originalCount)
        {
            if (originalCount != _items.Count)
            {
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            }
        }
        
        /// <summary>
        /// Raises the CollectionChanged event with the provided arguments.
        /// </summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged(this, e);
        }

        /// <summary>
        /// Raises the ItemUpdated event with the provided arguments.
        /// </summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected virtual void OnItemUpdated(ItemUpdatedEventArgs e)
        {
            ItemUpdated(this, e);
        }

        /// <summary>
        /// Raises the PropertyChanged event with the provided arguments.
        /// </summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged(this, e);
        }

        private void SetAt(int index, T value) 
            => ProcessChanges(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, _items[index], index));
        
        int IList.Add(object item)
        {
            Add(AssertArgumentIsOfTypeT(item));

            return Count - 1;
        }

        /// <inheritdoc />
        /// <remarks>The index of the newly added item is determined on addition, so if in a collection change handler
        /// the number of items grows before the change is applied, the item is still inserted at the end of the
        /// ObservableList&lt;T&gt; when the change is applied.</remarks>
        public void Add(T item) => ProcessChanges(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));

        /// <inheritdoc cref="IList&lt;T&gt;" />
        public void Clear() => ProcessChanges(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

        /// <inheritdoc />
        public bool Contains(T item) => _items.Contains(item);
        bool IList.Contains(object item) => ((IList)_items).Contains(item);

        /// <inheritdoc />
        public void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

        /// <inheritdoc />
        public void CopyTo(T[] array, int index) => _items.CopyTo(array, index);

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        /// <inheritdoc />
        public int IndexOf(T item) => _items.IndexOf(item);
        int IList.IndexOf(object item) => ((IList)_items).IndexOf(item);

        /// <inheritdoc />
        public void Insert(int index, T item) => ProcessChanges(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item , index));
        void IList.Insert(int index, object item) => Insert(index, AssertArgumentIsOfTypeT(item));

        /// <summary>
        /// Initiate an ItemUpdated event.
        /// </summary>
        /// <param name="item">The item that changed its state.</param>
        public void ItemUpdate(T item)
        {
            ItemUpdateAt(_items.IndexOf(item));
        }
        
        /// <summary>
        /// Initiate an ItemUpdated event.
        /// </summary>
        /// <param name="index">The index of the item that changed its state.</param>
        public void ItemUpdateAt(int index)
        {
            if (index < 0 || _items.Count <= index)
            {
                return;
            }
            
            OnItemUpdated(new ItemUpdatedEventArgs(index));
        }
        
        /// <summary>
        /// Moves the item at the specified index to a new location in the collection.
        /// </summary>
        /// <param name="oldIndex">The zero-based index specifying the location of the old item to be moved.</param>
        /// <param name="newIndex">The zero-based index specifying the new location of the item.</param>
        /// <remarks>If the item is not found at the specified oldIndex position when the request is processed, the request will be ignored.
        /// The request is also ignored if oldIndex == newIndex at the time of initiating the request.</remarks>
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
        
        /// <inheritdoc />
        /// <remarks><see cref="RemoveAt" /></remarks>
        public bool Remove(T item)
        {
            var index = _items.IndexOf(item);
            if (index != -1)
            {
                RemoveAt(index);
            }

            return index != -1;
        }

        /// <inheritdoc cref="IList&lt;T&gt;" />
        /// <remarks>If the item has been moved from the requested position or has been replaced or removed before the
        /// change request is processed, the request will be ignored.</remarks>
        public void RemoveAt(int index) => ProcessChanges(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, _items[index], index)); 
    }
}