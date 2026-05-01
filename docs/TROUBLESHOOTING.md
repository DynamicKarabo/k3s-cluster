# Troubleshooting Guide

## Common Issues & Fixes

### 1. ArgoCD won't sync

**Symptoms:** Application status shows `OutOfSync` or `SyncFailed`.
**Logs:** `sudo k3s kubectl -n argocd logs -l app.kubernetes.io/name=argocd-application-controller`

**Common causes:**

| Cause | Fix |
|-------|-----|
| Helm values invalid | Run `helm template ./argocd/projects/demo-api/helm` locally |
| Namespace doesn't exist | Enable `CreateNamespace=true` in sync options |
| Image pull failure | Check image tag in `values.yaml` vs `ghcr.io` |
| Git repo not accessible | Verify GitHub token / SSH key |

```bash
# Force a hard refresh
argocd app get demo-api --hard-refresh
argocd app sync demo-api

# Or delete and re-create
sudo k3s kubectl delete application -n argocd demo-api
sudo k3s kubectl apply -f argocd/projects/demo-api/application.yaml
```

### 2. Pods stuck in `Pending` or `CrashLoopBackOff`

```bash
# Get detailed status
sudo k3s kubectl describe pod -n demo-api <pod-name>

# Check events
sudo k3s kubectl get events -n demo-api --sort-by=.lastTimestamp

# Common fixes:
# - Insufficient resources → check `kubectl describe nodes`
# - Image pull error → verify image exists in ghcr.io
# - Liveness probe failing → check /healthz endpoint responds
```

### 3. cert-manager certificates not issuing

```bash
# Check certificate status
sudo k3s kubectl describe certificate -n demo-api demo-api-tls

# Check ClusterIssuer
sudo k3s kubectl describe clusterissuer letsencrypt-staging

# Check cert-manager logs
sudo k3s kubectl logs -n cert-manager -l app.kubernetes.io/name=cert-manager

# Common issues:
# - DNS not pointing to this server
# - Port 80 blocked (HTTP-01 challenge fails)
# - Rate limited by Let's Encrypt
#   → Use staging first, then switch to prod
```

### 4. Can't reach the API from outside

```bash
# Check the ingress route
sudo k3s kubectl get ingressroute -A

# Check Traefik logs
sudo k3s kubectl logs -n kube-system -l app.kubernetes.io/name=traefik

# Ensure ports 80/443 are open in UFW
sudo ufw status verbose
```

### 5. cluster is unresponsive

```bash
# Check k3s service status
sudo systemctl status k3s
sudo journalctl -u k3s -n 100 --no-pager

# Restart k3s (last resort)
sudo systemctl restart k3s

# Check disk space
df -h

# Check memory
free -h
```

### 6. GitHub Actions failing

| Failure | Fix |
|---------|-----|
| Docker build fails | Check Dockerfile syntax locally with `docker build apps/demo-api/` |
| Test failures | Run `dotnet test apps/demo-api/` locally |
| Push to ghcr.io fails | Verify `GITHUB_TOKEN` has `packages:write` permission |
| Helm values update fails | Check `sed` command or commit conflicts |

### 7. Grafana shows no data

```bash
# Check Prometheus targets
sudo k3s kubectl port-forward -n monitoring svc/kube-prometheus-stack-prometheus 9090:9090
# Open http://localhost:9090/targets

# Check Prometheus logs
sudo k3s kubectl logs -n monitoring -l app.kubernetes.io/name=prometheus

# Ensure scrape config is correct
# Check that the demo-api has prometheus annotations:
#   prometheus.io/scrape: "true"
#   prometheus.io/port: "8080"
```

### 8. Ansible playbook fails

```bash
# Run with verbose output
ansible-playbook -i ansible/inventory.yml ansible/playbook.yml -vvv

# Check SSH connectivity
ssh root@178.105.76.236

# If idempotency fails, check roles/ for missing `creates:` or `when:` conditions
```

### 9. Quick Recovery

If everything is broken:

```bash
# Option A: Re-run Ansible (idempotent)
cd /root/projects/k3s-cluster
ansible-playbook -i ansible/inventory.yml ansible/playbook.yml

# Option B: Full reset (⚠️ destroys everything)
./scripts/reset.sh

# Option C: Bootstrap from scratch
curl -fsSL https://raw.githubusercontent.com/DynamicKarabo/k3s-cluster/main/scripts/bootstrap.sh | bash
```

### 10. Getting Help

- Check the [docs/ARCHITECTURE.md](ARCHITECTURE.md) for system design
- Run `k3s kubectl` commands with `-o yaml` for full object definitions
- Enable verbose logging: `k3s server --debug`
- Open an issue on [GitHub](https://github.com/DynamicKarabo/k3s-cluster/issues)
