using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.Impl
{
    public class ChangeTracker<TItem>
        where TItem : class, IChangeTracking
    {
        public delegate bool ChangeHandler(TItem original, TItem current);
        public delegate bool ItemComparer(TItem original, TItem current);

        private readonly ItemComparer _itemComparer;
        private readonly ChangeHandler _update;
        private readonly ChangeHandler _delete;
        private readonly ConcurrentBag<ItemChangeTracker<TItem>> _trackedItems;

        public ChangeTracker(ItemComparer itemComparer, ChangeHandler update, ChangeHandler delete)
        {
            _itemComparer = itemComparer;
            _update = update;
            _delete = delete;
            _trackedItems = new ConcurrentBag<ItemChangeTracker<TItem>>();
        }

        public void Track(TItem item)
        {
            _trackedItems.Add(new ItemChangeTracker<TItem>(item));
        }

        public void ResetTracking()
        {
            foreach (var item in _trackedItems)
                item.AcceptChanges();
        }

        public void Persist()
        {
            var dirtyItems = _trackedItems.Where(item => item.IsChanged);
            foreach(var item in dirtyItems)
            {
                
            }
        }
    }
}
