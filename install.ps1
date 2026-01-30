# install.ps1
# Instalador remoto de APSYS Backend Prompts (para repos privados)
# Requiere: gh auth login
# Version: 1.0.0

$ErrorActionPreference = "Stop"

# Configuracion
$VERSION = "1.0.0"
$REPO = "apsys-mx/apsys-backend-development-guides"
$INSTALL_DIR = "$env:USERPROFILE\.apsys-backend-prompts"

# Archivos a instalar
$PROMPTS_TO_INSTALL = @(
    "init-backend.md",
    "add-event-store.md",
    "review-backend-code.md"
)

# Destinos segun IA
$DESTINATIONS = @{
    "claude" = @{
        "path" = "$env:USERPROFILE\.claude\commands"
        "name" = "Claude Code"
    }
    "chatgpt" = @{
        "path" = "$env:USERPROFILE\.chatgpt\prompts"
        "name" = "ChatGPT"
    }
}

# Colores
function Write-Success { param($msg) Write-Host $msg -ForegroundColor Green }
function Write-Info { param($msg) Write-Host $msg -ForegroundColor Cyan }
function Write-Warn { param($msg) Write-Host $msg -ForegroundColor Yellow }
function Write-Err { param($msg) Write-Host $msg -ForegroundColor Red }

# Banner
function Show-Banner {
    Write-Host ""
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host "   APSYS Backend Prompts Installer v$VERSION" -ForegroundColor Cyan
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host ""
}

# Verificar GitHub CLI
function Test-GitHubCLI {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        Write-Err "Error: GitHub CLI (gh) no esta instalado"
        Write-Host "Instala gh desde: https://cli.github.com"
        exit 1
    }

    # Verificar autenticacion
    $null = gh auth status 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Err "Error: No estas autenticado en GitHub CLI"
        Write-Warn "Ejecuta: gh auth login"
        exit 1
    }
}

# Clonar o actualizar repositorio
function Sync-Repository {
    if (Test-Path $INSTALL_DIR) {
        Write-Info "Actualizando repositorio..."
        Push-Location $INSTALL_DIR
        try {
            git pull origin master --quiet 2>&1 | Out-Null
            Write-Success "Repositorio actualizado"
        }
        catch {
            Write-Warn "No se pudo actualizar, usando version local"
        }
        Pop-Location
    }
    else {
        Write-Info "Descargando repositorio..."
        gh repo clone $REPO $INSTALL_DIR -- --quiet 2>&1 | Out-Null
        Write-Success "Repositorio descargado en $INSTALL_DIR"
    }
}

# Seleccion de IA
function Select-AI {
    Write-Host "Selecciona la IA donde instalar los prompts:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  [1] Claude Code  - Slash commands (~/.claude/commands/)"
    Write-Host "  [2] ChatGPT      - Prompts (~/.chatgpt/prompts/)"
    Write-Host "  [0] Cancelar"
    Write-Host ""

    $choice = Read-Host "Opcion"

    switch ($choice) {
        "1" { return "claude" }
        "2" { return "chatgpt" }
        "0" {
            Write-Info "Instalacion cancelada."
            exit 0
        }
        default {
            Write-Err "Opcion no valida"
            return Select-AI
        }
    }
}

# Instalar prompts
function Install-Prompts {
    param([string]$SelectedAI)

    $dest = $DESTINATIONS[$SelectedAI]
    $destPath = $dest["path"]
    $aiName = $dest["name"]

    Write-Host ""
    Write-Info "Instalando prompts para $aiName..."
    Write-Host "Destino: $destPath" -ForegroundColor Gray
    Write-Host ""

    # Crear directorio destino
    if (-not (Test-Path $destPath)) {
        New-Item -ItemType Directory -Path $destPath -Force | Out-Null
    }

    # Copiar archivos
    $sourcePath = Join-Path $INSTALL_DIR "prompts\commands"
    $installed = 0

    foreach ($file in $PROMPTS_TO_INSTALL) {
        $sourceFile = Join-Path $sourcePath $file
        $destFile = Join-Path $destPath $file

        if (Test-Path $sourceFile) {
            Copy-Item -Path $sourceFile -Destination $destFile -Force
            Write-Success "  [OK] $file"
            $installed++
        }
        else {
            Write-Warn "  [SKIP] $file - No encontrado"
        }
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Success "Instalacion completada! ($installed archivos)"
    Write-Host "========================================" -ForegroundColor Cyan

    # Instrucciones de uso
    if ($SelectedAI -eq "claude") {
        Write-Host ""
        Write-Host "Uso en Claude Code:" -ForegroundColor Yellow
        Write-Host "  /init-backend"
        Write-Host "  /add-event-store"
        Write-Host "  /review-backend-code"
    }

    Write-Host ""
    Write-Host "Para actualizar en el futuro, ejecuta el mismo comando." -ForegroundColor Gray
}

# Main
Show-Banner
Test-GitHubCLI
Sync-Repository

$selectedAI = Select-AI
Install-Prompts -SelectedAI $selectedAI
