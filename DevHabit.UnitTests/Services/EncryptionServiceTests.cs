using DevHabit.Api.Services;
using DevHabit.Api.Settings;
using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography;

namespace DevHabit.UnitTests.Services;
public sealed class EncryptionServiceTests
{
    private readonly EncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        IOptions<EncryptionOptions> options = Options.Create(new EncryptionOptions
        {
            Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        });

        _encryptionService = new EncryptionService(options);
    }

    [Fact]
    public void Decrypt_ShouldReturnPlainText_WhenDecryptingCorrectCiphertext()
    {
        //Arrange
        const string plainText = "senstive data";
        string ciphertext = _encryptionService.Encrypt(plainText);

        //Act
        string decryptedCiphertext = _encryptionService.Decrypt(ciphertext);

        //Assert
        Assert.Equal(plainText, decryptedCiphertext);
    }

}
