using System.Collections.Specialized;

namespace ATZ.ObservableLists.Tests
{
    // ReSharper disable UnusedMember.Global => API expectations for deriving ObservableList<T>.
    internal class AllowOverridingOfOnCollectionChangedInObservableList : ObservableList<int>
        // ReSharper restore UnusedMember.Global
    {
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
        }
    }
    
}