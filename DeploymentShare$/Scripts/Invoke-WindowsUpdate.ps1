
<#
+===========================================================+
|                                                           |
|                                                  ____     |
|   __   __   ___   _ __   ___    __ _    ___     |___ \    |
|   \ \ / /  / _ \ | '__| / __|  / _` |  / _ \      __) |   |
|    \ V /  |  __/ | |    \__ \ | (_| | | (_) |    / __/    |
|     \_/    \___| |_|    |___/  \__,_|  \___/    |_____|   |
|                                                           |
+===========================================================+

#>


#Requires -RunAsAdministrator
[CmdletBinding()]
param()

# --- Configuração Essencial e Funções ---

# Define o protocolo de segurança para TLS 1.2 para todas as conexões web no script.
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Oculta as barras de progresso para uma execução mais limpa em ambientes automatizados.
$ProgressPreference = 'SilentlyContinue'

# Flag para rastrear a necessidade de reinicialização durante todo o processo.
$global:rebootNeeded = $false

# Função de log para padronizar as mensagens de saída.
function Write-Log([string]$msg, [ConsoleColor]$color = 'White') {
    Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') :: $msg" -ForegroundColor $color
}


# --- Lógica Principal do Script ---

try {
    # =================================================================================
    # ETAPA 1: CICLO COMPLETO DE ATUALIZAÇÕES DO WINDOWS UPDATE
    # Esta seção garante que TODAS as atualizações sejam instaladas antes de prosseguir.
    # =================================================================================
    Write-Log 'INICIANDO ETAPA 1: ATUALIZAÇÕES DO WINDOWS UPDATE' -Color Cyan

    # 1.1. Garante que o módulo PSWindowsUpdate está instalado.
    Write-Log 'Verificando a presença do módulo PSWindowsUpdate...'
    if (-not (Get-Module -ListAvailable -Name PSWindowsUpdate)) {
        Write-Log 'Módulo não encontrado. Instalando PSWindowsUpdate...' -Color Yellow
        Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -ErrorAction Stop
        Install-Module -Name PSWindowsUpdate -Force -Confirm:$false -ErrorAction Stop
        Write-Log 'Módulo PSWindowsUpdate instalado com sucesso.' -Color Green
    }
    Import-Module PSWindowsUpdate -ErrorAction Stop
    Write-Log 'Módulo PSWindowsUpdate carregado.' -Color Green

    # 1.2. Garante que o serviço do Windows Update está ativo.
    Write-Log 'Verificando o status do serviço Windows Update (wuauserv)...'
    Start-Service -Name wuauserv -ErrorAction SilentlyContinue

    # 1.3. Loop de instalação: continua buscando e instalando até não haver mais atualizações.
    $updateCycle = 0
    do {
        $updateCycle++
        Write-Log "Iniciando ciclo de verificação de atualizações nº $updateCycle..." -Color Yellow

        # Verifica se há atualizações disponíveis.
        $updates = Get-WindowsUpdate -MicrosoftUpdate -NotCategory "Upgrades" -ErrorAction SilentlyContinue

        if ($updates) {
            Write-Log "Foram encontradas $($updates.Count) atualizações. Iniciando instalação..." -Color Yellow
            
            # Instala as atualizações encontradas.
            $installResult = Install-WindowsUpdate -MicrosoftUpdate -NotCategory "Upgrades" -AcceptAll -IgnoreReboot -Verbose
            
            if ($installResult.RebootRequired -contains $true) {
                Write-Log 'AVISO: Uma reinicialização do sistema é necessária para aplicar todas as atualizações.' -Color Magenta
                exit 3010 # Código padrão do Windows para "reinicialização pendente".
            }
            
            # Marca que atualizações foram encontradas neste ciclo para continuar o loop.
            $updatesFoundInThisCycle = $true
            Write-Log "Ciclo de instalação nº $updateCycle concluído." -Color Green
        }
        else {
            # Nenhuma atualização encontrada, o loop pode parar.
            $updatesFoundInThisCycle = $false
            Write-Log 'Nenhuma nova atualização do Windows foi encontrada.' -Color Green
        }
    } while ($updatesFoundInThisCycle)

    Write-Log 'ETAPA 1 CONCLUÍDA: O sistema está totalmente atualizado com o Windows Update.' -Color Cyan

    # =================================================================================
    # ETAPA 2: ATUALIZAÇÃO DA MICROSOFT STORE E INSTALAÇÃO DO WINGET
    # Esta etapa só é executada após a conclusão bem-sucedida da Etapa 1.
    # =================================================================================
    Write-Log 'INICIANDO ETAPA 2: MICROSOFT STORE E WINGET' -Color Cyan

    # 2.1. Solicita a atualização da Microsoft Store e seus aplicativos.
    Write-Log 'Solicitando verificação de atualizações da Microsoft Store...' -Color Yellow
    try {
        Get-CimInstance -Namespace 'Root\cimv2\mdm\dmmap' -ClassName 'MDM_EnterpriseModernAppManagement_AppManagement01' |
        Invoke-CimMethod -MethodName UpdateScanMethod -ErrorAction Stop | Out-Null
        Write-Log 'Scan de atualização da Store iniciado com sucesso.' -Color Green
    }
    catch {
        Write-Log "Erro ao solicitar atualização da Store: $_" -Color Red
    }

    # 2.2. Verifica e instala o Winget, se necessário.
    if (Get-Command winget -ErrorAction SilentlyContinue) {
        Write-Log 'Winget já está instalado.' -Color Green
    }
    else {
        Write-Log 'Winget não encontrado. Iniciando processo de instalação...' -Color Yellow
        $tmpDir = "$env:TEMP\winget_deps"
        New-Item -Path $tmpDir -ItemType Directory -Force | Out-Null

        function Install-FromUrl([string]$url, [string]$file, [string]$tempPath) {
            $filePath = Join-Path -Path $tempPath -ChildPath $file
            if (-not (Test-Path $filePath)) {
                Write-Log "Baixando $file..." -Color Yellow
                Invoke-WebRequest $url -OutFile $filePath -UseBasicParsing -ErrorAction Stop
            }
            Write-Log "Instalando $file..." -Color Yellow
            Add-AppxPackage -Path $filePath -ErrorAction Stop
        }

        try {
            # Instala as dependências necessárias.
            Install-FromUrl 'https://aka.ms/Microsoft.VCLibs.x64.14.00.Desktop.appx' 'Microsoft.VCLibs.x64.Desktop.appx' $tmpDir
            Install-FromUrl 'https://github.com/microsoft/microsoft-ui-xaml/releases/download/v2.8.6/Microsoft.UI.Xaml.2.8.x64.appx' 'Microsoft.UI.Xaml.2.8.x64.appx' $tmpDir

            # Encontra a URL da última versão do Winget.
            Write-Log 'Buscando a última versão do Winget no GitHub...'
            $latestRelease = Invoke-RestMethod 'https://api.github.com/repos/microsoft/winget-cli/releases/latest' -UseBasicParsing
            $wingetUrl = $latestRelease.assets | Where-Object name -Like '*DesktopAppInstaller*msixbundle' | Select-Object -First 1 -ExpandProperty browser_download_url

            if (-not $wingetUrl) { throw "Não foi possível encontrar o pacote de instalação do Winget (*.msixbundle)." }

            # Instala o Winget.
            Install-FromUrl $wingetUrl 'winget.msixbundle' $tmpDir

            # Recarrega a variável de ambiente PATH para encontrar o novo comando.
            $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
            
            if (Get-Command winget -ErrorAction SilentlyContinue) {
                Write-Log 'Winget instalado com sucesso.' -Color Green
            } else {
                Write-Log 'ERRO: Winget foi instalado, mas o comando não está disponível nesta sessão. Uma nova sessão de terminal pode ser necessária.' -Color Red
            }
        }
        catch {
            # Captura erros específicos da instalação do Winget.
            Write-Log "ERRO durante a instalação do Winget: $_" -Color Red
        }
        finally {
            # Limpa os arquivos temporários.
            if (Test-Path $tmpDir) {
                Write-Log "Limpando diretório temporário..."
                Remove-Item $tmpDir -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }
}
catch {
    # Captura qualquer erro fatal durante o processo
    Write-Log "ERRO FATAL NO SCRIPT: Falha no ciclo de atualização. Detalhes: $($_.Exception.Message)" -Color Red
    exit 1
}

# --- Verificação Final e Código de Saída ---
Write-Log 'SCRIPT CONCLUÍDO.' -Color Cyan

if ($global:rebootNeeded) {
    Write-Log 'AVISO: Uma reinicialização do sistema é necessária para aplicar todas as atualizações.' -Color Magenta
    exit 3010 # Código padrão do Windows para "reinicialização pendente".
}
else {
    Write-Log 'O sistema está atualizado e nenhuma reinicialização é necessária.' -Color Green
    exit 0 # Sucesso.
}








<#

+=======================================================+
|                                                       |
|                                                  _    |
|   __   __   ___   _ __   ___    __ _    ___     / |   |
|   \ \ / /  / _ \ | '__| / __|  / _` |  / _ \    | |   |
|    \ V /  |  __/ | |    \__ \ | (_| | | (_) |   | |   |
|     \_/    \___| |_|    |___/  \__,_|  \___/    |_|   |
|                                                       |
+=======================================================+



[CmdletBinding()]
param()

# --- Configuração Essencial ---
$ProgressPreference = 'SilentlyContinue'
try {
    # Garante que a conexão com a galeria de módulos use TLS 1.2
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

    # 1. VERIFICA E INSTALA O MÓDULO PSWindowsUpdate
    Write-Host "Verificando a presença do módulo PSWindowsUpdate..."
    if (-not (Get-Module -ListAvailable -Name PSWindowsUpdate)) {
        Write-Host "Módulo não encontrado. Instalando..."
        Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -ErrorAction Stop
        Install-Module -Name PSWindowsUpdate -Force -Confirm:$false -ErrorAction Stop
    }
    Import-Module PSWindowsUpdate -ErrorAction Stop
    Write-Host "Módulo PSWindowsUpdate carregado."

    # 2. GARANTE QUE O SERVIÇO DO WINDOWS UPDATE ESTÁ ATIVO
    Write-Host "Verificando serviço Windows Update (wuauserv)..."
    Start-Service -Name wuauserv -ErrorAction SilentlyContinue

    # 3. BUSCA, BAIXA E INSTALA AS ATUALIZAÇÕES EM UM ÚNICO COMANDO
    Write-Host "ETAPA ÚNICA: Buscando, baixando e instalando atualizações..."
    
    # Usando o alias Install-WindowsUpdate, que é mais intuitivo.
    # -AcceptAll: Aceita todas as atualizações encontradas.
    # -IgnoreReboot: Instala e, se precisar reiniciar, NÃO PERGUNTA e continua o script.
    $installResult = Install-WindowsUpdate -MicrosoftUpdate -NotCategory "Upgrades" -AcceptAll -IgnoreReboot -Verbose

    if (-not $installResult) {
        Write-Host "NENHUMA ATUALIZAÇÃO ENCONTRADA OU APLICÁVEL. O sistema está atualizado."
        exit 0
    }
    
    Write-Host "Processo de instalação concluído."

    # 4. VERIFICA SE UMA REINICIALIZAÇÃO É NECESSÁRIA E SINALIZA
    Write-Host "Verificando status de reinicialização..."
    # A variável $installResult já contém a informação se o reboot é necessário. É mais eficiente.
    if ($installResult.RebootRequired -contains $true) {
        Write-Host "SINALIZANDO: REINICIALIZAÇÃO NECESSÁRIA."
        exit 3010 # Código de saída padrão para "reboot pending"
    } else {
        Write-Host "Atualizações instaladas. Nenhuma reinicialização é necessária."
        exit 0
    }

} catch {
    # Captura qualquer erro fatal durante o processo
    Write-Error "ERRO FATAL: Falha no ciclo de atualização. Erro: $($_.Exception.Message)"
    exit 1
}

#>
