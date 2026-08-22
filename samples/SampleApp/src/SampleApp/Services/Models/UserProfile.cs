namespace Ploch.CommandLine.Spectre.SampleApp.Services.Models;

/// <summary>
///     Represents a sample user profile.
/// </summary>
public record UserProfile(int Id, string Name, string Email, string Role, bool IsActive, DateTime CreatedAt);
