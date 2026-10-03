# Connection Credential Persistence

## Public Boundaries

ConfigEditor implements optional Models `IConnectionConfigurationPersistence`.
`SaveDataConnectionsAcknowledged(CancellationToken)` returns Saved, Failed,
Cancelled or Unsupported. Inspect `Status`; do not equate attempted persistence
with acknowledgement. Legacy `SaveDataconnectionsValues()` keeps its signature
but now throws on failed saves. A custom catalog's boolean acknowledgement is
trusted; it owns its protection/storage implementation, not the fallback policy.

The built-in fallback serializes protected snapshots before coordinated atomic
replacement. Existing empty, corrupt or undecryptable data cannot be overwritten
as fresh state. Loading changes the live collection only after successful complete
validation. Missing files are fresh state; failure is not. Saves are explicit
whole-snapshot replacement, not optimistic merge of stale edits.

`BeepConnectionRepository` captures ConnectionsChanged subscribers after a
successful storage operation and invokes them individually outside scope locks.
Observer exceptions do not change the storage result or suppress later observers.
Concrete `NotificationFailed` reports operation/scope/exception type only; failures
in that event or the logger are isolated too. Notifications remain synchronous,
can delay their caller, and may arrive out of commit order under concurrency.
They are refresh signals, not transactional snapshots or a durable event log.

## Per-Runtime Protection

Models declares `IConnectionSecretProtector`, `IConnectionCredentialCipher`,
`IConnectionCredentialKeyProvider` and `IConnectionProtectionContext`.
ConfigEditor captures its policy at construction. For DI, set
`BeepServiceOptions.ConnectionSecretProtector` during AddBeepRuntime registration;
for a direct BeepService, set its property before Configure. Changing that property
after configuration does not replace the existing runtime's captured policy.

The default `ConnectionCredentialProtection` uses Windows DPAPI CurrentUser.
It explicitly rejects credential encryption/decryption on unsupported platforms;
there is no plaintext fallback. Credential-free metadata can work without invoking
the cipher. A Machine/User/Project catalog scope changes catalog selection, not
the DPAPI principal or access-control/key-distribution policy.

For portable hosts, inject `AesGcmConnectionCredentialCipher` with a host-owned
key provider. It requires 32-byte keys, generates a fresh 12-byte nonce per write,
uses a 16-byte authentication tag and authenticates key/version/purpose identity.
The purpose includes GuidID; changing the identity requires decrypt/reprotect.
Encrypted import rename does that before creating the new envelope.

Keys are never generated/persisted by this cipher. The host owns secure key storage,
access, rotation, retirement and recovery. CurrentKeyId selects the write key;
GetKey(storedId) must retain old keys for reads. To rotate, retain the old key,
read/validate all affected records, save with the current key, verify restart and
backups, then retire the old key under the host policy. Do not log keys or place
them in connection files, exports or command-line arguments.

## Coverage And Limits

Protection consolidates an explicit whitelist into ProtectedCredentialPayload:
named passwords/tokens/client secrets, ConnectionString, Parameters, ParameterList,
AdditionalAuthInfo, OAuth verifier/state, authentication/connection/proxy URLs,
Headers, QueryParameters, BodyParameters, FormParameters and FileParameters.
Covered containers are protected wholesale rather than heuristically parsing
arbitrary strings. Source objects/containers are deep-cloned, not mutated.
TypeNameHandling is disabled; supported property types are defined by Models.

Unprotect restores these fields for runtime drivers and clears the opaque payload.
Mixed protected payload plus nonempty plaintext containers, unknown payload fields,
wrong versions/keys/identity, invalid JSON and oversized payloads fail closed.
Provider exception messages are not propagated by the built-in protection wrapper
because they may contain plaintext; failures report the type without inner details.

This is not a vault for arbitrary metadata. Do not embed secrets in names, labels,
uncovered properties, general environment files or diagnostics. Custom ciphers
must actually provide authenticated encryption; injection is not certification.
Plaintext strings remain in managed memory during driver use and serialization;
zeroing local byte buffers does not erase managed strings. Limits are 8 MiB UTF-8
plaintext for AES and 16 Mi characters for the whole-container wrapper; JSON and
envelope overhead reduce usable field size. This is not a cryptographic audit.

## Format, Export And Recovery

New built-in catalog writes/exports explicitly use package/record version 2.0.
Readers accept legacy 1.0 plaintext or legacy named-field DPAPI envelopes and
upgrade records when reprotected. Version 1.0 records containing the new opaque
payload are rejected, not guessed; preserve such provisional/development files
and recover with the matching code/key before importing a valid upgraded package.
Duplicate properties, invalid record identity and unknown versions are rejected.
Whole-scope mutation validates every existing credential snapshot before replacing
or removing records, including other profiles: unavailable old keys block mutation.
Retained 1.0 records can coexist in a newly written 2.0 package until reprotected.

Version-aware 1.0-only readers reject a 2.0 package. This does not make older
unchecked readers safe. Upgrade all writers/readers and verify package consumers;
do not run mixed versions or downgrade without an explicit host migration.
The legacy fallback DataConnections.json remains an array with the added opaque
field, not a version-gated outer package. Old loaders may ignore it; a fallback
downgrade/mixed-reader contract is not established.

`includeEncryptedSecretsOnly=true` means decrypt/reprotect using the selected
policy, not portable-by-default. Import requires the original DPAPI user or the
appropriate supplied key ring; copying ciphertext is not sharing key access.
`false` means redact all covered containers and payload without decryption/key
access. It also removes operational URLs/connection strings; hosts must configure
those values again. It is not plaintext credential export.

Do not delete unreadable files or lock sidecars to bypass failure. Preserve bytes,
restore the matching principal/key or verified backup, and use an explicit recovery
workflow. Automatic key recovery, distributed stores, power-loss guarantees and
all-platform qualification are not implemented. See
[atomic persistence boundaries](../Services/Persistence/README.md).
