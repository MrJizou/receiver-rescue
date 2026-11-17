# Reporting issues

The launcher only prepares a fixed, reviewed patch and launches it after device checks and explicit user actions. There is no arbitrary firmware upload, shell command, remote-control endpoint or telemetry upload feature.

Report hash-verification bypasses, wrong-target launches or unexpected updater launches to the repository owner using GitHub's private reporting option when available. Do not attach proprietary firmware, credentials, device serial numbers or unredacted personal logs to public issues.

## Protected launch in 0.5.0

The updater now runs from an administrator/SYSTEM-only directory under Program Files after a separate elevated helper validates hashes, directory ownership, ACLs and reparse points. The UI does not elevate the updater directly from its user-writable cache. Failure stops the launch; there is no legacy fallback. All application P/Invoke DLL imports are restricted to System32. See [protected launch design and validation](docs/PROTECTED-LAUNCH.md). Versions before 0.5.0 do not include this staging protection.

## Known limitations

- **Trust of the launcher and administrator.** This is not protection against administrator/kernel compromise or replacement of the unsigned launcher before UAC approval. Nonstandard Program Files permissions or redirected source paths fail closed. Version 0.5.0 has not been exercised with a physical failed receiver.

- **Unsigned binaries.** Neither the launcher nor the patched updater is digitally signed, so Windows SmartScreen or antivirus tools may warn, and Windows 11 Smart App Control, where it is switched on, can block them outright. Do not switch off Windows security features for this tool. Compare the SHA-256 values published in the release with your download before running anything.
- **Logs.** The launcher writes only hashes and status text to its log. Windows error messages can contain file paths. The Copy report button masks your user name and profile path, but you must still review a report before posting it.
- **No hardware backup.** The launcher cannot read back or restore a receiver's previous firmware.

## Supply chain

The only network request is an HTTPS download of the pinned Logitech updater, limited to 12 MB and rejected unless its SHA-256 matches. The repository's build workflow uses actions pinned to commit hashes, read-only repository permission and no secrets. Releases should list the SHA-256 of every file.

If Logitech changes or removes the pinned source download, stop rather than replacing it with another version. Updates to patch addresses and hashes require review and fresh binary validation.
