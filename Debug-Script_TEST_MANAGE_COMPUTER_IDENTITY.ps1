<#
.SYNOPSIS
    Script de teste e depuração autônomo para a função Invoke-ComputerIdentity.

.DESCRIPTION
    Este arquivo contém tanto a definição da função quanto os blocos de teste,
    facilitando a depuração em um único local.

    COMO USAR:
    1. Salve este arquivo (por exemplo, como 'Debug-ComputerIdentity.ps1').
    2. Altere os valores na seção "PARÂMETROS DE TESTE".
    3. Descomente UM dos blocos de teste na seção "EXECUÇÃO DOS TESTES".
    4. Coloque breakpoints dentro da função 'Invoke-ComputerIdentity' abaixo.
    5. Pressione F5 no VS Code para iniciar a depuração.
#>

# --- DEFINIÇÃO DA FUNÇÃO ---

function Invoke-ComputerIdentity {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [ValidateSet('GetNewName', 'GetExistingNames', 'ResetADObject')]
        [string]$Mode,
    
        # Parâmetro para o caminho do arquivo de credencial criptografado
        [Parameter(Mandatory=$true)]
        [string]$CredentialFilePath,
    
        # Parâmetros originais
        [string]$PerfilId,
        [string]$ComputerName,
        [string]$ServidorDhcp = "10.113.1.1"
    )
    
    # --- Carregar Credencial ---
    # Esta parte é executada assim que o script é iniciado.
    try {
        if (-not (Test-Path -Path $CredentialFilePath -PathType Leaf)) {
            throw "Arquivo de credencial não encontrado em: $CredentialFilePath"
        }
        # Carrega a credencial criptografada
        $credential = Import-CliXml -Path $CredentialFilePath
    } catch {
        Write-Error "Falha ao carregar o arquivo de credencial. Erro: $_"
        # Para a execução do script se a credencial não puder ser carregada
        exit 1
    }
    
    
    # --- Lógica Principal do Script ---
    try {
        switch ($Mode) {
            'GetExistingNames' {
                Write-Host "MODO: GetExistingNames | Perfil: $PerfilId"
                if ([string]::IsNullOrWhiteSpace($PerfilId)) { throw "-PerfilId é obrigatório para este modo." }
            
                $prefixo = ""
                switch ($PerfilId) {
                    "matriz"      { $prefixo = "sepro"; break }
                    "filial_se"   { $prefixo = "se-se"; break }
                    default       { throw "PerfilId '$PerfilId' inválido para listar nomes." }
                }
                
                # USA A CREDENCIAL CARREGADA
                $computadores = Get-ADComputer -Filter "Name -like 'sepro*'" -Credential $credential | Select-Object -ExpandProperty Name | Sort-Object
                if ($computadores) {
                    # Retorna os nomes para a aplicação C#
                    return ($computadores -join [Environment]::NewLine)
                }
                return ""
            }
            'GetNewName' {
                Write-Host "MODO: GetNewName | Perfil: $PerfilId"
                if ([string]::IsNullOrWhiteSpace($PerfilId)) { throw "-PerfilId é obrigatório para este modo." }
            
                $prefixo = ""
                switch ($PerfilId) {
                    "matriz"      { $prefixo = "sepro"; break }
                    "filial_se"   { $prefixo = "se-se"; break }
                    default       { throw "PerfilId '$PerfilId' inválido para gerar novo nome." }
                }
                $numerosPossiveis = 8..190
                $numerosUsados = [System.Collections.Generic.List[int]]::new()
                
                # USA A CREDENCIAL CARREGADA
                Get-ADComputer -Filter "Name -like '$($prefixo)*'" -Credential $credential -ErrorAction SilentlyContinue | ForEach-Object {
                    if ($_.Name -match '\d+') { $numerosUsados.Add([int]$matches[0]) }
                }
            
                # EXECUTA O COMANDO DE DHCP REMOTAMENTE USANDO A CREDENCIAL
                $sbDhcp = {
                    param($prefixoLike)
                    Get-DhcpServerv4Reservation -ScopeId 10.113.11.0 | Where-Object { $_.Name -like $prefixoLike }
                }
                $reservasDhcp = Invoke-Command -ComputerName $ServidorDhcp  -Credential $credential -ScriptBlock $sbDhcp -ArgumentList "$($prefixo)*"
            
                $reservasDhcp | ForEach-Object {
                    if ($_.Name -match '\d+') { $numerosUsados.Add([int]$matches[0]) }
                }
            
                $numerosUsadosUnicos = $numerosUsados | Sort-Object -Unique
                $numerosLivres = Compare-Object $numerosPossiveis $numerosUsadosUnicos | Where-Object { $_.SideIndicator -eq '<=' }
                $proximoNumero = $numerosLivres | Select-Object -ExpandProperty InputObject -First 1
                if (-not $proximoNumero) { throw "Não há números disponíveis no range de 8 a 190 para o perfil '$PerfilId'." }
                $novoNome = "{0}{1:D4}" -f $prefixo, $proximoNumero
                return $novoNome
            }
            'ResetADObject' {
                Write-Host "MODO: ResetADObject | Computador: $ComputerName"
                if ([string]::IsNullOrWhiteSpace($ComputerName)) { throw "-ComputerName é obrigatório para o modo ResetADObject." }
                
                # EXECUTA O COMANDO DE DHCP REMOTAMENTE USANDO A CREDENCIAL
                $sbRemoveDhcp = {
                    param($NomeComputador)
                    if (Get-DhcpServerv4Reservation -Name $NomeComputador -ScopeId 10.113.11.0 -ErrorAction SilentlyContinue) {
                        Remove-DhcpServerv4Reservation -Name $NomeComputador -ScopeId 10.113.11.0 -Force
                        Write-Host "Reserva DHCP para '$NomeComputador' removida com sucesso (executado remotamente)."
                    }
                }
                Invoke-Command -ComputerName $ServidorDhcp  -Credential $credential -ScriptBlock $sbRemoveDhcp -ArgumentList $ComputerName
            
                # USA A CREDENCIAL CARREGADA
                if (Get-ADComputer -Identity $ComputerName -Credential $credential -ErrorAction SilentlyContinue) {
                    Remove-ADComputer -Identity $ComputerName -Credential $credential -Confirm:$false
                    Write-Host "Objeto do computador '$ComputerName' removido do Active Directory com sucesso."
                } else {
                    Write-Warning "AVISO: O objeto do computador '$ComputerName' não foi encontrado no AD."
                }
                return "Success"
            }
        }
    }
    catch {
        Write-Error "ERRO FATAL no script Manage-ComputerIdentity.ps1: $($_.Exception.Message)"
        exit 1
    }
}


# --- SCRIPT DE TESTE ---

# --- PRÉ-REQUISITOS ---
# Garanta que você está executando este script em uma máquina com os módulos
# 'ActiveDirectory' e 'DHCPServer' instalados.
# Import-Module ActiveDirectory
# Import-Module DhcpServer

# --- PARÂMETROS DE TESTE (Altere conforme necessário) ---
$servidorDhcpTeste = "10.113.1.1" # IMPORTANTE: Coloque o nome real do seu DC/Servidor DHCP
$perfilTeste = "matriz" # Pode ser "matriz" ou "filial_se"
$computadorParaResetar = "sepro0008" # Nome de um computador existente para testar o modo 'ResetADObject'

# --- EXECUÇÃO DOS TESTES (Descomente APENAS UM bloco por vez) ---

# --- Teste 1: Listar nomes existentes ---
# Objetivo: Ver se a função retorna a lista de computadores do AD com o prefixo correto.
try {
     Write-Host "--- INICIANDO TESTE: GetExistingNames ---" -ForegroundColor Yellow
     $nomesExistentes = Invoke-ComputerIdentity -Mode 'GetNewName' -PerfilId $perfilTeste -Credential "C:\temp\secure_credential.xml"
     Write-Host "Resultado retornado:"
     Write-Host $nomesExistentes -ForegroundColor Cyan
     Write-Host "--- TESTE FINALIZADO ---" -ForegroundColor Yellow
} catch {
     Write-Error "Ocorreu um erro no teste GetExistingNames: $_"
}


# --- Teste 2: Gerar um novo nome ---
# Objetivo: Ver se a função encontra o próximo número disponível e gera um nome de máquina.
# try {
#     Write-Host "--- INICIANDO TESTE: GetNewName ---" -ForegroundColor Yellow
#     $novoNomeGerado = Invoke-ComputerIdentity -Mode 'GetNewName' -PerfilId $perfilTeste -ServidorDhcp $servidorDhcpTeste
#     Write-Host "Resultado retornado (novo nome):"
#     Write-Host $novoNomeGerado -ForegroundColor Green
#     Write-Host "--- TESTE FINALIZADO ---" -ForegroundColor Yellow
# } catch {
#     Write-Error "Ocorreu um erro no teste GetNewName: $_"
# }


# --- Teste 3: Resetar um objeto de computador ---
# Objetivo: Testar a remoção do objeto do AD e da reserva do DHCP. CUIDADO: Esta ação é destrutiva.
# try {
#     Write-Host "--- INICIANDO TESTE: ResetADObject ---" -ForegroundColor Yellow
#     Write-Host "AVISO: Este teste irá remover o computador '$computadorParaResetar' do AD e do DHCP." -ForegroundColor Red
#     # Read-Host "Pressione Enter para continuar ou Ctrl+C para cancelar..."
#     $resultadoReset = Invoke-ComputerIdentity -Mode 'ResetADObject' -ComputerName $computadorParaResetar -ServidorDhcp $servidorDhcpTeste
#     Write-Host "Resultado retornado:"
#     Write-Host $resultadoReset -ForegroundColor Magenta
#     Write-Host "--- TESTE FINALIZADO ---" -ForegroundColor Yellow
# } catch {
#     Write-Error "Ocorreu um erro no teste ResetADObject: $_"
# }


Write-Host "`nDepuração concluída. Descomente o próximo bloco para testar outro modo."

