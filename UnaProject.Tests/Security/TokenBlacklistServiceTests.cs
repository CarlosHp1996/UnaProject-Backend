using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using UnaProject.Application.Services;
using Xunit;

namespace UnaProject.Tests.Security
{
    /// <summary>
    /// Testa TokenBlacklistService (revogação de JWT via IDistributedCache).
    /// Usa um fake de IDistributedCache em memória (Dictionary) para não depender de Redis.
    /// </summary>
    public class TokenBlacklistServiceTests
    {
        private const string Key = "chave-super-secreta-de-teste-com-mais-de-32-bytes-1234567890";

        private static string BuildJwt(TimeSpan lifetime)
        {
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "una",
                audience: "una",
                claims: new[] { new Claim("id", Guid.NewGuid().ToString()) },
                expires: DateTime.UtcNow.Add(lifetime),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Fact]
        public async Task BlacklistedToken_IsReportedAsBlacklisted()
        {
            var sut = new TokenBlacklistService(new FakeDistributedCache());
            var token = BuildJwt(TimeSpan.FromHours(1));

            await sut.BlacklistTokenAsync(token);

            (await sut.IsBlacklistedAsync(token)).Should().BeTrue();
        }

        [Fact]
        public async Task UnknownToken_IsNotBlacklisted()
        {
            var sut = new TokenBlacklistService(new FakeDistributedCache());
            var token = BuildJwt(TimeSpan.FromHours(1));

            (await sut.IsBlacklistedAsync(token)).Should().BeFalse();
        }

        [Fact]
        public async Task InvalidToken_IsIgnored_AndNotBlacklisted()
        {
            var sut = new TokenBlacklistService(new FakeDistributedCache());

            await sut.BlacklistTokenAsync("nao-e-um-jwt");

            (await sut.IsBlacklistedAsync("nao-e-um-jwt")).Should().BeFalse();
        }

        [Fact]
        public async Task AlreadyExpiredToken_IsNotStored()
        {
            var cache = new FakeDistributedCache();
            var sut = new TokenBlacklistService(cache);
            var expired = BuildJwt(TimeSpan.FromMinutes(-5));

            await sut.BlacklistTokenAsync(expired);

            // Nada foi armazenado: TTL <= 0 => o serviço nem chama o cache.
            cache.Store.Should().BeEmpty();
            (await sut.IsBlacklistedAsync(expired)).Should().BeFalse();
        }

        /// <summary>Fake mínimo de IDistributedCache backed por dicionário (sem expiração real).</summary>
        private sealed class FakeDistributedCache : IDistributedCache
        {
            public readonly Dictionary<string, byte[]> Store = new();

            public byte[]? Get(string key) => Store.TryGetValue(key, out var v) ? v : null;
            public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));
            public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => Store[key] = value;
            public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
            {
                Set(key, value, options);
                return Task.CompletedTask;
            }
            public void Refresh(string key) { }
            public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
            public void Remove(string key) => Store.Remove(key);
            public Task RemoveAsync(string key, CancellationToken token = default)
            {
                Remove(key);
                return Task.CompletedTask;
            }
        }
    }
}
