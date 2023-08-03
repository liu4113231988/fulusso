using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace FuLu.IdentityServer.Stores
{
    public class ResourceStore : IResourceStore
    {
        private readonly IEnumerable<IdentityResource> _identityResources;
        private readonly IEnumerable<ApiResource> _apis;
        private readonly IEnumerable<ApiScope> _scopes;
        public ResourceStore()
        {
            _identityResources = new[] {
                new IdentityResources.OpenId()
            };
            _apis = new[]
            {
                new ApiResource("api", "api")
                    {
                        ApiSecrets =  new List<Secret>{ new Secret("secret".Sha256()) },
                        //配置token携带的claim
                        UserClaims = new List<string>
                        {
                            JwtClaimTypes.Name,
                            JwtClaimTypes.NickName,
                            JwtClaimTypes.Email,
                            JwtClaimTypes.PhoneNumber,
                            JwtClaimTypes.Id,
                            JwtClaimTypes.ClientId,
                            JwtClaimTypes.Role,
                            ClaimTypes.Role,
                            CusClaimTypes.OpenId,
                            CusClaimTypes.BindingStatus,
                            CusClaimTypes.LastLoginAddress,
                            CusClaimTypes.LastLoginIp,
                            CusClaimTypes.LoginAddress,
                            CusClaimTypes.LoginIp
                        },
                        Scopes =  new List<string>
                        {
                            "get_user_info",
                            "api"
                        }
                    }
            };
            _scopes = new ApiScope[] {
            new ApiScope("get_user_info"),
            new ApiScope("api")
            };
        }

        public Task<ApiResource> FindApiResourceAsync(string name)
        {
            return Task.FromResult(_apis.FirstOrDefault(x => x.Name == name));
        }

        public Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            return Task.FromResult(_apis.Where(x => x.Name == "api"));
        }

        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            return Task.FromResult(_identityResources.Where(x => scopeNames.Contains(x.Name)));
        }

        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            return Task.FromResult(_identityResources.Where(x => scopeNames.Contains(x.Name)));
        }

        public Task<Resources> GetAllResourcesAsync()
        {
            return Task.FromResult(new Resources(_identityResources, _apis, _scopes));
        }
    }
}
