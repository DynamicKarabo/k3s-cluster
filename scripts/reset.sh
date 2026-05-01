#!/usr/bin/env bash
# ── k3s-cluster Reset Script ──
# ⚠️  DESTRUCTIVE — removes all workloads, ArgoCD apps, cert-manager, and monitoring.
#     Preserves k3s itself, Docker, and system packages.
#
# Usage: sudo ./scripts/reset.sh [--keep-k3s]

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info()  { echo -e "${GREEN}[INFO]${NC}  $*"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC}  $*"; }
log_error() { echo -e "${RED}[ERROR]${NC} $*"; }

if [[ $EUID -ne 0 ]]; then
    log_error "This script must be run as root"
    exit 1
fi

echo ""
log_warn "╔══════════════════════════════════════════════════════╗"
log_warn "║   This will DESTROY all ArgoCD apps and workloads   ║"
log_warn "╚══════════════════════════════════════════════════════╝"
echo ""
read -rp "Are you sure? Type 'yes' to continue: " confirm
if [[ "$confirm" != "yes" ]]; then
    log_info "Aborted."
    exit 0
fi

# ── Remove ArgoCD applications ──
log_info "Removing ArgoCD applications..."
k3s kubectl delete application -n argocd --all --ignore-not-found 2>/dev/null || true

# ── Remove namespaces ──
for ns in demo-api monitoring cert-manager; do
    log_info "Removing namespace: $ns"
    k3s kubectl delete namespace "$ns" --ignore-not-found --wait=false 2>/dev/null || true
done

# ── Remove ArgoCD ──
if [[ "${1:-}" != "--keep-k3s" ]]; then
    log_info "Removing ArgoCD..."
    k3s kubectl delete namespace argocd --ignore-not-found --wait=false 2>/dev/null || true

    # ── Remove cert-manager CRDs ──
    log_info "Removing cert-manager..."
    k3s kubectl delete namespace cert-manager --ignore-not-found --wait=false 2>/dev/null || true
    k3s kubectl delete crd -l app.kubernetes.io/instance=cert-manager --ignore-not-found 2>/dev/null || true
    k3s kubectl delete clusterissuer --all --ignore-not-found 2>/dev/null || true
fi

log_info "Waiting for resources to terminate (30s)..."
sleep 30

log_info "Cleanup complete!"

if [[ "${1:-}" != "--keep-k3s" ]]; then
    echo ""
    log_info "To re-deploy, run: cd /root/k3s-cluster && ansible-playbook -i ansible/inventory.yml ansible/playbook.yml"
    log_info "Or for a full cluster reinstall: sudo k3s-uninstall.sh && curl -fsSL https://raw.githubusercontent.com/DynamicKarabo/k3s-cluster/main/scripts/bootstrap.sh | bash"
fi
