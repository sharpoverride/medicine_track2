#!/bin/bash

# .NET Aspire Local Kubernetes Deployment Script
# This script deploys the MedicineTrack application to a local Kubernetes cluster

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
NAMESPACE="medicine-track"
APP_NAME="medicine-track"
ASPIRE_DASHBOARD_PORT="18888"
API_PORT="5000"

echo -e "${GREEN}Starting .NET Aspire deployment to local Kubernetes cluster...${NC}"

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check prerequisites
echo -e "${YELLOW}Checking prerequisites...${NC}"

if ! command_exists kubectl; then
    echo -e "${RED}Error: kubectl is not installed${NC}"
    exit 1
fi

if ! command_exists docker; then
    echo -e "${RED}Error: Docker is not installed${NC}"
    exit 1
fi

if ! command_exists dotnet; then
    echo -e "${RED}Error: .NET SDK is not installed${NC}"
    exit 1
fi

# Check if kubectl can connect to cluster
if ! kubectl cluster-info >/dev/null 2>&1; then
    echo -e "${RED}Error: Cannot connect to Kubernetes cluster${NC}"
    echo "Please ensure your local Kubernetes cluster is running (Docker Desktop, minikube, etc.)"
    exit 1
fi

echo -e "${GREEN}All prerequisites met!${NC}"

# Create namespace if it doesn't exist
echo -e "${YELLOW}Creating namespace: ${NAMESPACE}${NC}"
kubectl create namespace ${NAMESPACE} --dry-run=client -o yaml | kubectl apply -f -

# Navigate to src directory
cd src

# Check if Aspire workload is installed
echo -e "${YELLOW}Checking .NET Aspire workload...${NC}"
if ! dotnet workload list | grep -q "aspire"; then
    echo -e "${YELLOW}Installing .NET Aspire workload...${NC}"
    dotnet workload install aspire
fi

# Build the application
echo -e "${YELLOW}Building the application...${NC}"
dotnet build

# Generate Aspire manifest
echo -e "${YELLOW}Generating Aspire manifest...${NC}"
dotnet run --project MedicineTrack.AppHost --publisher manifest --output-path ../infra/aspire-manifest.json

# Convert Aspire manifest to Kubernetes manifests
echo -e "${YELLOW}Converting to Kubernetes manifests...${NC}"
cd ../infra

# Check if aspirate tool is installed
if ! command_exists aspirate; then
    echo -e "${YELLOW}Installing aspirate tool...${NC}"
    dotnet tool install -g aspirate
fi

# Generate Kubernetes manifests
aspirate generate \
    --input-path aspire-manifest.json \
    --output-path k8s-manifests \
    --namespace ${NAMESPACE} \
    --container-registry docker.io \
    --container-image-tag latest

# Apply Kubernetes manifests
echo -e "${YELLOW}Applying Kubernetes manifests...${NC}"
kubectl apply -f k8s-manifests -n ${NAMESPACE}

# Wait for deployments to be ready
echo -e "${YELLOW}Waiting for deployments to be ready...${NC}"
kubectl wait --for=condition=available --timeout=300s deployment --all -n ${NAMESPACE}

# Get service information
echo -e "${GREEN}Deployment completed successfully!${NC}"
echo -e "${GREEN}Service information:${NC}"
kubectl get services -n ${NAMESPACE}

echo -e "${GREEN}Pod information:${NC}"
kubectl get pods -n ${NAMESPACE}

# Port forwarding setup
echo -e "${YELLOW}Setting up port forwarding...${NC}"
echo "To access the Aspire dashboard, run:"
echo "kubectl port-forward -n ${NAMESPACE} svc/aspire-dashboard ${ASPIRE_DASHBOARD_PORT}:80"
echo ""
echo "To access the API, run:"
echo "kubectl port-forward -n ${NAMESPACE} svc/medicine-track-api ${API_PORT}:80"
echo ""
echo "Or use the following commands:"
echo -e "${GREEN}# Access Aspire Dashboard${NC}"
echo "kubectl port-forward -n ${NAMESPACE} svc/aspire-dashboard ${ASPIRE_DASHBOARD_PORT}:80 &"
echo -e "${GREEN}# Access API${NC}"
echo "kubectl port-forward -n ${NAMESPACE} svc/medicine-track-api ${API_PORT}:80 &"
echo ""
echo -e "${GREEN}Deployment completed! Check the services above for access details.${NC}"
