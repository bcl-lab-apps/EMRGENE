// (c) Microsoft. All rights reserved

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthVault.Foundation;
using Windows.Storage;

namespace HealthVault.Store
{
    /// <summary>
    /// Thread safe
    /// </summary>
    public sealed class LocalRecordStoreTable
    {
        private readonly ICache<string, object> m_itemCache;
        private readonly Dictionary<string, LocalRecordStore> m_recordStores;
        private readonly IObjectStore m_root;

        internal LocalRecordStoreTable(IObjectStore root)
        {
            m_root = root;
            m_itemCache = new LRUCache<string, object>(0);
            m_recordStores = new Dictionary<string, LocalRecordStore>();
        }

        public LocalRecordStoreTable(StorageFolder root)
            : this(new FolderObjectStore(root))
        {
        }

        public int MaxCachedItems
        {
            get { return m_itemCache.MaxCount; }
            set { m_itemCache.MaxCount = value; }
        }

        internal ICache<string, object> ItemCache
        {
            get { return m_itemCache; }
        }

        public LocalRecordStore GetStoreForRecord(IRecord record)
        {
            if (record == null)
            {
                throw new ArgumentException(null);
            }

            lock (m_recordStores)
            {
                LocalRecordStore recordStore = null;
                string recordID = record.ID;
                if (!m_recordStores.TryGetValue(recordID, out recordStore))
                {
                    recordStore = new LocalRecordStore(record, m_root, this);
                    m_recordStores[recordID] = recordStore;
                }

                recordStore.Record = record;
                return recordStore;
            }
        }

        public void RemoveAllStores()
        {
            lock (m_recordStores)
            {
                m_itemCache.Clear();
                m_recordStores.Clear();
                Task.Run(() => m_root.DeleteAllAsync()).Wait();
            }
        }

        public void RemoveStoreForRecord(IRecord record)
        {
            if (record == null)
            {
                throw new ArgumentException("record");
            }

            RemoveStoreForRecordID(record.ID);
        }

        public void RemoveStoreForRecordID(string recordID)
        {
            if (string.IsNullOrEmpty(recordID))
            {
                throw new ArgumentException("recordID");
            }

            lock (m_recordStores)
            {
                m_itemCache.Clear();
                m_recordStores.Remove(recordID);
                Task.Run(() => m_root.DeleteChildStoreAsync(recordID)).Wait();
            }
        }
    }
}