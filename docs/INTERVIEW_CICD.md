# Interview Cheat-Sheet — CI/CD (Azure DevOps + Octopus Deploy)

A one-page reference for answering "Tell me about the CI/CD pipelines in your
project" — built around the common real-world combo of **Azure DevOps for CI**
and **Octopus Deploy for CD / configuration management**.

---

## The one-sentence summary (memorize this)

> "Azure DevOps builds and tests the code into a single versioned artifact;
> Octopus Deploy promotes that same artifact through Dev, QA, UAT, and Production,
> injecting environment-specific configuration and secrets at deploy time, with
> lifecycle gates for approvals and one-click rollback."

## The key concept interviewers want to hear

- **Azure DevOps (CI):** builds code, runs tests, produces a versioned artifact.
  Answers *"is the code good?"*
- **Octopus (CD):** takes that one artifact, deploys and configures it across
  environments. Answers *"get this exact build safely into each environment with
  the right settings."*
- **Golden phrase: "build once, deploy many."** You never rebuild per
  environment — the same artifact is promoted, and only the configuration changes.

---

## The full spoken answer (60–90 seconds)

> "In our project we used Azure DevOps for CI and Octopus Deploy for CD and
> configuration management. When a developer merged a pull request into main,
> Azure DevOps kicked off the CI pipeline — it restored packages, built the
> solution, ran unit tests and SonarQube analysis, and if everything passed it
> packaged the app, pushed the artifact to the Octopus feed, and created a release
> in Octopus.
>
> Octopus handled deployment across environments — Dev, QA, UAT, and Production.
> The same build artifact was promoted through each; we never rebuilt. The big
> advantage was configuration management: Octopus stored environment-specific
> variables — connection strings, API keys, feature flags — as scoped variables.
> So the same package deployed to QA got the QA connection string, and in
> Production it got the Production one, automatically at deploy time. Sensitive
> values were stored as sensitive variables or pulled from Azure Key Vault.
>
> We used the Octopus lifecycle to enforce promotion rules — a release had to be
> verified in QA before UAT, and Production required manual approval from the
> release manager. Deploy steps used deploy-to-IIS and run-a-script, with health
> checks after deployment. If something failed we rolled back to the previous
> release in one click, because Octopus keeps every release and its exact config."

---

## The real-time scenario (tell it as a story if pushed)

Environments in Octopus: **Dev → QA → UAT → Production**.

1. **Developer merges a PR** to main → Azure DevOps CI triggers.
2. **CI builds**, runs unit tests + SonarQube gate, packages each service as a
   versioned artifact (e.g. `Catalog.API.2024.3.15`).
3. **CI pushes** the package to Octopus's feed and calls the Octopus API to
   **create a release**.
4. **Octopus auto-deploys to Dev**, injecting **Dev-scoped variables** — rewrites
   `appsettings.json` so the connection string points to the Dev DB, etc.
   (This is the configuration-management part.)
5. **Smoke tests** run against Dev. If green, the lifecycle allows promotion.
6. **Deploy to QA** — same package, QA variables injected. QA tests.
7. **Promote to UAT** — business validates. UAT config applied.
8. **Production** — sits at an approval gate. **Release manager approves**
   (in a bank, tied to a change ticket). Deploys with Prod variables + Key Vault
   secrets.
9. **Post-deploy health checks.** If a check fails → **one-click rollback** to the
   last known-good release (restores both code and exact config).

---

## The payoff line about Octopus + configuration

> "Octopus separates the deployment package from the configuration. Developers
> build the code once; environment differences live entirely in Octopus variables,
> scoped per environment. That means no environment-specific code, no config drift,
> and a full audit of what config was applied where and by whom."

---

## Follow-up questions & short answers

**How do you handle secrets?**
Sensitive variables in Octopus are encrypted, or we integrate with Azure Key Vault
so secrets are pulled at deploy time and never stored in source or logs.

**How do variables get into the app?**
Octopus's structured configuration / variable replacement substitutes values into
`appsettings.json` / `web.config` during deployment based on the target
environment's scope.

**How do you promote between environments?**
Octopus Lifecycles define the allowed path and phases. A release must succeed in
one phase before progressing; higher environments require manual approval gates.

**How do you roll back?**
Octopus keeps every release and a snapshot of its variables. Rollback = redeploy
the previous release, which restores both the code and the exact config.

**What triggers a deployment?**
CI creates the release automatically on merge to main; Dev deploys automatically;
higher environments are gated by approvals or automated test results.

**Blue-green or canary?**
Octopus supports rolling and blue-green deployments via deployment targets and
load-balancer steps. (Say what you actually did.)

---

## Honesty tips

- Only claim what you did. "I worked on the Azure DevOps CI side and integrated it
  with our Octopus release process" is stronger than a vague "we did everything."
- If asked about something you didn't own: "That was handled by our DevOps team,
  but my understanding is…" then explain the concept.

---

## Azure DevOps vs Octopus — who does what

| Responsibility | Tool |
|----------------|------|
| Source control + branch policies | Azure Repos / GitHub |
| Build, unit tests, code analysis | Azure Pipelines (CI) |
| Versioned artifact / package | Azure Artifacts / Octopus feed |
| Deploy to environments | Octopus Deploy |
| Environment-specific config | Octopus scoped variables |
| Secrets | Azure Key Vault (+ Octopus sensitive vars) |
| Promotion rules & approvals | Octopus Lifecycles + gates |
| Rollback | Octopus (redeploy previous release) |
