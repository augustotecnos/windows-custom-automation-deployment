[CmdletBinding()]
param()

# --- Configuração Essencial ---
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$ProgressPreference = 'SilentlyContinue'

try {
    Write-Host "Verificando a presença do módulo PSWindowsUpdate..."
    if (-not (Get-Module -ListAvailable -Name PSWindowsUpdate)) {
        Write-Host "Módulo não encontrado. Instalando..."
        Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -ErrorAction Stop
        Install-Module -Name PSWindowsUpdate -Force -Confirm:$false -ErrorAction Stop
    }
    Import-Module PSWindowsUpdate -ErrorAction Stop
    Write-Host "Módulo PSWindowsUpdate carregado."

    Write-Host "Verificando serviço Windows Update (wuauserv)..."
    Start-Service -Name wuauserv -ErrorAction SilentlyContinue

    # ETAPA 1: Buscar e FILTRAR apenas por atualizações que não pedem interação do usuário.
    Write-Host "ETAPA 1/4: Buscando atualizações silenciosas..."
    # CORREÇÃO: Removido o parâmetro duplicado -IgnoreUserInput. -Silent faz o trabalho.
    $updatesFiltradas = Get-WUList -MicrosoftUpdate -NotCategory "Upgrades" -Silent -Verbose

    if (-not $updatesFiltradas) {
        Write-Host "NENHUMA ATUALIZAÇÃO ENCONTRADA. O sistema está totalmente atualizado."
        exit 0
    }
    Write-Host "Encontradas $($updatesFiltradas.Count) atualizações compatíveis com instalação silenciosa."

    # ETAPA 2: BAIXAR as atualizações filtradas.
    Write-Host "ETAPA 2/4: Iniciando o DOWNLOAD das atualizações..."
    $downloadResult = $updatesFiltradas | Get-WUInstall -Download -AcceptAll -Verbose
    if (-not $downloadResult) {
        Write-Error "A etapa de download não retornou nenhum resultado ou falhou."
        exit 1
    }
    Write-Host "Download concluído."

    # ETAPA 3: INSTALAR apenas o que foi baixado com sucesso.
    Write-Host "ETAPA 3/4: Verificando e instalando atualizações baixadas..."
    $updatesParaInstalar = Get-WUList -MicrosoftUpdate | Where-Object { $_.IsDownloaded -eq $true }
    
    if (-not $updatesParaInstalar) {
        Write-Host "Nenhuma atualização foi baixada com sucesso. Verifique os logs para erros."
        exit 1
    }
    
    $installResult = $updatesParaInstalar | Get-WUInstall -Install -AcceptAll -Verbose
    if (-not $installResult) {
        Write-Error "A etapa de instalação não retornou nenhum resultado ou falhou."
        exit 1
    }
    Write-Host "Instalação concluída."

    # ETAPA 4: Verificar a necessidade de reinicialização e SINALIZAR para o C#.
    Write-Host "ETAPA 4/4: Verificando status de reinicialização..."
    if (Get-WURebootStatus) {
        Write-Host "SINALIZANDO: REINICIALIZAÇÃO NECESSÁRIA."
        exit 3010
    } else {
         Write-Host "Atualizações instaladas. Nenhuma reinicialização necessária nesta rodada."
         exit 0
    }

} catch {
    Write-Error "ERRO FATAL: Falha no ciclo de atualização. Erro: $($_.Exception.Message)"
    exit 1
}


<#
[CmdletBinding()]
param()

# Garante que a conexão com a galeria use o protocolo TLS 1.2, essencial em instalações novas.
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

Write-Host "Verificando a presença do módulo PSWindowsUpdate, a ferramenta para automação do Windows Update..."
try {
    if (-not (Get-Module -ListAvailable -Name PSWindowsUpdate)) {
        Write-Host "Módulo não encontrado. Tentando instalar a partir da Galeria do PowerShell..."
        Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -ErrorAction Stop
        Install-Module -Name PSWindowsUpdate -Force -Confirm:$false -ErrorAction Stop
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
    # ETAPA ADICIONAL: Garantir que o serviço do Windows Update (wuauserv) está em execução.
    Write-Host "Verificando o status do serviço Windows Update (wuauserv)..."
    $wuService = Get-Service -Name wuauserv -ErrorAction SilentlyContinue
    if ($wuService -and $wuService.Status -ne 'Running') {
        Write-Host "Serviço não está em execução. Tentando iniciar o serviço wuauserv..."
        Start-Service -Name wuauserv -ErrorAction Stop
        Start-Sleep -Seconds 5 
        Write-Host "Serviço wuauserv iniciado com sucesso."
    } elseif (-not $wuService) {
         Write-Error "ERRO CRÍTICO: O serviço Windows Update (wuauserv) não foi encontrado no sistema."
         exit 1
    } else {
        Write-Host "Serviço wuauserv já está em execução."
    }

    # --- NOVA LÓGICA EM ETAPAS ---

    # ETAPA 1: BUSCAR a lista de atualizações disponíveis.
    Write-Host "ETAPA 1/3: Buscando todas as atualizações disponíveis..."
    $updates = Get-WUList -MicrosoftUpdate -NotCategory "Upgrades" -Verbose
    
    if (-not $updates) {
        Write-Host "Nenhuma atualização nova (exceto Upgrades) foi encontrada. O sistema já está atualizado."
        exit 0
    }

    Write-Host "Foram encontradas $($updates.Count) atualizações para processar."

    # ETAPA 2: BAIXAR as atualizações encontradas.
    Write-Host "ETAPA 2/3: Iniciando o DOWNLOAD das atualizações..."
    $downloadResult = $updates | Get-WUInstall -AcceptAll -Download -Verbose
    if (-not $downloadResult) {
        Write-Error "ERRO: A etapa de download não retornou nenhum resultado ou falhou."
        exit 1
    }
    Write-Host "DOWNLOAD concluído."

    # ETAPA 3: INSTALAR as atualizações já baixadas.
    Write-Host "ETAPA 3/3: Iniciando a INSTALAÇÃO das atualizações..."
    $installResult = $updates | Get-WUInstall -AcceptAll -Install -Verbose
    if (-not $installResult) {
        Write-Error "ERRO: A etapa de instalação não retornou nenhum resultado ou falhou."
        exit 1
    }
    Write-Host "INSTALAÇÃO concluída."

    # Verificação final da necessidade de reinicialização.
    $rebootNecessario = $installResult | Where-Object { $_.RebootRequired -eq $true }

    if ($rebootNecessario) {
        Write-Host "ATENÇÃO: Uma ou mais atualizações foram instaladas e uma REINICIALIZAÇÃO é necessária para continuar."
        exit 3010
    } else {
        Write-Host "Sucesso. Todas as atualizações disponíveis foram processadas e nenhuma reinicialização é necessária."
        exit 0
    }
} catch {
    Write-Error "ERRO FATAL: Falha durante o processo de instalação do Windows Update. Erro: $($_.Exception.Message)"
    exit 1
} finally {
    $ProgressPreference = 'Continue'
}

#>
