# Azure DevOps CI/CD — Complete Interview Q&A

Covers **core concepts** and **scenario-based** questions. Organized so you can
study top-to-bottom or jump to a section.

## Contents
1. Fundamentals
2. Azure Repos & Branching
3. Azure Pipelines — CI
4. Azure Pipelines — CD / Releases
5. YAML Pipelines (deep dive)
6. Variables, Secrets & Key Vault
7. Agents & Pools
8. Artifacts
9. Environments, Approvals & Gates
10. Scenario-Based Questions
11. Rapid-fire one-liners

---

## 1. Fundamentals

**Q: What is CI/CD?**
Continuous Integration (CI) is automatically building and testing code every time
it's committed, so integration problems are caught early. Continuous Delivery/
Deployment (CD) automatically takes that validated build and releases it to
environments — Delivery means it's ready to deploy at the click of a button,
Deployment means it goes to production automatically. The goal is small, frequent,
low-risk releases.

**Q: What are the main services in Azure DevOps?**
- **Azure Repos** — Git source control
- **Azure Pipelines** — CI/CD automation
- **Azure Boards** — work items, sprints, Kanban
- **Azure Artifacts** — package feeds (NuGet, npm, etc.)
- **Azure Test Plans** — manual/exploratory test management

**Q: Difference between Continuous Delivery and Continuous Deployment?**
Both automate up to production readiness. In Continuous **Delivery** there's a
manual approval before production. In Continuous **Deployment** every change that
passes the pipeline goes to production automatically with no human gate.

**Q: What is a build vs a release pipeline?**
A build (CI) pipeline compiles, tests, and produces artifacts. A release (CD)
pipeline takes those artifacts and deploys them to environments. In YAML both can
live in a single multi-stage pipeline; in Classic they're separate.

---

## 2. Azure Repos & Branching

**Q: What branching strategies have you used?**
GitFlow (feature/develop/release/main), GitHub Flow (feature branches off main),
and trunk-based development. Trunk-based with short-lived feature branches plus
feature flags is increasingly preferred for fast CI/CD.

**Q: What are branch policies?**
Rules on protected branches (like main): require a pull request, minimum number of
reviewers, linked work items, successful build validation, comment resolution, and
no direct pushes. They enforce quality and (with different-reviewer rules)
segregation of duties.

**Q: What is a pull request and why does it matter for CI/CD?**
A PR is a request to merge a branch, triggering code review and (via branch
policies) a **build validation** pipeline. The merge is blocked until the build
and tests pass, so broken code never reaches main.

---

## 3. Azure Pipelines — CI

**Q: What triggers a CI pipeline?**
A `trigger` on branch pushes, a `pr` trigger for pull requests, scheduled
triggers (cron), or pipeline-completion triggers (one pipeline triggering
another). You can filter by branch and by path.

**Q: What is a path filter and when is it useful?**
It scopes triggers to changes in specific folders. In a monorepo/microservices
setup, path filters let you build only the service that changed instead of
everything.

**Q: What steps are typical in a .NET CI pipeline?**
Restore → build → run unit tests → publish test results and code coverage →
static analysis (SonarQube) → publish the artifact (or build a Docker image).

**Q: How do you enforce code quality in CI?**
Fail the build on test failures, enforce a code-coverage threshold, and add a
SonarQube/quality-gate step and dependency/security scans (Snyk, WhiteSource) that
block on critical issues.

---

## 4. Azure Pipelines — CD / Releases

**Q: How do you deploy to multiple environments?**
Define stages/environments (Dev → QA → UAT → Prod). The same artifact is promoted
through each. Lower environments deploy automatically; higher ones use approvals.
Each environment supplies its own scoped variables.

**Q: What is "build once, deploy many"?**
You build a single immutable artifact and promote that exact artifact through all
environments, changing only configuration. You never rebuild per environment —
this guarantees what you tested is what you ship, which is critical for audit.

**Q: How do you roll back a bad deployment?**
Redeploy the previous known-good artifact/release. With immutable artifacts and
retained releases this is fast. Better yet, blue-green or canary deployments allow
instant traffic switch-back.

---

## 5. YAML Pipelines (deep dive)

**Q: YAML vs Classic pipelines — pros and cons?**
YAML lives in the repo (version-controlled, reviewable, portable, supports
templates and PR validation of the pipeline itself). Classic is a visual designer,
easier for beginners but not version-controlled and harder to reuse. Modern best
practice is YAML.

**Q: Explain the YAML hierarchy.**
`Pipeline → Stages → Jobs → Steps (tasks/scripts)`. A stage groups jobs (e.g.
Build, Deploy). A job runs on one agent. Steps are the individual tasks/scripts.

**Q: What are templates in YAML pipelines?**
Reusable YAML files you include with `template:` and pass `parameters` to. They
avoid duplication — e.g. one "build-and-containerize a service" template reused for
every microservice.

**Q: What is a matrix strategy?**
Runs the same job multiple times in parallel with different variable sets — e.g.
building 8 microservice images at once, or testing across multiple OS/runtime
versions.

**Q: What is a deployment job vs a regular job?**
A `deployment` job targets an **Environment**, records deployment history, and
supports strategies like `runOnce`, `rolling`, and `canary`. A regular `job` just
runs steps.

---

## 6. Variables, Secrets & Key Vault

**Q: How do you manage variables across environments?**
Pipeline variables, **variable groups** (in the Library, shared across pipelines),
and scoped/stage-level variables. Environment-specific values (connection strings)
differ per stage.

**Q: How do you handle secrets securely?**
Never in YAML or plain variables. Use **secret variables** (masked, encrypted) or
link a variable group to **Azure Key Vault** so secrets are fetched at runtime with
a managed identity/service connection. Secrets are masked in logs.

**Q: What is a service connection?**
A stored, secured credential that lets pipelines talk to external systems — Azure
subscriptions, container registries, Key Vault, SSH targets, SonarQube. Managed in
Project Settings, with scoped permissions.

---

## 7. Agents & Pools

**Q: Microsoft-hosted vs self-hosted agents?**
Microsoft-hosted are managed VMs spun up fresh per run (no maintenance, but limited
customization and time limits). Self-hosted are your own machines — needed for
custom software, private-network access, more power, or specific OS. You maintain
them.

**Q: When would you use a self-hosted agent?**
To deploy into a private network/on-prem, when you need software not on hosted
agents, for faster builds with caching, or to control cost at high build volumes.

**Q: What is an agent pool?**
A group of agents that jobs can run on. You assign pipelines/jobs to a pool; the
pool schedules the job on an available agent.

---

## 8. Artifacts

**Q: What are pipeline artifacts?**
Files produced by a build (binaries, packages, published web output) that later
stages/pipelines consume. Published with `PublishBuildArtifacts`/`PublishPipeline
Artifact` and downloaded downstream.

**Q: What is Azure Artifacts?**
A package management service hosting feeds for NuGet, npm, Maven, Python, etc. —
used to share internal packages and cache upstream dependencies.

**Q: Why keep artifacts immutable and versioned?**
So the exact thing you tested is the exact thing you deploy to production, and so
you can roll back to a specific prior version. Essential for traceability/audit.

---

## 9. Environments, Approvals & Gates

**Q: What is an Environment in Azure DevOps?**
A named target (Dev, UAT, Prod) that deployment jobs target. It gives deployment
history, and supports **approvals and checks** — manual approvals, business hours,
Azure Monitor alerts, ServiceNow change tickets, and more.

**Q: Difference between an approval and a gate?**
An **approval** is a human sign-off before/after a stage. A **gate** is an
automated check (e.g. query Azure Monitor for no active alerts, or verify a
ServiceNow change is approved) that must pass automatically.

**Q: How do you enforce segregation of duties?**
Approval checks requiring a different person/group than the one who submitted the
change, plus branch policies preventing self-approval. Common in regulated domains.
