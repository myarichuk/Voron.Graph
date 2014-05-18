using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.Impl
{
    public class ItemChangeTracker<TItem> : IChangeTracking
        where TItem : class,IChangeTracking
    {
        private TItem _originalItem;
        private TItem _currentItem;

        public TItem CurrentItem
        {
            get
            {
                return _currentItem;
            }
        }

        public TItem OriginalItem
        {
            get
            {
                return _originalItem;
            }
        }

        public ItemChangeTracker(TItem item)
        {
            _currentItem = item;
            _originalItem = Util.DeepClone(item);
        }

        public void AcceptChanges()
        {
            _currentItem.AcceptChanges();
            _originalItem = Util.DeepClone(_currentItem);
        }

        public bool IsChanged
        {
            get { return _currentItem.IsChanged; }
        }
    }
}
