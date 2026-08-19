#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Deploy RustDesk + WARP + Monitor Service
.DESCRIPTION
    Cài đặt hàng loạt: RustDesk, Cloudflare WARP, RustDeskMonitor Service
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$OrgName,

    [Parameter(Mandatory=$true)]
    [string]$WarpServiceTokenId,

    [Parameter(Mandatory=$true)]
    [string]$WarpServiceTokenSecret,

    [string]$RustDeskServer = "rs-ny1.rustdesk.com",
    [string]$RelayServer = "100.96.0.11",
    [string]$InstallPath = "$env:LOCALAPPDATA\Programs\RustDeskMonitor"
)

$ErrorActionPreference = "Stop"
$LogFile = "$env:TEMP\Deploy_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"

function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "$timestamp - $Message" | Tee-Object -FilePath $LogFile -Append
}

function Install-RustDesk {
    Write-Log "=== Installing RustDesk ==="

    $rustdeskPath = "$env:ProgramFiles\RustDesk\RustDesk.exe"
    if (Test-Path $rustdeskPath) {
        Write-Log "RustDesk already installed"
        return
    }

    $installer = "$env:TEMP\RustDeskSetup.exe"
    Write-Log "Downloading RustDesk..."
    Invoke-WebRequest -Uri "https://github.com/rustdesk/rustdesk/releases/latest/download/rustdesk-setup.exe" -OutFile $installer

    Write-Log "Installing RustDesk..."
    Start-Process -FilePath $installer -ArgumentList "/S" -Wait

    if (!(Test-Path $rustdeskPath)) {
        throw "RustDesk installation failed"
    }
    Write-Log "RustDesk installed successfully"
}

function Set-RustDeskConfig {
    Write-Log "=== Configuring RustDesk ==="

    $configDir = "$env:APPDATA\RustDesk\config"
    $configFile = "$configDir\RustDesk2.toml"

    if (!(Test-Path $configDir)) {
        New-Item -ItemType Directory -Path $configDir -Force | Out-Null
    }

    $config = @"
rendezvous_server = '$RustDeskServer:21116'
nat_type = 1
serial = 0
unlock_pin = ''
trusted_devices = ''

[options]
direct-server = 'Y'
key = 'ZdTwjNN1aMXSRl+9sH1I2yTpoon7WxBsWqJO5R+RHhI='
disable-udp = 'N'
custom-rendezvous-server = '$RustDeskServer'
access-mode = 'full'
direct-access-port = '21118'
av1-test = 'Y'
local-ip-addr = '100.96.0.9'
"@

    Set-Content -Path $configFile -Value $config -Force
    Write-Log "RustDesk config written to: $configFile"
}

function Install-CloudflareWARP {
    Write-Log "=== Installing Cloudflare WARP ==="

    $warpPath = "$env:ProgramFiles\Cloudflare\Cloudflare WARP\Cloudflare WARP.exe"
    if (Test-Path $warpPath) {
        Write-Log "Cloudflare WARP already installed"
        return
    }

    $installer = "$env:TEMP\CloudflareWARP.msi"
    Write-Log "Downloading Cloudflare WARP..."
    Invoke-WebRequest -Uri "https://pkg.cloudflareclient.com/warp/release/x86_64/Cloudflare_WARP_x64.msi" -OutFile $installer

    Write-Log "Installing Cloudflare WARP..."
    Start-Process -FilePath "msiexec.exe" -ArgumentList "/i `"$installer`" /qn" -Wait

    if (!(Test-Path $warpPath)) {
        throw "WARP installation failed"
    }
    Write-Log "Cloudflare WARP installed successfully"
}

function Connect-WARPToOrg {
    param(
        [string]$OrgName,
        [string]$ServiceTokenId,
        [string]$ServiceTokenSecret
    )

    Write-Log "=== Connecting WARP to Organization: $OrgName ==="

    $warpCli = "$env:ProgramFiles\Cloudflare\Cloudflare WARP\warp-cli.exe"
    if (!(Test-Path $warpCli)) {
        throw "warp-cli not found"
    }

    # Create bootstrap file
    $bootstrapDir = "$env:ProgramData\Cloudflare\warp-svc"
    if (!(Test-Path $bootstrapDir)) {
        New-Item -ItemType Directory -Path $bootstrapDir -Force | Out-Null
    }

    $bootstrap = @"
{
    "organization": "$OrgName",
    "service": "$ServiceTokenId`:$ServiceTokenSecret",
    "auto_connect": 1,
    "mode": "warp"
}
"@

    Set-Content -Path "$bootstrapDir\bootstrap" -Value $bootstrap -Force
    Write-Log "Bootstrap file created"

    # Wait for WARP service to be ready
    Start-Sleep -Seconds 5

    # Connect using service token
    Write-Log "Connecting to organization..."
    & $warpCli connect 2>&1 | Out-Null

    # Enable always-on
    & $warpCli enable-always-on 2>&1 | Out-Null

    Write-Log "WARP connected to $OrgName"
}

function Install-MonitorService {
    param([string]$InstallPath)

    Write-Log "=== Installing RustDeskMonitor Service ==="

    # Build service first
    $projectDir = Split-Path -Parent $MyInvocation.ScriptName
    $serviceExe = "$projectDir\bin\Release\net10.0\RustDeskMonitor.exe"

    if (!(Test-Path $serviceExe)) {
        Write-Log "Building service..."
        Push-Location "$projectDir\RustDesk"
        dotnet publish -c Release -o "$projectDir\Deploy\bin"
        Pop-Location
    }

    # Create install directory
    if (!(Test-Path $InstallPath)) {
        New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    }

    # Copy files
    Copy-Item -Path $serviceExe -Destination $InstallPath -Force
    Copy-Item -Path "$projectDir\RustDesk\appsettings.json" -Destination $InstallPath -Force

    # Create service
    $serviceName = "RustDeskMonitor"
    $existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

    if ($existing) {
        Write-Log "Stopping existing service..."
        Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
        sc.exe delete $serviceName | Out-Null
        Start-Sleep -Seconds 2
    }

    Write-Log "Creating service..."
    $binPath = "`"$InstallPath\RustDeskMonitor.exe`""
    sc.exe create $serviceName binPath= $binPath start= auto | Out-Null

    # Set service to interact with desktop
    sc.exe config $serviceName type= own type= interact`$with`$desktop | Out-Null

    # Start service
    Start-Service -Name $serviceName
    Write-Log "Service started"

    # Create uninstaller
    $uninstallScript = @"
`$ErrorActionPreference = "Stop"
Write-Host "Uninstalling RustDeskMonitor..."
Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
sc.exe delete $serviceName
Remove-Item -Path "$InstallPath" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Done"
"@

    Set-Content -Path "$InstallPath\Uninstall.ps1" -Value $uninstallScript
}

# ============================================
# MAIN EXECUTION
# ============================================

Write-Log "========================================="
Write-Log "Deployment Started"
Write-Log "Organization: $OrgName"
Write-Log "Install Path: $InstallPath"
Write-Log "========================================="

try {
    Install-RustDesk
    Set-RustDeskConfig
    Install-CloudflareWARP
    Connect-WARPToOrg -OrgName $OrgName -ServiceTokenId $WarpServiceTokenId -ServiceTokenSecret $WarpServiceTokenSecret
    Install-MonitorService -InstallPath $InstallPath

    Write-Log "========================================="
    Write-Log "Deployment Completed Successfully!"
    Write-Log "Log file: $LogFile"
    Write-Log "========================================="

    # Restart RustDesk
    Write-Log "Restarting RustDesk to apply config..."
    Stop-Process -Name "RustDesk" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
    Start-Process -FilePath "$env:ProgramFiles\RustDesk\RustDesk.exe"

    Write-Host "`nDeployment completed! Press any key to exit..."
    Read-Host

} catch {
    Write-Log "ERROR: $_"
    Write-Host "`nDeployment failed! Check log: $LogFile"
    Read-Host
    exit 1
}
