# Protected updater launch — 0.5.0

The UI elevates this same application in a narrowly scoped `--protected-launch` command mode. It accepts only `probe` or `recovery` plus a base64-encoded source path. The expected hashes and updater arguments are compiled constants; callers cannot choose another hash, output directory or command.

The elevated helper reads exactly 6,259,024 bytes from a fixed local drive, rejects reparse points anywhere in the source path, and verifies the pinned digest. Under the actual Program Files directory it creates `ReceiverRescue.SecureRuns` with a protected DACL owned by Administrators and writable only by Administrators/SYSTEM. It validates existing directories instead of repairing unknown permissions, rejects reparse points, checks resolved paths, and holds read handles that deny directory rename/delete. Each run gets a unique directory with the same explicit permissions.

The protected copy is re-hashed under a write/delete lock. Device eligibility is re-scanned after UAC and before the fixed updater is started. Its working directory, TEMP and TMP point to the protected run directory, and PATH is limited to the Windows system directory. The helper waits for the updater to exit before releasing the file lock and attempts to remove its own run directory. Failures do not fall back to the old elevated launch path. A completed helper is not proof that firmware recovery succeeded.

## Validation on 17 November 2026

- Automated builds and application self-tests passed locally for both source copies.
- ACL/owner/null-DACL policy, invalid modes/paths, bounded pinned reads, directory rename denial, unelevated refusal and legacy elevated-path rejection passed local regression tests.
- The owner ran the separate administrator-only security test and reported PASS. This test checks the actual protected directory, source/staging symlink rejection and DLL planting/overwrite/delete/DACL-change attempts under a restricted token. It does not access USB or launch firmware tools. This is a user-reported integration result, not a captured device-recovery test.
- No 0.5.0 firmware recovery was performed. Firmware resources, patch manifests and pinned output hashes were not changed. The protected launch flow still requires validation with a matching failed receiver.

## Limits and troubleshooting

This is not protection against an administrator, kernel compromise, or replacement of the unsigned launcher before the user approves UAC. Use a trusted launcher from a trusted PC. Nonstandard Program Files permissions, redirected source paths, long/UNC/network paths or policy restrictions can cause refusal; there is deliberately no unsafe fallback. Do not disable UAC, antivirus or Smart App Control.

`ReceiverRescue.exe --protected-self-test` is the isolated administrator test. Run it with administrator permission only when explicitly testing. It creates harmless data and a symlink in its own protected test directory, impersonates a restricted token, and removes the test directory. The empty `ReceiverRescue.SecureRuns` root can remain. Logs created by the native updater are temporary in the protected run directory; cleanup failure may leave an administrator-only run directory. Do not manually clean anything while an updater is active.
