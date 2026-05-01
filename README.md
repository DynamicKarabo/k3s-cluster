# k3s-cluster 🚀

> **Production-grade single-node Kubernetes cluster** — GitOps-driven portfolio showcase featuring ArgoCD, .NET 10 API, and automated TLS via cert-manager.

[![GitHub Actions](https://img.shields.io/github/actions/workflow/status/DynamicKarabo/k3s-cluster/ci.yml?branch=main&style=flat-square)](https://github.com/DynamicKarabo/k3s-cluster/actions)
[![License](https://img.shields.io/github/license/DynamicKarabo/k3s-cluster?style=flat-square)](LICENSE)

**Version:** 0.1.1

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
   4 vCPU · 8GB RAM · 80GB SSD · Johannesburg, SA
```

## Core Components

- **Kubernetes:** k3s v1.35.4
- **GitOps:** ArgoCD v3.3.9
- **API Backend:** .NET 10.0.7
- **TLS Management:** cert-manager v1.16.5
- **Ingress Controller:** Traefik (k3s built-in)
- **Monitoring:** Prometheus/Grafana (pending deployment)

## Quick Start

### 1. Access ArgoCD

ArgoCD is exposed via NodePort for easy access:

```bash
# Get the admin password
sudo k3s kubectl -n argocd get secret argocd-initial-admin-secret \
  -o jsonpath="{.data.password}" | base64 -d

# Access via NodePort on your node's IP
# Open https://<NODE_IP>:30443 — username: admin
```

### 2. Test the Demo API

You can access the Demo API locally using `kubectl port-forward`:

```bash
# Port-forward the Demo API
sudo k3s kubectl port-forward -n demo-api svc/demo-api 8080:8080

# In another terminal, test the endpoints
curl http://localhost:8080/healthz
curl http://localhost:8080/api/info
```

### 3. CI/CD Flow

The deployment is managed automatically:
1. Developer pushes to `main`.
2. GitHub Actions builds the image and pushes it to GHCR.
3. GitHub Actions updates the Helm chart with the new image tag.
4. ArgoCD detects the change and syncs the cluster.

## Repository Structure

```
k3s-cluster/
├── .github/           → CI/CD workflows and actions
├── ansible/           → Infrastructure as Code for node provisioning
├── apps/              → Application source code
│   └── demo-api/      → .NET 10 minimal API backend
├── argocd/            → GitOps definitions and Helm charts
│   ├── root.yaml      → ArgoCD root application (app-of-apps)
│   └── projects/      → Application project manifests
├── docs/              → Comprehensive documentation
├── scripts/           → Helper scripts for bootstrapping and utilities
├── DECISIONS.md       → Architecture Decision Records
├── SYSTEM_DESIGN.md   → Full system design specification
└── README.md          → Project overview
```

## Deployment Pipeline

```
[ Developer ]
      │
      ▼ (git push)
[ GitHub Repository ]
      │
      ▼ (trigger)
[ GitHub Actions ] ──► Build Docker Image (.NET 10)
      │
      ▼ (push)
[ GitHub Container Registry ]
      │
      ▼ (commit new tag)
[ Helm Values in Git ]
      │
      ▼ (pull)
[ ArgoCD in Cluster ]
      │
      ▼ (sync)
[ k3s Cluster ] ──► Traefik ──► Demo API Pods
```

## Development Guide

To work on this project locally:

```bash
# Clone the repository
git clone https://github.com/DynamicKarabo/k3s-cluster.git
cd k3s-cluster

# Run the API locally
cd apps/demo-api
dotnet run --project src/DemoApi

# Run tests
dotnet test

# Build the Docker image
docker build -t demo-api apps/demo-api/
```

## Architecture Decisions Summary

Key technical choices include:
- Using k3s over kind/kubeadm.
- ArgoCD over Flux.
- Single-node architecture over multi-node.
- .NET 10 Minimal API for the backend.
- And more.

For a full breakdown of these decisions, their context, and trade-offs, please see [DECISIONS.md](DECISIONS.md).

## Documentation

- [ARCHITECTURE.md](docs/ARCHITECTURE.md) — System design, component decisions, deployment flow
- [DECISIONS.md](DECISIONS.md) — Architecture Decision Records
- [OPS.md](docs/OPS.md) — Day 2 operations, cheatsheet, scaling
- [SYSTEM_DESIGN.md](SYSTEM_DESIGN.md) — Full system design specification
- [TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) — Common issues and fixes

## Bootstrap a Fresh VPS

To reproduce this setup on a blank Ubuntu 24.04 VPS:

```bash
curl -fsSL https://raw.githubusercontent.com/DynamicKarabo/k3s-cluster/main/scripts/bootstrap.sh | bash
```

This runs the Ansible playbook to install Docker, k3s, ArgoCD, and cert-manager.

## Known Issues

- **Single-Node limitation:** No High Availability (HA) as the cluster runs on a single node.
- **Monitoring Pending:** Prometheus and Grafana are configured but currently pending deployment.
- **No Automated Backups:** Backups are currently manual; no automated backup strategy is implemented.
- **No Secrets Management:** Currently lacking a dedicated external secrets manager (e.g., HashiCorp Vault, External Secrets Operator).
- **GitHub Actions Warnings:** Node.js 20 actions are deprecated and may show warnings during CI runs.

---

<p align="center">
Made with ❤️ by <a href="https://github.com/DynamicKarabo">Karabo</a>
</p>
