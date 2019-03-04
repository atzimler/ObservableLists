using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using FluentAssertions;
using NUnit.Framework;

namespace ATZ.ObservableLists.Tests
{
    [TestFixture]
    public class ObservableListShould
    {
        #region ICollection<T>
        [Test]
        public void NotContainItemNotAdded()
        {
            // ReSharper disable once CollectionNeverUpdated.Local => Checking default state.
            var ol = new ObservableList<int>();
            ol.Contains(13).Should().BeFalse();
        }

        [Test]
        public void ContainAddedItem()
        {
            var ol = new ObservableList<int> { 42 };
            ol.Contains(42).Should().BeTrue();
        }

        [Test]
        public void HaveZeroAsDefaultCount()
        {
            // ReSharper disable once CollectionNeverUpdated.Local => Checking default state.
            var ol = new ObservableList<int>();
            ol.Count.Should().Be(0);
        }

        [Test]
        public void AddingItemIncreasesCount()
        {
            var ol = new ObservableList<int> { 42 };
            ol.Count.Should().Be(1);
        }

        [Test]
        public void ClearItemsCorrectly()
        {
            var ol = new ObservableList<int> { 42 };

            ol.Clear();
            ol.Count.Should().Be(0);
        }

        [Test]
        public void RemoveItemCorrectly()
        {
            var ol = new ObservableList<int> { 42, 13 };

            ol.Remove(13);
            ol.Count.Should().Be(1);
            ol.Contains(42).Should().BeTrue();
            ol.Contains(13).Should().BeFalse();
        }

        [Test]
        public void CopyToArrayCorrectly()
        {
            var ol = new ObservableList<int> { 42, 13 };

            var items = new int[2];
            ol.CopyTo(items, 0);
            items.Should().Contain(new[] { 13, 42 }).And.HaveCount(2);
        }
        #endregion
        
        #region ICollection
        [Test]
        public void CopyToGenericArrayCorrectly()
        {
            var ol = new ObservableList<int> { 42, 13 };

            Array items = new int[2];
            ol.CopyTo(items, 0);
            items.Should().Contain(new[] { 13, 42 }).And.HaveCount(2);
        }
        #endregion

        #region IReadOnlyList<T>
        [Test]
        public void BeAbleToReturnItemByIndex()
        {
            var ol = new ObservableList<int> { 42 };

            ol[0].Should().Be(42);
        }
        #endregion
        
        #region IList
        [Test]
        public void ThrowProperExceptionWhenAddingObjectWithIncorrectType()
        {
            foreach (var obj in new object[] { "x", 13.42 })
            {
                var comparisonList = new List<int>();
                var correctException = Assert.Throws<ArgumentException>(() => ((IList)comparisonList).Add(obj));
            
                var ol = new ObservableList<int>();
                var ex = Assert.Throws<ArgumentException>(() => ((IList)ol).Add(obj));
                ex.Message.Should().Be(correctException.Message);
                ex.ParamName.Should().Be(correctException.ParamName);
            }
        }

        [Test]
        public void InsertItemCorrectly()
        {
            var ol = new ObservableList<int>();
            ((IList)ol).Insert(0, 42);

            ol.Count.Should().Be(1);
            ol[0].Should().Be(42);
        }

        [Test]
        public void IgnoreRemovalRequestOfIncorrectType()
        {
            var ol = new ObservableList<int?> { null };
            ((IList)ol).Remove(13.42);

            ol.Count.Should().Be(1);
            ol[0].Should().BeNull();
        }

        [Test]
        public void RemoveItemWithCorrectTypePresentInTheList()
        {
            var ol = new ObservableList<int> { 13, 42 };
            ((IList)ol).Remove(13);

            ol.Count.Should().Be(1);
            ol[0].Should().Be(42);
        }

        [Test]
        public void RemoveItemAtSpecifiedIndex()
        {
            var ol = new ObservableList<int> { 8, 13, 42 };
            ol.RemoveAt(1);

            ol.Should().ContainInOrder(new[] { 8, 42 }).And.HaveCount(2);
        }
        #endregion
        
        #region INotifyCollectionChanged
        [Test]
        public void NotifyWhenAddingItemToTheList()
        {
            var ol = new ObservableList<int> { 12, 43 };

            var monitor = ol.Monitor();
            ol.CollectionChanged += (o, e) =>
            {
                e.Action.Should().Be(NotifyCollectionChangedAction.Add);
                e.NewStartingIndex.Should().Be(1);
                e.NewItems[0].Should().Be(42);
            };
            
            ol.Insert(1, 42);
            monitor.Should().Raise(nameof(ol.CollectionChanged));
        }

        [Test]
        public void NotifyWhenRemovingItemFromTheList()
        {
            var ol = new ObservableList<int> { 12, 13, 42 };

            var monitor = ol.Monitor();
            ol.CollectionChanged += (o, e) =>
            {
                e.Action.Should().Be(NotifyCollectionChangedAction.Remove);
                e.OldStartingIndex.Should().Be(1);
                e.OldItems[0].Should().Be(13);
            };
            
            ol.RemoveAt(1);
            monitor.Should().Raise(nameof(ol.CollectionChanged));
        }

        [Test]
        public void NotifyWhenRemovingItemFromTheListByReference()
        {
            var ol = new ObservableList<int> { 12, 13, 42 };

            var monitor = ol.Monitor();
            ol.CollectionChanged += (o, e) =>
            {
                e.Action.Should().Be(NotifyCollectionChangedAction.Remove);
                e.OldStartingIndex.Should().Be(1);
                e.OldItems[0].Should().Be(13);
            };

            ol.Remove(13);
            monitor.Should().Raise(nameof(ol.CollectionChanged));
        }

        [Test]
        public void NotifyWhenReplacingItemInTheList()
        {
            var ol = new ObservableList<int> { 8, 13, 42 };

            var monitor = ol.Monitor();
            ol.CollectionChanged += (o, e) =>
            {
                e.Action.Should().Be(NotifyCollectionChangedAction.Replace);
                e.NewStartingIndex.Should().Be(1);
                e.OldStartingIndex.Should().Be(1);
                e.NewItems[0].Should().Be(12);
                e.OldItems[0].Should().Be(13);
            };

            ol[1] = 12;
            monitor.Should().Raise(nameof(ol.CollectionChanged));
        }

        [Test]
        public void NotifyWhenClearingTheList()
        {
            var ol = new ObservableList<int> { 4, 13 };
            ol.CollectionChanged += (o, e) => { e.Action.Should().Be(NotifyCollectionChangedAction.Reset); };

            var monitor = ol.Monitor();
            ol.Clear();

            monitor.Should().Raise(nameof(ol.CollectionChanged));
        }

        [Test]
        public void NotifyWhenMovingItemInTheList()
        {
            var ol = new ObservableList<int> { 42, 12 };
            ol.CollectionChanged += (o, e) =>
            {
                e.Action.Should().Be(NotifyCollectionChangedAction.Move);
                e.OldItems[0].Should().Be(42);
                e.NewItems[0].Should().Be(42);
                e.OldStartingIndex.Should().Be(0);
                e.NewStartingIndex.Should().Be(1);
            };

            var monitor = ol.Monitor();
            ol.Move(0, 1);

            monitor.Should().Raise(nameof(ol.CollectionChanged));
        }

        [Test]
        public void MoveItemLowerCorrectly()
        {
            var ol = new ObservableList<int> { 2, 3, 1 };
            ol.Move(2, 0);

            ol.Should().ContainInOrder(1, 2, 3).And.HaveCount(3);
        }

        [Test]
        public void MoveItemHigherCorrectly()
        {
            var ol = new ObservableList<int> { 3, 1, 2 };
            ol.Move(0, 2);

            ol.Should().ContainInOrder(1, 2, 3).And.HaveCount(3);
        }

        [Test]
        public void NotRaiseCollectionChangedWhenItemIsMovedToItsCurrentPosition()
        {
            var ol = new ObservableList<int> { 1, 2, 3 };

            var monitor = ol.Monitor();
            ol.Move(1, 1);
            ol.Should().ContainInOrder(1, 2, 3).And.HaveCount(3);
            
            monitor.Should().NotRaise(nameof(ol.CollectionChanged));
        }

        [Test]
        public void NotReplaceItemIfItHasBeenChangedWhileNotifyCollectionChangedWasProcessed()
        {
            var ol = new ObservableList<int> { 1, 2, 3 };
            var changeHandlerExecuted = false;

            ol.CollectionChanged += (o, e) =>
            {
                if (changeHandlerExecuted)
                {
                    return;
                }
                
                ol.Move(3, 0);
                ol[0] = 4;
                changeHandlerExecuted = true;
            };
            
            ol.Add(0);

            ol.Should().ContainInOrder(0, 1, 2, 3).And.HaveCount(4);
        }

        [Test]
        public void NotMoveItemIfItHasBeenReplacedWhileNotifyCollectionChangedWasProcessed()
        {
            var ol = new ObservableList<int> { 1, 2, 3 };
            var changeHandlerExecuted = false;

            ol.CollectionChanged += (o, e) =>
            {
                if (changeHandlerExecuted)
                {
                    return;
                }

                ol[0] = 0;
                ol.Move(0, 1);
                changeHandlerExecuted = true;
            };
            
            ol.Insert(0, 1);

            ol.Should().ContainInOrder(0, 1, 2, 3).And.HaveCount(4);
        }

        [Test]
        public void NotRemoveItemIfItHasBeenReplacedWhileNotifyCollectionChangedWasProcessed()
        {
            var ol = new ObservableList<int> { 1, 1, 2, 2 };
            var changeHandlerExecuted = false;

            ol.CollectionChanged += (o, e) =>
            {
                if (changeHandlerExecuted)
                {
                    return;
                }

                ol[3] = 3;
                ol.RemoveAt(3);
                changeHandlerExecuted = true;
            };

            ol[0] = 0;

            ol.Should().ContainInOrder(0, 1, 2, 3).And.HaveCount(4);
        }
        
        [Test]
        public void NotInsertItemIfLocationBecameInvalidWhileNotifyCollectionChangedWasProcessed()
        {
            var ol = new ObservableList<int> { 1, 2, 3, 4, 5 };
            var changeHandlerExecuted = false;

            ol.CollectionChanged += (o, e) =>
            {
                if (changeHandlerExecuted)
                {
                    return;
                }

                ol.RemoveAt(3);
                ol.Insert(4, 5);
                changeHandlerExecuted = true;
            };

            ol.RemoveAt(4);
            ol.Should().ContainInOrder(1, 2, 3).And.HaveCount(3);
        }

        [Test]
        public void NotMoveItemIfTargetLocationBecameInvalidWhileNotifyCollectionChangedWasProcessed()
        {
            var ol = new ObservableList<int> { 1, 2, 3, 4 };
            var changeHandlerExecuted = false;

            ol.CollectionChanged += (o, e) =>
            {
                if (changeHandlerExecuted)
                {
                    return;
                }

                ol.RemoveAt(2);
                ol.Move(0, 2);
                changeHandlerExecuted = true;
            };
            
            ol.RemoveAt(3);
            ol.Should().ContainInOrder(1, 2).And.HaveCount(2);
        }

        [Test]
        public void AllowMultipleAddsDuringNotifyCollectionChangedProcessing()
        {
            var ol = new ObservableList<int>();
            var changeHandlerExecuted = false;

            ol.CollectionChanged += (o, e) =>
            {
                if (changeHandlerExecuted)
                {
                    return;
                }

                ol.RemoveAt(0);
                ol.Add(1);
                ol.Add(2);
                ol.Add(3);
                changeHandlerExecuted = true;
            };
            
            ol.Add(5);
            ol.Should().ContainInOrder(1, 2, 3).And.HaveCount(3);
        }

        [Test]
        public void SignalCorrectItemUpdateByIndexIfIndexIsValid()
        {
            var ol = new ObservableList<int> { 42 };
            ol.ItemUpdated += (o, e) => e.Index.Should().Be(0);

            var monitor = ol.Monitor();
            ol.ItemUpdateAt(0);

            monitor.Should().Raise(nameof(ol.ItemUpdated));
        }

        [Test]
        public void NotSignalItemUpdateByIndexIfIndexIsInvalid()
        {
            var ol = new ObservableList<int>();

            var monitor = ol.Monitor();
            ol.ItemUpdateAt(-1);
            
            monitor.Should().NotRaise(nameof(ol.ItemUpdated));
            monitor.Clear();
            
            ol.ItemUpdateAt(0);
            monitor.Should().NotRaise(nameof(ol.ItemUpdated));
        }
        
        [Test]
        public void SignalCorrectItemUpdateByObjectIfPresentInTheList()
        {
            var ol = new ObservableList<int> { 42 };
            ol.ItemUpdated += (o, e) => e.Index.Should().Be(0);

            var monitor = ol.Monitor();
            ol.ItemUpdate(42);

            monitor.Should().Raise(nameof(ol.ItemUpdated));
        }

        [Test]
        public void NotSignalItemUpdatedByObjectIfNotPresentInTheList()
        {
            var ol = new ObservableList<int> { 42 };

            var monitor = ol.Monitor();
            ol.ItemUpdate(0);
            
            monitor.Should().NotRaise(nameof(ol.ItemUpdated));
        }
        #endregion
        
        #region INotifyPropertyChanged
        [Test]
        public void RaisePropertyChangedWhenItemIsAdded()
        {
            var ol = new ObservableList<int>();

            var monitor = ol.Monitor();
            ol.Add(42);

            monitor.Should().RaisePropertyChangeFor(_ => _.Count);
        }

        [Test]
        public void RaisePropertyChangedWhenItemIsRemoved()
        {
            var ol = new ObservableList<int> { 42 };

            var monitor = ol.Monitor();
            ol.Remove(42);

            monitor.Should().RaisePropertyChangeFor(_ => _.Count);
        }

        [Test]
        public void NotRaisePropertyChangedWhenItemIsMoved()
        {
            var ol = new ObservableList<int> { 42, 12 };

            var monitor = ol.Monitor();
            ol.Move(1, 0);
            
            monitor.Should().NotRaisePropertyChangeFor(_ => _.Count);
        }
        
        [Test]
        public void NotRaisePropertyChangedWhenItemAdditionTriggersRemoval()
        {
            var ol = new ObservableList<int>();
            var changeHandlerExecuted = false;

            ol.CollectionChanged += (o, e) =>
            {
                if (changeHandlerExecuted)
                {
                    return;
                }

                ol.RemoveAt(0);
                changeHandlerExecuted = true;
            };

            var monitor = ol.Monitor();
            ol.Add(5);
            
            monitor.Should().NotRaisePropertyChangeFor(_ => _.Count);
        }

        #endregion
        
        
        #region OriginalRequest
        private NotifyCollectionChangedEventArgs _correctOriginalRequest;
        
        private void FirstOriginalRequestVerification(object sender, NotifyCollectionChangedEventArgs e)
        {
            var ol = (ObservableList<int>)sender;

            ol.OriginalRequest.Should().NotBeNull();
            _correctOriginalRequest = ol.OriginalRequest;

            ol.CollectionChanged -= FirstOriginalRequestVerification;
            ol.CollectionChanged += SecondOriginalRequestVerification;
            ol.Add(42);
        }

        private void SecondOriginalRequestVerification(object sender, NotifyCollectionChangedEventArgs e)
        {
            var ol = (ObservableList<int>)sender;

            ol.OriginalRequest.Should().Be(_correctOriginalRequest);
        }

        private void CollectionChangedProcessed(object sender, PropertyChangedEventArgs e)
        {
            var ol = (ObservableList<int>)sender;

            ol.OriginalRequest.Should().Be(_correctOriginalRequest);
        }
        
        [Test]
        public void SetTheOriginalRequestCorrectlyAtVariousStagesOfTheEventProcessingCycle()
        {
            var ol = new ObservableList<int>();

            ol.CollectionChanged += FirstOriginalRequestVerification;
            ol.PropertyChanged += CollectionChangedProcessed;

            ol.OriginalRequest.Should().BeNull();

            ol.Add(12);
            ol.OriginalRequest.Should().BeNull();
        }
        
        #endregion
    }
}