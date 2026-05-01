# k3s-cluster 🚀

> **Production-grade single-node Kubernetes cluster** — GitOps-driven with ArgoCD, .NET 10 API, Prometheus monitoring, and automated TLS via cert-manager.

[![GitHub Actions](https://img.shields.io/github/actions/workflow/status/DynamicKarabo/k3s-cluster/ci.yml?branch=main&style=flat-square)](https://github.com/DynamicKarabo/k3s-cluster/actions)
[![License](https://img.shields.io/github/license/DynamicKarabo/k3s-cluster?style=flat-square)](LICENSE)
[![k3s](https://img.shields.io/badge/k3s-1.35+-orange?style=flat-square)](https://k3s.io)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square)](https://dotnet.microsoft.com)
[![ArgoCD](https://img.shields.io/badge/ArgoCD-2.x-EF7B4D?style=flat-square)](https://argo-cd.readthedocs.io)
[![Helm](https://img.shields.io/badge/Helm-3.x-0F1689?style=flat-square)](https://helm.sh)

---

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
│  │ .NET    │  │ Monitor  │  │ Addons │  │
│  │ API     │  │ Prom+Graf│  │ cert   │  │
│  └─────────┘  └──────────┘  └────────┘  │
└──────────────────────────────────────────┘
        4 vCPU · 8GB RAM · 80GB SSD
         Johannesburg, South Africa
```

## Quick Start

### 1. Bootstrap a Fresh VPS

```bash
curl -fsSL https://raw.githubusercontent.com/DynamicKarabo/k3s-cluster/main/scripts/bootstrap.sh | bash
```

This installs Docker, k3s, ArgoCD, and cert-manager in one run.

### 2. Access ArgoCD

```bash
# Get the admin password
sudo k3s kubectl -n argocd get secret argocd-initial-admin-secret \
  -o jsonpath="{.data.password}" | base64 -d

# Port-forward the UI
sudo k3s kubectl port-forward -n argocd svc/argocd-server 8080:443

# Open https://localhost:8080 — username: admin
```

### 3. Test the Demo API

```bash
# Via NodePort
curl http://localhost:30080/healthz
# → {"status":"healthy"}

curl http://localhost:30080/api/info
# → {"version":"0.1.0","cluster":"k3s-cluster",...}
```

### 4. Check Monitoring

```bash
# Port-forward Grafana
sudo k3s kubectl port-forward -n monitoring svc/kube-prometheus-stack-grafana 3000:80
# Open http://localhost:3000 — admin / prom-operator
```

## What's Inside

| Component | Tech | Purpose |
|-----------|------|---------|
| **Kubernetes** | k3s 1.35+ | Lightweight, CNCF-certified K8s distribution |
| **GitOps** | ArgoCD 2.x | App-of-apps pattern, auto-sync from Git |
| **API** | .NET 10 Minimal API | Health checks, Prometheus metrics, env-driven config |
| **Ingress** | Traefik (k3s built-in) | CRD-based routing, TLS termination |
| **TLS** | cert-manager + Let's Encrypt | Auto-renewing certificates |
| **Monitoring** | Prometheus + Grafana | Cluster metrics, dashboards |
| **Provisioning** | Ansible | Reproducible, version-controlled infra |
| **CI/CD** | GitHub Actions | Build → Test → Push → Deploy |

## Repository Structure

```
k3s-cluster/
├── ansible/           → Infrastructure as Code
├── argocd/            → GitOps applications + Helm charts
├── apps/              → Application source code
│   └── demo-api/      → .NET 10 minimal API
├── docs/              → Architecture, ops, troubleshooting
├── scripts/           → Bootstrap & reset utilities
└── .github/           → CI/CD workflows
```

## Deployment Flow

```
Developer push → GitHub Actions builds image → pushes to GHCR
    → updates Helm values → ArgoCD detects drift
    → applies Helm chart → k3s rolling update
    → Prometheus scrapes /metrics → Grafana dashboard updates
```

## Development

```bash
# Clone the repo
git clone https://github.com/DynamicKarabo/k3s-cluster.git
cd k3s-cluster

# Run the API locally
cd apps/demo-api
dotnet run --project src/DemoApi

# Run tests
dotnet test

# Build Docker image
docker build -t demo-api apps/demo-api/
```

## Prerequisites for Bootstrap

- **Ubuntu 24.04+** (or Debian 12+)
- **Root access** (sudo)
- **Minimum 2 vCPU, 4GB RAM** (recommended: 4 vCPU, 8GB)
- **Ports 80, 443, 6443** accessible
- **curl, git** installed

## Documentation

| Document | What it covers |
|----------|---------------|
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | System design, component decisions, deployment flow |
| [OPS.md](docs/OPS.md) | Day 2 operations, cheatsheet, scaling |
| [TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) | Common issues & fixes |
| [SYSTEM_DESIGN.md](SYSTEM_DESIGN.md) | Full architecture document |

## License

MIT — see [LICENSE](LICENSE).

---

<p align="center">
Made with ❤️ by <a href="https://github.com/DynamicKarabo">Karabo</a>
</p>
