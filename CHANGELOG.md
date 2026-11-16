# Changelog

## 0.4.2 — private launch folder

- Security fix: the prepared updater is now started from a new, private folder that contains only the verified file, and the launch is refused if any other file appears beside it. Before this, it was started with administrator permission from a long-lived folder under `%LOCALAPPDATA%`, where another program running in the same account could leave a DLL that the elevated updater would load. Leftover private folders are removed.
- `--self-test` now checks the private folder, the refusal when a DLL is planted beside the tool, and the clean-up.
- Document that Windows 11 Smart App Control can block the unsigned programs.
- Device logic, target guards, patch manifests and output hashes are unchanged. The new launch path was tested with a harmless executable, including once under a real Windows administrator prompt (it started elevated from a folder holding only the tool, a write to the locked file was blocked, a planted DLL stopped the launch before any prompt, and a declined prompt started nothing). It has not been exercised against a physical failed receiver.

## 0.4.1 — plainer wording

- Replace the unclear "bootloader candidate" status with "Receiver found in recovery mode" and explain in one sentence that Standard and Shroud receivers look the same in this mode, so the user confirms the model. Other status messages now say "recovery mode" or "update mode" instead of "bootloader"; the technical USB ID stays visible.
- Device logic, target guards, patch manifests and output hashes are unchanged.

## 0.4.0 — launch safety and checks

- Show the elapsed time while the Logitech tool runs and record the total duration in the log.
- Launch the prepared updater through a write-locked handle: its SHA-256 is re-verified under a lock that denies writes, renames and deletes, and the process is created before the lock is released, so the file cannot be swapped between the check and the launch. A declined Windows administrator prompt now reports that nothing was started.
- After a recovery, automatically check (read-only) whether Windows lists a working PRO X audio device, retrying for about 12 seconds, with a "Check again" button.
- Replace the plain log copy with a structured report: launcher, Windows and .NET versions, interface language, device-state history, automatic audio result and checklist. User name, machine name and profile paths are masked.
- `--scan` also prints how many PRO X Windows audio devices are present.
- Add `--ui-test` (layout self-check across Turkish/English, three window widths and all device states) and extend `--self-test` with the locked launch, elapsed-time formatting, report content and personal-data masking.
- Device logic, target guards, patch manifests and output hashes are unchanged. The locked launch was verified with a harmless executable under a real Windows administrator prompt; it has not been exercised against a physical failed receiver.

## 0.3.0 — interface redesign

- Replace the MIT license with a proprietary, non-open-source license (personal recovery of your own matching receiver only).

- Rebuild the interface as a three-step flow with explicit done / active / locked states and a reason shown for every locked step.
- Status is conveyed by icon and text as well as colour; caution message moved into a dedicated warning panel next to the recovery button.
- New dark navy / teal design tokens, higher-contrast secondary text, clearer disabled states, visible keyboard focus, hover and pressed states.
- Layout is computed from the window size and DPI (no fixed pixel heights), scrolls on small screens and no longer needs 890 px of height; process is DPI-aware.
- Technical log and verification chain moved into a collapsible section that opens itself on errors.
- Add a confirmation dialog before the recovery tool is launched (default answer: No).
- Add "Copy report" with user name and profile path masked.
- Add an after-recovery checklist (pairing, audio, microphone) shown only after a normal receiver is seen following recovery.
- State in the interface that recovery always writes receiver firmware 2.12.
- Device logic, target guards, patch manifests and output hashes are unchanged (`--self-test` passes). The interface has not been exercised by hand against a physical device.

## 0.2.0 — first-generation detection

- Focus the bilingual UI on first-generation Standard / Shroud receivers; remove later-generation wording from the model confirmation.
- Enumerate USB and HID devices together; group collections by their physical USB parent.
- Require the vendor updater's FF00/0001 HID collection before enabling a receiver that is in recovery (bootloader) mode.
- Distinguish missing/unreadable HID, headset USB, normal receiver, multiple receivers and unsupported Avnera products.
- Reset confirmations when the connected target changes; add a serial-free read-only scan summary.
- Default to the Windows UI language (Turkish or English), localize error guidance, and include previews for both languages.
- Keep firmware payloads, patch manifests and the previously validated recovery output unchanged.

## 0.1.0 — experimental

- Extract the receiver-only patch from one successful recovery into a reproducible C# patcher.
- Pin the original, probe and recovery SHA-256 digests.
- Preserve embedded firmware and original receiver transfer routines.
- Add a Turkish/English custom launcher, local source selection, official-source download, detection probe and guarded recovery launch.
- Add target guards, offline verification, source-only CI and documented validation limits.
