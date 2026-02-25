using System.Security.Cryptography;
using DevHabit.Api.Settings;
using Microsoft.Extensions.Options;

namespace DevHabit.Api.Services;

/// <summary>
/// Provides AES encryption and decryption using a master key from configuration.
/// Uses CBC mode with PKCS7 padding and prepends IV to the cipher text.
/// </summary>
public sealed class EncryptionService(IOptions<EncryptionOptions> options)
{
    // Master key loaded from configuration (Base64 encoded)
    private readonly byte[] _masterKey = Convert.FromBase64String(options.Value.Key);

    // AES block size for IV (16 bytes = 128 bits)
    private const int IvSize = 16;

    /// <summary>
    /// Encrypts plain text using AES CBC and returns Base64 string.
    /// IV is generated randomly and prepended to output.
    /// </summary>
    public string Encrypt(string plainText)
    {
        try
        {
            using var aes = Aes.Create();

            // Configure AES settings
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = _masterKey;

            // Generate random IV for each encryption
            aes.IV = RandomNumberGenerator.GetBytes(IvSize);

            using var memoryStream = new MemoryStream();

            // Store IV at beginning of output
            memoryStream.Write(aes.IV, 0, IvSize);

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            using (var streamWriter = new StreamWriter(cryptoStream))
            {
                // Write plain text into crypto stream to encrypt
                streamWriter.Write(plainText);
            }

            // Return combined IV + cipher as Base64
            return Convert.ToBase64String(memoryStream.ToArray());
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Encryption Failed", ex);
        }
    }

    /// <summary>
    /// Decrypts Base64 cipher text produced by Encrypt().
    /// Extracts IV then decrypts remaining bytes.
    /// </summary>
    public string Decrypt(string cipherText)
    {
        try
        {
            byte[] cipherData = Convert.FromBase64String(cipherText);

            // Validate minimum length (must contain IV)
            if (cipherData.Length < IvSize)
            {
                throw new InvalidOperationException("Invalid cipher text format");
            }

            // Extract IV and encrypted payload
            byte[] iv = new byte[IvSize];
            byte[] encryptedData = new byte[cipherData.Length - IvSize];

            Buffer.BlockCopy(cipherData, 0, iv, 0, IvSize);
            Buffer.BlockCopy(cipherData, IvSize, encryptedData, 0, encryptedData.Length);

            using var aes = Aes.Create();

            // Configure AES with extracted IV
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = _masterKey;
            aes.IV = iv;

            using MemoryStream memoryStream = new(encryptedData);
            using ICryptoTransform cryptoTransform = aes.CreateDecryptor();
            using CryptoStream cryptoStream = new(memoryStream, cryptoTransform, CryptoStreamMode.Read);
            using StreamReader streamReader = new(cryptoStream);

            // Read decrypted plain text
            return streamReader.ReadToEnd();
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }
}
