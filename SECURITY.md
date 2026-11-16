# Reporting issues

The launcher only prepares a fixed, reviewed patch and launches it after device checks and explicit user actions. There is no arbitrary firmware upload, shell command, remote-control endpoint or telemetry upload feature.

Report hash-verification bypasses, wrong-target launches or unexpected updater launches to the repository owner using GitHub's private reporting option when available. Do not attach proprietary firmware, credentials, device serial numbers or unredacted personal logs to public issues.

## Known limitations

- **Elevated launch from a user-writable folder.** The patched updater is written under `%LOCALAPPDATA%\ReceiverRescue` and launched with administrator permission. Since 0.4.0 its SHA-256 is re-verified while the launcher holds a handle that denies writes, renames and deletes, and the process is created before that handle is released, so the file cannot be swapped between the check and the launch. Since 0.4.2 the updater is started from a new, private folder that holds nothing but the verified file, and the launch is refused if anything else appears beside it. Earlier versions started it from a long-lived folder, where another program running in your account could have left a DLL (for example `hid.dll`) that the elevated updater would load. The folder is still writable by your account, so a program that watches for it could try to add a file in the short moment between the check and the start; this cannot be closed completely without installing into a protected location. Malware that already runs with your account could also tamper with the launcher program itself before you start it. Run it only on a PC you trust, compare the published SHA-256 values with your download and read the Windows administrator prompt.
- **Unsigned binaries.** Neither the launcher nor the patched updater is digitally signed, so Windows SmartScreen or antivirus tools may warn, and Windows 11 Smart App Control, where it is switched on, can block them outright. Do not switch off Windows security features for this tool. Compare the SHA-256 values published in the release with your download before running anything.
- **Logs.** The launcher writes only hashes and status text to its log. Windows error messages can contain file paths. The Copy report button masks your user name and profile path, but you must still review a report before posting it.
- **No hardware backup.** The launcher cannot read back or restore a receiver's previous firmware.

## Supply chain

The only network request is an HTTPS download of the pinned Logitech updater, limited to 12 MB and rejected unless its SHA-256 matches. The repository's build workflow uses actions pinned to commit hashes, read-only repository permission and no secrets. Releases should list the SHA-256 of every file.

If Logitech changes or removes the pinned source download, stop rather than replacing it with another version. Updates to patch addresses and hashes require review and fresh binary validation.
