# k3s-cluster — Security Posture

## Principle

Every workload on this cluster runs with the **least privilege necessary**. The default is deny — pods get exactly the permissions they need and nothing more.

---

## Pod Security Standards

### Pod Security Context (enforced on all workloads)

```yaml
securityContext:
  runAsNonRoot: true
  runAsUser: 1000
  runAsGroup: 1000
  fsGroup: 1000
  seccompProfile:
    type: RuntimeDefault
```

### Container Security Context

```yaml
securityContext:
  allowPrivilegeEscalation: false
  capabilities:
    drop:
      - ALL
  readOnlyRootFilesystem: true
  runAsNonRoot: true
  runAsUser: 1000
```

- **No privilege escalation** — `allowPrivilegeEscalation: false`
- **All capabilities dropped** — `capabilities.drop: [ALL]`
- **Read-only root filesystem** — prevents tampering with container filesystem at runtime
- **Non-root user** — `runAsNonRoot: true`
- **Seccomp profile** — `RuntimeDefault` (kills forbidden syscalls)

## Network Security

### Network Policies

Each namespace has granular ingress/egress rules:

```
┌─────────────────┐
│   Traefik       │ ◀── Only Traefik can reach app pods
│   (ingress)     │
└────────┬────────┘
         │ port 8080
┌────────▼────────┐
│   demo-api      │
│   (app)         │
└────────┬────────┘
         │ port 53 (DNS only)
┌────────▼────────┐
│   kube-system   │
│   (coredns)     │
└─────────────────┘
```

- **Ingress:** Only pods from the `traefik` namespace can reach app containers
- **Egress:** Only DNS queries to `kube-system` (coredns) are allowed
- **Default deny:** Any traffic not explicitly permitted is blocked

### Host Firewall (UFW)

| Port | Service | Source |
|------|---------|--------|
| 22 | SSH | Admin IPs only |
| 6443 | k3s API server | Internal |
| 80 | HTTP | Internet |
| 443 | HTTPS | Internet |
| 30443 | ArgoCD UI | Admin IPs only |

### Fail2ban

- SSH brute force protection
- 5 failed attempts → 10-minute ban

## Cluster Hardening

- **k3s CIS benchmark** alignment (key controls)
- **etcd encrypted at rest** — encryption key managed by k3s
- **Audit logging enabled** — all API server requests logged
- **Default service accounts disabled** in namespaces that don't need them
- **No anonymous access** to the kube-apiserver

## Supply Chain Security

- **GitHub Actions** — least-privilege token permissions (`contents: read` for build, `contents: write` + `packages: write` only for publish job)
- **Dependabot** — automated dependency updates for NuGet, GitHub Actions, Docker
- **Container signing** — images come from GHCR (private registry)
- **Image pull policy** — `IfNotPresent` prevents tag drift
- **Trivy scanning** — vulnerability scan in CI pipeline

## Secrets Management

- **External Secrets Operator** manages secret lifecycle
- **No secrets in Git** — Helm values reference external secrets, never inline
- **Bitwarden** backs the ESO provider
- **etcd encryption at rest** for all secret data on disk

## Host Hardening

- **Ubuntu 24.04** with automatic unattended security upgrades
- **SSH key-only authentication** — password login disabled
- **Root login disabled** — only key-based SSH
- **Fail2ban** for SSH brute force detection
- **UFW firewall** — default deny inbound
- **Docker daemon** runs with non-root context

## Observability & Detection

- **Prometheus** scrape targets configured for all workloads
- **kube-state-metrics** exposes cluster object state
- **Alertmanager** configured for incident notification
- **Grafana dashboards** for cluster health visualization
- **Kubernetes audit logs** captured for forensics

## Verified By

- `kubectl auth can-i` validation for all service accounts
- Manual review of each Helm chart template
- CI pipeline with YAML validation

---

*This document is a living artifact — updated as the cluster evolves.*
