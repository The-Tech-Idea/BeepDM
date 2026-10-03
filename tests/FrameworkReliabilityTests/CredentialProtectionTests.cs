using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Security;
using Xunit;

namespace FrameworkReliabilityTests;

internal sealed class CredentialTestKeys : IConnectionCredentialKeyProvider
{
    public string CurrentKeyId { get; set; } = "first";
    public Dictionary<string, byte[]> Keys { get; } = new() { ["first"] = RandomNumberGenerator.GetBytes(32) };
    public ReadOnlyMemory<byte> GetKey(string keyId) => Keys.TryGetValue(keyId, out var key) ? key : ReadOnlyMemory<byte>.Empty;
    public IConnectionSecretProtector Protector() => new ConnectionCredentialProtection(new AesGcmConnectionCredentialCipher(this));
}

public sealed class CredentialProtectionTests
{
    internal const string Secret = "sentinel-credential-not-for-storage-3974";
    public static IEnumerable<object[]> StringContainers => (
        "Password ApiKey KeyToken ClientSecret ProxyPassword ClientCertificatePassword OAuthAccessToken OAuthRefreshToken OAuthClientSecret AuthCode " +
        "ConnectionString Parameters AdditionalAuthInfo OAuthCodeVerifier OAuthState Url ProxyUrl AuthUrl TokenUrl OAuthTokenEndpoint " +
        "Authority RedirectUriAuth Resource Audience RedirectUri").Split(' ').Select(name => new object[] { name });

    internal static ConnectionProperties SensitiveConnection() => new()
    {
        ConnectionName = "sensitive", GuidID = "stable-connection-identity", Password = Secret,
        ConnectionString = "Server=local;Password=" + Secret,
        ParameterList = new() { ["token"] = Secret },
        Headers = new() { new WebApiHeader { Headername = "Authorization", Headervalue = Secret } },
        QueryParameters = new() { new WebApiParameter { Key = "token", Value = Secret, ExampleValue = Secret } },
        BodyParameters = new() { new WebApiParameter { Key = "token", DefaultValue = Secret } },
        FormParameters = new() { new WebApiParameter { Key = "token", Description = Secret } },
        FileParameters = new() { new WebApiFileParameter { Key = "token", Value = Secret, Description = Secret } }
    };

    [Theory]
    [MemberData(nameof(StringContainers))]
    public void NamedContainer_IsProtectedRestoredAndRedactedWithoutMutatingInput(string propertyName)
    {
        var protector = new CredentialTestKeys().Protector();
        var connection = new ConnectionProperties { ConnectionName = "one", GuidID = "one-id" };
        var property = typeof(ConnectionProperties).GetProperty(propertyName)!;
        property.SetValue(connection, Secret);
        var stored = protector.Protect(connection);
        Assert.DoesNotContain(Secret, JsonConvert.SerializeObject(stored));
        Assert.False(string.IsNullOrEmpty(stored.ProtectedCredentialPayload));
        Assert.Equal(Secret, property.GetValue(protector.Unprotect(stored)));
        Assert.Equal(Secret, property.GetValue(connection));
        Assert.DoesNotContain(Secret, JsonConvert.SerializeObject(protector.Redact(connection)));
        Assert.Null(protector.Redact(stored).ProtectedCredentialPayload);
    }

    [Fact]
    public void WholeContainers_RoundTripDeeply_AndNeitherProtectionNorRedactionAliasesInput()
    {
        var protector = new CredentialTestKeys().Protector();
        var original = SensitiveConnection();
        var stored = protector.Protect(original);
        Assert.DoesNotContain(Secret, JsonConvert.SerializeObject(stored));
        Assert.Null(stored.ParameterList);
        Assert.Null(stored.Headers);
        var restored = protector.Unprotect(stored);
        Assert.Equal(Secret, restored.ParameterList["token"]);
        Assert.Equal(Secret, restored.Headers[0].Headervalue);
        Assert.Equal(Secret, restored.QueryParameters[0].ExampleValue);
        Assert.Equal(Secret, restored.BodyParameters[0].DefaultValue);
        Assert.Equal(Secret, restored.FormParameters[0].Description);
        Assert.Equal(Secret, restored.FileParameters[0].Description);
        restored.Headers[0].Headervalue = "changed";
        restored.ParameterList["token"] = "changed";
        Assert.Equal(Secret, original.Headers[0].Headervalue);
        Assert.Equal(Secret, original.ParameterList["token"]);
        var redacted = protector.Redact(original);
        Assert.DoesNotContain(Secret, JsonConvert.SerializeObject(redacted));
        Assert.Equal(original.ConnectionName, redacted.ConnectionName);
        Assert.Equal(original.GuidID, redacted.GuidID);
    }

    [Fact]
    public void Redaction_NeedsNoCipherOrKey_EvenForMalformedEnvelope()
    {
        var cipher = new Mock<IConnectionCredentialCipher>(MockBehavior.Strict);
        var protector = new ConnectionCredentialProtection(cipher.Object);
        var connection = SensitiveConnection();
        connection.ProtectedCredentialPayload = Secret;
        Assert.DoesNotContain(Secret, JsonConvert.SerializeObject(protector.Redact(connection)));
        cipher.VerifyNoOtherCalls();
    }

    [Fact]
    public void MetadataOnly_DoesNotInvokeCipher()
    {
        var cipher = new Mock<IConnectionCredentialCipher>(MockBehavior.Strict);
        var protector = new ConnectionCredentialProtection(cipher.Object);
        var stored = protector.Protect(new ConnectionProperties { ConnectionName = "metadata" });
        Assert.Null(stored.ProtectedCredentialPayload);
        Assert.Equal("metadata", protector.Unprotect(stored).ConnectionName);
        cipher.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("mixed")]
    [InlineData("retired-key")]
    [InlineData("wrong-key")]
    public void InvalidProtectedInput_FailsClosedWithoutLeakingProviderMessages(string fault)
    {
        var keys = new CredentialTestKeys();
        var protector = keys.Protector();
        var stored = protector.Protect(SensitiveConnection());
        if (fault == "identity") stored.GuidID = "different-id";
        if (fault == "mixed") stored.Password = Secret;
        if (fault == "retired-key") keys.Keys.Clear();
        if (fault == "wrong-key") keys.Keys["first"] = RandomNumberGenerator.GetBytes(32);
        var failure = Assert.Throws<CryptographicException>(() => protector.Unprotect(stored));
        Assert.DoesNotContain(Secret, failure.ToString());
        Assert.Null(failure.InnerException);
        Assert.NotNull(stored.ProtectedCredentialPayload);
    }

    [Fact]
    public void HostCipherFailure_IsSanitizedAndNeverReturnsPlaintextFallback()
    {
        var cipher = new Mock<IConnectionCredentialCipher>();
        cipher.Setup(c => c.Protect(It.IsAny<string>(), It.IsAny<string>())).Throws(new IOException(Secret));
        var failure = Assert.Throws<CryptographicException>(() => new ConnectionCredentialProtection(cipher.Object).Protect(SensitiveConnection()));
        Assert.DoesNotContain(Secret, failure.ToString());
        Assert.Contains(nameof(IOException), failure.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnsupportedCipher_IsExplicitWithoutPlaintextFallback(bool platform)
    {
        var cipher = new Mock<IConnectionCredentialCipher>();
        cipher.Setup(c => c.Protect(It.IsAny<string>(), It.IsAny<string>())).Throws(platform
            ? new PlatformNotSupportedException(Secret) : new NotSupportedException(Secret));
        var exception = Record.Exception(() => new ConnectionCredentialProtection(cipher.Object).Protect(SensitiveConnection()));
        Assert.NotNull(exception);
        Assert.Equal(platform ? typeof(PlatformNotSupportedException) : typeof(NotSupportedException), exception.GetType());
        Assert.DoesNotContain(Secret, exception.ToString());
    }

    [Theory]
    [InlineData("{\"Version\":2,\"Values\":{}}")]
    [InlineData("{\"Version\":1,\"Version\":1,\"Values\":{}}")]
    [InlineData("{\"Version\":1,\"Values\":{\"Password\":\"one\",\"Password\":\"two\"}}")]
    [InlineData("{\"Version\":1,\"Values\":{\"UnknownSecret\":\"value\"}}")]
    [InlineData("{\"Version\":1,\"Values\":{}} {}")]
    [InlineData("null")]
    [InlineData("{\"Version\":1,\"Values\":[]}")]
    public void DecryptedPayload_RejectsForeignDuplicateAndTrailingContent(string payload)
    {
        var cipher = new Mock<IConnectionCredentialCipher>();
        cipher.Setup(c => c.Unprotect("opaque", It.IsAny<string>())).Returns(payload);
        var protector = new ConnectionCredentialProtection(cipher.Object);
        Assert.Throws<CryptographicException>(() => protector.Unprotect(new ConnectionProperties { ProtectedCredentialPayload = "opaque" }));
    }

    [Fact]
    public void AesRotation_ReadsOldKeys_ReprotectsWithCurrentKey_AndDoesNotZeroHostKeys()
    {
        var keys = new CredentialTestKeys();
        var originalKey = keys.Keys["first"].ToArray();
        var cipher = new AesGcmConnectionCredentialCipher(keys);
        var first = cipher.Protect(Secret, "purpose");
        keys.Keys["next"] = RandomNumberGenerator.GetBytes(32);
        keys.CurrentKeyId = "next";
        Assert.Equal(Secret, cipher.Unprotect(first, "purpose"));
        var second = cipher.Protect(Secret, "purpose");
        Assert.Equal(originalKey, keys.Keys["first"]);
        keys.Keys.Remove("first");
        Assert.Equal(Secret, cipher.Unprotect(second, "purpose"));
        Assert.Throws<CryptographicException>(() => cipher.Unprotect(first, "purpose"));
        Assert.Contains(Convert.ToBase64String(Encoding.UTF8.GetBytes("next")), second);
    }

    [Fact]
    public void AesUsesFreshNonces_AndBindsPurpose()
    {
        var keys = new CredentialTestKeys();
        var key = keys.Keys["first"].ToArray();
        var cipher = new AesGcmConnectionCredentialCipher(keys);
        var one = cipher.Protect(Secret, "one");
        var two = cipher.Protect(Secret, "one");
        Assert.NotEqual(one, two);
        Assert.Equal(Secret, cipher.Unprotect(one, "one"));
        Assert.ThrowsAny<CryptographicException>(() => cipher.Unprotect(one, "two"));
        Assert.Equal(key, keys.Keys["first"]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void AesAuthenticatesVersionKeyIdentityNonceTagAndCiphertext(int component)
    {
        var keys = new CredentialTestKeys();
        keys.Keys["alias"] = keys.Keys["first"];
        var cipher = new AesGcmConnectionCredentialCipher(keys);
        var parts = cipher.Protect(Secret, "purpose").Split(':');
        if (component == 1) parts[component] = "2";
        else if (component == 3) parts[component] = Convert.ToBase64String(Encoding.UTF8.GetBytes("alias"));
        else
        {
            var bytes = Convert.FromBase64String(parts[component]);
            bytes[0] ^= 1;
            parts[component] = Convert.ToBase64String(bytes);
        }
        Assert.ThrowsAny<CryptographicException>(() => cipher.Unprotect(string.Join(':', parts), "purpose"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(24)]
    public void AesRequiresAvailable256BitHostKey(int length)
    {
        var keys = new CredentialTestKeys();
        keys.Keys["first"] = new byte[length];
        Assert.Throws<CryptographicException>(() => new AesGcmConnectionCredentialCipher(keys).Protect(Secret, "purpose"));
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("legacy")]
    [InlineData("size")]
    public void Protection_RejectsMissingIdentityUnreadableLegacyAndOversizedSecret(string fault)
    {
        var protector = new CredentialTestKeys().Protector();
        var connection = new ConnectionProperties { ConnectionName = "test", Password = Secret };
        if (fault == "identity") connection.GuidID = "";
        if (fault == "legacy") connection.Password = "__enc__:unreadable";
        if (fault == "size") connection.Password = new string('x', 8 * 1024 * 1024 + 1);
        var error = Record.Exception(() => protector.Protect(connection));
        Assert.NotNull(error);
        Assert.Equal(fault == "legacy" ? typeof(NotSupportedException) : typeof(CryptographicException), error.GetType());
        Assert.Null(connection.ProtectedCredentialPayload);
    }

    [Fact]
    public void WindowsDpapi_NewEnvelopeAndLegacyUpgrade_RoundTripOrExplicitlyRejectPlatform()
    {
        var protector = ConnectionCredentialProtection.Default;
        if (!OperatingSystem.IsWindows())
        {
            Assert.Throws<PlatformNotSupportedException>(() => protector.Protect(SensitiveConnection()));
            return;
        }
        var connection = SensitiveConnection();
        connection.Password = "__enc__:" + Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(Secret), null, DataProtectionScope.CurrentUser));
        var stored = protector.Protect(connection);
        Assert.StartsWith("__beepenc__:1:dpapi-user:", stored.ProtectedCredentialPayload);
        Assert.Equal(Secret, protector.Unprotect(stored).Password);
        Assert.DoesNotContain(Secret, JsonConvert.SerializeObject(stored));
        stored.GuidID = "different-id";
        Assert.Throws<CryptographicException>(() => protector.Unprotect(stored));
    }
}
