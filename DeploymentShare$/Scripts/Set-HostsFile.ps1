[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$HostsFileName
)

try {
    $sourcePath = "\\seu-servidor\DeploymentShare$\Config\Hosts\$HostsFileName"
    $destinationPath = Join-Path -Path $env:SystemRoot -ChildPath "System32\drivers\etc\hosts"
    Write-Host "Iniciando processo de configuração do arquivo hosts."
    Write-Host "Origem: $sourcePath"
    Write-Host "Destino: $destinationPath"
    if (-not (Test-Path -Path $sourcePath -PathType Leaf)) { throw "Arquivo hosts de origem não encontrado em '$sourcePath'. Verifique o nome do arquivo e o compartilhamento." }
    Write-Host "Arquivo de origem encontrado."
    Write-Host "Copiando arquivo de origem para o destino, sobrescrevendo se necessário..."
    Copy-Item -Path $sourcePath -Destination $destinationPath -Force -ErrorAction Stop
    Write-Host "Arquivo hosts configurado com sucesso."
    exit 0
} catch {
    $errorMessage = "ERRO FATAL ao configurar o arquivo hosts: $($_.Exception.Message)"
    Write-Error $errorMessage
    exit 1
}
