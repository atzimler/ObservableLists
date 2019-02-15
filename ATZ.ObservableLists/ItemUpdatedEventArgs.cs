using System;

namespace ATZ.ObservableLists
{
    /// <summary>
    /// Event argument when signalling that an item in the list has been declared to have its state changed while
    /// the item remains at the exact same position in the list. This means that the list itself has no changed
    /// references.
    /// </summary>
    /// <remarks>
    /// This event has been added beside the standard INotifyCollectionChanged interface, because usually developers
    /// try to solve this problem by removing and re-adding the item in question to the list, which in turns may cause
    /// extensive reactions to the changes.
    /// </remarks>
    public class ItemUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// The index of the item changing its state.
        /// </summary>
        public int Index { get; }

        /// <param name="index">The index of the item changing its state.</param>
        public ItemUpdatedEventArgs(int index)
        {
            Index = index;
        }
    }
}