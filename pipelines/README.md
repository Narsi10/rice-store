# Azure DevOps CI/CD — Rice Store

This folder contains **YAML pipelines**, and this README also documents how to
build the equivalent **Classic (visual designer) pipelines**.

## Files

| File | Purpose |
|------|---------|
| `azure-pipelines.yml` | Full CI/CD: build, test, Docker build+push, deploy (staging/prod) |
| `azure-pipelines-ci.yml` | Lightweight CI (build+test only) — good for PR validation |
| `azure-pipelines-release.yml` | Standalone release/deploy pipeline with environment approvals |
| `templates/dotnet-service-template.yml` | Reusable per-service build/containerize steps |

---

## Prerequisites (one-time setup in Azure DevOps)

1. **Create a project** in Azure DevOps (dev.azure.com).
2. **Push this repo** to Azure Repos (or connect GitHub).
3. **Create a container registry** — Azure Container Registry (ACR) or Docker Hub.
4. **Create service connections** (Project Settings → Service connections):
   - **Docker Registry** connection → name it, note the name for
     `dockerRegistryServiceConnection`.
   - **SSH** connection to your Linux server → for `deploySshServiceConnection`.
5. **Create environments** (Pipelines → Environments):
   - `ricestore-staging`
   - `ricestore-production` (add an **Approval** check so prod needs sign-off).
6. **Set pipeline variables** (Pipelines → Library, or in the pipeline UI):
   - `imageRegistry` = e.g. `ricestore.azurecr.io`
   - `dockerRegistryServiceConnection` = the connection name from step 4
   - `deploySshServiceConnection` = the SSH connection name

---

## Part 1 — YAML pipelines (recommended)

### Set up the main CI/CD pipeline

1. Pipelines → **New pipeline**
2. Choose your repo (Azure Repos Git / GitHub)
3. Select **Existing Azure Pipelines YAML file**
4. Pick `/pipelines/azure-pipelines.yml`
5. Review → **Run**

That's it. The pipeline will:
- **Build stage:** restore, build the .NET solution, run tests, build the Angular app
- **DockerBuildPush stage:** build & push a Docker image for each of the 8
  backend services + the frontend (skipped on PRs)
- **DeployStaging:** auto-deploys when you push to `develop`
- **DeployProduction:** deploys when you push to `main` (gate it with an approval
  on the `ricestore-production` environment)

### Set up the CI-only pipeline (PR gate)

1. New pipeline → existing YAML → `/pipelines/azure-pipelines-ci.yml`
2. In **Branch policies** for `main`, add this pipeline as a **required build**.

Now every PR must build the backend + frontend before it can merge.

---

## Part 2 — Classic (visual designer) pipelines

If you prefer the classic UI instead of YAML, here's how to reproduce the same
flow. Classic pipelines split into a **Build pipeline** and a **Release pipeline**.

### A. Classic Build pipeline

1. Pipelines → **New pipeline** → click **"Use the classic editor"** (link at
   the bottom).
2. Select your repo and branch (`main`).
3. Start with an **Empty job**.
4. Set **Agent pool** = `Azure Pipelines`, **Agent Specification** = `ubuntu-latest`.
5. Add these tasks **in order** (click "+" on the agent job):

   **Backend build:**
   - **Use .NET Core** — version `10.0.x`, "Use SDK" checked.
   - **.NET Core** task, command = `restore`, Path to projects = `RiceStore.sln`.
   - **.NET Core** task, command = `build`, projects = `RiceStore.sln`,
     arguments = `--configuration Release`.
   - **.NET Core** task, command = `test`, projects = `**/*Tests.csproj`
     (optional until you add tests).

   **Frontend build:**
   - **Node.js tool installer** — version `20.x`.
   - **npm** task, command = `custom`, command and arguments = `ci`,
     working folder = `frontend/rice-store-web`.
   - **npm** task, command = `custom`, command and arguments = `run build`,
     working folder = `frontend/rice-store-web`.

   **Docker images** (one Docker task per service — repeat for each):
   - **Docker** task, container registry = your registry service connection,
     command = `buildAndPush`,
     Dockerfile = `services/Catalog/Catalog.API/Dockerfile`,
     repository = `ricestore-catalog`, tags = `$(Build.BuildId)` and `latest`,
     build context = `$(Build.SourcesDirectory)`.
     Repeat for identity, cart, inventory, order, payment, notification, gateway
     (change Dockerfile + repository each time). For the frontend, set the
     Dockerfile to `frontend/rice-store-web/Dockerfile` and build context to
     `frontend/rice-store-web`.

   **Publish artifact** (if not using Docker for deploy):
   - **Publish build artifacts** — path = `frontend/rice-store-web/dist`,
     artifact name = `frontend-dist`.

6. **Triggers** tab → enable **Continuous integration**, include branches
   `main` and `develop`.
7. **Save & queue.**

### B. Classic Release pipeline

1. Pipelines → **Releases** → **New release pipeline** → start **Empty job**.
2. **Add an artifact** → source = your Build pipeline → default version = latest.
3. Enable the **Continuous deployment trigger** (rocket icon on the artifact) so
   a new build auto-creates a release.
4. Rename **Stage 1** to `Staging`. In the stage, add an **SSH** task:
   - SSH service connection = your server connection.
   - Run = **Inline**, script:
     ```
     cd /opt/rice-store
     git pull
     docker compose -f docker-compose.prod.yml --env-file .env pull
     docker compose -f docker-compose.prod.yml --env-file .env up -d
     ```
5. Add a second stage `Production` (clone the Staging stage).
6. On the `Production` stage, click the **pre-deployment conditions** (lightning
   bolt) → enable **Pre-deployment approvals** → add yourself/approvers.
7. **Save**, then **Create release** to deploy.

---

## Deployment model

Both YAML and Classic deploy the same way: SSH into the Linux server, pull the
new images, and `docker compose up`. The server must have:
- Docker installed
- The repo at `/opt/rice-store`
- A valid `.env` file (see `.env.example`)

See `docs/AWS_DEPLOYMENT.md` for server setup.

---

## Notes

- **Tests:** the test steps use `continueOnError`/optional matching since there
  are no test projects yet. Add `*Tests.csproj` projects and the pipeline picks
  them up automatically.
- **Path-based builds:** for large teams you can add `paths` filters per
  service so only changed services rebuild. The main pipeline builds everything
  for simplicity and correctness.
- **Secrets:** never put DB passwords, JWT keys, SMTP or Razorpay keys in the
  pipeline YAML. Use Azure DevOps **secret variables** or **Azure Key Vault**,
  and inject them into the `.env` on the server.
