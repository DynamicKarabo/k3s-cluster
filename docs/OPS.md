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

### Horizontal Pod Autoscaling

The demo-api has an HPA configured in its Helm chart. It auto-scales between 1-5 replicas based on CPU (70% target) and memory (80% target):

```bash
# Check HPA status
sudo k3s kubectl -n demo-api get hpa

# Describe HPA for detailed metrics
sudo k3s kubectl -n demo-api describe hpa demo-api
```

### Pod Disruption Budget

A PDB ensures at most 1 pod is unavailable during voluntary disruptions (node drains, rolling updates):

```bash
# Check PDB
sudo k3s kubectl -n demo-api get pdb
```

### Disaster Recovery

#### Scenario A: Complete Node Failure

1. Provision a new VPS (Hetzner CX33 or equivalent, Ubuntu 24.04)
2. Run the bootstrap script:
   ```bash
   curl -fsSL https://raw.githubusercontent.com/DynamicKarabo/k3s-cluster/main/scripts/bootstrap.sh | bash
   ```
3. Restore Velero backup:
   ```bash
   # List available backups
   velero get backups
   # Restore latest
   velero restore create --from-backup <latest-backup-name>
   ```
4. ArgoCD auto-syncs all Application resources from Git
5. Verify: `curl https://demo-api.karabo.tech/healthz`

#### Scenario B: Accidental Resource Deletion

1. ArgoCD detects drift within 3 minutes and auto-syncs
2. If auto-sync fails, sync manually:
   ```bash
   argocd app sync demo-api
   ```
   Or via the ArgoCD UI.

#### Scenario C: Bad Deployment (crash-loop)

1. Rollback via ArgoCD CLI:
   ```bash
   argocd app rollback demo-api <previous-version>
   ```
2. Or revert the Git commit that introduced the bad image tag and push
3. ArgoCD detects the revert and syncs the previous working version

#### Backup Schedule (Velero)

| Window | Type | Retention |
|--------|------|-----------|
| Daily 02:00 UTC | Automatic | 7 daily, 4 weekly |
| Before deploys | On-demand | Manual cleanup |

### Scaling

For manual scaling outside of HPA:

```bash
# Scale via kubectl (temporary)
sudo k3s kubectl -n demo-api scale deployment demo-api --replicas=3

# Scale via Helm values (permanent)
# Update values.yaml → replicas: 3 → ArgoCD syncs
```

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
