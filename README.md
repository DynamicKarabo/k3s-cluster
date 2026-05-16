# k3s-cluster — GitOps-Driven Kubernetes on a Single Node

[![GitHub Actions](https://img.shields.io/github/actions/workflow/status/DynamicKarabo/k3s-cluster/ci.yml?branch=main&style=flat-square&logo=githubactions&logoColor=white)](https://github.com/DynamicKarabo/k3s-cluster/actions)
[![ArgoCD](https://img.shields.io/badge/ArgoCD-EF7B4D?style=flat-square&logo=argo&logoColor=white)]()
[![K3s](https://img.shields.io/badge/k3s_v1.35-FFC61C?style=flat-square&logo=k3s&logoColor=black)]()
[![Helm](https://img.shields.io/badge/Helm-0F1689?style=flat-square&logo=helm&logoColor=white)]()
[![OpenSSF Scorecard](https://img.shields.io/badge/OpenSSF%20Scorecard-Passing-2ea44f?style=flat-square)]()
[![CodeQL](https://img.shields.io/badge/CodeQL-Analyzed-30A3E6?style=flat-square)]()
[![License](https://img.shields.io/github/license/DynamicKarabo/k3s-cluster?style=flat-square)](LICENSE)

A **single-node K3s cluster** provisioned entirely from code, managed via ArgoCD's app-of-apps pattern, and observable through Prometheus and Grafana. Demonstrates GitOps workflows, automated CI/CD, TLS certificate lifecycle management, namespace-isolated workloads, and disaster recovery procedures — all on a €4/month Hetzner VPS.

> Designed as a production-oriented reference architecture. Every component was chosen with specific trade-offs in mind. See [System Design](SYSTEM_DESIGN.md) for the full specification and [Architecture Decision Records](DECISIONS.md) for detailed rationale.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Internet (80/443)                      │
└────────────────────────┬────────────────────────────────────┘
                         │
                  ┌──────▼──────┐
                  │  Cloudflare  │  (DNS, optional)
                  └──────┬──────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│                    K3s (Single Node)                          │
│                                                               │
│  ┌───────────────────────────────────────────────────────┐   │
│  │                Traefik Ingress                          │   │
│  │   (k3s built-in, TLS termination via cert-manager)     │   │
│  └─────────────────────┬─────────────────────────────────┘   │
│                        │                                      │
│  ┌─────────────────────▼─────────────────────────────────┐   │
│  │          ArgoCD (GitOps Controller)                     │   │
│  │   Watches Git → Auto-syncs cluster state               │   │
│  │   App-of-apps: root.yaml → 5 child Applications        │   │
│  └──┬──────────┬──────────┬──────────────────────────────┘   │
│     │          │          │                                   │
│  ┌──▼──┐  ┌────▼────┐  ┌─▼───────────┐                      │
│  │Demo │  │Monitoring│  │   Addons     │                      │
│  │.NET │  │Prom+Graf │  │ cert-manager │                      │
│  │API  │  │Helm chart│  │ ext-secrets  │                      │
│  │Helm │  │          │  │ velero      │                      │
│  └─────┘  └─────────┘  └─────────────┘                      │
│                                                               │
│  ┌───────────────────────────────────────────────────────┐   │
│  │   Infra Layer: containerd, Local Path Provisioner,    │   │
│  │   UFW, fail2ban, unattended-upgrades                  │   │
│  └───────────────────────────────────────────────────────┘   │
│                                                               │
│  Spec: 4 vCPU · 8 GB RAM · 80 GB SSD · Hetzner CX33         │
│  Region: Johannesburg, South Africa · Ubuntu 24.04           │
└─────────────────────────────────────────────────────────────┘
```

**How the pieces fit together:**

Traffic arrives on ports 80/443 and hits Traefik, the built-in K3s ingress controller. Traefik terminates TLS using certificates provisioned and auto-renewed by cert-manager with Let's Encrypt. Authorized requests route to the appropriate backend service (demo API, Grafana). ArgoCD acts as the control plane for all workloads — it polls this Git repository every 3 minutes and reconciles any drift between the declared state (Helm charts in `argocd/projects/`) and the live cluster state. Prometheus scrapes metrics from kube-state-metrics, node-exporter, and the demo API's `/metrics` endpoint. Grafana visualises the telemetry through pre-configured dashboards.

---

## Design Decisions

Every tool and pattern in this stack was evaluated against alternatives. The table below captures the key choices and their rationale.

| Decision | Why This | What We Gave Up |
|---|---|---|
| **K3s** over kind/kubeadm | CNCF-certified, single-binary install, built-in Traefik ingress and local storage provisioner. Ideal for resource-constrained single-node setups. | Less flexibility to customise the control plane components compared to kubeadm. |
| **ArgoCD** over Flux | Richer web UI for demos, clear app-of-apps pattern for monorepo management, mature multi-cluster support if we scale. | Slightly larger resource footprint than Flux. |
| **Ansible** over Terraform | The node is provisioned once via cloud-init; Ansible excels at sequential configuration management without state files or cloud provider coupling. | Less declarative for cloud resource lifecycle; server provisioning is manual. |
| **Single-node** over multi-node | Fits comfortably within 4 vCPU / 8 GB RAM. Avoids etcd quorum complexity and inter-node networking overhead while still exercising the full Kubernetes API surface. | No high availability. Node failure means total cluster downtime. |
| **Traefik** over nginx-ingress | Ships with K3s by default. CRD-based `IngressRoute` resources provide better type safety than annotation-heavy nginx configs. | Nginx-ingress is more prevalent in enterprise environments. |
| **Helm** over raw YAML | Parameterisable manifests make CI/CD image tag injection straightforward. ArgoCD natively supports Helm rendering. | Go template syntax adds debugging overhead compared to plain YAML. |
| **Prometheus/Grafana** over Datadog | Self-hosted, open-source, zero per-node licensing cost. Industry standard for Kubernetes observability. | Requires managing the monitoring stack itself; no SaaS convenience. |
| **cert-manager + Let's Encrypt** | Free, automated TLS with staging/production ClusterIssuers for safe testing. | Adds CRD management overhead. |
| **External Secrets Operator** | Keeps secrets out of Git entirely. Bitwarden-backed, GitOps-friendly. | Adds cluster-side infrastructure and a dependency on the external provider's availability. |

> **Bottom line:** This stack prioritises observability, reproducibility, and security hardening over raw cost optimisation. The decisions reflect production-oriented thinking constrained to a single-node budget. See [DECISIONS.md](DECISIONS.md) for the full Architecture Decision Records.

---

## GitOps Workflow

The deployment pipeline is fully automated from commit to running workload:

```
  git push (main)
       │
       ▼
  GitHub Actions
       ├── dotnet restore/build/test
       ├── Build Docker image (multi-stage, cached layers)
       ├── Push to ghcr.io (tagged with commit SHA)
       ├── Update Helm values.yaml with new image tag
       └── git commit --push (auto-commit by github-actions[bot])
       │
       ▼
  ArgoCD detects drift ─── (3-minute poll interval, or webhook)
       │
       ▼
  ArgoCD syncs Helm chart to K3s
       ├── Creates/updates Deployment
       ├── Creates/updates Service + IngressRoute
       ├── ConfigMap injection
       └── Rolling update with liveness + readiness probes
       │
       ▼
  Monitoring
       ├── Prometheus scrapes /metrics
       ├── Grafana dashboards reflect new data
       └── (Alertmanager alerts if available)
```

**Key properties of this pipeline:**

- **Immutable image tags:** Every build produces a unique SHA-based tag. No `latest` tag on production — rollback means pointing ArgoCD to a previous SHA.
- **Auto-prune:** ArgoCD's `prune: true` removes any resource that no longer exists in Git. The cluster is a direct reflection of the repository.
- **Self-healing:** If a pod or deployment is manually deleted, ArgoCD restores it within the sync interval.
- **Git as single source of truth:** Secrets excluded via External Secrets Operator. No manual `kubectl apply`. No configuration drift.

**The ArgoCD app-of-apps structure:**

| Application | Purpose | Source Path | Namespace |
|---|---|---|---|
| `demo-api` | .NET 10 minimal API (the workload) | `argocd/projects/demo-api/helm/` | `demo-api` |
| `monitoring` | Prometheus + Grafana via kube-prometheus-stack | `argocd/projects/monitoring/helm/` | `monitoring` |
| `cert-manager` | Let's Encrypt TLS automation (staging + prod issuers) | `argocd/projects/cert-manager/helm/` | `cert-manager` |
| `external-secrets` | Bitwarden-backed secret management | `argocd/projects/external-secrets/helm/` | `external-secrets` |
| `velero` | Daily cluster backups with 7-day retention | `argocd/projects/velero/helm/` | `velero` |

The root Application (`argocd/root.yaml`) watches the `argocd/projects/` directory for any `*/application.yaml` files and deploys them recursively. Adding a new workload means creating a new project directory with an `application.yaml` and a Helm chart — ArgoCD picks it up automatically.

---

## Infrastructure & Hardware

| Aspect | Detail |
|---|---|
| **Provider** | Hetzner Cloud |
| **Plan** | CX33 |
| **vCPU** | 4 (AMD) |
| **RAM** | 8 GB |
| **Storage** | 80 GB NVMe SSD |
| **OS** | Ubuntu 24.04 LTS |
| **Location** | Johannesburg, South Africa |
| **Cost** | ~€4 / month (~ZAR 80) |
| **Provisioning** | Ansible playbook (one-shot) or `curl bootstrap.sh | bash` |

The node is provisioned entirely through Ansible, from a blank Ubuntu 24.04 install to a fully configured cluster with ArgoCD, cert-manager, and monitoring. The `bootstrap.sh` script handles dependency installation (Ansible, Helm, git, curl) before invoking the Ansible playbook.

---

## Security Posture

| Layer | Measures |
|---|---|
| **Pods** | Non-root user, read-only root filesystem, all Linux capabilities dropped, seccomp `RuntimeDefault`, restricted Pod Security Standard via `securityContext` |
| **Network** | `NetworkPolicy` per namespace — only Traefik namespace can reach app pods, DNS-only egress permitted. See `argocd/projects/demo-api/helm/templates/networkpolicy.yaml` |
| **Cluster** | k3s CIS benchmark alignment (via installation flags), etcd encrypted at rest, Kubernetes audit logging enabled |
| **Supply Chain** | Least-privilege GitHub Actions permissions, Dependabot for weekly dependency updates, auto-merge on Dependabot PRs, CodeQL static analysis on every push, OpenSSF Scorecard evaluation, signed commits configured |
| **Secrets** | External Secrets Operator — no secrets in Git. Secrets stored in Bitwarden, fetched at runtime, encrypted at rest via etcd |
| **Host** | Ubuntu 24.04 with unattended-upgrades (auto-patching), fail2ban (SSH brute-force protection, 5-retry ban), UFW firewall (default deny, only ports 22/80/443/6443 open), SSH key-only authentication |
| **TLS** | cert-manager with dual ClusterIssuers: staging (Let's Encrypt staging endpoint for testing) and production (Let's Encrypt prod, auto-renewal before expiry) |

> **CNCF Security Patterns:** The deployment template follows Kubernetes Pod Security Standards at the restricted level. Container images use distroless-style Debian runtime images with non-root users. See [SECURITY.md](SECURITY.md) for the complete security policy and reporting procedures.

---

## Disaster Recovery

| Scenario | Recovery Procedure |
|---|---|
| **Complete node failure** | Provision fresh Hetzner VPS → `curl bootstrap.sh | bash` → restore Velero backup → ArgoCD auto-syncs all application state from Git |
| **Accidental resource deletion** | ArgoCD detects drift within 3 minutes (or immediately via webhook), auto-syncs the deleted resource back |
| **Bad deployment (crash-loop)** | `argocd app rollback demo-api <version>` for immediate rollback, or revert the Git commit and let ArgoCD sync the previous working state |
| **Backup schedule** | Velero daily at 02:00 UTC, 7 daily + 4 weekly retention. Backups stored locally (configurable to S3-compatible storage) |

See [docs/OPS.md](docs/OPS.md) for the full Day 2 operations guide including scaling, certificate management, and monitoring access.

---

## Quick Start

```bash
# Bootstrap a blank Ubuntu 24.04 VPS
curl -fsSL https://raw.githubusercontent.com/DynamicKarabo/k3s-cluster/main/scripts/bootstrap.sh | bash

# Retrieve the ArgoCD admin password
sudo k3s kubectl -n argocd get secret argocd-initial-admin-secret \
  -o jsonpath="{.data.password}" | base64 -d

# Port-forward to verify the deployed API
sudo k3s kubectl port-forward -n demo-api svc/demo-api 8080:8080
curl http://localhost:8080/healthz
```

> **Prerequisites:** Ubuntu 24.04+ (or Debian 12+), root access, outbound internet connectivity. The bootstrap script installs Git, Ansible, Helm, and then runs the provisioning playbook.

---

## Repository Structure

```
k3s-cluster/
├── .github/               CI/CD workflows
│   ├── workflows/
│   │   ├── ci.yml              Build, test, push, update Helm values
│   │   ├── ansible-lint.yml    Ansible playbook linting
│   │   ├── codeql.yml          GitHub CodeQL security analysis
│   │   ├── scorecards.yml      OpenSSF Scorecard evaluation
│   │   └── auto-merge.yml      Dependabot PR auto-merge
│   ├── dependabot.yml          Weekly dependency updates (Actions + Docker)
│   └── CODEOWNERS
├── ansible/                Infrastructure as Code
│   ├── playbook.yml            Master playbook (common → k3s → argocd → cert-manager)
│   ├── inventory.yml           Host configuration
│   ├── vars/main.yml           Tunable variables (versions, ports, flags)
│   └── roles/
│       ├── common/             Base deps, UFW, fail2ban, unattended-upgrades, Docker
│       ├── k3s/                K3s install + config + TLS SANs
│       ├── argocd/             ArgoCD Helm install + NodePort config
│       └── cert-manager/       cert-manager Helm install + ClusterIssuer setup
├── apps/                   Application source code
│   └── demo-api/              .NET 10 minimal API
│       ├── Dockerfile            Multi-stage build (SDK → runtime)
│       ├── src/DemoApi/          Program.cs with OpenTelemetry + health checks
│       └── tests/                Unit tests
├── argocd/                  GitOps definitions
│   ├── root.yaml               App-of-apps root Application
│   └── projects/
│       ├── demo-api/             Helm chart for the .NET workload
│       ├── monitoring/           kube-prometheus-stack chart values
│       ├── cert-manager/         cert-manager chart + ClusterIssuer templates
│       ├── external-secrets/     External Secrets Operator chart
│       └── velero/               Velero backup chart
├── docs/                   Documentation
│   ├── ARCHITECTURE.md          Architecture deep dive
│   ├── OPS.md                   Day 2 operations runbook
│   └── TROUBLESHOOTING.md       Common issues and resolutions
├── scripts/                Utility scripts
│   ├── bootstrap.sh             One-shot: curl | bash cluster provisioning
│   └── reset.sh                 Destructive: removes all ArgoCD apps and workloads
├── SECURITY.md              Security policy and vulnerability reporting
├── SYSTEM_DESIGN.md         Full system design specification
├── DECISIONS.md             Architecture Decision Records (12 documented)
└── LICENSE
```

---

## Development

```bash
git clone https://github.com/DynamicKarabo/k3s-cluster.git
cd k3s-cluster/apps/demo-api

# Run locally
dotnet run --project src/DemoApi

# Run tests
dotnet test

# Build container image (for local testing)
docker build -t demo-api:local .
```

---

## Why This Matters

This repository is a **portfolio-grade demonstration** of practical Kubernetes engineering. It shows:

- **GitOps in practice** — not just installing ArgoCD, but using the app-of-apps pattern with self-healing, prune, and automated sync
- **Production thinking at small scale** — PodSecurityContext, NetworkPolicy, PDB, HPA, readiness/liveness probes on a single-node cluster
- **Supply chain security** — Dependabot, CodeQL, Scorecards, least-privilege Actions, auto-merge with guardrails
- **Observability built in** — OpenTelemetry metrics, Prometheus scraping, Grafana dashboards from day one
- **Infrastructure as Code** — the entire cluster from blank VPS to running workloads is one Ansible run away
- **Documented trade-offs** — every architectural decision includes the alternatives considered and what was sacrificed

---

<p align="center">
  <a href="SYSTEM_DESIGN.md">System Design</a> ·
  <a href="DECISIONS.md">Decision Records</a> ·
  <a href="docs/ARCHITECTURE.md">Architecture</a> ·
  <a href="docs/OPS.md">Operations</a> ·
  <a href="SECURITY.md">Security</a>
</p>

<p align="center">
  Built by <a href="https://github.com/DynamicKarabo">Karabo</a> ·
  <a href="LICENSE">MIT License</a>
</p>
