using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using UnaProject.Application.Interfaces;

namespace UnaProject.Application.Services
{
    /// <summary>
    /// Blacklist de tokens JWT baseada em IDistributedCache.
    /// - Com Redis: revogação persiste entre restarts e escala entre instâncias.
    /// - Sem Redis (fallback em memória): comportamento equivalente ao antigo dicionário estático.
    /// A entrada expira automaticamente junto com o próprio token (AbsoluteExpirationRelativeToNow = ValidTo - agora),
    /// então não precisa de limpeza manual de tokens expirados.
    /// </summary>
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private const string KeyPrefix = "jwt-blacklist:";
        private readonly IDistributedCache _cache;

        public TokenBlacklistService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task BlacklistTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;

            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
                return; // token inválido — nada a revogar

            var expires = handler.ReadJwtToken(token).ValidTo; // sempre em UTC
            var ttl = expires - DateTime.UtcNow;
            if (ttl <= TimeSpan.Zero)
                return; // já expirou; a validação de tempo do JWT já o rejeita

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };

            await _cache.SetStringAsync(BuildKey(token), "1", options, cancellationToken);
        }

        public async Task<bool> IsBlacklistedAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var value = await _cache.GetStringAsync(BuildKey(token), cancellationToken);
            return value != null;
        }

        // Usa o hash do token como chave (evita chaves gigantes no cache e não expõe o token bruto no store).
        private static string BuildKey(string token)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return KeyPrefix + Convert.ToHexString(hash);
        }
    }
}
