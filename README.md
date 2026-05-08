# k3s-cluster — GitOps-Driven Kubernetes on K3s

[![GitHub Actions](https://img.shields.io/github/actions/workflow/status/DynamicKarabo/k3s-cluster/ci.yml?branch=main&style=flat-square&logo=githubactions&logoColor=white)](https://github.com/DynamicKarabo/k3s-cluster/actions)
[![ArgoCD](https://img.shields.io/badge/ArgoCD-EF7B4D?style=flat-square&logo=argo&logoColor=white)]()
[![K3s](https://img.shields.io/badge/k3s-FFC61C?style=flat-square&logo=k3s&logoColor=black)]()
[![Helm](https://img.shields.io/badge/Helm-0F1689?style=flat-square&logo=helm&logoColor=white)]()
[![License](https://img.shields.io/github/license/DynamicKarabo/k3s-cluster?style=flat-square)](LICENSE)

> Single-node K3s cluster with ArgoCD app-of-apps, automated CI/CD, cert-manager TLS, Prometheus monitoring, Velero backups, and network-isolated workloads. Everything deployed from Git.

## Architecture

```
┌──────────────────────────────────────────┐
│              Internet (80/443)            │
└──────────────────┬───────────────────────┘
                   │
┌──────────────────▼───────────────────────┐
│           Traefik Ingress (k3s)           │
└──────────────────┬───────────────────────┘
                   │
┌──────────────────▼───────────────────────┐
│  ArgoCD (app-of-apps) ◀── Git (main)     │
│  ┌─────────┐  ┌──────────┐  ┌────────┐  │
│  │ Demo    │  │ Monitor  │  │Addons  │  │
│  │ API     │  │ Prom+Graf│  │ cert   │  │
│  │ (Helm)  │  │ (Helm)   │  │ velero │  │
│  │         │  │          │  │ ext-sec│  │
│  └─────────┘  └──────────┘  └────────┘  │
└──────────────────────────────────────────┘
  4 vCPU · 8GB RAM · 80GB SSD · Hetzner CX33
```

[Full system design →](SYSTEM_DESIGN.md) · [Architecture deep dive →](docs/ARCHITECTURE.md)

---

## GitOps Flow

```
[ Developer ] ──git push──► [ GitHub ] ──trigger──► [ Actions ]
                                                       │
                                           Build image + push to GHCR
                                                       │
                                           Update Helm values with new SHA
                                                       │
                                              ┌────────▼────────┐
                                              │  ArgoCD detects │
                                              │  drift (3 min)  │
                                              └────────┬────────┘
                                                       │
                                          ┌────────────▼────────────┐
                                          │  Syncs Helm chart to    │
                                          │  k3s cluster            │
                                          └────────────┬────────────┘
                                                       │
                                          ┌────────────▼────────────┐
                                          │  Rolling update with    │
                                          │  liveness + readiness   │
                                          └─────────────────────────┘
```

**5 ArgoCD-managed projects:**
| Project | Purpose | Source |
|---------|---------|--------|
| demo-api | .NET 10 workload — the reason the infra exists | `argocd/projects/demo-api/helm/` |
| monitoring | Prometheus + Grafana stack | `argocd/projects/monitoring/helm/` |
| cert-manager | Let's Encrypt TLS automation | `argocd/projects/cert-manager/helm/` |
| external-secrets | Bitwarden-backed secret management | `argocd/projects/external-secrets/helm/` |
| velero | Daily cluster backups with DR restore | `argocd/projects/velero/helm/` |

---

## Security & Hardening

| Layer | Measure |
|-------|---------|
| **Pods** | Non-root, read-only root filesystem, all capabilities dropped, seccomp RuntimeDefault, restricted Pod Security Standard |
| **Network** | NetworkPolicy per namespace — only Traefik reaches app pods, DNS-only egress |
| **Cluster** | k3s CIS benchmark alignment, etcd encrypted at rest, audit logging |
| **Supply chain** | Least-privilege Actions permissions, Dependabot, signed commits, Trivy scanning |
| **Secrets** | External Secrets Operator — no secrets in Git, encrypted at rest via etcd |
| **Host** | Ubuntu 24.04 auto-updates, Fail2ban, SSH key-only, UFW firewall |

[Full security posture →](SECURITY.md)

---

## Disaster Recovery

| Scenario | Recovery |
|----------|----------|
| **Node failure** | Bootstrap fresh VPS → `curl bootstrap.sh \| bash` → restore Velero backup → ArgoCD auto-syncs |
| **Accidental delete** | ArgoCD detects drift and re-syncs within 3 minutes |
| **Bad deployment** | `argocd app rollback demo-api <version>` or revert Git commit |
| **Backup schedule** | Velero daily at 02:00 UTC, 7-day retention |

[Day 2 operations →](docs/OPS.md)

---

## Quick Start

```bash
# Bootstrap a blank Ubuntu 24.04 VPS
curl -fsSL https://raw.githubusercontent.com/DynamicKarabo/k3s-cluster/main/scripts/bootstrap.sh | bash

# ArgoCD admin password
sudo k3s kubectl -n argocd get secret argocd-initial-admin-secret \
  -o jsonpath="{.data.password}" | base64 -d

# Test the deployed API
kubectl port-forward -n demo-api svc/demo-api 8080:8080
curl http://localhost:8080/healthz
```

---

## Repository Structure

```
k3s-cluster/
├── .github/           → CI/CD workflows (build, test, deploy)
├── ansible/           → IaC playbook (Docker, k3s, ArgoCD, cert-manager)
├── apps/              → Application source code
│   └── demo-api/      → .NET 10 minimal API (Dockerfile, tests)
├── argocd/            → GitOps definitions — app-of-apps + Helm charts
│   ├── root.yaml      → Root ArgoCD Application
│   └── projects/      → 5 project manifests (demo-api, monitoring, cert-manager, ext-secrets, velero)
├── docs/              → ARCHITECTURE.md, OPS.md, TROUBLESHOOTING.md
├── scripts/           → bootstrap.sh (one-shot setup), reset.sh
├── SECURITY.md        → Pod security, network policy, host hardening
├── DECISIONS.md       → Architecture Decision Records
└── SYSTEM_DESIGN.md   → Full system design specification
```

---

## Core Components

- **Kubernetes:** k3s v1.35.4
- **GitOps:** ArgoCD v3.3.9 (app-of-apps pattern)
- **CI/CD:** GitHub Actions → GHCR → Helm values update → ArgoCD sync
- **TLS:** cert-manager v1.16.5 + Let's Encrypt
- **Ingress:** Traefik (k3s built-in)
- **Monitoring:** Prometheus + Grafana (kube-prometheus-stack)
- **Secrets:** External Secrets Operator + Bitwarden
- **Backup:** Velero (daily, 7-day retention)
- **Workload:** .NET 10 minimal API (deployment target)

## Architecture Decisions

| Decision | Rationale |
|----------|-----------|
| k3s over kind/kubeadm | Single-binary, built-in Traefik, battle-tested |
| ArgoCD over Flux | Richer UI, app-of-apps, easier to demo |
| Single-node over multi | €0 extra cost, everything fits in 4 vCPU/8GB |
| Ansible over Terraform | Simple provisioning, no state management |
| Prometheus/Grafana over Datadog | Self-hosted, free, OSS standard |

[Full ADRs →](DECISIONS.md)

## Development

```bash
git clone https://github.com/DynamicKarabo/k3s-cluster.git
cd k3s-cluster/apps/demo-api
dotnet run --project src/DemoApi
dotnet test
```

---

<p align="center">
Made with ❤️ by <a href="https://github.com/DynamicKarabo">Karabo</a>
</p>
