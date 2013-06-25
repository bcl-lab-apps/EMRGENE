// (c) Microsoft. All rights reserved

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using HealthVault.ItemTypes;
using HealthVault.Types;
using Windows.Foundation;
using HealthVault.Foundation;

namespace HealthVault.Store
{
    public sealed class LocalItemStore
    {
        private readonly IObjectStore m_objectStore;
        private CrossThreadLock m_lock;

        internal LocalItemStore(IObjectStore store, ICache<string, object> cache)
        {
            m_lock = new CrossThreadLock(false);

            if (cache != null)
            {
                m_objectStore = new CachingObjectStore(store, cache);
            }
            else
            {
                m_objectStore = store;
            }
        }

        public IAsyncOperation<IList<string>> GetItemIDsAsync()
        {
            return AsyncInfo.Run(
                async cancelToken =>
                      {
                          using (await CrossThreadLockScope.Enter(m_lock))
                          {
                              return await m_objectStore.GetAllKeysAsync();
                          }
                      });
        }

        public IAsyncOperation<IItemDataTyped> GetAsync(ItemKey key)
        {
            key.ValidateRequired("key");

            return GetAsyncImpl(key).AsAsyncOperation();
        }

        public IAsyncOperation<IList<IItemDataTyped>> GetMultipleAsync(IEnumerable<ItemKey> keys)
        {
            return GetMultipleAsyncImpl(keys, false).AsAsyncOperation();
        }

        public IAsyncOperation<IItemDataTyped> GetByIDAsync(string itemID)
        {
            if (string.IsNullOrEmpty(itemID))
            {
                throw new ArgumentException("itemID");
            }

            return GetByIDAsyncImpl(itemID).AsAsyncOperation();
        }

        public IAsyncOperation<RecordItem> GetItemAsync(ItemKey key)
        {
            key.ValidateRequired("key");

            return GetItemAsyncImpl(key).AsAsyncOperation();
        }

        public IAsyncOperation<RecordItem> RefreshAndGetItemAsync(ItemKey key)
        {
            key.ValidateRequired("key");

            return RefreshAndGetItemAsyncImpl(key).AsAsyncOperation();
        }

        public IAsyncOperation<IList<RecordItem>> GetItemsAsync(IEnumerable<ItemKey> keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }

            return GetItemsAsyncImpl(keys, false).AsAsyncOperation();
        }

        public IAsyncOperation<RecordItem> GetItemByIDAsync(string itemID)
        {
            if (string.IsNullOrEmpty(itemID))
            {
                throw new ArgumentException("itemID");
            }

            return GetItemByIDAsyncImpl(itemID).AsAsyncOperation();
        }

        public IAsyncAction PutAsync(IItemDataTyped item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            return PutItemAsync(item.Item);
        }

        public IAsyncAction PutMultipleAsync(IEnumerable<IItemDataTyped> items)
        {
            if (items == null)
            {
                throw new ArgumentException("items");
            }

            IEnumerable<RecordItem> recordItems = (
                from item in items
                select item.Item
                );

            return PutItemsAsync(recordItems);
        }

        public IAsyncAction PutItemAsync(RecordItem item)
        {
            return AsyncInfo.Run(
                async cancelToken =>
                      {
                          if (item == null)
                          {
                              throw new ArgumentNullException("item");
                          }

                          item.Key.ValidateRequired("Key");
                          
                          using (await CrossThreadLockScope.Enter(m_lock))
                          {
                              await m_objectStore.PutAsync(item.Key.ID, item);

                          }
                      });
        }

        public IAsyncAction PutItemsAsync(IEnumerable<RecordItem> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            return PutItemsAsyncImpl(items).AsAsyncAction();
        }

        public IAsyncAction RemoveItemAsync(ItemKey key)
        {
            return AsyncInfo.Run(
                async cancelToken =>
                      {
                          key.ValidateRequired("key");
                          
                          using (await CrossThreadLockScope.Enter(m_lock))
                          {
                              await m_objectStore.DeleteAsync(key.ID);
                          }
                      });
        }

        public IAsyncOperation<DateTimeOffset> UpdateDateForAsync(ItemKey key)
        {
            return AsyncInfo.Run(
                async cancelToken =>
                      {
                          if (key == null)
                          {
                              throw new ArgumentNullException("key");
                          }

                          using (await CrossThreadLockScope.Enter(m_lock))
                          {
                              return await m_objectStore.GetUpdateDateAsync(key.ID);
                          }
                      });
        }

        internal async Task<IItemDataTyped> GetAsyncImpl(ItemKey key)
        {
            RecordItem item = await GetItemAsyncImpl(key);
            if (item == null || !item.HasTypedData)
            {
                return null;
            }

            return item.TypedData;
        }

        internal async Task<IItemDataTyped> RefreshAndGetAsyncImpl(ItemKey key)
        {
            RecordItem item = await RefreshAndGetItemAsyncImpl(key);
            if (item == null || !item.HasTypedData)
            {
                return null;
            }

            return item.TypedData;
        }

        internal async Task<IList<IItemDataTyped>> GetMultipleAsyncImpl(
            IEnumerable<ItemKey> keys, bool includeNullItems)
        {
            var items = new LazyList<IItemDataTyped>();
            foreach (ItemKey key in keys)
            {
                key.Validate();

                IItemDataTyped item = await GetAsyncImpl(key);
                if (includeNullItems)
                {
                    items.Add(item);
                }
                else if (item != null)
                {
                    items.Add(item);
                }
            }

            return (items.Count > 0) ? items.Value : null;
        }

        internal async Task<IItemDataTyped> GetByIDAsyncImpl(string itemID)
        {
            RecordItem item = await GetItemByIDAsyncImpl(itemID);
            if (item == null || !item.HasTypedData)
            {
                return null;
            }

            return item.TypedData;
        }

        internal async Task<RecordItem> GetItemAsyncImpl(ItemKey key)
        {
            using (await CrossThreadLockScope.Enter(m_lock))
            {
                var item = (RecordItem) await m_objectStore.GetAsync(key.ID, typeof (RecordItem));
                if (item == null)
                {
                    return null;
                }
                //
                // Verify the version stamp
                //
                if (!item.Key.IsVersion(key.Version))
                {
                    return null;
                }

                return item;
            }
        }

        internal async Task<RecordItem> RefreshAndGetItemAsyncImpl(ItemKey key)
        {
            using (await CrossThreadLockScope.Enter(m_lock))
            {
                var item = (RecordItem)await m_objectStore.RefreshAndGetAsync(key.ID, typeof(RecordItem));
                if (item == null)
                {
                    return null;
                }
                //
                // Verify the version stamp
                //
                if (!item.Key.IsVersion(key.Version))
                {
                    return null;
                }

                return item;
            }
        }


        internal async Task<IList<RecordItem>> GetItemsAsyncImpl(IEnumerable<ItemKey> keys, bool includeNullItems)
        {
            var items = new LazyList<RecordItem>();
            foreach (ItemKey key in keys)
            {
                key.Validate();

                RecordItem item = await GetItemAsyncImpl(key);
                if (includeNullItems)
                {
                    items.Add(item);
                }
                else if (item != null)
                {
                    items.Add(item);
                }
            }

            return (items.Count > 0) ? items.Value : null;
        }

        internal async Task<RecordItem> GetItemByIDAsyncImpl(string itemID)
        {
            using (await CrossThreadLockScope.Enter(m_lock))
            {
                return (RecordItem) await m_objectStore.GetAsync(itemID, typeof (RecordItem));
            }
        }

        internal async Task PutItemsAsyncImpl(IEnumerable<RecordItem> items)
        {
            using (await CrossThreadLockScope.Enter(m_lock))
            {
                foreach (RecordItem item in items)
                {
                    await m_objectStore.PutAsync(item.Key.ID, item);
                }
            }
        }
    }
}