namespace DevHabit.Api.Settings;

/// <summary>
/// Configuration settings for encryption.
/// Key must be Base64 encoded.
/// </summary>
public sealed class EncryptionOptions
{
    public string Key { get; init; }
}
