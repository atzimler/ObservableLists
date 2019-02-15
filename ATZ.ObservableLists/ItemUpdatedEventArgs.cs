using System;

namespace ATZ.ObservableLists
{
    public class ItemUpdatedEventArgs : EventArgs
    {
        public int Index { get; }

        public ItemUpdatedEventArgs(int index)
        {
            Index = index;
        }
    }
}