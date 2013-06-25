// (c) Microsoft. All rights reserved

using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using HealthVault.Foundation;
using HealthVault.Types;
using Windows.Foundation;
using Windows.Storage;

namespace HealthVault.Store
{
    public sealed class LocalRecordStore
    {
        private LocalStore m_blobs;
        private SynchronizedStore m_dataStore;
        private LocalStore m_metadataStore;
        private IRecord m_record;
        private CrossThreadLock m_metadataLock;

        internal LocalRecordStore(IRecord record, IObjectStore parentStore, LocalRecordStoreTable recordStoreTable)
        {
            m_record = record;
            m_metadataLock = new CrossThreadLock(false);
            Task.Run(() => EnsureFolders(parentStore, recordStoreTable)).Wait();
        }

        public LocalRecordStore(IRecord record, StorageFolder folder)
            : this(record, new FolderObjectStore(folder), null)
        {
        }

        public IRecord Record
        {
            get { return m_record; }
            set
            {
                // Update local store's record reference
                m_record = value;
                if (m_dataStore != null)
                {
                    m_dataStore.Record = m_record;
                }
            }
        }

        public SynchronizedStore Data
        {
            get { return m_dataStore; }
        }

        public LocalStore Blobs
        {
            get { return m_blobs; }
        }

        public SynchronizedView CreateView(ItemQuery query)
        {
            return new SynchronizedView(m_dataStore, query, query.Name);
        }

        public SynchronizedView CreateView(string viewName, ItemQuery query)
        {
            if (string.IsNullOrEmpty(viewName))
            {
                throw new ArgumentException("viewName");
            }

            return new SynchronizedView(m_dataStore, query, viewName);
        }

        public IAsyncOperation<SynchronizedView> GetViewAsync(string viewName)
        {
            return GetViewAsyncImpl(viewName).AsAsyncOperation();
        }

        public IAsyncAction PutViewAsync(SynchronizedView view)
        {
            return AsyncInfo.Run(
                async cancelToken =>
                      {
                          if (view == null)
                          {
                              throw new ArgumentNullException("view");
                          }
                          view.Name.ValidateRequired("view");

                          using (await CrossThreadLockScope.Enter(m_metadataLock))
                          {
                              await m_metadataStore.PutAsync(MakeViewKey(view.Name), view.Data);
                          }
                      });
        }

        public IAsyncAction DeleteViewAsync(string viewName)
        {
            return AsyncInfo.Run(
                async cancelToken =>
                      {
                          using (await CrossThreadLockScope.Enter(m_metadataLock))
                          {
                              await m_metadataStore.DeleteAsync(MakeViewKey(viewName));
                          }
                      });
        }

        public IAsyncOperation<StoredQuery> GetStoredQueryAsync(string name)
        {
            return GetStoredQueryAsyncImpl(MakeStoredQueryKey(name)).AsAsyncOperation();
        }

        public IAsyncAction PutStoredQueryAsync(string name, StoredQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("value");
            }

            return AsyncInfo.Run(
                async cancelToken =>
                      {
                          using (await CrossThreadLockScope.Enter(m_metadataLock))
                          {
                              await m_metadataStore.PutAsync(MakeStoredQueryKey(name), query);
                          }
                      });
        }

        public IAsyncAction DeleteStoredQueryAsync(string name)
        {
            return AsyncInfo.Run(
                async cancelToken =>
                      {
                          using (await CrossThreadLockScope.Enter(m_metadataLock))
                          {
                              await m_metadataStore.DeleteAsync(MakeStoredQueryKey(name));
                          }
                      });
        }

        internal async Task<SynchronizedView> GetViewAsyncImpl(string viewName)
        {
            using (await CrossThreadLockScope.Enter(m_metadataLock))
            {
                var viewData = (ViewData) await m_metadataStore.Store.GetAsync(MakeViewKey(viewName), typeof (ViewData));
                if (viewData == null)
                {
                    return null;
                }
                if (!string.Equals(viewData.Name, viewName))
                {
                    return null;
                }

                return new SynchronizedView(m_dataStore, viewData);
            }
        }

        internal async Task<StoredQuery> GetStoredQueryAsyncImpl(string queryKey)
        {
            using (await CrossThreadLockScope.Enter(m_metadataLock))
            {
                return (StoredQuery) await m_metadataStore.Store.GetAsync(queryKey, typeof (StoredQuery));
            }
        }

        internal async Task EnsureFolders(IObjectStore parentStore, LocalRecordStoreTable recordStoreTable)
        {
            IObjectStore root = await parentStore.CreateChildStoreAsync(m_record.ID);

            IObjectStore child;

            child = await root.CreateChildStoreAsync("Data");

            var itemStore = new LocalItemStore(child, (recordStoreTable != null) ? recordStoreTable.ItemCache : null);
            m_dataStore = new SynchronizedStore(m_record, itemStore);

            child = await root.CreateChildStoreAsync("Metadata");
            m_metadataStore = new LocalStore(child);

            child = await root.CreateChildStoreAsync("Blobs");
            m_blobs = new LocalStore(child);
        }

        private string MakeViewKey(string name)
        {
            return name + "_View";
        }

        private string MakeStoredQueryKey(string name)
        {
            return name + "_StoredQuery";
        }
    }
}