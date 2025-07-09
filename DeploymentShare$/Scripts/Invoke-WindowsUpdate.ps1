[CmdletBinding()]
param()

Write-Host "Verificando a presença do módulo PSWindowsUpdate, a ferramenta para automação do Windows Update..."
try {
    if (-not (Get-Module -ListAvailable -Name PSWindowsUpdate)) {
        Write-Host "Módulo não encontrado. Tentando instalar a partir da Galeria do PowerShell..."
        Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -ErrorAction Stop
        Install-Module -Name PSWindowsUpdate -Force -AcceptLicense -Confirm:$false -ErrorAction Stop
        Write-Host "Módulo PSWindowsUpdate instalado com sucesso."
    }
    Import-Module PSWindowsUpdate -ErrorAction Stop
    Write-Host "Módulo PSWindowsUpdate carregado e pronto para uso."
} catch {
    Write-Error "ERRO FATAL: Não foi possível instalar ou importar o módulo PSWindowsUpdate. Verifique a conexão com a internet. Erro: $($_.Exception.Message)"
    exit 1
}

Write-Host "Iniciando busca por novas atualizações do Windows Update..."
$ProgressPreference = 'SilentlyContinue'
try {
    $resultadoDaInstalacao = Get-WUInstall -MicrosoftUpdate -AcceptAll -IgnoreReboot -Verbose
    if (-not $resultadoDaInstalacao) {
        Write-Host "Nenhuma atualização nova foi encontrada. O sistema já está atualizado neste ciclo."
        exit 0
    }
    $rebootNecessario = $resultadoDaInstalacao | Where-Object { $_.RebootRequired -eq $true }
    if ($rebootNecessario) {
        Write-Host "ATENÇÃO: Uma ou mais atualizações foram instaladas e uma REINICIALIZAÇÃO é necessária para continuar."
        exit 3010
    } else {
        Write-Host "Sucesso. Todas as atualizações disponíveis foram instaladas e nenhuma reinicialização é necessária neste ciclo."
        exit 0
    }
} catch {
    Write-Error "ERRO FATAL: Falha durante o processo de instalação do Windows Update. Erro: $($_.Exception.Message)"
    exit 1
} finally {
    $ProgressPreference = 'Continue'
}
