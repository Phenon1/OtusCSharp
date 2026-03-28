using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OtusCSharpModels
{
    public class SimpleStore : IDisposable
    {
        private Dictionary<string, byte[]> _store;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private long _setCount, _getCount, _deleteCount;

        public SimpleStore()
        {
            _store = new Dictionary<string, byte[]>();
        }

        public(long,long,long) GetStatistics()
        {
            return (_setCount, _getCount, _deleteCount);
        }
        public void Set(string key, UserProfile? profile)
        {
            _lock.EnterWriteLock();
            try
            {
                _store[key] = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(profile);
            }
            finally
            {
                Interlocked.Increment(ref _setCount);
                _lock.ExitWriteLock();
            }
        }
        public UserProfile? Get(string key)
        {
            _lock.EnterReadLock();
            try
            {
                _store.TryGetValue(key, out var value);
                return JsonSerializer.Deserialize<UserProfile>(value);
            }
            finally
            {
                Interlocked.Increment(ref _getCount);
                _lock.ExitReadLock();
            }
            
        }
        public void Delete(string key)
        {
            _lock.EnterWriteLock();
            try
            {
                _store.Remove(key);
            }
            finally
            {
                Interlocked.Increment(ref _deleteCount);
                _lock.ExitWriteLock();
            }
        }
        public void Dispose()
        {
            _lock.Dispose(); 
        }
    }

}
