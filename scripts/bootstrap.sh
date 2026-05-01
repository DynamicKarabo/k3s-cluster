#!/usr/bin/env bash
# ── k3s-cluster Bootstrap Script ──
# One-liner: curl -fsSL https://raw.githubusercontent.com/DynamicKarabo/k3s-cluster/main/scripts/bootstrap.sh | bash
#
# Prerequisites:
#   - Ubuntu 24.04+ (or Debian 12+)
#   - Root access (sudo)
#   - Git, curl installed

set -euo pipefail

# ── Colors ──
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_info()  { echo -e "${BLUE}[INFO]${NC}  $*"; }
log_ok()    { echo -e "${GREEN}[OK]${NC}    $*"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC}  $*"; }
log_error() { echo -e "${RED}[ERROR]${NC} $*"; }

# ── Check prerequisites ──
if [[ $EUID -ne 0 ]]; then
    log_error "This script must be run as root"
    exit 1
fi

if ! command -v curl &>/dev/null; then
    log_info "Installing curl..."
    apt-get update -qq && apt-get install -y -qq curl
fi

if ! command -v git &>/dev/null; then
    log_info "Installing git..."
    apt-get install -y -qq git
fi

if ! command -v ansible-playbook &>/dev/null; then
    log_info "Installing Ansible..."
    apt-get update -qq && apt-get install -y -qq ansible
fi

if ! command -v helm &>/dev/null; then
    log_info "Installing Helm..."
    curl -fsSL https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
fi

# ── Clone repo ──
REPO_URL="https://github.com/DynamicKarabo/k3s-cluster.git"
REPO_DIR="/root/k3s-cluster"

if [[ -d "$REPO_DIR" ]]; then
    log_warn "$REPO_DIR already exists — pulling latest..."
    cd "$REPO_DIR" && git pull
else
    log_info "Cloning k3s-cluster from $REPO_URL..."
    git clone "$REPO_URL" "$REPO_DIR"
    cd "$REPO_DIR"
fi

# ── Run Ansible playbook ──
log_info "Running Ansible playbook..."
cd "$REPO_DIR"
ansible-playbook -i ansible/inventory.yml ansible/playbook.yml

log_ok "Bootstrap complete!"
log_info "ArgoCD URL: https://$(curl -4 -s ifconfig.me):30443"
log_info "ArgoCD admin password: sudo k3s kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath='{.data.password}' | base64 -d"
log_info "Demo API: curl http://localhost:30080/healthz"
