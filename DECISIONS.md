# Architecture Decision Records (ADRs)

This document tracks the important architectural decisions made for the `k3s-cluster` project. Each record includes the context, the decision itself, the rationale, and the trade-offs involved.

---

## 1. Kubernetes Distribution: k3s over kind/kubeadm

- **Date:** 2024-05-18
- **Status:** Accepted
- **Context:** The goal is to run a lightweight, production-like cluster on a single VPS. Setting up Kubernetes from scratch (`kubeadm`) is complex for a single node, and local solutions like `kind` or `minikube` are not ideal for internet-facing persistent setups.
- **Decision:** Use **k3s v1.35.4**.
- **Rationale:** k3s is a single-binary, CNCF-certified Kubernetes distribution designed for edge and resource-constrained environments. It comes with built-in Traefik ingress and a local storage provisioner, eliminating the need to configure these core components manually.
- **Trade-offs:** Customizing built-in components (like replacing Traefik with nginx) requires disabling them during installation, which slightly complicates the setup.

## 2. GitOps Controller: ArgoCD over Flux

- **Date:** 2024-05-18
- **Status:** Accepted
- **Context:** To implement GitOps, a controller is needed to sync cluster state with the Git repository. Both ArgoCD and Flux are CNCF graduated projects.
- **Decision:** Use **ArgoCD v3.3.9**.
- **Rationale:** ArgoCD provides a richer web UI compared to Flux, making it significantly easier to demo and visualize the cluster state. The "app-of-apps" pattern also provides a clean structure for managing multiple applications in a monorepo.
- **Trade-offs:** ArgoCD has a slightly larger resource footprint than Flux.

## 3. Infrastructure as Code: Ansible over Terraform

- **Date:** 2024-05-18
- **Status:** Accepted
- **Context:** A tool is needed to provision the base VPS, install dependencies, and bootstrap the cluster.
- **Decision:** Use **Ansible**.
- **Rationale:** For a simple single-node setup, Ansible is straightforward and doesn't require maintaining state files (unlike Terraform). It excels at configuration management and executing sequential setup tasks.
- **Trade-offs:** Ansible is less declarative than Terraform for cloud resource lifecycle management, though this is acceptable since the server itself is provisioned manually or via cloud-init.

## 4. Cluster Topology: Single-node over Multi-node

- **Date:** 2024-05-18
- **Status:** Accepted
- **Context:** The cluster needs to demonstrate Kubernetes concepts while keeping infrastructure complexity manageable for a portfolio project.
- **Decision:** Deploy a **single-node** architecture.
- **Rationale:** A single-node cluster fits comfortably within standard small VPS specs (e.g., 4 vCPU, 8GB RAM). It avoids the networking and quorum complexity of multi-node setups while still allowing full demonstration of Kubernetes APIs, GitOps, and microservices.
- **Trade-offs:** No High Availability (HA). If the node goes down, the entire cluster is offline.

## 5. Ingress Controller: Traefik over nginx

- **Date:** 2024-05-18
- **Status:** Accepted
- **Context:** An ingress controller is required to route external traffic to internal services.
- **Decision:** Use **Traefik**.
- **Rationale:** Traefik comes pre-installed with k3s, minimizing setup effort. It natively supports CRD-based routing (`IngressRoute`), which provides better typing and configuration options than standard Kubernetes `Ingress` annotations.
- **Trade-offs:** Nginx is arguably more prevalent in enterprise environments, meaning less direct translation of ingress configurations if migrating to an nginx-based setup later.

## 6. Demo Application Backend: .NET 10 Minimal API

- **Date:** 2024-05-18
- **Status:** Accepted
- **Context:** A reference workload is needed to demonstrate application deployment, health checks, and metrics.
- **Decision:** Use **.NET 10.0.7 Minimal API**.
- **Rationale:** .NET Minimal APIs provide a lightweight, high-performance web framework. It perfectly aligns with modern microservice architectures and showcases the core backend stack effectively.
- **Trade-offs:** Requires the .NET runtime environment in the container image, though this is mitigated by using optimized base images.

## 7. TLS Certificate Management: cert-manager + Let's Encrypt

- **Date:** 2024-05-18
- **Status:** Accepted
- **Context:** Public-facing services need secure HTTPS endpoints. Managing certificates manually is error-prone.
- **Decision:** Use **cert-manager v1.16.5** with Let's Encrypt.
- **Rationale:** Provides automated, free TLS certificate provisioning and renewal. Supporting both staging and production ClusterIssuers allows safe testing without hitting Let's Encrypt rate limits.
- **Trade-offs:** Introduces additional cluster components and CRDs to manage.

## 8. Observability Stack: Prometheus/Grafana over Datadog

- **Date:** 2024-05-18
- **Status:** Accepted
- **Context:** The cluster and applications need monitoring for resource usage and health.
- **Decision:** Use **Prometheus and Grafana** (via kube-prometheus-stack).
- **Rationale:** Self-hosted, free, open-source, and considered the industry standard for Kubernetes monitoring. It avoids vendor lock-in compared to SaaS solutions like Datadog.
- **Trade-offs:** Requires self-management of monitoring infrastructure and storage, whereas SaaS solutions are fully managed.

## 9. Telemetry Standard: OpenTelemetry

- **Date:** 2024-05-18
- **Status:** Accepted
- **Context:** The application needs to expose metrics for Prometheus to scrape.
- **Decision:** Use **OpenTelemetry**.
- **Rationale:** OpenTelemetry is the emerging industry standard for observability. .NET has excellent built-in support for OpenTelemetry, making it straightforward to expose standardized metrics.
- **Trade-offs:** The configuration can be slightly more verbose initially compared to older, framework-specific libraries.

## 10. Kubernetes Manifest Management: Helm over raw YAML

- **Date:** 2024-05-18
- **Status:** Accepted
- **Context:** Application deployments require Kubernetes manifests that need to change per environment (e.g., image tags).
- **Decision:** Use **Helm**.
- **Rationale:** Helm allows parameterization of manifests, making it easy to inject new image tags via CI/CD. ArgoCD natively supports Helm, seamlessly generating manifests from the charts.
- **Trade-offs:** Adds a layer of templating abstraction (Go templates) which can be complex to debug compared to plain YAML.

## 11. Application Container Base: Debian over Alpine

- **Date:** 2024-05-18
- **Status:** Accepted
- **Context:** The .NET application needs a base Docker image.
- **Decision:** Use **Debian** as the base image.
- **Rationale:** Official .NET images are built on Debian. Using the officially supported base reduces compatibility issues and ensures standard glibc support.
- **Trade-offs:** Debian images are larger than Alpine images, resulting in slightly longer pull times and higher storage usage.

## 12. ArgoCD Access: NodePort over Cloud Load Balancer

- **Date:** 2024-05-18
- **Status:** Accepted
- **Context:** The ArgoCD UI needs to be accessible from outside the cluster for management.
- **Decision:** Use **NodePort** (port 30443).
- **Rationale:** In a single-node setup without a cloud provider integration, NodePort provides a direct and simple way to expose services without relying on complex LoadBalancer configurations or external ingress rules that might conflict during bootstrap.
- **Trade-offs:** Exposes the service on a non-standard port and directly on the node's IP, which is less elegant than standard HTTPS routing on port 443.
