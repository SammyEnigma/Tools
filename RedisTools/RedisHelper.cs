using RedisTools.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisTools
{
    public class RedisHelper
    {
        private IDatabase _db;
        private static ConnectionMultiplexer _connector;
        private ISerializer _serializer;

        public RedisHelper()
        {
            _connector = ConnectionMultiplexer.Connect("");
            _db = _connector.GetDatabase();
            _serializer = new NewtonsoftSerializer();
        }

        public bool Exists(string key)
        {
            EnsureKey(key);

            return _db.KeyExists(key);
        }

        public Task<bool> ExistsAsync(string key)
        {
            EnsureKey(key);

            return _db.KeyExistsAsync(key);
        }

        public bool Remove(string key)
        {
            EnsureKey(key);

            return _db.KeyDelete(key);
        }

        public Task<bool> RemoveAsync(string key)
        {
            EnsureKey(key);

            return _db.KeyDeleteAsync(key);
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            EnsureNotNull(nameof(keys), keys);

            var redisKeys = keys.Select(x => (RedisKey)x).ToArray();
            _db.KeyDelete(redisKeys);
        }

        public Task RemoveAllAsync(IEnumerable<string> keys)
        {
            EnsureNotNull(nameof(keys), keys);

            var redisKeys = keys.Select(x => (RedisKey)x).ToArray();
            return _db.KeyDeleteAsync(redisKeys);
        }

        public T Get<T>(string key)
        {
            EnsureKey(key);

            var valueBytes = _db.StringGet(key);

            if (!valueBytes.HasValue)
                return default(T);

            return _serializer.Deserialize<T>(valueBytes);
        }

        public T Get<T>(string key, DateTimeOffset expiresAt)
        {
            EnsureKey(key);

            var valueBytes = _db.StringGet(key);

            if (!valueBytes.HasValue)
                return default(T);

            _db.KeyExpire(key, expiresAt.UtcDateTime.Subtract(DateTime.UtcNow));
            return _serializer.Deserialize<T>(valueBytes);
        }

        public T Get<T>(string key, TimeSpan expiresIn)
        {
            EnsureKey(key);

            var valueBytes = _db.StringGet(key);

            if (!valueBytes.HasValue)
                return default(T);

            _db.KeyExpire(key, expiresIn);
            return _serializer.Deserialize<T>(valueBytes);
        }

        public async Task<T> GetAsync<T>(string key)
        {
            EnsureKey(key);

            var valueBytes = await _db.StringGetAsync(key).ConfigureAwait(false);

            if (!valueBytes.HasValue)
                return default(T);

            return _serializer.Deserialize<T>(valueBytes);
        }

        public async Task<T> GetAsync<T>(string key, DateTimeOffset expiresAt)
        {
            EnsureKey(key);

            var valueBytes = await _db.StringGetAsync(key).ConfigureAwait(false);

            if (!valueBytes.HasValue)
                return default(T);

            await _db.KeyExpireAsync(key, expiresAt.UtcDateTime.Subtract(DateTime.UtcNow))
                .ConfigureAwait(false);
            return _serializer.Deserialize<T>(valueBytes);
        }

        public async Task<T> GetAsync<T>(string key, TimeSpan expiresIn)
        {
            EnsureKey(key);

            var valueBytes = await _db.StringGetAsync(key).ConfigureAwait(false);

            if (!valueBytes.HasValue)
                return default(T);

            await _db.KeyExpireAsync(key, expiresIn).ConfigureAwait(false);
            return _serializer.Deserialize<T>(valueBytes);
        }

        public bool Add<T>(string key, T value)
        {
            EnsureKey(key);

            var entryBytes = _serializer.Serialize(value);

            return _db.StringSet(key, entryBytes);
        }

        public async Task<bool> AddAsync<T>(string key, T value)
        {
            EnsureKey(key);

            var entryBytes = _serializer.Serialize(value);

            return await _db.StringSetAsync(key, entryBytes).ConfigureAwait(false);
        }

        public bool Add<T>(string key, T value, DateTimeOffset expiresAt)
        {
            EnsureKey(key);

            var entryBytes = _serializer.Serialize(value);
            var expiration = expiresAt.UtcDateTime.Subtract(DateTime.UtcNow);

            return _db.StringSet(key, entryBytes, expiration);
        }

        public async Task<bool> AddAsync<T>(string key, T value, DateTimeOffset expiresAt)
        {
            EnsureKey(key);

            var entryBytes = _serializer.Serialize(value);
            var expiration = expiresAt.UtcDateTime.Subtract(DateTime.UtcNow);

            return await _db.StringSetAsync(key, entryBytes, expiration).ConfigureAwait(false);
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            EnsureKey(key);

            var entryBytes = _serializer.Serialize(value);

            return _db.StringSet(key, entryBytes, expiresIn);
        }

        public async Task<bool> AddAsync<T>(string key, T value, TimeSpan expiresIn)
        {
            EnsureKey(key);

            var entryBytes = _serializer.Serialize(value);

            return await _db.StringSetAsync(key, entryBytes, expiresIn).ConfigureAwait(false);
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            EnsureNotNull(nameof(keys), keys);

            var redisKeys = keys.Select(x => (RedisKey)x).ToArray();
            var result = _db.StringGet(redisKeys);

            var dict = new Dictionary<string, T>(StringComparer.Ordinal);
            for (var index = 0; index < redisKeys.Length; index++)
            {
                var value = result[index];
                dict.Add(redisKeys[index], value == RedisValue.Null ? default(T) : _serializer.Deserialize<T>(value));
            }

            return dict;
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys, DateTimeOffset expiresAt)
        {
            var result = GetAll<T>(keys);
            UpdateExpiryAll(keys.ToArray(), expiresAt);

            return result;
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys, TimeSpan expiresIn)
        {
            var result = GetAll<T>(keys);
            UpdateExpiryAll(keys.ToArray(), expiresIn);

            return result;
        }

        public async Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys)
        {
            EnsureNotNull(nameof(keys), keys);

            var redisKeys = keys.Select(x => (RedisKey)x).ToArray();
            var result = await _db.StringGetAsync(redisKeys).ConfigureAwait(false);

            var dict = new Dictionary<string, T>(StringComparer.Ordinal);
            for (var index = 0; index < redisKeys.Length; index++)
            {
                var value = result[index];
                dict.Add(redisKeys[index], value == RedisValue.Null ? default(T) : _serializer.Deserialize<T>(value));
            }

            return dict;
        }

        public async Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys, DateTimeOffset expiresAt)
        {
            var result = await GetAllAsync<T>(keys).ConfigureAwait(false);
            await UpdateExpiryAllAsync(keys.ToArray(), expiresAt).ConfigureAwait(false);

            return result;
        }

        public async Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys, TimeSpan expiresIn)
        {
            var result = await GetAllAsync<T>(keys).ConfigureAwait(false);
            await UpdateExpiryAllAsync(keys.ToArray(), expiresIn).ConfigureAwait(false);

            return result;
        }

        public bool AddAll<T>(IDictionary<string, T> items)
        {
            EnsureNotNull(nameof(items), items);

            var values = items
                .Select(p => new KeyValuePair<RedisKey, RedisValue>(p.Key, _serializer.Serialize(p.Value)))
                .ToArray();

            return _db.StringSet(values);
        }

        public async Task<bool> AddAllAsync<T>(IDictionary<string, T> items)
        {
            EnsureNotNull(nameof(items), items);

            var values = items
                .Select(p => new KeyValuePair<RedisKey, RedisValue>(p.Key, _serializer.Serialize(p.Value)))
                .ToArray();

            return await _db.StringSetAsync(values).ConfigureAwait(false);
        }

        public bool AddAll<T>(IDictionary<string, T> items, DateTimeOffset expiresAt)
        {
            EnsureNotNull(nameof(items), items);

            var values = items
                .Select(p => new KeyValuePair<RedisKey, RedisValue>(p.Key, _serializer.Serialize(p.Value)))
                .ToArray();

            var result = _db.StringSet(values);
            foreach (var value in values)
                _db.KeyExpire(value.Key, expiresAt.UtcDateTime);

            return result;
        }

        public async Task<bool> AddAllAsync<T>(IDictionary<string, T> items, DateTimeOffset expiresAt)
        {
            EnsureNotNull(nameof(items), items);

            var values = items
                .Select(p => new KeyValuePair<RedisKey, RedisValue>(p.Key, _serializer.Serialize(p.Value)))
                .ToArray();

            var result = await _db.StringSetAsync(values).ConfigureAwait(false);
            Parallel.ForEach(values, async value =>
                await _db.KeyExpireAsync(value.Key, expiresAt.UtcDateTime)
                .ConfigureAwait(false));

            return result;
        }

        public bool AddAll<T>(IDictionary<string, T> items, TimeSpan expiresOn)
        {
            EnsureNotNull(nameof(items), items);

            var values = items
                .Select(p => new KeyValuePair<RedisKey, RedisValue>(p.Key, _serializer.Serialize(p.Value)))
                .ToArray();

            var result = _db.StringSet(values);
            foreach (var value in values)
                _db.KeyExpire(value.Key, expiresOn);

            return result;
        }

        public async Task<bool> AddAllAsync<T>(IDictionary<string, T> items, TimeSpan expiresOn)
        {
            EnsureNotNull(nameof(items), items);

            var values = items
                .Select(p => new KeyValuePair<RedisKey, RedisValue>(p.Key, _serializer.Serialize(p.Value)))
                .ToArray();

            var result = await _db.StringSetAsync(values).ConfigureAwait(false);
            Parallel.ForEach(values, async value =>
                await _db.KeyExpireAsync(value.Key, expiresOn).ConfigureAwait(false));

            return result;
        }

        #region hash
        public bool HashDelete(string hashKey, string key)
        {
            EnsureKey(hashKey);
            EnsureKey(key);

            return _db.HashDelete(hashKey, key);
        }

        public long HashDelete(string hashKey, IEnumerable<string> keys)
        {
            EnsureKey(hashKey);
            EnsureNotNull(nameof(keys), keys);

            return _db.HashDelete(hashKey, keys.Select(x => (RedisValue)x).ToArray());
        }

        public bool HashExists(string hashKey, string key)
        {
            EnsureKey(hashKey);
            EnsureKey(key);

            return _db.HashExists(hashKey, key);
        }

        public T HashGet<T>(string hashKey, string key)
        {
            EnsureKey(hashKey);
            EnsureKey(key);

            var redisValue = _db.HashGet(hashKey, key);
            return redisValue.HasValue ? _serializer.Deserialize<T>(redisValue) : default(T);
        }

        public Dictionary<string, T> HashGet<T>(string hashKey, IEnumerable<string> keys)
        {
            EnsureKey(hashKey);
            EnsureNotNull(nameof(keys), keys);

            return keys.Select(x => new { key = x, value = HashGet<T>(hashKey, x) })
                .ToDictionary(kv => kv.key, kv => kv.value, StringComparer.Ordinal);
        }

        public Dictionary<string, T> HashGetAll<T>(string hashKey)
        {
            EnsureKey(hashKey);

            return _db
                .HashGetAll(hashKey)
                .ToDictionary(
                    x => x.Name.ToString(),
                    x => _serializer.Deserialize<T>(x.Value),
                    StringComparer.Ordinal);
        }

        public long HashIncerementBy(string hashKey, string key, long value)
        {
            EnsureKey(hashKey);
            EnsureKey(key);

            return _db.HashIncrement(hashKey, key, value);
        }

        public double HashIncerementBy(string hashKey, string key, double value)
        {
            EnsureKey(hashKey);
            EnsureKey(key);

            return _db.HashIncrement(hashKey, key, value);
        }

        public IEnumerable<string> HashKeys(string hashKey)
        {
            EnsureKey(hashKey);

            return _db.HashKeys(hashKey).Select(x => x.ToString());
        }

        public long HashLength(string hashKey)
        {
            EnsureKey(hashKey);

            return _db.HashLength(hashKey);
        }

        public bool HashSet<T>(string hashKey, string key, T value, bool nx = false)
        {
            EnsureKey(hashKey);
            EnsureKey(key);

            return _db.HashSet(hashKey, key, _serializer.Serialize(value), nx ? When.NotExists : When.Always);
        }

        public void HashSet<T>(string hashKey, Dictionary<string, T> values)
        {
            EnsureKey(hashKey);
            EnsureNotNull(nameof(values), values);

            var entries = values.Select(kv => new HashEntry(kv.Key, _serializer.Serialize(kv.Value)));
            _db.HashSet(hashKey, entries.ToArray());
        }

        public IEnumerable<T> HashValues<T>(string hashKey)
        {
            EnsureKey(hashKey);

            return _db.HashValues(hashKey).Select(x => _serializer.Deserialize<T>(x));
        }
        #endregion

        #region pub/sub
        public long Publish<T>(RedisChannel channel, T message)
        {
            var sub = _connector.GetSubscriber();
            return sub.Publish(channel, _serializer.Serialize(message));
        }

        public async Task<long> PublishAsync<T>(RedisChannel channel, T message)
        {
            var sub = _connector.GetSubscriber();
            return await sub.PublishAsync(channel, _serializer.Serialize(message))
                .ConfigureAwait(false);
        }

        public void Subscribe<T>(RedisChannel channel, Action<T> handler)
        {
            EnsureNotNull(nameof(handler), handler);

            var sub = _connector.GetSubscriber();
            sub.Subscribe(channel, (redisChannel, value) => handler(_serializer.Deserialize<T>(value)));
        }

        public async Task SubscribeAsync<T>(RedisChannel channel, Func<T, Task> handler)
        {
            EnsureNotNull(nameof(handler), handler);

            var sub = _connector.GetSubscriber();
            await sub.SubscribeAsync(channel, async (redisChannel, value) =>
                await handler(_serializer.Deserialize<T>(value)).ConfigureAwait(false));
        }

        public void Unsubscribe<T>(RedisChannel channel, Action<T> handler)
        {
            EnsureNotNull(nameof(handler), handler);

            var sub = _connector.GetSubscriber();
            sub.Unsubscribe(channel, (redisChannel, value) => handler(_serializer.Deserialize<T>(value)));
        }

        public async Task UnsubscribeAsync<T>(RedisChannel channel, Func<T, Task> handler)
        {
            EnsureNotNull(nameof(handler), handler);

            var sub = _connector.GetSubscriber();
            await sub.UnsubscribeAsync(channel, (redisChannel, value) => handler(_serializer.Deserialize<T>(value)))
                .ConfigureAwait(false);
        }

        public void UnsubscribeAll()
        {
            var sub = _connector.GetSubscriber();
            sub.UnsubscribeAll();
        }

        public async Task UnsubscribeAllAsync()
        {
            var sub = _connector.GetSubscriber();
            await sub.UnsubscribeAllAsync().ConfigureAwait(false);
        }
        #endregion

        public bool UpdateExpiry(string key, DateTimeOffset expiresAt)
        {
            EnsureKey(key);

            return _db.KeyExpire(key, expiresAt.UtcDateTime.Subtract(DateTime.UtcNow));
        }

        public bool UpdateExpiry(string key, TimeSpan expiresIn)
        {
            EnsureKey(key);

            return _db.KeyExpire(key, expiresIn);
        }

        public async Task<bool> UpdateExpiryAsync(string key, DateTimeOffset expiresAt)
        {
            EnsureKey(key);

            return await _db.KeyExpireAsync(key, expiresAt.UtcDateTime.Subtract(DateTime.UtcNow))
                .ConfigureAwait(false);
        }

        public async Task<bool> UpdateExpiryAsync(string key, TimeSpan expiresIn)
        {
            EnsureKey(key);

            return await _db.KeyExpireAsync(key, expiresIn).ConfigureAwait(false);
        }

        public IDictionary<string, bool> UpdateExpiryAll(string[] keys, DateTimeOffset expiresAt)
        {
            EnsureNotNull(nameof(keys), keys);

            var results = new Dictionary<string, bool>(StringComparer.Ordinal);

            for (var i = 0; i < keys.Length; i++)
                results.Add(keys[i], _db.KeyExpire(keys[i], expiresAt.UtcDateTime.Subtract(DateTime.UtcNow)));

            return results;
        }

        public IDictionary<string, bool> UpdateExpiryAll(string[] keys, TimeSpan expiresIn)
        {
            EnsureNotNull(nameof(keys), keys);

            var results = new Dictionary<string, bool>(StringComparer.Ordinal);

            for (var i = 0; i < keys.Length; i++)
                results.Add(keys[i], _db.KeyExpire(keys[i], expiresIn));

            return results;
        }

        public async Task<IDictionary<string, bool>> UpdateExpiryAllAsync(string[] keys, DateTimeOffset expiresAt)
        {
            EnsureNotNull(nameof(keys), keys);

            var results = new Dictionary<string, bool>(StringComparer.Ordinal);

            for (var i = 0; i < keys.Length; i++)
                results.Add(keys[i],
                    await _db.KeyExpireAsync(keys[i], expiresAt.UtcDateTime.Subtract(DateTime.UtcNow))
                    .ConfigureAwait(false));

            return results;
        }

        public async Task<IDictionary<string, bool>> UpdateExpiryAllAsync(string[] keys, TimeSpan expiresIn)
        {
            EnsureNotNull(nameof(keys), keys);

            var results = new Dictionary<string, bool>(StringComparer.Ordinal);

            for (var i = 0; i < keys.Length; i++)
                results.Add(keys[i], await _db.KeyExpireAsync(keys[i], expiresIn).ConfigureAwait(false));

            return results;
        }


        private void EnsureKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("缓存键不能为空");
        }

        private void EnsureNotNull<T>(string name, IEnumerable<T> value)
        {
            EnsureNotNull(name, value);
            if (!value.Any())
                throw new InvalidOperationException("提供的集合中含有空项");
        }

        private void EnsureNotNull(string name, object value)
        {
            if (value == null)
                throw new ArgumentNullException($"{name}不允许为空");
        }
    }
}
