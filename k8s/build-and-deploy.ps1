# Build all Docker images and deploy to local Docker Desktop Kubernetes.
# Run from the repo root: .\k8s\build-and-deploy.ps1
# Requires: Docker Desktop with Kubernetes enabled, temporal server start-dev running on host.

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

Write-Host "Building images..." -ForegroundColor Cyan

docker build -f "$root/src/PaymentApi/Dockerfile" -t heracles/payment-api:latest $root
docker build -f "$root/src/AchApi/Dockerfile"     -t heracles/ach-api:latest     $root
docker build -f "$root/src/SftpApi/Dockerfile"    -t heracles/sftp-api:latest    $root
docker build -f "$root/src/AchWorker/Dockerfile"  -t heracles/ach-worker:latest  $root

Write-Host "Applying manifests..." -ForegroundColor Cyan

kubectl apply -f "$PSScriptRoot/namespace.yaml"
kubectl apply -f "$PSScriptRoot/configmap.yaml"
kubectl apply -f "$PSScriptRoot/payment-api.yaml"
kubectl apply -f "$PSScriptRoot/ach-api.yaml"
kubectl apply -f "$PSScriptRoot/sftp-api.yaml"
kubectl apply -f "$PSScriptRoot/ach-worker.yaml"

Write-Host ""
Write-Host "Waiting for rollout..." -ForegroundColor Cyan
kubectl rollout status deployment/payment-api -n heracles
kubectl rollout status deployment/ach-api     -n heracles
kubectl rollout status deployment/sftp-api    -n heracles
kubectl rollout status deployment/ach-worker  -n heracles

Write-Host ""
Write-Host "Done. Endpoints:" -ForegroundColor Green
Write-Host "  PaymentApi -> http://localhost:30081"
Write-Host "  AchApi     -> http://localhost:30082"
Write-Host "  SftpApi    -> http://localhost:30083"
