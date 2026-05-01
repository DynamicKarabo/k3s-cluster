# Operations Guide

## Day 2 Operations

### Accessing the Cluster

All commands assume you're SSH'd into the VPS or have `kubectl` configured:

```bash
# SSH to VPS
ssh root@178.105.76.236

# Check k3s status
sudo k3s kubectl get nodes

# Check all pods across all namespaces
sudo k3s kubectl get pods -A

# Tail ArgoCD logs
sudo k3s kubectl logs -n argocd -l app.kubernetes.io/name=argocd-server
```

### ArgoCD

Access the ArgoCD web UI:

```bash
# Port-forward to access locally
sudo k3s kubectl port-forward -n argocd svc/argocd-server 8080:443

# Then open https://localhost:8080

# Get admin password
sudo k3s kubectl -n argocd get secret argocd-initial-admin-secret \
  -o jsonpath="{.data.password}" | base64 -d
```

CLI usage:

```bash
# Login
argocd login localhost:8080 --username admin --password $(...)

# List applications
argocd app list

# Sync a specific app
argocd app sync demo-api

# View app details
argocd app get demo-api
```

### Demo API

```bash
# Check API deployment
sudo k3s kubectl -n demo-api get all

# Test health endpoint
curl http://localhost:30080/healthz

# Test API info
curl http://localhost:30080/api/info

# View logs
sudo k3s kubectl -n demo-api logs -l app=demo-api
```

### Monitoring (Grafana)

```bash
# Port-forward Grafana
sudo k3s kubectl port-forward -n monitoring svc/kube-prometheus-stack-grafana 3000:80
# Open http://localhost:3000 — admin / prom-operator
```

Or access via the ingress at `https://grafana.karabo.tech` (if DNS is configured).

### Updating the Deployment

1. Make code changes in `apps/demo-api/`
2. Push to `main` → GitHub Actions builds + pushes image
3. Helm values are auto-updated with the new SHA tag
4. ArgoCD syncs within 3 minutes (or trigger manually)

### Managing TLS Certificates

```bash
# List certificates
sudo k3s kubectl get certificates -A

# Check cert-manager logs
sudo k3s kubectl logs -n cert-manager -l app.kubernetes.io/name=cert-manager

# Renew a certificate (delete it, cert-manager recreates)
sudo k3s kubectl delete certificate -n demo-api demo-api-tls
```

### Backup

Phase 1 does not include automated backups. Key paths to backup manually:

```bash
# k3s data (etcd, manifests, certs)
/etc/rancher/k3s/

# ArgoCD config
sudo k3s kubectl get applications -n argocd -o yaml

# Prometheus data (if you want to keep history)
sudo k3s kubectl get pvc -n monitoring
```

### Scaling

```bash
# Scale the API
sudo k3s kubectl -n demo-api scale deployment demo-api --replicas=3
```

Or update `values.yaml` and let ArgoCD sync.

### Common Commands Cheatsheet

| Action | Command |
|--------|---------|
| View nodes | `k3s kubectl get nodes` |
| View pods | `k3s kubectl get pods -A` |
| View services | `k3s kubectl get svc -A` |
| View ingress routes | `k3s kubectl get ingressroute -A` |
| View events | `k3s kubectl get events -A --sort-by=.lastTimestamp` |
| Describe pod | `k3s kubectl describe pod -n <ns> <pod>` |
| Follow logs | `k3s kubectl logs -f -n <ns> <pod>` |
| Exec into pod | `k3s kubectl exec -it -n <ns> <pod> -- sh` |
| Resource usage | `k3s kubectl top pods -A` |
