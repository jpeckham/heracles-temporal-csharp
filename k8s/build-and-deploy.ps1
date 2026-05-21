# Build all Docker images, push to local registry, and deploy to Docker Desktop Kubernetes.
# Run from anywhere: .\k8s\build-and-deploy.ps1
# Requires: Docker Desktop with Kubernetes enabled.

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$registry = "localhost:5000"

# Ensure local registry is running
if (-not (docker ps --filter "name=local-registry" --format "{{.Names}}" | Select-String "local-registry")) {
    Write-Host "Starting local registry..." -ForegroundColor Cyan
    docker run -d -p 5000:5000 --restart=always --name local-registry registry:2
}

function BuildAndPush($dockerfile, $name, $context) {
    $tag = "$registry/heracles/$name`:latest"
    Write-Host "Building $name..." -ForegroundColor Cyan
    docker build -f $dockerfile -t $tag $context
    docker push $tag
}

BuildAndPush "$PSScriptRoot/temporal.dockerfile"         "temporal-dev" $PSScriptRoot
BuildAndPush "$root/src/PaymentApi/Dockerfile"           "payment-api"  $root
BuildAndPush "$root/src/AchApi/Dockerfile"               "ach-api"      $root
BuildAndPush "$root/src/SftpApi/Dockerfile"              "sftp-api"     $root
BuildAndPush "$root/src/AchWorker/Dockerfile"            "ach-worker"   $root

Write-Host "Applying manifests..." -ForegroundColor Cyan
kubectl apply -f "$PSScriptRoot/namespace.yaml"
kubectl apply -f "$PSScriptRoot/configmap.yaml"
kubectl apply -f "$PSScriptRoot/temporal.yaml"

Write-Host "Waiting for Temporal..." -ForegroundColor Cyan
kubectl rollout status deployment/temporal -n heracles --timeout=120s

kubectl apply -f "$PSScriptRoot/payment-api.yaml"
kubectl apply -f "$PSScriptRoot/ach-api.yaml"
kubectl apply -f "$PSScriptRoot/sftp-api.yaml"
kubectl apply -f "$PSScriptRoot/ach-worker.yaml"

Write-Host "Waiting for rollout..." -ForegroundColor Cyan
kubectl rollout status deployment/payment-api -n heracles --timeout=120s
kubectl rollout status deployment/ach-api     -n heracles --timeout=120s
kubectl rollout status deployment/sftp-api    -n heracles --timeout=120s
kubectl rollout status deployment/ach-worker  -n heracles --timeout=120s

Write-Host ""
Write-Host "Done. Endpoints:" -ForegroundColor Green
Write-Host "  PaymentApi   -> http://localhost:8081"
Write-Host "  AchApi       -> http://localhost:8082"
Write-Host "  SftpApi      -> http://localhost:8083"
Write-Host "  Temporal UI  -> http://localhost:8233"

