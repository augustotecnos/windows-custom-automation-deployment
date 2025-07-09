[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$ComputerNameToJoin,
    [Parameter(Mandatory=$true)]
    [string]$PerfilId,
    [Parameter(Mandatory=$true)]
    [string]$SetorId,
    [string]$DomainController = "seu-dc-01.sepromotora.local",
    [string]$DomainName = "sepromotora.local"
)

try {
    Write-Host "Iniciando verificação de pré-requisitos para ingresso no domínio..."
    if (-not (Test-NetConnection -ComputerName $DomainController -Port 389 -InformationLevel "Quiet")) {
        throw "Falha na comunicação de rede com o Controlador de Domínio '$DomainController'. Verifique as configurações de rede e firewall."
    }
    Write-Host "Conectividade com o Controlador de Domínio '$DomainController' confirmada."
    $ouPath = "OU=Workstations,DC=sepromotora,DC=local"
    if ($PerfilId -eq "matriz") {
        $ouBaseMatriz = "OU=Workstations-Matriz,DC=sepromotora,DC=local"
        switch ($SetorId) {
            "ti"         { $ouPath = "OU=TI,$ouBaseMatriz" }
            "financeiro" { $ouPath = "OU=Financeiro,$ouBaseMatriz" }
            "rh"         { $ouPath = "OU=RH,$ouBaseMatriz" }
            "vendas"     { $ouPath = "OU=Vendas,$ouBaseMatriz" }
            default      { $ouPath = "OU=Geral,$ouBaseMatriz" }
        }
    }
    Write-Host "A máquina será ingressada na seguinte OU: $ouPath"
    Write-Host "Solicitando credenciais do técnico de TI..."
    $credential = Get-Credential -Message "Forneça as credenciais de administrador do domínio para ingressar a máquina '$ComputerNameToJoin'"
    if (-not $credential) { throw "Operação cancelada pelo usuário. Nenhuma credencial foi fornecida." }
    Write-Host "Tentando ingressar a máquina '$ComputerNameToJoin' no domínio '$DomainName'..."
    Add-Computer -DomainName $DomainName -Credential $credential -OUPath $ouPath -Force -ErrorAction Stop
    Write-Host "SUCESSO: Máquina ingressada no domínio com sucesso."
    exit 0
} catch {
    $errorMessage = "ERRO FATAL NA FASE DE INGRESSO: $($_.Exception.Message)"
    Write-Error $errorMessage
    exit 1
}
