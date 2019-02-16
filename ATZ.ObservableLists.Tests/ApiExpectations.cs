using System.Collections.Specialized;
using System.ComponentModel;

namespace ATZ.ObservableLists.Tests
{
    // ReSharper disable UnusedMember.Global => API expectations for deriving ObservableList<T>.
    internal class AllowOverridingOfOnCollectionChangedInObservableList : ObservableList<int>
        // ReSharper restore UnusedMember.Global
    {
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
        }

        protected override void OnItemUpdated(ItemUpdatedEventArgs e)
        {
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
        }
    }
    
}