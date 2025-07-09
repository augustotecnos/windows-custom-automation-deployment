[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$InstallerPath,
    [Parameter(Mandatory=$true)]
    [string]$ConfigFilePath
)

try {
    Write-Host "Iniciando processo de instalação e configuração da VPN..."
    Write-Host "Verificando a existência dos arquivos de origem..."
    if (-not (Test-Path -Path $InstallerPath -PathType Leaf)) { throw "Arquivo instalador da VPN não encontrado em '$InstallerPath'." }
    if (-not (Test-Path -Path $ConfigFilePath -PathType Leaf)) { throw "Arquivo de configuração da VPN não encontrado em '$ConfigFilePath'." }
    Write-Host "Arquivos de origem validados com sucesso."
    Write-Host "Executando instalador da VPN de forma silenciosa: '$InstallerPath'..."
    $extension = [System.IO.Path]::GetExtension($InstallerPath).ToLower()
    if ($extension -eq ".msi") {
        $arguments = "/i `\"$InstallerPath`\" /qn /norestart"
        $processo = Start-Process "msiexec.exe" -ArgumentList $arguments -Wait -PassThru
    } elseif ($extension -eq ".exe") {
        $arguments = "/S /VERYSILENT /NORESTART"
        $processo = Start-Process -FilePath $InstallerPath -ArgumentList $arguments -Wait -PassThru
    } else {
        throw "Tipo de instalador '$extension' não suportado por este script."
    }
    if ($processo.ExitCode -ne 0 -and $processo.ExitCode -ne 3010) { throw "O instalador da VPN terminou com um código de erro: $($processo.ExitCode)." }
    Write-Host "Instalação do software da VPN concluída."
    $vpnConfigFolder = Join-Path -Path $env:ProgramFiles -ChildPath "OpenVPN\config"
    Write-Host "Verificando a pasta de destino da configuração: $vpnConfigFolder"
    if (-not (Test-Path -Path $vpnConfigFolder)) { Write-Host "Pasta de configuração não encontrada. Criando..."; New-Item -Path $vpnConfigFolder -ItemType Directory -Force }
    $configFileName = [System.IO.Path]::GetFileName($ConfigFilePath)
    $destinationFile = Join-Path -Path $vpnConfigFolder -ChildPath $configFileName
    Write-Host "Copiando arquivo de configuração '$configFileName' para '$destinationFile'..."
    Copy-Item -Path $ConfigFilePath -Destination $destinationFile -Force -ErrorAction Stop
    Write-Host "Configuração da VPN concluída com sucesso. O cliente está pronto para uso."
    exit 0
} catch {
    $errorMessage = "ERRO FATAL ao instalar ou configurar a VPN: $($_.Exception.Message)"
    Write-Error $errorMessage
    exit 1
}
