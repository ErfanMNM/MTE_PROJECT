# Deploy Script Usage

## Cách 1: Chạy trực tiếp

```powershell
cd D:\TANTIEN\CZCODE\VINA CF\MTE_PROJECT\Deploy

.\install.ps1 `
    -OrgName "YourOrg" `
    -WarpServiceTokenId "your-token-id" `
    -WarpServiceTokenSecret "your-token-secret"
```

## Cách 2: Batch file

Tạo `deploy.bat`:

```batch
@echo off
powershell -ExecutionPolicy Bypass -File "%~dp0install.ps1" ^
    -OrgName "YourOrg" ^
    -WarpServiceTokenId "your-token-id" ^
    -WarpServiceTokenSecret "your-token-secret"
pause
```

## Cách 3: Silent Install (không cần tương tác)

```powershell
$pass = ConvertTo-SecureString "YourPassword" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("Domain\User", $pass)

Invoke-Command -ComputerName "PC01" -Credential $cred -ScriptBlock {
    cd C:\Deploy
    .\install.ps1 -OrgName "YourOrg" ...
}
```

## Tạo MSI bằng WiX Toolset

### 1. Cài WiX
```powershell
winget install WiX.WiXToolset
```

### 2. Tạo Product.wxs
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
    <Product Id="*" Name="RustDesk + WARP Deployment" 
             Language="1033" Version="1.0.0" 
             Manufacturer="YourCompany" UpgradeCode="GUID-HERE">
        
        <Package InstallerVersion="500" 
                 Compressed="yes" 
                 InstallScope="perMachine" />
        
        <MajorUpgrade DowngradeErrorMessage="A newer version is installed." />
        
        <Feature Id="ProductFeature" Title="RustDesk + WARP" Level="1">
            <ComponentGroupRef Id="ProductComponents" />
        </Feature>
        
        <UI>
            <UIRef Id="WixUI_InstallDir" />
        </UI>
        
    </Product>

    <Fragment>
        <Directory Id="TARGETDIR" Name="SourceRoot">
            <Directory Id="ProgramFilesFolder">
                <Directory Id="INSTALLFOLDER" Name="RustDeskDeploy" />
            </Directory>
        </Directory>
    </Fragment>

    <Fragment>
        <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
            <Component Id="InstallScript">
                <File Source="install.ps1" />
            </Component>
        </ComponentGroup>
    </Fragment>
</Wix>
```

### 3. Build MSI
```powershell
candle.exe -nologo -ext WixUIExtension Product.wxs
light.exe -nologo -ext WixUIExtension Product.wixobj -o RustDeskDeploy.msi
```

## Cloudflare Service Token Setup

1. Login [dashboard.cloudflare.com](https://dashboard.cloudflare.com)
2. Go to **Zero Trust** → **Settings** → **Service Auth**
3. Create **New Service Token**
4. Copy **Token ID** và **Secret**
5. Dùng trong script deploy

## Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| `OrgName` | Tên tổ chức Cloudflare | Yes |
| `WarpServiceTokenId` | Service Token ID | Yes |
| `WarpServiceTokenSecret` | Service Token Secret | Yes |
| `RustDeskServer` | RustDesk server mặc định | No |
| `RelayServer` | Relay server để check | No |
| `InstallPath` | Thư mục cài service | No |
