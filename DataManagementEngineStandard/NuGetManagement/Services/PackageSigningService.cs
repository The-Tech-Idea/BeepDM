using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging;
using NuGet.Packaging.Signing;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.NuGetManagement.Services
{
    /// <summary>
    /// Provides NuGet package signature verification capabilities.
    /// </summary>
    public class PackageSigningService
    {
        private readonly IDMLogger _logger;

        /// <summary>
        /// Initializes a new instance of the PackageSigningService.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output.</param>
        public PackageSigningService(IDMLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Checks if a NuGet package file is signed.
        /// </summary>
        /// <param name="packagePath">Path to the .nupkg file.</param>
        /// <returns>True if the package contains a signature; otherwise, false.</returns>
        public async Task<bool> IsSignedAsync(string packagePath)
        {
            try
            {
                if (!File.Exists(packagePath))
                {
                    _logger?.LogWithContext($"Package file not found: {packagePath}", null);
                    return false;
                }

                using (var packageStream = File.OpenRead(packagePath))
                using (var packageReader = new PackageArchiveReader(packageStream))
                {
                    var signature = await packageReader.GetPrimarySignatureAsync(CancellationToken.None);
                    return signature != null;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error checking signature for {packagePath}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Verifies a NuGet package signature including certificate validation.
        /// </summary>
        /// <param name="packagePath">Path to the .nupkg file.</param>
        /// <param name="allowUntrusted">If true, allows untrusted certificates.</param>
        /// <returns>A <see cref="SignatureVerificationResult"/> with detailed verification information.</returns>
        public async Task<SignatureVerificationResult> VerifySignatureAsync(string packagePath, bool allowUntrusted = false)
        {
            var result = new SignatureVerificationResult { PackagePath = packagePath };

            try
            {
                if (!File.Exists(packagePath))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Package file not found: {packagePath}";
                    return result;
                }

                using (var packageStream = File.OpenRead(packagePath))
                using (var packageReader = new PackageArchiveReader(packageStream))
                {
                    var signature = await packageReader.GetPrimarySignatureAsync(CancellationToken.None);
                    
                    if (signature == null)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = "Package is not signed";
                        result.IsSigned = false;
                        return result;
                    }

                    result.IsSigned = true;
                    result.SignatureType = signature.Type.ToString();

                    // Extract certificate information
                    X509Certificate2 certificate = null;
                    if (signature.SignerInfo != null)
                    {
                        certificate = signature.SignerInfo.Certificate;
                        if (certificate != null)
                        {
                            result.Subject = certificate.Subject;
                            result.Issuer = certificate.Issuer;
                            result.Thumbprint = certificate.Thumbprint;
                            result.ValidFrom = certificate.NotBefore;
                            result.ValidTo = certificate.NotAfter;
                            result.IsExpired = DateTime.Now > certificate.NotAfter;
                            result.IsNotYetValid = DateTime.Now < certificate.NotBefore;
                        }
                    }

                    // Verify certificate chain
                    if (!allowUntrusted && certificate != null)
                    {
                        using (var chain = new X509Chain())
                        {
                            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                            chain.ChainPolicy.VerificationTime = DateTime.Now;

                            var chainBuilt = chain.Build(certificate);
                            if (!chainBuilt)
                            {
                                result.IsValid = false;
                                result.ErrorMessage = $"Certificate chain validation failed: {string.Join(", ", chain.ChainStatus.Select(s => s.StatusInformation))}";
                                return result;
                            }
                        }
                    }

                    // Determine signature type from the signature
                    var signatureType = signature.Type.ToString().ToLowerInvariant();
                    if (signatureType.Contains("repository"))
                    {
                        result.IsRepositorySignature = true;
                    }
                    else if (signatureType.Contains("author"))
                    {
                        result.IsAuthorSignature = true;
                    }

                    result.IsValid = !result.IsExpired && !result.IsNotYetValid;
                    
                    if (result.IsValid)
                    {
                        _logger?.LogWithContext($"Package signature verified: {Path.GetFileName(packagePath)} by {result.Subject}", null);
                    }
                    else
                    {
                        result.ErrorMessage = result.IsExpired ? "Certificate has expired" : "Certificate is not yet valid";
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Error verifying signature: {ex.Message}";
                _logger?.LogWithContext($"Signature verification failed for {packagePath}", ex);
            }

            return result;
        }

        /// <summary>
        /// Gets detailed signature information without full verification.
        /// </summary>
        /// <param name="packagePath">Path to the .nupkg file.</param>
        /// <returns>A <see cref="SignatureInfo"/> object with signature details.</returns>
        public async Task<SignatureInfo> GetSignatureInfoAsync(string packagePath)
        {
            var info = new SignatureInfo { PackagePath = packagePath };

            try
            {
                if (!File.Exists(packagePath))
                    return info;

                using (var packageStream = File.OpenRead(packagePath))
                using (var packageReader = new PackageArchiveReader(packageStream))
                {
                    var signature = await packageReader.GetPrimarySignatureAsync(CancellationToken.None);
                    
                    if (signature == null)
                        return info;

                    info.IsSigned = true;
                    info.SignatureType = signature.Type.ToString();
                    var timestamp = signature.Timestamps.FirstOrDefault()?.GeneralizedTime;
                    info.Timestamp = timestamp.HasValue ? timestamp.Value.DateTime : (DateTime?)null;

                    if (signature.SignerInfo?.Certificate != null)
                    {
                        var cert = signature.SignerInfo.Certificate;
                        info.Subject = cert.Subject;
                        info.Issuer = cert.Issuer;
                        info.Thumbprint = cert.Thumbprint;
                        info.ValidFrom = cert.NotBefore;
                        info.ValidTo = cert.NotAfter;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error getting signature info for {packagePath}", ex);
            }

            return info;
        }
    }

    /// <summary>
    /// Represents the result of a signature verification operation.
    /// </summary>
    public class SignatureVerificationResult
    {
        /// <summary>The package file path.</summary>
        public string PackagePath { get; set; }
        /// <summary>True if the package is signed.</summary>
        public bool IsSigned { get; set; }
        /// <summary>True if the signature is valid and trusted.</summary>
        public bool IsValid { get; set; }
        /// <summary>Error message if verification failed.</summary>
        public string ErrorMessage { get; set; }
        /// <summary>Type of signature (Author, Repository).</summary>
        public string SignatureType { get; set; }
        /// <summary>Certificate subject.</summary>
        public string Subject { get; set; }
        /// <summary>Certificate issuer.</summary>
        public string Issuer { get; set; }
        /// <summary>Certificate thumbprint.</summary>
        public string Thumbprint { get; set; }
        /// <summary>Certificate validity start date.</summary>
        public DateTime ValidFrom { get; set; }
        /// <summary>Certificate validity end date.</summary>
        public DateTime ValidTo { get; set; }
        /// <summary>True if certificate has expired.</summary>
        public bool IsExpired { get; set; }
        /// <summary>True if certificate is not yet valid.</summary>
        public bool IsNotYetValid { get; set; }
        /// <summary>True if this is an author signature.</summary>
        public bool IsAuthorSignature { get; set; }
        /// <summary>True if this is a repository signature.</summary>
        public bool IsRepositorySignature { get; set; }
        /// <summary>The repository URL if repository signature.</summary>
        public string RepositoryUrl { get; set; }
    }

    /// <summary>
    /// Represents basic signature information without verification status.
    /// </summary>
    public class SignatureInfo
    {
        /// <summary>The package file path.</summary>
        public string PackagePath { get; set; }
        /// <summary>True if the package is signed.</summary>
        public bool IsSigned { get; set; }
        /// <summary>Type of signature.</summary>
        public string SignatureType { get; set; }
        /// <summary>Certificate subject.</summary>
        public string Subject { get; set; }
        /// <summary>Certificate issuer.</summary>
        public string Issuer { get; set; }
        /// <summary>Certificate thumbprint.</summary>
        public string Thumbprint { get; set; }
        /// <summary>Certificate validity start date.</summary>
        public DateTime ValidFrom { get; set; }
        /// <summary>Certificate validity end date.</summary>
        public DateTime ValidTo { get; set; }
        /// <summary>Signature timestamp if available.</summary>
        public DateTime? Timestamp { get; set; }
    }
}
