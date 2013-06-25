// (c) Microsoft. All rights reserved

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using HealthVault.Foundation;
using HealthVault.ItemTypes;
using HealthVault.Types;
using Windows.Foundation;

namespace HealthVault.Store
{
    /// <summary>
    /// Currently only provides read-only sychronization
    /// </summary>
    public sealed class SynchronizedStore
    {
        private readonly LocalItemStore m_itemStore;
        private IRecord m_record;

        public SynchronizedStore(IRecord record, LocalItemStore itemStore)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }
            if (itemStore == null)
            {
                throw new ArgumentNullException("itemStore");
            }
            m_record = record;
            m_itemStore = itemStore;
            SectionsToFetch = ItemSectionType.Standard;
        }

        public IRecord Record
        {
            get { return m_record; }
            set { m_record = value; }
        }

        public LocalItemStore Local
        {
            get { return m_itemStore; }
        }

        public ItemSectionType SectionsToFetch { get; set; }

        /// <summary>
        /// This completes only when all locally available AND any pending items have been downloaded
        /// See GetAsync(keys, callback) for an alternative
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="typeVersions"></param>
        /// <returns></returns>
        public IAsyncOperation<IList<IItemDataTyped>> GetAsync(
            IList<ItemKey> keys,
            IList<string> typeVersions)
        {
            return GetAsync(keys, typeVersions, null);
        }

        /// <summary>
        /// Returns a list of items such that:
        ///     - If an item matching the equivalent key is available locally, returns it
        ///     - ELSE returns a NULL item at the ordinal matching the key - indicating that the item is PENDING
        ///     - Issues a background get for the pending items, and notifies you (callback) when done
        /// This technique is useful for making responsive UIs. 
        ///   - User can view locally available items quickly
        ///   - Pending items are shown as 'loading' and updated as they become available
        /// </summary>
        /// <param name="keys">keys to retrieve</param>
        /// <param name="typeVersions"></param>
        /// <returns>List with COUNT == keys.Count. If an item was not found locally, the equivalent item in the list is NULL</returns>
        public IAsyncOperation<IList<IItemDataTyped>> GetAsync(
            IList<ItemKey> keys,
            IList<string> typeVersions, 
            PendingGetCompletionDelegate callback)
        {
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }

            return AsyncInfo.Run(cancelToken => GetAsyncImpl(keys, typeVersions, callback, cancelToken));
        }


        /// <summary>
        /// Returns a list of items such that:
        ///     - If an item matching the equivalent key is available locally, returns it
        ///     - ELSE returns a NULL item at the ordinal matching the key - indicating that the item is PENDING
        ///     - Issues a background get for the pending items, and notifies you (callback) when done
        /// This technique is useful for making responsive UIs. 
        ///   - User can view locally available items quickly
        ///   - Pending items are shown as 'loading' and updated as they become available
        /// </summary>
        /// <param name="keys">keys to retrieve</param>
        /// <param name="typeVersions"></param>
        /// <returns>List with COUNT == keys.Count. If an item was not found locally, the equivalent item in the list is NULL</returns>
        /// 
        public IAsyncOperation<IList<RecordItem>> GetItemsAsync(
            IList<ItemKey> keys,
            IList<string> typeVersions)
        {
            return GetItemsAsync(keys, typeVersions, null);
        }

        public IAsyncOperation<IList<RecordItem>> GetItemsAsync(
            IList<ItemKey> keys,
            IList<string> typeVersions, 
            PendingGetCompletionDelegate callback)
        {
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }

            return AsyncInfo.Run(async cancelToken => await GetItemsAsyncImpl(keys, typeVersions, callback, cancelToken));
        }

        public IAsyncOperation<IList<IItemDataTyped>> GetByViewKeysAsync(
            IList<ViewKey> keys,
            IList<string> typeVersions)
        {
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }
            
            return GetAsync(keys.Select(k => k.Key).ToArray(), typeVersions);
        }


        public IAsyncAction PutAsync(IList<IItemDataTyped> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }
            
            return m_itemStore.PutItemsAsync(items.Select(t => t.Item));
        }

        public IAsyncAction PutItemsAsync(IList<RecordItem> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            return m_itemStore.PutItemsAsync(items);
        }

        /// <summary>
        /// Refresh any items that need refreshing
        /// </summary>
        public IAsyncOperation<PendingGetResult> RefreshAsync(
            IList<ItemKey> keys,
            IList<string> typeVersions)
        {
            return AsyncInfo.Run(async cancelToken => await RefreshAsyncImpl(keys, typeVersions, null, cancelToken));
        }

        public IAsyncAction RefreshAsync(
            IList<ItemKey> keys,
            IList<string> typeVersions, 
            PendingGetCompletionDelegate callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }

            return AsyncInfo.Run(async cancelToken => { await RefreshAsyncImpl(keys, typeVersions, callback, cancelToken); });
        }

        /// <summary>
        /// Triggers a FORCED refresh in the background and returns as soon as it can
        /// When the background fetch completes, invokes the callback
        /// </summary>
        public IAsyncAction DownloadAsync(
            IList<ItemKey> keys,
            IList<string> typeVersions, 
            PendingGetCompletionDelegate callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }

            return AsyncInfo.Run(async cancelToken => { await DownloadAsyncImpl(keys, typeVersions, callback, cancelToken); });
        }

        public IAsyncOperation<PendingGetResult> DownloadAsync(
            IList<ItemKey> keys,
            IList<string> typeVersions)
        {
            return AsyncInfo.Run(async cancelToken => await DownloadAsyncImpl(keys, typeVersions, null, cancelToken));
        }


        internal async Task<IList<IItemDataTyped>> GetAsyncImpl(
            IList<ItemKey> keys,
            IList<string> typeVersions,
            PendingGetCompletionDelegate callback, 
            CancellationToken cancelToken)
        {
            IList<RecordItem> items = await GetItemsAsyncImpl(keys, typeVersions, callback, cancelToken);
            if (items != null)
            {
                return (
                    from item in items
                    select (item != null ? item.TypedData : null)
                    ).ToArray();
            }

            return null;
        }

        private async Task<IList<RecordItem>> GetItemsAsyncImpl(
            IList<ItemKey> keys,
            IList<string> typeVersions,
            PendingGetCompletionDelegate callback, 
            CancellationToken cancelToken)
        {
            //
            // true: include null items - i.e. items not found in the local store
            //
            IList<RecordItem> foundItems = await Local.GetItemsAsyncImpl(keys, true);

            //
            // Trigger a download of items that are not available yet...
            //
            IList<ItemKey> pendingKeys = CollectKeysNeedingDownload(keys, typeVersions, foundItems);
            if (pendingKeys.IsNullOrEmpty())
            {
                return foundItems;
            }

            PendingGetResult pendingResult = await DownloadAsyncImpl(pendingKeys, typeVersions, callback, cancelToken);
            if (pendingResult == null)
            {
                return foundItems;
            }

            //
            // Load fresh items
            //
            if (pendingResult.HasKeysFound)
            {
                await LoadNewItems(foundItems, keys, pendingResult.KeysFound);
            }

            return foundItems;
        }

        internal async Task<PendingGetResult> DownloadAsyncImpl(
            IList<ItemKey> keys,
            IList<string> typeVersions,
            PendingGetCompletionDelegate callback, 
            CancellationToken cancelToken)
        {
            if (callback != null)
            {
                //
                // Run the download in the background. 
                // Return what we have right away, and notify caller when pending items arrive
                //
                Task task = DownloadItems(keys, typeVersions, callback, cancelToken);
                return null;
            }

            //
            // Wait for download to complete...
            //
            PendingGetResult result = await DownloadItems(keys, typeVersions, callback, cancelToken);
            result.EnsureSuccess();

            return result;
        }

        internal async Task<PendingGetResult> RefreshAsyncImpl(
            IList<ItemKey> keys,
            IList<string> typeVersions,
            PendingGetCompletionDelegate callback, 
            CancellationToken cancelToken)
        {
            IList<ItemKey> pendingKeys = await CollectKeysNotInLocalStore(keys, typeVersions);
            if (pendingKeys.IsNullOrEmpty())
            {
                return null; // No pending work
            }

            return await DownloadAsyncImpl(pendingKeys, typeVersions, callback, cancelToken);
        }

        private async Task<PendingGetResult> DownloadItems(
            IList<ItemKey> keys, 
            IList<string> typeVersions,
            PendingGetCompletionDelegate callback, 
            CancellationToken cancelToken)
        {
            var result = new PendingGetResult();
            try
            {
                result.KeysRequested = keys;

                ItemQuery query = ItemQuery.QueryForKeys(keys);
                query.View.SetSections(SectionsToFetch);
                if (typeVersions != null && typeVersions.Count > 0)
                {
                    query.View.TypeVersions.AddRange(typeVersions);
                }

                IList<RecordItem> items = await m_record.GetAllItemsAsync(query);
                await m_itemStore.PutItemsAsyncImpl(items);

                result.KeysFound = (
                    from item in items
                    select item.Key
                    ).ToArray();
            }
            catch (Exception ex)
            {
                result.Exception = ex;
            }

            NotifyPendingGetComplete(callback, result);

            return result;
        }

        internal async Task<IList<ItemKey>> CollectKeysNotInLocalStore(
            IList<ItemKey> keys,
            IList<string> typeVersions)
        {
            //
            // true: include null items - i.e. items not found in the local store
            //
            IList<RecordItem> foundItems = await Local.GetItemsAsyncImpl(keys, true);
            //
            // Trigger a download of items that are not available yet...
            //
            return CollectKeysNeedingDownload(keys, typeVersions, foundItems);
        }

        internal IList<ItemKey> CollectKeysNeedingDownload(
            IList<ItemKey> requestedKeys, 
            IList<string> typeVersions,
            IList<RecordItem> collectedLocalItems)
        {
            var checkVersion = typeVersions != null;
            var typeVersionHash = checkVersion ? new HashSet<string>(typeVersions) : null;

            var pendingKeys = new LazyList<ItemKey>();
            for (int i = 0, count = requestedKeys.Count; i < count; ++i)
            {
                var localItem = collectedLocalItems[i];
                if (localItem == null ||
                    (checkVersion && !typeVersionHash.Contains(localItem.Type.ID)))
                {
                    pendingKeys.Add(requestedKeys[i]);
                }
            }

            return (pendingKeys.Count > 0) ? pendingKeys.Value : null;
        }

        internal async Task LoadNewItems(
            IList<RecordItem> itemList, IList<ItemKey> keysRequested, IList<ItemKey> newKeysFound)
        {
            if (itemList.Count != keysRequested.Count)
            {
                throw new InvalidOperationException();
            }

            int iNewKey = 0;
            for (int i = 0, count = keysRequested.Count; i < count; ++i)
            {
                ItemKey keyRequested = keysRequested[i];
                if (keyRequested.EqualsKey(newKeysFound[i]))
                {
                    itemList[i] = await Local.GetItemAsyncImpl(keyRequested);
                    ++iNewKey;
                }
            }
        }

        private void NotifyPendingGetComplete(PendingGetCompletionDelegate callback, PendingGetResult result)
        {
            if (callback != null)
            {
                try
                {
                    callback(this, result);
                }
                catch
                {
                }
            }
        }
    }
}