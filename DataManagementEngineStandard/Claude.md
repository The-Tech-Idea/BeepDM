<!-- smarterasp-pem-rule:2026-10-02 -->
> **Fahad's SmarterASP hosting rule (2026-10-02):** Use IdentityServer's PEM RSA key method,
> parsed in memory. No PFX/PKCS#12 loading, Windows certificate store or user-profile key container;
> do not suggest import-flag retries or changing `Load User Profile`. Follow the hosting rule in
> `The-Tech-Idea/CLAUDE.md` and IdentityServer's `CredentialFiles.RsaKey`/`PemKeyRingCertificate` owners.
> Family Needs uses `DataProtection:Key`, never `CertificatePath`/`CertificatePassword`.
> Preserve each installation's existing keys and database key ring; no new key on startup, disabled
> encryption, key-ring deletion or copying IdentityServer's private key into another application.
> This supersedes older PFX deployment examples. Authentication migration still requires each app
> to protect its own cookies and stored secrets. Check deployed builds and log timestamps.
<!-- /smarterasp-pem-rule -->

