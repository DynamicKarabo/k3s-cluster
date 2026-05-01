# Architecture

## Overview

k3s-cluster is a **production-grade single-node Kubernetes cluster** running on a Hetzner CX33 VPS (4 vCPU, 8GB RAM, 80GB SSD) in Johannesburg, South Africa. It demonstrates GitOps workflows, observability, and .NET microservice deployment — all managed through version-controlled infrastructure.

```
┌──────────────────────────────────────────────────────────────┐
│                         Internet                              │
└──────────────────┬───────────────────────────────────────────┘
                   │
            ┌──────▼──────┐
            │  Cloudflare  │  (DNS, optional)
            └──────┬──────┘
                   │
            ┌──────▼──────────────────────────────────────────┐
            │            k3s (single node)                     │
            │                                                  │
            │  ┌─────────────────────────────────────────┐     │
            │  │            Traefik Ingress               │     │
            │  │  (k3s built-in · TLS with cert-manager)   │     │
            │  └──────────┬──────────────────────────────┘     │
            │             │                                     │
            │  ┌──────────▼──────────────────────────────┐     │
            │  │         ArgoCD (GitOps Controller)        │     │
            │  │  watches Git → syncs cluster state       │     │
            │  └──┬──────────┬──────────┬─────────────────┘     │
            │     │          │          │                        │
            │  ┌──▼──┐  ┌───▼───┐  ┌───▼────┐                  │
            │  │ Demo │  │ Monitor│  │ Addons │                  │
            │  │ .NET │  │ Prom + │  │        │                  │
            │  │ API  │  │ Grafana│  │ cert   │                  │
            │  └──────┘  └───────┘  │ etc.   │                  │
            │                       └────────┘                  │
            └──────────────────────────────────────────────────┘
```

## Component Deep Dive

### 1. Base Layer

| Component | Technology | Purpose |
|-----------|-----------|---------|
| OS | Ubuntu 24.04 LTS | Stable, widely-supported Linux distribution |
| Kubernetes | k3s 1.35+ | CNCF-certified, single-binary Kubernetes distribution |
| Container Runtime | containerd (embedded in k3s) | Lightweight OCI runtime |
| Storage | Local Path Provisioner (k3s built-in) | Simple persistent volumes for single-node |
| Ingress | Traefik (k3s built-in) | Layer 7 routing, CRD-based configuration |

### 2. GitOps & Delivery

**ArgoCD** is the heart of the deployment flow. It watches this Git repository and automatically syncs the cluster state to match. The **app-of-apps** pattern means a single root Application deploys all workloads.

Flow:
1. Developer pushes code to `main`
2. GitHub Actions builds Docker image → pushes to `ghcr.io`
3. GitHub Actions updates Helm values with new image tag
4. ArgoCD detects the drift (3-min poll or webhook)
5. ArgoCD applies the updated Helm chart to the cluster
6. k3s performs a rolling update of the deployment

### 3. Demo API

A .NET 10 minimal API that serves as the reference workload:

| Endpoint | Purpose |
|----------|---------|
| `GET /` | Root — service info |
| `GET /healthz` | Liveness probe (always 200) |
| `GET /readyz` | Readiness probe (200 when ready) |
| `GET /metrics` | Prometheus metrics (OpenTelemetry) |
| `GET /api/info` | Version, cluster, runtime details |

### 4. Observability

- **Prometheus** scrapes metrics from kube-state-metrics, node-exporter, and the demo API's `/metrics` endpoint
- **Grafana** visualizes everything with pre-configured dashboards
- **Alertmanager** is disabled in Phase 1 (single-node, no HA requirements)

### 5. Security

- **cert-manager** provisions TLS certificates from Let's Encrypt
- **Staging ClusterIssuer** for development testing
- **Production ClusterIssuer** for live traffic
- **UFW** firewall blocks all ports except 22, 80, 443, 6443
- **fail2ban** protects SSH from brute force attacks
- **Unattended-upgrades** keeps the system patched

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| **k3s over kind/kubeadm** | Already running, single-binary, built-in ingress & storage |
| **Ansible over Terraform** | Simple provisioning, no state management, version-controlled |
| **Traefik over nginx-ingress** | Built into k3s, CRD-based routing, simpler TLS |
| **ArgoCD over Flux** | Richer UI, better demo experience, app-of-apps pattern |
| **Single-node over multi-node** | Zero additional cost, everything fits in 4 vCPU / 8GB |
| **Prometheus/Grafana over Datadog** | Self-hosted, free, standard OSS stack |
| **OpenTelemetry over AppMetrics** | Industry standard, vendor-neutral, richer ecosystem |

## Repo Structure

```
k3s-cluster/
├── ansible/           → Infrastructure as Code (provisioning)
├── argocd/            → GitOps application definitions
│   ├── root.yaml      → App-of-apps entry point
│   └── projects/      → Individual workloads
├── apps/              → Application source code
│   └── demo-api/      → .NET 10 API (Dockerfile + src + tests)
├── docs/              → Documentation
├── scripts/           → Utility scripts
└── .github/           → CI/CD workflows
```
