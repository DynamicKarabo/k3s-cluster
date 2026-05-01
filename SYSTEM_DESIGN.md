# k3s-cluster — System Design

> Production-grade single-node k3s cluster on Hetzner CX33 (4 vCPU, 8GB RAM, 80GB SSD).  
> Johannesburg, SA · Ubuntu 24.04

---

## 1. Goals

- **Portfolio showpiece** — prove k8s competence to hiring managers in fintech/SaaS
- **GitOps-driven** — everything deployed via ArgoCD from Git
- **Real workload** — .NET API running on the cluster with proper ingress, TLS, and monitoring
- **Reproducible** — Ansible playbook provisions the node from blank OS to running cluster
- **Self-documenting** — architecture, decisions, and runbooks live in this repo

---

## 2. Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                         Internet                              │
└──────────────────┬───────────────────────────────────────────┘
                   │
            ┌──────▼──────┐
            │  Cloudflare  │  (DNS · optional)
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
            │  │ App  │  │ Demo  │  │ Addons │                  │
            │  │ 1    │  │ .NET  │  │        │                  │
            │  │ Helm │  │ API   │  │ Monitor│                  │
            │  └──────┘  │ Helm  │  │ cert   │                  │
            │            └───────┘  │ etc.   │                  │
            │                       └────────┘                  │
            │                                                  │
            │  ┌─────────────────────────────────────────┐     │
            │  │    Prometheus ← Grafana (monitoring)     │     │
            │  │    Cert-Manager → Let's Encrypt          │     │
            │  │    Local Path Provisioner (storage)      │     │
            │  └─────────────────────────────────────────┘     │
            │                                                  │
            │   VPS: 178.105.76.236                             │
            │   Spec: 4 vCPU · 8GB RAM · 80GB SSD              │
            └──────────────────────────────────────────────────┘
```

---

## 3. Components

### 3.1 Base Layer

| Component | Choice | Why |
|-----------|--------|-----|
| OS | Ubuntu 24.04 | Standard, well-supported |
| Kubernetes | k3s + traefik | Lightweight, single-binary, built-in ingress |
| Provisioning | Ansible | Reproducible, version-controlled infra |
| Storage | Local Path Provisioner | Built into k3s, simple for single-node |

### 3.2 GitOps & Deployments

| Component | Choice | Why |
|-----------|--------|-----|
| GitOps | ArgoCD | Industry standard, auto-sync, Rollback |
| Package Mgmt | Helm 3 | Charts are the k8s standard |
| CI/CD | GitHub Actions | Already in the stack, builds Docker images |
| Registry | GitHub Container Registry (ghcr.io) | Free, private, integrated with Actions |

### 3.3 Security

| Component | Choice | Why |
|-----------|--------|-----|
| TLS | cert-manager + Let's Encrypt | Free auto-renewing certs |
| Secrets | External Secrets Operator + Bitwarden/SOPS | GitOps-friendly secrets |
| Network | k3s built-in + Network Policies | Isolation between namespaces |

### 3.4 Observability

| Component | Choice | Why |
|-----------|--------|-----|
| Metrics | Prometheus + kube-state-metrics | Cluster + app metrics |
| Dashboards | Grafana | Visualise everything |
| Logging | loki + promtail (optional phase 2) | Centralised logs |
| Alerts | Alertmanager | Notify on failures |

---

## 4. Repo Structure

```
k3s-cluster/
├── SYSTEM_DESIGN.md                    ← this file
├── ansible/
│   ├── inventory.yml                   # VPS host config
│   ├── playbook.yml                    # Full provision playbook
│   ├── roles/
│   │   ├── common/                     # base deps, firewall, fail2ban
│   │   ├── k3s/                        # k3s install + config
│   │   ├── argocd/                     # ArgoCD install via Helm
│   │   └── cert-manager/               # cert-manager install
│   └── vars/
│       ├── main.yml
│       └── secrets.yml                 # (encrypted with ansible-vault)
├── argocd/
│   ├── root.yaml                       # Root ArgoCD Application — apps of apps
│   └── projects/
│       ├── demo-api/
│       │   ├── application.yaml        # ArgoCD app pointing to Helm chart
│       │   └── helm/
│       │       ├── Chart.yaml
│       │       ├── values.yaml
│       │       └── templates/
│       │           ├── deployment.yaml
│       │           ├── service.yaml
│       │           ├── ingress.yaml
│       │           └── configmap.yaml
│       └── monitoring/
│           ├── application.yaml
│           └── helm/
│               └── ...                 # kube-prometheus-stack values
├── apps/
│   └── demo-api/
│       ├── Dockerfile                  # .NET API container
│       ├── src/
│       └── tests/
├── scripts/
│   ├── bootstrap.sh                    # one-shot: curl | bash setup
│   └── reset.sh                        # nuke everything
└── docs/
    ├── ARCHITECTURE.md                 # Deep dive
    ├── OPS.md                          # Day 2 operations
    └── TROUBLESHOOTING.md              # Common issues + fixes
```

---

## 5. Phases

### Phase 1 — Foundation (this sprint)

1. **Ansible playbook** — install Docker, k3s, hardening
2. **ArgoCD install** — via Helm, app-of-apps pattern
3. **cert-manager** — Let's Encrypt ClusterIssuer
4. **Demo .NET API** — simple health endpoint, Dockerfile, Helm chart
5. **Monitoring stack** — Prometheus + Grafana via kube-prometheus-stack
6. **GitHub Actions** — CI builds + pushes to ghcr.io

### Phase 2 — Polish (future)

7. External Secrets + Vault integration
8. Loki/Promtail for log aggregation
9. Network Policies
10. Canary deployments demo (Flagger? Manual?)
11. Backup strategy (Velero + Hetzner Object Storage?)

---

## 6. Deployment Flow

```
Developer pushes to main
        │
        ▼
GitHub Actions
  ├── Build .NET API → ghcr.io/image:sha
  └── Update Helm chart values (if needed)
        │
        ▼
ArgoCD detects drift (3-min sync or webhook)
        │
        ▼
Applies Helm chart to k3s
  ├── Creates/Updates Deployment
  ├── Creates/Updates Service
  ├── Creates/Updates IngressRoute
  └── Waits for rollout
        │
        ▼
Monitoring
  ├── Prometheus scrapes /metrics
  ├── Grafana dashboard updates
  └── Alertmanager on duty
```

---

## 7. Demo API Design

```
demo-api.karabo.tech (or .local)
  GET  /healthz     → 200 OK
  GET  /readyz      → 200 OK (checks DB)
  GET  /metrics     → Prometheus metrics
  GET  /api/info    → { "version", "cluster", "uptime" }
```

Built in .NET 8/10 minimal API. Single Dockerfile.  
Shows: health checks, liveness/readiness probes, Prometheus metrics, ConfigMap-driven config.

---

## 8. Non-Goals

- Multi-node cluster (single-node is intentional — Hetzner CX33)
- Service mesh (Istio/Linkerd — overkill for portfolio)
- Full PCI/HIPAA compliance (it's a learning cluster)
- Production SLA (no HA, no backups in Phase 1)
- Persistent complex stateful workloads (DBs stay in Docker for now)

---

## 9. Key Decisions

| Decision | Rationale |
|----------|-----------|
| k3s over kind/kubeadm | Already running, single-binary, simple |
| Ansible over Terraform | Simple provisioning, no state management overhead |
| Traefik over nginx-ingress | Built into k3s, CRD-based routing |
| ArgoCD over Flux | Richer UI, easier to demo, app-of-apps pattern |
| Single-node over multi-node | €0 extra cost, everything fits in 4 vCPU/8GB |
| Prometheus/Grafana over Datadog | Self-hosted, free, standard OSS stack |

---

## 10. Success Criteria

- [ ] Ansible playbook provisions bare Ubuntu → k3s + ArgoCD in one run
- [ ] Demo .NET API deploys via ArgoCD with TLS
- [ ] `curl https://demo-api/healthz` returns 200
- [ ] Grafana shows cluster metrics (CPU, RAM, pods, network)
- [ ] GitHub Actions builds + pushes image to ghcr.io
- [ ] Git push → auto-deploy via ArgoCD in under 5 minutes
- [ ] Ansible playbook passes in fresh VPS (tested via Hetzner snapshot)
