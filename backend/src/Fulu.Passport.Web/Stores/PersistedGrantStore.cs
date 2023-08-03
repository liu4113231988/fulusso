using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fulu.Passport.Web.Stores;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using StackExchange.Redis;

namespace FuLu.IdentityServer.Stores
{
    public class PersistedGrantStore : IPersistedGrantStore
    {
        private const string _dateFormatString = "yyyy-MM-dd HH:mm:ss";
        private readonly IRedisCache _redisCache;
        protected readonly RedisOperationalStoreOptions options;
        private readonly ILogger<PersistedGrantStore> _logger;
        public PersistedGrantStore(IRedisCache redisCache, ILogger<PersistedGrantStore> logger)
        {
            _redisCache = redisCache;
            _logger = logger;
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            if (string.IsNullOrWhiteSpace(subjectId))
                return new List<PersistedGrant>();

            var db = await _redisCache.GetDatabaseAsync();

            var keys = await db.ListRangeAsync(subjectId);

            var list = new List<PersistedGrant>();
            foreach (string key in keys)
            {
                var items = await db.HashGetAllAsync(key);
                list.Add(GetPersistedGrant(items));
            }

            return list;
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
        {
            filter.Validate();

            var db = await _redisCache.GetDatabaseAsync();

            //var keys = await db.ListRangeAsync(subjectId);

            try
            {
                var setKey = GetSetKey(filter);
                var (grants, keysToDelete) = await GetGrants(db, setKey);
                if (keysToDelete.Any())
                {
                    var keys = keysToDelete.ToArray();
                    var transaction = db.CreateTransaction();
                    await transaction.SetRemoveAsync(GetSetKey(filter.SubjectId), keys);
                    await transaction.SetRemoveAsync(GetSetKey(filter.SubjectId, filter.ClientId), keys);
                    await transaction.SetRemoveAsync(GetSetKeyWithType(filter.SubjectId, filter.ClientId, filter.Type), keys);
                    await transaction.SetRemoveAsync(GetSetKeyWithSession(filter.SubjectId, filter.ClientId, filter.SessionId), keys);
                    await transaction.ExecuteAsync();
                }
                _logger.LogDebug("{grantsCount} persisted grants found for {subjectId}", grants.Count(), filter.SubjectId);
                return grants.Where(_ => _.HasValue).Select(_ => ConvertFromJson(_)).Where(_ => IsMatch(_, filter));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "exception while retrieving grants");
                throw;
            }
        }

        protected virtual string GetSetKey(PersistedGrantFilter filter)
        {
            return (!filter.ClientId.IsNullOrEmpty(), !filter.SessionId.IsNullOrEmpty(), !filter.Type.IsNullOrEmpty()) switch
            {
                (true, true, false) => GetSetKeyWithSession(filter.SubjectId, filter.ClientId, filter.SessionId),
                (true, _, false) => GetSetKey(filter.SubjectId, filter.ClientId),
                (true, _, true) => GetSetKeyWithType(filter.SubjectId, filter.ClientId, filter.Type),
                _ => GetSetKey(filter.SubjectId),
            };
        }


        protected virtual async Task<(IEnumerable<RedisValue> grants, IEnumerable<RedisValue> keysToDelete)> GetGrants(IDatabase db, string setKey)
        {
            var grantsKeys = await db.SetMembersAsync(setKey);
            if (!grantsKeys.Any())
                return (Enumerable.Empty<RedisValue>(), Enumerable.Empty<RedisValue>());
            var grants = await db.StringGetAsync(grantsKeys.Select(_ => (RedisKey)_.ToString()).ToArray());
            var keysToDelete = grantsKeys.Zip(grants, (key, value) => new KeyValuePair<RedisValue, RedisValue>(key, value))
                                         .Where(_ => !_.Value.HasValue).Select(_ => _.Key);
            return (grants, keysToDelete);
        }

        protected string GetKey(string key) => $"{this.options.KeyPrefix}{key}";

        protected string GetSetKey(string subjectId) => $"{this.options.KeyPrefix}{subjectId}";

        protected string GetSetKey(string subjectId, string clientId) => $"{this.options.KeyPrefix}{subjectId}:{clientId}";

        protected string GetSetKeyWithType(string subjectId, string clientId, string type) => $"{this.options.KeyPrefix}{subjectId}:{clientId}:{type}";

        protected string GetSetKeyWithSession(string subjectId, string clientId, string sessionId) => $"{this.options.KeyPrefix}{subjectId}:{clientId}:{sessionId}";

        public async Task<PersistedGrant> GetAsync(string key)
        {
            var db = await _redisCache.GetDatabaseAsync();
            var items = await db.HashGetAllAsync(key);

            return GetPersistedGrant(items);
        }

        protected bool IsMatch(PersistedGrant grant, PersistedGrantFilter filter)
        {
            return (filter.SubjectId.IsNullOrEmpty() ? true : grant.SubjectId == filter.SubjectId)
                && (filter.ClientId.IsNullOrEmpty() ? true : grant.ClientId == filter.ClientId)
                && (filter.SessionId.IsNullOrEmpty() ? true : grant.SessionId == filter.SessionId)
                && (filter.Type.IsNullOrEmpty() ? true : grant.Type == filter.Type);
        }

        #region Json
        protected static string ConvertToJson(PersistedGrant grant)
        {
            return JsonConvert.SerializeObject(grant);
        }

        protected static PersistedGrant ConvertFromJson(string data)
        {
            return JsonConvert.DeserializeObject<PersistedGrant>(data);
        }

        #endregion

        public async Task RemoveAllAsync(string subjectId, string clientId)
        {
            if (string.IsNullOrEmpty(subjectId) || string.IsNullOrEmpty(clientId))
                return;
            var db = await _redisCache.GetDatabaseAsync();
            await db.KeyDeleteAsync($"{subjectId}:{clientId}");
        }

        public async Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            if (string.IsNullOrEmpty(subjectId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(type))
                return;
            var db = await _redisCache.GetDatabaseAsync();
            await db.KeyDeleteAsync($"{subjectId}:{clientId}:{type}");
        }

        public async Task RemoveAllAsync(PersistedGrantFilter filter)
        {
            try
            {
                filter.Validate();
                var db = await _redisCache.GetDatabaseAsync();
                var setKey = GetSetKey(filter);
                var grants = await db.SetMembersAsync(setKey);
                _logger.LogDebug("removing {grantKeysCount} persisted grants from database for subject {subjectId}, clientId {clientId}, grantType {type} and session {session}", grants.Count(), filter.SubjectId, filter.ClientId, filter.Type, filter.SessionId);
                if (!grants.Any()) return;
                var transaction = db.CreateTransaction();
                await transaction.KeyDeleteAsync(grants.Select(_ => (RedisKey)_.ToString()).Concat(new RedisKey[] { setKey }).ToArray());
                await transaction.SetRemoveAsync(GetSetKey(filter.SubjectId), grants);
                await transaction.SetRemoveAsync(GetSetKey(filter.SubjectId, filter.ClientId), grants);
                await transaction.SetRemoveAsync(GetSetKeyWithType(filter.SubjectId, filter.ClientId, filter.Type), grants);
                await transaction.SetRemoveAsync(GetSetKeyWithSession(filter.SubjectId, filter.ClientId, filter.SessionId), grants);
                await transaction.ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "exception removing persisted grants from database for subject {subjectId}, clientId {clientId}, grantType {type} and session {session}", filter.SubjectId, filter.ClientId, filter.Type, filter.SessionId);
                throw;
            }
        }

        public async Task RemoveAsync(string key)
        {
            var db = await _redisCache.GetDatabaseAsync();
            await db.KeyDeleteAsync(key);
        }

        public async Task StoreAsync(PersistedGrant grant)
        {
            //var expiresIn = grant.Expiration - DateTimeOffset.UtcNow;
            var db = await _redisCache.GetDatabaseAsync();

            var trans = db.CreateTransaction();

            var expiry = grant.Expiration.Value.ToLocalTime();

            db.HashSetAsync(grant.Key, GetHashEntries(grant));
            db.KeyExpireAsync(grant.Key, expiry);


            if (!string.IsNullOrEmpty(grant.SubjectId))
            {
                db.ListLeftPushAsync(grant.SubjectId, grant.Key);
                db.KeyExpireAsync(grant.SubjectId, expiry);

                var key1 = $"{grant.SubjectId}:{grant.ClientId}";
                db.ListLeftPushAsync(key1, grant.Key);
                db.KeyExpireAsync(key1, expiry);

                var key2 = $"{grant.SubjectId}:{grant.ClientId}:{grant.Type}";
                db.ListLeftPushAsync(key2, grant.Key);
                db.KeyExpireAsync(key2, expiry);
            }

            await trans.ExecuteAsync();
        }

        private HashEntry[] GetHashEntries(PersistedGrant grant)
        {
            return new[]
            {
                new HashEntry("key", grant.Key),
                new HashEntry("type", grant.Type),
                new HashEntry("sub", grant.SubjectId??""),
                new HashEntry("client", grant.ClientId),
                new HashEntry("create", grant.CreationTime.ToString(_dateFormatString)),
                new HashEntry("expire", grant.Expiration == null ? default(DateTime).ToString(_dateFormatString) : grant.Expiration.Value.ToString(_dateFormatString)),
                new HashEntry("data", grant.Data),
            };
        }

        private PersistedGrant GetPersistedGrant(HashEntry[] entries)
        {
            if (entries.Length != 7)
                return null;

            var grant = new PersistedGrant();
            foreach (var item in entries)
            {
                if (item.Name == "key")
                {
                    grant.Key = item.Value;
                }
                if (item.Name == "type")
                {
                    grant.Type = item.Value;
                }
                if (item.Name == "sub")
                {
                    grant.SubjectId = item.Value;
                }
                if (item.Name == "client")
                {
                    grant.ClientId = item.Value;
                }
                if (item.Name == "create")
                {
                    grant.CreationTime = DateTime.Parse(item.Value);
                }
                if (item.Name == "expire")
                {
                    grant.Expiration = DateTime.Parse(item.Value);
                    if (grant.Expiration.Value == default)
                    {
                        grant.Expiration = null;
                    }
                }
                if (item.Name == "data")
                {
                    grant.Data = item.Value;
                }
            }

            return grant;
        }
    }
}
