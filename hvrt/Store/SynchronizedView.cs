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
using DateTime = HealthVault.Types.DateTime;

namespace HealthVault.Store
{
    public sealed class SynchronizedView
    {
        private const int DefaultReadAheadChunkSize = 25;

        private readonly ViewData m_data;
        private readonly SynchronizedStore m_store;
        private readonly HashSet<string> m_typeVersions; 
        private int m_readAheadChunkSize = DefaultReadAheadChunkSize;

        public SynchronizedView(SynchronizedStore store, ViewData data)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            m_store = store;
            m_data = data;
            m_typeVersions = new HashSet<string>(m_data.Query.View.TypeVersions);
        }

        public SynchronizedView(SynchronizedStore store, ItemQuery query, string name)
            : this(store, new ViewData(query, name))
        {
        }

        /// <summary>
        /// Record over which this is a view
        /// </summary>
        public IRecord Record
        {
            get { return m_store.Record; }
        }

        /// <summary>
        /// Local synchronized store this view is working with
        /// </summary>
        public SynchronizedStore Store
        {
            get { return m_store; }
        }

        public string Name
        {
            get { return m_data.Name; }
            set { m_data.Name = value; }
        }

        public int KeyCount
        {
            get { return m_data.KeyCount; }
        }

        /// <summary>
        /// This view's data!
        /// </summary>
        public ViewData Data
        {
            get { return m_data; }
        }

        public int ReadAheadChunkSize
        {
            get { return m_readAheadChunkSize; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("ReadAheadChunkSize");
                }
                m_readAheadChunkSize = value;
            }
        }

        /// <summary>
        /// The keys for which items are now available
        /// </summary>
        public event EventHandler<IList<ItemKey>> ItemsAvailable;

        /// <summary>
        /// Passes the keys which were not found
        /// </summary>
        public event EventHandler<IList<ItemKey>> ItemsNotFound;

        /// <summary>
        /// Exception!
        /// </summary>
        public event EventHandler<Exception> Error;

        public bool IsStale(int maxAgeInSeconds)
        {
            return (!m_data.HasKeys || m_data.IsStale(maxAgeInSeconds));
        }

        public IAsyncAction SynchronizeAsync()
        {
            return AsyncInfo.Run(async cancelToken => { await SynchronizeAsyncImpl(cancelToken); });
        }

        /// <summary>
        /// 1. If the item for the key at index is available in the local cache, returns the item
        /// 2. If not, OR the item was stale (version stamp mismatch), returns NULL
        /// 3. If returns NULL, then also triggers a refresh (with readahead) in the background
        /// 4. Does NOT await the background refresh
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IAsyncOperation<IItemDataTyped> GetItemAsync(int index)
        {
            m_data.ValidateIndex(index);

            return AsyncInfo.Run(
                async cancelToken => { return await GetAsyncImpl(index, false, cancelToken); }
                );
        }

        public IAsyncOperation<IList<IItemDataTyped>> GetItemsAsync(int startAt, int count)
        {
            if (!m_data.HasKeys)
            {
                return null;
            }

            m_data.ValidateIndex(startAt);
            count = m_data.Keys.GetCorrectedCount(startAt, count);

            return AsyncInfo.Run<IList<IItemDataTyped>>(
                async cancelToken =>
                      {
                          var items = new LazyList<IItemDataTyped>();
                          for (int i = startAt, max = startAt + count; i < max; ++i)
                          {
                              IItemDataTyped item = await GetItemAsync(i).AsTask(cancelToken);
                              items.Add(item);
                          }

                          return items.HasValue ? items.Value : null;
                      });
        }

        /// <summary>
        /// Call this WARILY. Will block
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IItemDataTyped GetItemSync(int index)
        {
            Task<IItemDataTyped> task = Task.Run(() => GetAsyncImpl(index, false, CancellationToken.None));
            task.Wait();

            return task.Result;
        }

        /// <summary>
        /// 1. If the item for the key at index is available in the local cache, returns the item
        /// 2. If not, OR the item was stale (version stamp mismatch), triggers a refresh (with readahead)
        /// 3. AWAITS the refresh completion
        /// 4. If the item was not found, returns NULL
        /// 5. If you called this method and did NOT await its result before calling it again, MAY return NULL if the 
        /// item you requested already has a load pending
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IAsyncOperation<IItemDataTyped> EnsureItemAvailableAndGetAsync(int index)
        {
            m_data.ValidateIndex(index);

            return AsyncInfo.Run(
                async cancelToken => { return await GetAsyncImpl(index, true, cancelToken); }
                );
        }

        public IAsyncOperation<IList<IItemDataTyped>> EnsureItemsAvailableAndGetAsync(int startAt, int count)
        {
            if (!m_data.HasKeys)
            {
                return null;
            }

            m_data.ValidateIndex(startAt);
            count = m_data.Keys.GetCorrectedCount(startAt, count);

            return AsyncInfo.Run<IList<IItemDataTyped>>(
                async cancelToken =>
                      {
                          var items = new LazyList<IItemDataTyped>();
                          for (int i = startAt, max = startAt + count; i < max; ++i)
                          {
                              IItemDataTyped item = await EnsureItemAvailableAndGetAsync(i).AsTask(cancelToken);
                              items.Add(item);
                          }

                          return items.HasValue ? items.Value : null;
                      });
        }

        /// <summary>
        /// Call this warily. Will block.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IItemDataTyped EnsureItemAvailableAndGetSync(int index)
        {
            Task<IItemDataTyped> task = Task.Run(() => GetAsyncImpl(index, true, CancellationToken.None));
            task.Wait();

            return task.Result;
        }

        private async Task<IItemDataTyped> GetAsyncImpl(
            int index, bool shouldAwaitRefresh, CancellationToken cancelToken)
        {
            if (!m_data.HasKeys)
            {
                return null;
            }

            ViewKey viewKey = m_data.Keys[index];
            //
            // Try to load the item from the local store
            //
            IItemDataTyped item = await Store.Local.GetAsyncImpl(viewKey.Key);
            if (item != null && m_typeVersions.Contains(item.Type.ID))
            {
                return item;
            }

            //
            // Don't have the item locally available. Will need to fetch it. 
            // While we do this, might as well read ahead
            // 
            await BeginRefreshAsync(index, shouldAwaitRefresh, cancelToken);

            if (!shouldAwaitRefresh)
            {
                return null;
            }
            //
            // Reload the item
            //
            return await Store.Local.GetAsyncImpl(viewKey.Key);
        }

        private async Task BeginRefreshAsync(int startAt, bool shouldAwait, CancellationToken cancelToken)
        {
            IList<ItemKey> keysToDownload = CollectKeysNeedingDownload(startAt, ReadAheadChunkSize);
            if (keysToDownload.IsNullOrEmpty())
            {
                return;
            }
            //
            // Refresh happens in the background
            // This will return as soon as the task is launched
            //
            PendingGetCompletionDelegate completionCallback = null;
            if (!shouldAwait)
            {
                completionCallback = PendingGetCompletion; // Callback => download items in background
            }

            PendingGetResult result = await Store.RefreshAsyncImpl(
                keysToDownload, 
                m_typeVersions.ToList(),
                completionCallback, 
                cancelToken);
            if (result == null)
            {
                return; // NO pending work
            }

            PendingGetCompletion(this, result);
        }

        private IList<ItemKey> CollectKeysNeedingDownload(int startAt, int keyCount)
        {
            IList<ViewKey> viewKeys = m_data.Keys.SelectKeysNotAlreadyLoading(startAt, keyCount);
            if (viewKeys.IsNullOrEmpty())
            {
                return null;
            }

            var keys = new LazyList<ItemKey>();
            for (int i = 0, count = viewKeys.Count; i < count; ++i)
            {
                ViewKey viewKey = viewKeys[i];
                viewKey.IsLoadPending = true;
                keys.Add(viewKey.Key);
            }

            return keys.HasValue ? keys.Value : null;
        }

        private void PendingGetCompletion(object sender, PendingGetResult result)
        {
            try
            {
                result.EnsureSuccess();

                ProcessFoundItems(result, true);

                ProcessNotFoundItems(result);
            }
            catch (Exception ex)
            {
                ProcessError(result, ex);
            }
        }

        private void ProcessFoundItems(PendingGetResult pendingResult, bool fireEvent)
        {
            if (pendingResult.KeysFound.IsNullOrEmpty() || !m_data.HasKeys)
            {
                return;
            }

            SetLoadingStateForKeys(pendingResult.KeysFound, false);

            if (fireEvent)
            {
                ItemsAvailable.SafeInvoke(this, pendingResult.KeysFound);
            }
        }

        private void ProcessNotFoundItems(PendingGetResult pendingResult)
        {
            if (pendingResult.KeysNotFound.IsNullOrEmpty() || !m_data.HasKeys)
            {
                return;
            }

            SetLoadingStateForKeys(pendingResult.KeysNotFound, false);
            ItemsNotFound.SafeInvoke(this, pendingResult.KeysNotFound);
        }

        private void ProcessError(PendingGetResult pendingResult, Exception ex)
        {
            try
            {
                SetLoadingStateForKeys(pendingResult.KeysRequested, false);
                Error.SafeInvoke(this, ex);
            }
            catch
            {
            }
        }

        private void SetLoadingStateForKeys(IList<ItemKey> keys, bool isLoading)
        {
            ViewKeyCollection viewKeys = m_data.Keys;
            for (int i = 0, count = keys.Count; i < count; ++i)
            {
                ViewKey viewKey = viewKeys.GetByItemKey(keys[i]);
                if (viewKey != null)
                {
                    viewKey.IsLoadPending = isLoading;
                }
            }
        }

        private async Task SynchronizeAsyncImpl(CancellationToken cancelToken)
        {
            if (!m_data.HasQuery)
            {
                throw new ArgumentException("Query");
            }

            IList<PendingItem> pendingItems =
                await Record.GetKeysAndDateAsync(m_data.Query.Filters.ToArray(), m_data.Query.MaxResults != null ? m_data.Query.MaxResults.Value : 0).AsTask(cancelToken);
            var newKeys = new ViewKeyCollection();
            if (!pendingItems.IsNullOrEmpty())
            {
                newKeys.AddFromPendingItems(pendingItems);
            }

            m_data.Keys = newKeys;
            m_data.LastUpdated = DateTime.Now();
        }

        public bool ShouldSerializeName()
        {
            return !String.IsNullOrEmpty(Name);
        }
    }
}