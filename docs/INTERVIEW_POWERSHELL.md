# PowerShell — Interview Q&A

Concept and scenario questions for a Windows/DevOps/SysAdmin role. Focus is
practical automation, not obscure trivia.

## Contents
1. Fundamentals
2. Common cmdlets & pipeline
3. Scripting (variables, loops, functions, error handling)
4. Remoting & automation
5. Real-world scenarios
6. Rapid-fire one-liners

---

## 1. Fundamentals

**Q: What is PowerShell and how is it different from the old Command Prompt?**
PowerShell is a task-automation shell and scripting language built on .NET. The
big difference from CMD is that PowerShell works with **objects, not just text** —
cmdlets output .NET objects you can filter, sort, and pass down the pipeline by
their properties, instead of parsing raw text.

**Q: What is a cmdlet?**
A lightweight command in PowerShell, named as **Verb-Noun** (e.g. `Get-Process`,
`Set-Item`, `New-Website`). The consistent naming makes commands easy to guess and
discover.

**Q: How do you find help for a command?**
`Get-Help <cmdlet>` for documentation (add `-Examples` or `-Full`), and
`Get-Command` to discover cmdlets. `Get-Member` shows the properties and methods of
whatever object a command returns — extremely useful for building pipelines.

**Q: What is the pipeline?**
The `|` operator passes the **object** output of one cmdlet as input to the next.
E.g. `Get-Process | Where-Object CPU -gt 100 | Sort-Object CPU`. Because it's
objects, you filter on real properties, not text position.

---

## 2. Common cmdlets & pipeline

**Q: Which cmdlets do you use most?**
`Get-ChildItem` (list files), `Where-Object` (filter), `ForEach-Object` (loop),
`Select-Object` (pick properties), `Sort-Object`, `Get-Content`/`Set-Content`,
`Get-Service`/`Restart-Service`, `Get-Process`, `Invoke-Command` (remoting),
`Test-Path`, `Copy-Item`/`Remove-Item`.

**Q: Difference between `Where-Object` and `Select-Object`?**
`Where-Object` **filters rows** — keeps only objects matching a condition.
`Select-Object` **picks columns** — chooses which properties to show (or takes the
first/last N). One reduces which items, the other reduces which fields.

**Q: How would you find the top 5 processes by CPU?**
`Get-Process | Sort-Object CPU -Descending | Select-Object -First 5`

**Q: How do you filter files older than 30 days in a folder?**
```powershell
Get-ChildItem C:\Logs -Recurse |
  Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) }
```

---

## 3. Scripting

**Q: How do you declare variables and what are the common types?**
Variables start with `$`, e.g. `$name = "server01"`. PowerShell is dynamically
typed but you can constrain: `[int]$count = 5`. Common types: strings, integers,
arrays (`@()`), and hashtables (`@{}`).

**Q: What loops does PowerShell have?**
`foreach`, `for`, `while`, `do-while/until`, and the pipeline `ForEach-Object`.
For iterating a collection, `foreach ($item in $collection) { }` is most common.

**Q: How do you write a function with parameters?**
```powershell
function Get-DiskFree {
    param([string]$Server = $env:COMPUTERNAME)
    Get-CimInstance Win32_LogicalDisk -ComputerName $Server |
      Select-Object DeviceID, @{n='FreeGB';e={[math]::Round($_.FreeSpace/1GB,1)}}
}
```

**Q: How do you handle errors in PowerShell?**
`try / catch / finally`, and set `-ErrorAction Stop` on a cmdlet so its error
becomes a terminating error the `catch` can handle. `$Error` holds recent errors.
Example:
```powershell
try {
    Remove-Item $path -ErrorAction Stop
}
catch {
    Write-Warning "Failed to delete $path: $_"
}
```

**Q: What is `-WhatIf` and why is it important?**
`-WhatIf` shows what a command *would* do without actually doing it. Critical for
destructive operations (`Remove-Item -WhatIf`) — you preview before you delete,
especially in production.

**Q: Difference between `Write-Host`, `Write-Output`, and `Write-Verbose`?**
`Write-Output` sends objects down the pipeline (use for real output).
`Write-Host` writes to the console only (not pipeline-able) — for messages.
`Write-Verbose` writes optional detail shown only when `-Verbose` is used — good
for logging inside scripts.

---

## 4. Remoting & automation

**Q: How do you run a command on a remote server?**
`Invoke-Command -ComputerName SERVER01 -ScriptBlock { Get-Service }`. For an
interactive session, `Enter-PSSession`. Remoting uses WinRM under the hood.

**Q: How do you run something across many servers at once?**
`Invoke-Command` accepts an array: `-ComputerName @('S1','S2','S3')` and runs in
parallel, returning results tagged by machine. Wrap in try/catch so one
unreachable server doesn't stop the rest.

**Q: How do you schedule a PowerShell script to run automatically?**
Windows **Task Scheduler** — create a task that runs
`powershell.exe -File C:\scripts\job.ps1` on a schedule. Or `New-ScheduledTask`/
`Register-ScheduledJob` from PowerShell itself.

**Q: How do you pass credentials securely in a script?**
Use `Get-Credential` to prompt, store secrets in an encrypted form (Secret
Management module / Key Vault), never hardcode passwords. For automation, use a
managed service account or a vault rather than plaintext.

---

## 5. Real-world scenarios

**S1: Write a script to delete files older than 30 days across several servers.**
Use PowerShell remoting: `Invoke-Command` against a server list, and inside the
script block `Get-ChildItem` filtered on `LastWriteTime` older than 30 days, then
`Remove-Item`. Add a preview/`-WhatIf` mode to verify before deleting, per-server
error handling, and schedule it in Task Scheduler to run daily. Benefit: no manual
cleanup, no disk-full incidents. (See `scripts/Cleanup-OldFiles.ps1` in this repo.)

**S2: Check whether a Windows service is running on 20 servers and restart it if stopped.**
```powershell
Invoke-Command -ComputerName $servers -ScriptBlock {
    $svc = Get-Service -Name 'W3SVC'
    if ($svc.Status -ne 'Running') {
        Start-Service $svc
        "$env:COMPUTERNAME: was stopped, started it"
    } else { "$env:COMPUTERNAME: running" }
}
```
This gives a per-server report and self-heals stopped services.

**S3: Generate a daily disk-space report for all servers and email it.**
Loop the servers with `Invoke-Command` collecting `Win32_LogicalDisk` free space,
build a report object, convert to HTML with `ConvertTo-Html`, and send with
`Send-MailMessage` (or an email API). Schedule it daily. This is proactive
monitoring — catch low disk before it's an incident.

**S4: A script must not delete anything by accident in production. How do you make it safe?**
Add a `-WhatIf`/preview mode that logs what would be affected, run it in test first,
add confirmation prompts (`-Confirm`), scope it tightly (specific path, specific
age), add error handling, and log every action. Test in a non-prod environment
before scheduling in production.

**S5: How would you automate creating an IIS site across environments?**
Use the `IISAdministration`/`WebAdministration` module: `New-WebAppPool`,
`New-Website`, `New-WebBinding`, set the app pool identity and permissions. Keep the
script parameterized (site name, path, port, cert) and version-controlled so Dev,
UAT, and Prod are provisioned identically with no manual drift.

**S6: You need to bulk-update something in Active Directory (e.g. disable stale accounts). Approach?**
Use the `ActiveDirectory` module: `Get-ADUser` with a filter (e.g. `LastLogonDate`
older than X), review the list first (preview!), then pipe to `Disable-ADAccount`.
Always confirm the target set before making bulk changes, and log what was changed.

---

## 6. Rapid-fire one-liners

- **Verb-Noun** = cmdlet naming convention (`Get-Service`).
- **Pipeline `|`** passes **objects**, not text.
- **`Where-Object`** filters items; **`Select-Object`** picks properties.
- **`Get-Member`** = inspect an object's properties/methods.
- **`Invoke-Command`** = run on remote servers (remoting via WinRM).
- **`-WhatIf`** = preview a destructive action without doing it.
- **`-ErrorAction Stop` + try/catch** = proper error handling.
- **Task Scheduler** = run scripts automatically.
- **`Get-Help -Examples`** = fastest way to learn a cmdlet.
- **Never hardcode credentials** — use Get-Credential / vault / managed identity.

---

## Interview tip

For any "write a script to do X" question, structure your spoken answer as:
**"I'd get the items → filter by the condition → act on them → and I'd add a
preview/error-handling step to be safe, then schedule it."** That covers logic,
safety, and automation in one breath — exactly what interviewers want to hear.
