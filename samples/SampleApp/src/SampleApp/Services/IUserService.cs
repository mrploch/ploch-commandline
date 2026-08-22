using Ploch.CommandLine.Spectre.SampleApp.Services.Models;

namespace Ploch.CommandLine.Spectre.SampleApp.Services;

/// <summary>
///     Service for managing users in the sample application.
/// </summary>
public interface IUserService
{
    Task<UserProfile> CreateUserAsync(string name, string email, string role, CancellationToken cancellationToken = default);

    Task<IEnumerable<UserProfile>> GetUsersAsync(bool activeOnly = false, CancellationToken cancellationToken = default);

    Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken = default);
}
