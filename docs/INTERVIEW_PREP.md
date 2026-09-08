# Interview Prep — DevOps / SysAdmin Role

Real-time scenario Q&A mapped to the job description (Windows Server, MSSQL,
IIS, Active Directory, Azure DevOps, PowerShell, cloud, second-line support).

> Two golden rules for the interview:
> 1. End technical answers with **"and I document it"** — the JD explicitly
>    values creating solution documentation.
> 2. In every scenario, mention **communicating with the business** — the JD
>    stresses clear communication and responding to their needs.

---

## Windows Server

**Q1: A Windows Server hosting a critical app suddenly shows 100% CPU and the app is unresponsive. Walk me through what you do.**

First I'd confirm the impact and communicate — let the business/second-line know
we're investigating. Then I'd remote in and open Task Manager or Resource Monitor
to identify the offending process. If it's the app pool (w3wp.exe), I'd check
which site/pool via `Get-IISWorkerProcess` or the PID. I'd capture evidence first
— a process dump or screenshot — before killing anything, so we can root-cause
later. Short term, I might recycle the specific IIS app pool to restore service.
Then I'd check Event Viewer (System and Application logs), recent deployments,
and Performance Monitor counters for a pattern. Once stable, I document the
incident, the temporary fix, and the root cause in our knowledge base.

**Q2: How do you keep Windows Servers patched without causing downtime?**

We use WSUS/SCCM/Azure Update Manager to control patch rollout rather than
auto-update. Patches go to a test/UAT ring first, then production during an
approved maintenance window. For zero-downtime, servers behind a load balancer
are patched one at a time — drain a node, patch, verify, rejoin, then the next.
I automate the drain/patch/verify steps in PowerShell so it's consistent and
auditable.

---

## MSSQL

**Q3: Users report the application is slow. You suspect the database. How do you investigate?**

Start with currently running queries using `sp_who2` or `sys.dm_exec_requests`
to spot blocking or long-running queries. Check `sys.dm_exec_query_stats` for the
most expensive queries by CPU/reads. Common culprits: missing indexes
(`sys.dm_db_missing_index_details`), outdated statistics, or blocking/deadlocks.
Look at wait stats via `sys.dm_os_wait_stats` to understand what SQL is waiting
on. Short term I might update statistics or rebuild a fragmented index; longer
term work with developers to tune the query or add an index. Validate in non-prod
first and document the finding.

**Q4: The MSSQL server disk is filling up fast. What do you check?**

Usually the transaction log growing because the database is in FULL recovery mode
but log backups aren't running. Check the recovery model, confirm backup jobs,
take a log backup to truncate it. Other causes: tempdb bloat, a runaway query, or
no maintenance cleanup. Never just shrink blindly in production — address the root
cause (fix the backup schedule) and document it. If genuinely out of space and
critical, add disk or move files as an emergency measure.

**Q5: How do you handle MSSQL backups and know they actually work?**

Full backups on a schedule, plus differential and transaction-log backups
depending on the RPO, stored off-server. The part people forget: **test restores**
— a backup you haven't restored is not a backup. Periodically restore to a test
instance and verify with `RESTORE VERIFYONLY` and `DBCC CHECKDB`. Automate the
jobs and monitor them so a failed backup raises an alert immediately.

---

## IIS

**Q6: After a deployment, a website returns HTTP 500.19 or 503. How do you troubleshoot?**

A 503 usually means the app pool is stopped or crashed — check the app pool state
and the Event Log for why (often bad config or a startup exception). A 500.19 is
typically a malformed web.config or a missing module/feature. Check web.config
syntax, confirm the required IIS features/runtime (like the .NET hosting bundle)
are installed, and check folder permissions for the app pool identity. Enable
detailed errors or failed request tracing to pinpoint it. Once fixed, document the
cause so the deployment checklist prevents a repeat.

**Q7: How do you configure IIS for a new application, and how would you automate it?**

Manually: create the app pool with the right .NET version and identity, create the
site/application, bind the hostname and SSL cert, set permissions. For consistency
across environments I automate with PowerShell using the `WebAdministration` /
`IISAdministration` module — `New-WebAppPool`, `New-Website`, `New-WebBinding`.
Dev, UAT, and Prod are then provisioned identically with no manual drift, and the
script is version-controlled and pipeline-runnable.

---

## Active Directory

**Q8: A user can't access a shared application resource. How do you approach it?**

Confirm exactly what they're accessing and the error. Determine if it's
authentication (can they log in at all?) or authorization (logged in but denied).
Verify the AD account isn't locked/disabled, check group membership — access is
usually granted via an AD security group. If they were just added, group changes
need a new token, so a logoff/logon or `gpupdate /force` may be needed. Confirm
the resource's permissions map to the correct group. Document the resolution; if
recurring, suggest a self-service or clearer group structure.

**Q9: How does Active Directory relate to your application and IIS?**

Many enterprise apps use Windows/Integrated authentication, so IIS authenticates
users against AD. App pool identities are often AD service accounts with specific
permissions. Group Policy enforces server configuration and security baselines.
When troubleshooting access, AD group membership, service account permissions, and
Kerberos/SPN configuration are all things I check — e.g. a "double-hop" auth
failure often comes down to missing SPNs or delegation settings.

---

## Azure DevOps

**Q10: Describe a CI/CD pipeline you'd set up for a .NET app on Windows/IIS.**

CI in Azure DevOps: on merge to main, restore, build, run unit tests, publish an
artifact. CD: a release pipeline deploys that same artifact through Dev → UAT →
Prod. Each environment has scoped variables for connection strings and config, and
Prod requires manual approval. The deploy step uses an IIS deployment task or a
PowerShell script to stop the app pool, copy files, update config, restart. Add
post-deployment smoke tests and rollback to the previous artifact. Key principle:
**build once, deploy many** — the same artifact is promoted, never rebuilt.

**Q11: A production release failed halfway. What do you do?**

Restore service first — roll back to the last known-good release (redeploy the
previous artifact). Communicate status to the business. Once stable, investigate
the pipeline logs to find where it failed — config, permissions, a failed
migration? Reproduce in a lower environment, fix, and add a check or gate so it
can't recur. Finally document the incident and resolution. Fast recovery first,
root cause second.

---

## PowerShell / Scripting

**Q12: Give an example of something repetitive you automated with PowerShell.**

Onboarding a new server environment: a script that installed IIS features, created
app pools and sites, set bindings and SSL certs, configured folder permissions for
the service account, and validated DB connectivity. What took an hour of
error-prone clicking became a repeatable 5-minute script producing identical
environments. I've also automated log cleanup, health-check reports, and bulk AD
tasks. Automation reduces human error and makes environments reproducible and
auditable.

**Q13: How would you write a script to check the health of multiple servers?**

Use PowerShell remoting — `Invoke-Command` against a server list — to collect CPU,
memory, disk space, service status, and IIS app pool state, then output a report
or push to a dashboard. Add error handling so one unreachable server doesn't stop
the run, and schedule it. The point is proactive detection — catching a disk
filling up before it causes an incident.

---

## Cloud (Private/Public — core responsibility)

**Q14: How do you configure and maintain application environments in the cloud?**

Infrastructure as Code so environments are reproducible — ARM/Bicep or Terraform
to define VMs, networking, load balancers, databases. Configuration is
version-controlled and deployed through pipelines, not manual portal clicks, which
prevents drift. For maintenance: monitoring and alerting (Azure Monitor, Log
Analytics), automated patching and backups, and runbooks for common operations.
The same IaC provisions Dev, UAT, and Prod identically with environment-specific
parameters.

---

## Second-line support & incident management (core responsibility)

**Q15: Walk me through how you handle a second-line incident.**

When an incident escalates to me, I first understand impact and urgency — how many
users, is it business-critical — and set expectations on communication. Gather
information: what changed recently, error messages, logs, monitoring data. Form a
hypothesis and validate it methodically rather than guessing. Priority is
**restore service** — workaround or rollback if needed — then root cause.
Throughout, keep the business updated in clear, non-technical language. After
resolution, write solution documentation: symptoms, diagnosis, fix, prevention, so
first-line or the next engineer resolves it faster. Recurring incidents become
candidates for a permanent fix or automation.

**Q16: How do you write good solution documentation?**

Structure it so someone less experienced can follow: clear title/symptom, the
environment affected, step-by-step diagnosis, resolution steps with exact commands,
and root cause plus prevention. Avoid assuming knowledge, include screenshots or
command output, keep it in a searchable knowledge base. Good documentation lets
first-line resolve an issue without escalating again — that's the measure of
whether it's good.

---

## Soft skills / behavioral (heavily weighted in this JD)

**Q17: Tell me about a time you explained a technical problem to a non-technical business user. (STAR)**

*Situation:* A business user reported the reporting app was "broken."
*Task:* The real issue was a nightly data job failing due to a schema change; I
needed to explain it without jargon.
*Action:* I said "the overnight process that refreshes your data didn't complete,
so you're seeing yesterday's numbers — we've found why and it'll be current by this
afternoon." Gave a time estimate and followed up.
*Result:* They were reassured because they understood the impact and timeline.
*Lesson:* Translate technical facts into business impact and next steps.

**Q18: How do you handle a dynamic environment with shifting priorities / new tech?**

I prioritize by business impact — a production incident beats a scheduled task.
With new tech, I invest time to learn it properly rather than resist it, and
document as I learn so the team benefits. I'm comfortable with ambiguity as long as
I communicate clearly about what I'm working on and any tradeoffs.

**Q19 (Bonus — Linux): You mostly work in Windows but a task needs Linux. How do you handle it?**

I'm comfortable in Windows with working Linux knowledge — navigating the
filesystem, checking logs under `/var/log`, managing services with `systemctl`,
permissions with `chmod`/`chown`, basic Bash. The concepts transfer: a service not
starting, a disk filling, a permission issue — same troubleshooting mindset,
different commands. If I hit something unfamiliar I'm resourceful with docs and man
pages, and lean on the team where needed. It's an area I'm actively improving.

---

## Quick-fire prep checklist

- [ ] Have 2–3 real STAR stories ready (an incident you fixed, something you
      automated, a time you communicated with the business).
- [ ] Be honest about ownership — "I owned the CI side" beats vague "we did it all."
- [ ] Know these commands cold: `sp_who2`, `Get-IISWorkerProcess`,
      `Invoke-Command`, `gpupdate /force`, `DBCC CHECKDB`, `systemctl status`.
- [ ] For any "how would you fix X" — structure: assess impact → communicate →
      restore service → root cause → document → prevent.
