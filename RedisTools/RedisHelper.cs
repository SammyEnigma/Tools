using FastMember;
using RedisTools.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RedisTools
{
    public class RedisHelper
    {
        private IDatabase _db;
        private static ConnectionMultiplexer _connector;
        private ISerializer _serializer;
        private uint _maxBytesLength;

        public IDatabase DB { get { return this._db; } }

        public RedisHelper(string host, ISerializer serializer = null, uint maxBytesLength = 10 * 1024)
        {
            _connector = ConnectionMultiplexer.Connect(host);
            _db = _connector.GetDatabase();
            _serializer = serializer;
            if (_serializer == null)
                _serializer = new NewtonsoftSerializer();
            _maxBytesLength = maxBytesLength;
        }

        public bool Exists(string key, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(key);

            return _db.KeyExists(key, flags);
        }

        public Task<bool> ExistsAsync(string key, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(key);

            return _db.KeyExistsAsync(key, flags);
        }

        public bool Remove(string key, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(key);

            return _db.KeyDelete(key, flags);
        }

        public Task<bool> RemoveAsync(string key, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(key);

            return _db.KeyDeleteAsync(key, flags);
        }

        public long RemoveAll(IEnumerable<string> keys, CommandFlags flags = CommandFlags.None)
        {
            EnsureNotNull(nameof(keys), keys);

            var redisKeys = keys.Select(x => (RedisKey)x).ToArray();
            return _db.KeyDelete(redisKeys, flags);
        }

        public Task<long> RemoveAllAsync(IEnumerable<string> keys, CommandFlags flags = CommandFlags.None)
        {
            EnsureNotNull(nameof(keys), keys);

            var redisKeys = keys.Select(x => (RedisKey)x).ToArray();
            return _db.KeyDeleteAsync(redisKeys, flags);
        }

        public T Get<T>(string key, T defaultVal = default, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(key);

            var valueBytes = _db.StringGet(key, flags);
            if (valueBytes.HasValue)
                return _serializer.Deserialize<T>(valueBytes);

            return defaultVal;
        }

        public T Get<T>(string key, DateTimeOffset expiresAt, T defaultVal = default, CommandFlags flags = CommandFlags.None)
        {
            return Get(key, expiresAt.ToTimeSpan(), defaultVal, flags);
        }

        public T Get<T>(string key, TimeSpan expiresIn, T defaultVal = default, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(key);

            var valueBytes = _db.StringGet(key, flags);
            if (valueBytes.HasValue)
            {
                _db.KeyExpire(key, expiresIn, CommandFlags.FireAndForget);
                return _serializer.Deserialize<T>(valueBytes);
            }

            return defaultVal;
        }

        public async Task<T> GetAsync<T>(string key, T defaultVal = default, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(key);

            var valueBytes = await _db.StringGetAsync(key, flags).ConfigureAwait(false);
            if (valueBytes.HasValue)
                return _serializer.Deserialize<T>(valueBytes);

            return defaultVal;
        }

        public Task<T> GetAsync<T>(string key, DateTimeOffset expiresAt, T defaultVal = default, CommandFlags flags = CommandFlags.None)
        {
            return GetAsync(key, expiresAt.ToTimeSpan(), defaultVal, flags);
        }

        public async Task<T> GetAsync<T>(string key, TimeSpan expiresIn, T defaultVal = default, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(key);

            var valueBytes = await _db.StringGetAsync(key, flags).ConfigureAwait(false);
            if (valueBytes.HasValue)
            {
                await _db.KeyExpireAsync(key, expiresIn, CommandFlags.FireAndForget).ConfigureAwait(false);
                return _serializer.Deserialize<T>(valueBytes);
            }

            return defaultVal;
        }

        public bool Add<T>(string key, T value, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(key);
            // 说明：
            // 这里T有可能是基础类型，这在redisvalue中是有完善的转换支持的，但是：
            // 1. 顶层接口的样子变得很别扭
            // 2. 仍然会有额外的对象生成
            // 因此最后还是选择统一过一次Serialize
            // link: https://stackoverflow.com/questions/15958830/c-sharp-generics-cast-generic-type-to-value-type
            var entryBytes = _serializer.Serialize(value);
            EnsureLength(nameof(value), entryBytes.Length);

            return _db.StringSet(key, entryBytes, null, when, flags);
        }

        public bool Add<T>(string key, T value, DateTimeOffset expiresAt, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return Add(key, value, expiresAt.ToTimeSpan(), when, flags);
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(key);

            var entryBytes = _serializer.Serialize(value);
            EnsureLength(nameof(value), entryBytes.Length);

            return _db.StringSet(key, entryBytes, expiresIn, when, flags);
        }

        public Task<bool> AddAsync<T>(string key, T value, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(key);

            var entryBytes = _serializer.Serialize(value);
            EnsureLength(nameof(value), entryBytes.Length);

            return _db.StringSetAsync(key, entryBytes, null, when, flags);
        }

        public Task<bool> AddAsync<T>(string key, T value, DateTimeOffset expiresAt, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return AddAsync(key, value, expiresAt.ToTimeSpan(), when, flags);
        }

        public Task<bool> AddAsync<T>(string key, T value, TimeSpan expiresIn, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(key);

            var entryBytes = _serializer.Serialize(value);
            EnsureLength(nameof(value), entryBytes.Length);

            return _db.StringSetAsync(key, entryBytes, expiresIn, when, flags);
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys, T defaultVal = default)
        {
            EnsureNotNull(nameof(keys), keys);

            var redisKeys = keys.Select(x => (RedisKey)x).ToArray();
            var result = _db.StringGet(redisKeys);

            var dict = new Dictionary<string, T>(StringComparer.Ordinal);
            for (var i = 0; i < redisKeys.Length; i++)
            {
                var value = result[i];
                dict.Add(redisKeys[i], value.HasValue ? defaultVal : _serializer.Deserialize<T>(value));
            }

            return dict;
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys, DateTimeOffset expiresAt, T defaultVal = default)
        {
            return GetAll(keys, expiresAt.ToTimeSpan(), defaultVal);
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys, TimeSpan expiresIn, T defaultVal = default)
        {
            var result = GetAll(keys, defaultVal);
            foreach (var key in keys)
                _db.KeyExpire(key, expiresIn, CommandFlags.FireAndForget);

            return result;
        }

        public async Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys, T defaultVal = default)
        {
            EnsureNotNull(nameof(keys), keys);

            var redisKeys = keys.Select(x => (RedisKey)x).ToArray();
            var result = await _db.StringGetAsync(redisKeys).ConfigureAwait(false);

            var dict = new Dictionary<string, T>(StringComparer.Ordinal);
            for (var i = 0; i < redisKeys.Length; i++)
            {
                var value = result[i];
                dict.Add(redisKeys[i], value.HasValue ? defaultVal : _serializer.Deserialize<T>(value));
            }

            return dict;
        }

        public Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys, DateTimeOffset expiresAt, T defaultVal = default)
        {
            return GetAllAsync(keys, expiresAt.ToTimeSpan(), defaultVal);
        }

        public async Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys, TimeSpan expiresIn, T defaultVal = default)
        {
            var result = await GetAllAsync(keys, defaultVal).ConfigureAwait(false);
            foreach (var key in keys)
                await _db.KeyExpireAsync(key, expiresIn, CommandFlags.FireAndForget).ConfigureAwait(false);

            return result;
        }

        public bool AddAll<T>(IDictionary<string, T> items, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            EnsureNotNull(nameof(items), items);

            var values = items
                .Select(p => new KeyValuePair<RedisKey, RedisValue>(p.Key, _serializer.Serialize(p.Value)));
            EnsureLength(nameof(items), values);

            return _db.StringSet(values.ToArray(), when, flags);
        }



        public bool AddAll<T>(IDictionary<string, T> items, DateTimeOffset expiresAt, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return AddAll(items, expiresAt.ToTimeSpan(), when, flags);
        }

        public bool AddAll<T>(IDictionary<string, T> items, TimeSpan expiresIn, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            EnsureNotNull(nameof(items), items);

            var values = items
                .Select(p => new KeyValuePair<RedisKey, RedisValue>(p.Key, _serializer.Serialize(p.Value)));
            EnsureLength(nameof(items), values);

            var result = _db.StringSet(values.ToArray(), when, flags);
            foreach (var value in values)
                _db.KeyExpire(value.Key, expiresIn, CommandFlags.FireAndForget);

            return result;
        }

        public Task<bool> AddAllAsync<T>(IDictionary<string, T> items, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            EnsureNotNull(nameof(items), items);

            var values = items
                .Select(p => new KeyValuePair<RedisKey, RedisValue>(p.Key, _serializer.Serialize(p.Value)));
            EnsureLength(nameof(items), values);

            return _db.StringSetAsync(values.ToArray(), when, flags);
        }

        public Task<bool> AddAllAsync<T>(IDictionary<string, T> items, DateTimeOffset expiresAt, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return AddAllAsync(items, expiresAt.ToTimeSpan(), when, flags);
        }

        public async Task<bool> AddAllAsync<T>(IDictionary<string, T> items, TimeSpan expiresIn, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            EnsureNotNull(nameof(items), items);

            var values = items
                .Select(p => new KeyValuePair<RedisKey, RedisValue>(p.Key, _serializer.Serialize(p.Value)));
            EnsureLength(nameof(items), values);

            var result = await _db.StringSetAsync(values.ToArray(), when, flags).ConfigureAwait(false);
            foreach (var value in values)
                await _db.KeyExpireAsync(value.Key, expiresIn, CommandFlags.FireAndForget).ConfigureAwait(false);

            return result;
        }

        #region hash
        public T HashFieldGet<T>(string hashKey, string field, T defaultVal = default, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(hashKey);
            EnsureKey(field);

            var redisValue = _db.HashGet(hashKey, field, flags);
            if (redisValue.HasValue)
                return _serializer.Deserialize<T>(redisValue);

            return defaultVal;
        }

        public async Task<T> HashFieldGetAsync<T>(string hashKey, string field, T defaultVal = default, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(hashKey);
            EnsureKey(field);

            var redisValue = await _db.HashGetAsync(hashKey, field, flags).ConfigureAwait(false);
            if (redisValue.HasValue)
                return _serializer.Deserialize<T>(redisValue);

            return defaultVal;
        }

        public bool HashFieldSet<T>(string hashKey, string field, T value, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(hashKey);
            EnsureKey(field);

            return _db.HashSet(hashKey, field, _serializer.Serialize(value), when, flags);
        }

        public Task<bool> HashFieldSetAsync<T>(string hashKey, string field, T value, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(hashKey);
            EnsureKey(field);

            return _db.HashSetAsync(hashKey, field, _serializer.Serialize(value), when, flags);
        }

        public void HashFieldsSet<T>(string hashKey, Dictionary<string, T> fields, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(hashKey);

            var entries = fields.Select(kv => new HashEntry(kv.Key, _serializer.Serialize(kv.Value)));
            _db.HashSet(hashKey, entries.ToArray(), flags);
        }

        public Task HashFieldsSetAsync<T>(string hashKey, Dictionary<string, T> fields, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(hashKey);

            var entries = fields.Select(kv => new HashEntry(kv.Key, _serializer.Serialize(kv.Value)));
            return _db.HashSetAsync(hashKey, entries.ToArray(), flags);
        }

        public long HashFieldIncr(string hashKey, string field, long value, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(hashKey);
            EnsureKey(field);

            return _db.HashIncrement(hashKey, field, value, flags);
        }

        public double HashFieldIncr(string hashKey, string field, double value, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(hashKey);
            EnsureKey(field);

            return _db.HashIncrement(hashKey, field, value, flags);
        }

        public Task<long> HashFieldIncrAsync(string hashKey, string field, long value, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(hashKey);
            EnsureKey(field);

            return _db.HashIncrementAsync(hashKey, field, value, flags);
        }

        public Task<double> HashFieldIncrAsync(string hashKey, string field, double value, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(hashKey);
            EnsureKey(field);

            return _db.HashIncrementAsync(hashKey, field, value, flags);
        }

        public T HashObjectGet<T>(string hashKey, T model)
            where T : class
        {
            EnsureKey(hashKey);
            EnsureNotNull(nameof(model), model);

            var hashValues = _db.HashGetAll(hashKey);
            var obj = Activator.CreateInstance<T>();
            var acc = TypeAccessor.Create(typeof(T));
            foreach (var val in hashValues)
                acc[obj, val.Name] = val.Value;

            return obj;
        }

        public async Task<T> HashObjectGetAsync<T>(string hashKey, T model)
            where T : class
        {
            EnsureKey(hashKey);
            EnsureNotNull(nameof(model), model);

            var hashValues = await _db.HashGetAllAsync(hashKey).ConfigureAwait(false);
            var obj = Activator.CreateInstance<T>();
            var acc = TypeAccessor.Create(typeof(T));
            foreach (var val in hashValues)
                acc[obj, val.Name] = val.Value;

            return obj;
        }

        public bool HashObjectSet<T>(string hashKey, T model)
            where T : class
        {
            EnsureKey(hashKey);
            EnsureNotNull(nameof(model), model);

            var acc = ObjectAccessor.Create(model);
            var members = acc.TypeAccessor.GetMembers();
            var entrys = members.Select(p => new HashEntry(p.Name, _serializer.Serialize(acc[p.Name]))).ToArray();
            _db.HashSet(hashKey, entrys);

            return true;
        }

        public Task HashObjectSetAsync<T>(string hashKey, T model)
            where T : class
        {
            EnsureKey(hashKey);
            EnsureNotNull(nameof(model), model);

            var acc = ObjectAccessor.Create(model);
            var members = acc.TypeAccessor.GetMembers();
            var entrys = members.Select(p => new HashEntry(p.Name, _serializer.Serialize(acc[p.Name]))).ToArray();

            return _db.HashSetAsync(hashKey, entrys);
        }
        #endregion

        #region pub/sub
        public long Publish<T>(string channel, T message, CommandFlags flags = CommandFlags.None)
        {
            EnsureChannel(channel);

            var sub = _connector.GetSubscriber();
            return sub.Publish(channel, _serializer.Serialize(message), flags);
        }

        public Task<long> PublishAsync<T>(string channel, T message, CommandFlags flags = CommandFlags.None)
        {
            EnsureChannel(channel);

            var sub = _connector.GetSubscriber();
            return sub.PublishAsync(channel, _serializer.Serialize(message), flags);
        }

        public void Subscribe<T>(string channel, Action<T> handler, CommandFlags flags = CommandFlags.None)
        {
            EnsureChannel(channel);
            EnsureNotNull(nameof(handler), handler);

            var sub = _connector.GetSubscriber();
            sub.Subscribe(channel, (redisChannel, value) => handler(_serializer.Deserialize<T>(value)), flags);
        }

        public void Subscribe<T>(string channel, Func<T, Task> handler, CommandFlags flags = CommandFlags.None)
        {
            EnsureChannel(channel);
            EnsureNotNull(nameof(handler), handler);

            var sub = _connector.GetSubscriber();
            sub.Subscribe(channel, async (redisChannel, value) =>
                await handler(_serializer.Deserialize<T>(value)).ConfigureAwait(false), flags);
        }

        public Task SubscribeAsync<T>(string channel, Action<T> handler, CommandFlags flags = CommandFlags.None)
        {
            EnsureChannel(channel);
            EnsureNotNull(nameof(handler), handler);

            var sub = _connector.GetSubscriber();
            return sub.SubscribeAsync(channel, (redisChannel, value) => handler(_serializer.Deserialize<T>(value)), flags);
        }

        public Task SubscribeAsync<T>(string channel, Func<T, Task> handler, CommandFlags flags = CommandFlags.None)
        {
            EnsureChannel(channel);
            EnsureNotNull(nameof(handler), handler);

            var sub = _connector.GetSubscriber();
            return sub.SubscribeAsync(channel, async (redisChannel, value) =>
                await handler(_serializer.Deserialize<T>(value)).ConfigureAwait(false), flags);
        }

        public void Unsubscribe<T>(string channel, Action<T> handler = null, CommandFlags flags = CommandFlags.None)
        {
            EnsureChannel(channel);

            var sub = _connector.GetSubscriber();
            if (handler == null)
                sub.Unsubscribe(channel, flags: flags);
            else
                sub.Unsubscribe(channel, (redisChannel, value) => handler(_serializer.Deserialize<T>(value)), flags);
        }

        public void Unsubscribe<T>(string channel, Func<T, Task> handler = null, CommandFlags flags = CommandFlags.None)
        {
            EnsureChannel(channel);

            var sub = _connector.GetSubscriber();
            if (handler == null)
                sub.Unsubscribe(channel, flags: flags);
            else
                sub.Unsubscribe(channel, (redisChannel, value) => handler(_serializer.Deserialize<T>(value)), flags);
        }

        public Task UnsubscribeAsync<T>(string channel, Action<T> handler = null, CommandFlags flags = CommandFlags.None)
        {
            EnsureChannel(channel);

            var sub = _connector.GetSubscriber();
            if (handler == null)
                return sub.UnsubscribeAsync(channel, flags: flags);
            else
                return sub.UnsubscribeAsync(channel, (redisChannel, value) => handler(_serializer.Deserialize<T>(value)), flags);
        }

        public Task UnsubscribeAsync<T>(string channel, Func<T, Task> handler = null, CommandFlags flags = CommandFlags.None)
        {
            EnsureChannel(channel);
            EnsureNotNull(nameof(handler), handler);

            var sub = _connector.GetSubscriber();
            if (handler == null)
                return sub.UnsubscribeAsync(channel, flags: flags);
            else
                return sub.UnsubscribeAsync(channel, (redisChannel, value) => handler(_serializer.Deserialize<T>(value)), flags);
        }

        public void UnsubscribeAll(CommandFlags flags = CommandFlags.None)
        {
            var sub = _connector.GetSubscriber();
            sub.UnsubscribeAll(flags);
        }

        public Task UnsubscribeAllAsync(CommandFlags flags = CommandFlags.None)
        {
            var sub = _connector.GetSubscriber();
            return sub.UnsubscribeAllAsync(flags);
        }
        #endregion

        public bool UpdateExpiry(string key, DateTimeOffset expiresAt, CommandFlags flags = CommandFlags.None)
        {
            return UpdateExpiry(key, expiresAt.ToTimeSpan(), flags);
        }

        public bool UpdateExpiry(string key, TimeSpan expiresIn, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(key);

            return _db.KeyExpire(key, expiresIn, flags);
        }

        public Task<bool> UpdateExpiryAsync(string key, DateTimeOffset expiresAt, CommandFlags flags = CommandFlags.None)
        {
            return UpdateExpiryAsync(key, expiresAt.ToTimeSpan(), flags);
        }

        public Task<bool> UpdateExpiryAsync(string key, TimeSpan expiresIn, CommandFlags flags = CommandFlags.None)
        {
            EnsureKey(key);

            return _db.KeyExpireAsync(key, expiresIn, flags);
        }

        public IDictionary<string, bool> UpdateExpiryAll(string[] keys, DateTimeOffset expiresAt, CommandFlags flags = CommandFlags.None)
        {
            return UpdateExpiryAll(keys, expiresAt.ToTimeSpan(), flags);
        }

        public IDictionary<string, bool> UpdateExpiryAll(string[] keys, TimeSpan expiresIn, CommandFlags flags = CommandFlags.None)
        {
            EnsureNotNull(nameof(keys), keys);

            var results = new Dictionary<string, bool>(StringComparer.Ordinal);
            for (var i = 0; i < keys.Length; i++)
                results.Add(keys[i], _db.KeyExpire(keys[i], expiresIn, flags));

            return results;
        }

        public Task<IDictionary<string, bool>> UpdateExpiryAllAsync(string[] keys, DateTimeOffset expiresAt, CommandFlags flags = CommandFlags.None)
        {
            return UpdateExpiryAllAsync(keys, expiresAt.ToTimeSpan(), flags);
        }

        public async Task<IDictionary<string, bool>> UpdateExpiryAllAsync(string[] keys, TimeSpan expiresIn, CommandFlags flags = CommandFlags.None)
        {
            EnsureNotNull(nameof(keys), keys);

            var results = new Dictionary<string, bool>(StringComparer.Ordinal);
            for (var i = 0; i < keys.Length; i++)
                results.Add(keys[i], await _db.KeyExpireAsync(keys[i], expiresIn, flags).ConfigureAwait(false));

            return results;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("缓存键不能为空");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureChannel(string channel)
        {
            if (string.IsNullOrEmpty(channel))
                throw new ArgumentNullException("channel不能为空");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureNotNull<T>(string name, IEnumerable<T> values)
        {
            if (values == null || !values.Any())
                throw new InvalidOperationException($"集合{name}为空或未包含项");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureNotNull(string name, object value)
        {
            if (value == null)
                throw new ArgumentNullException($"{name}不允许为空");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureLength(string name, int length)
        {
            if (length > _maxBytesLength)
                throw new ArgumentException($"值{name}的长度不允许超过设定的最大值（MaxBytesLength：{_maxBytesLength}");
        }

        private void EnsureLength(string name, IEnumerable<KeyValuePair<RedisKey, RedisValue>> values)
        {
            if (values.Any(p => p.Value.Length() > _maxBytesLength))
                throw new ArgumentException($"集合{name}某项的长度超过了设定的最大值（MaxBytesLength：{_maxBytesLength}");
        }
    }
}
