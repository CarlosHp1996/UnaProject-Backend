namespace UnaProject.Application.Interfaces
{
    /// <summary>
    /// Abstração para a blacklist de tokens JWT (revogação no logout).
    /// A implementação usa IDistributedCache, então funciona com Redis quando disponível
    /// (sobrevive a restart e escala entre instâncias) e com cache em memória como fallback.
    /// </summary>
    public interface ITokenBlacklistService
    {
        /// <summary>Invalida um token até o seu próprio prazo de expiração (ValidTo).</summary>
        Task BlacklistTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>Retorna true se o token foi revogado e ainda não expirou.</summary>
        Task<bool> IsBlacklistedAsync(string token, CancellationToken cancellationToken = default);
    }
}
