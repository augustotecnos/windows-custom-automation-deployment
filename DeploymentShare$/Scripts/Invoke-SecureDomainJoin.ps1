<#
.SYNOPSIS
    Ingressa um computador em um domínio do Active Directory com base na estrutura de OUs da empresa.

.DESCRIPTION
    Este script automatiza o processo de ingressar um computador no domínio 'gruposepromo.com.br'.
    Ele verifica a conectividade com o controlador de domínio, determina a Unidade Organizacional (OU) correta
    com base nos parâmetros de perfil (matriz, filial) e setor/localidade, solicita as credenciais necessárias e, em seguida,
    tenta ingressar o computador na OU 'COMPUTADORES' do respectivo setor.

.PARAMETER ComputerNameToJoin
    O nome do computador que será ingressado no domínio.

.PARAMETER PerfilId
    Identificador do perfil para determinar a OU base. Use 'matriz', 'filial' ou 'gleebem'.

.PARAMETER SetorId
    - Se PerfilId for 'matriz', informe o nome do setor (ex: 'ti', 'financeiro', 'rh').
    - Se PerfilId for 'filial', informe o nome da cidade (ex: 'fortaleza', 'natal').
    - Se PerfilId for 'gleebem', este parâmetro não é necessário.

.PARAMETER DomainController
    O endereço IP ou FQDN do controlador de domínio. O padrão é "10.113.1.1".

.PARAMETER DomainName
    O nome do domínio a ser ingressado. O padrão é "gruposepromo.com.br".

.EXAMPLE
    PS C:\> .\Join-DomainScript.ps1 -ComputerNameToJoin "PC-TI-01" -PerfilId "matriz" -SetorId "ti"

    Este comando ingressará o computador "PC-TI-01" na OU:
    "OU=COMPUTADORES,OU=TI,OU=LY MATRIZ,DC=gruposepromo,DC=com,DC=br"

.EXAMPLE
    PS C:\> .\Join-DomainScript.ps1 -ComputerNameToJoin "PC-FILIAL-01" -PerfilId "filial" -SetorId "fortaleza"

    Este comando ingressará o computador "PC-FILIAL-01" na OU:
    "OU=COMPUTADORES,OU=FORTALEZA,OU=FILIAIS,DC=gruposepromo,DC=com,DC=br"

.NOTES
    Autor: Seu Nome
    Data: 16/07/2024
    Versão: 2.1 - Convertido de função para script autônomo.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$ComputerNameToJoin,

    [Parameter(Mandatory=$true)]
    [ValidateSet('matriz', 'filial', 'gleebem')]
    [string]$PerfilId,

    [Parameter(Mandatory=$true)]
    [string]$SetorId,

    [string]$DomainController = "10.113.1.1",

    [string]$DomainName = "gruposepromo.com.br",
     # Parâmetro para o caminho do arquivo de credencial criptografado
    [Parameter(Mandatory=$true)]
    [string]$CredentialFilePath
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

try {
    Write-Host "Iniciando verificação de pré-requisitos para ingresso no domínio..."

    # Verifica a conectividade com o Controlador de Domínio na porta 389 (LDAP)
    if (-not (Test-NetConnection -ComputerName $DomainController -Port 389 -InformationLevel "Quiet")) {
        throw "Falha na comunicação de rede com o Controlador de Domínio '$DomainController'. Verifique as configurações de rede e firewall."
    }
    Write-Host "Conectividade com o Controlador de Domínio '$DomainController' confirmada."

    # --- LÓGICA DE DEFINIÇÃO DE OU ---
    $baseDN = "DC=gruposepromo,DC=com,DC=br"
    $ouComputers = "OU=COMPUTADORES"
    $ouPath = ""

    if ($PerfilId -eq "matriz") {
        $ouBase = "OU=LY MATRIZ,$baseDN"
        $departmentMap = @{
            'adm'                          = 'ADM'
            'banesecard'                   = 'BANESECARD'
            'comercial'                    = 'COMERCIAL'
            'comercial tecnico'            = 'COMERCIAL TECNICO'
            'diretoria'                    = 'DIRETORIA'
            'financeiro'                   = 'FINANCEIRO'
            'gestao de correspondente'     = 'GESTAO DE CORRESPONDENTE'
            'gestao filiais se'            = 'GESTAO FILIAIS SE'
            'micro credito'                = 'MICRO CREDITO'
            'monitoramento'                = 'MONITORAMENTO'
            'marketing'                    = 'MARKETING'
            'p d de servico'               = 'P D DE SERVICO'
            'qualidade'                    = 'QUALIDADE'
            'rh'                           = 'RH'
            'ti'                           = 'TI'
            'transacional'                 = 'TRANSACIONAL'
            'tvindoor'                     = 'TVINDOOR'
        }
        $ouDepartmentName = $departmentMap[$SetorId.ToLower()]
        if (-not $ouDepartmentName) {
            throw "Setor '$SetorId' não é válido para o perfil 'matriz'. Verifique a lista de setores disponíveis."
        }
        $ouDepartment = "OU=$ouDepartmentName"
        $ouPath = "$ouComputers,$ouDepartment,$ouBase"
    }
    elseif ($PerfilId -eq "filial") {
        $ouBase = "OU=FILIAIS,$baseDN"
        $branchMap = @{
            'juazeiro do norte' = 'JUAZEIRO DO NORTE'
            'fortaleza'         = 'FORTALEZA'
            'natal'             = 'NATAL'
        }
        $ouBranchName = $branchMap[$SetorId.ToLower()]
        if (-not $ouBranchName) {
            throw "Filial '$SetorId' não encontrada. As opções válidas são: $($branchMap.Keys -join ', ')."
        }
        $ouBranch = "OU=$ouBranchName"
        $ouPath = "$ouComputers,$ouBranch,$ouBase"
    }
    elseif ($PerfilId -eq "gleebem") {
        $ouBase = "OU=GLEEBEM,$baseDN"
        $ouPath = "$ouComputers,$ouBase"
    }
    else {
        # Este bloco é um fallback, mas o ValidateSet no parâmetro já deve prevenir isso.
        throw "PerfilId '$PerfilId' inválido."
    }

    Write-Host "A máquina será ingressada na seguinte OU: $ouPath" -ForegroundColor Green

    # Tenta ingressar o computador no domínio
    Write-Host "Tentando ingressar a máquina '$ComputerNameToJoin' no domínio '$DomainName'..."
    Add-Computer -DomainName $DomainName -Credential $credential -OUPath $ouPath -Force -ErrorAction Stop

    Write-Host "SUCESSO: Máquina '$ComputerNameToJoin' ingressada no domínio '$DomainName' com sucesso." -ForegroundColor Green
    exit 0
}
catch {
    # Captura e exibe erros que ocorreram durante a execução
    $errorMessage = "ERRO FATAL NA FASE DE INGRESSO: $($_.Exception.Message)"
    Write-Error $errorMessage
    exit 1
}
