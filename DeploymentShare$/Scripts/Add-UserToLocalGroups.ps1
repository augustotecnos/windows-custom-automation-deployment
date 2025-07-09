[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$UserName,
    [Parameter(Mandatory=$true)]
    [string[]]$GroupNames,
    [string]$DomainName = "SEPROMOTORA"
)

$fullUserName = "$DomainName\$UserName"
Write-Host "Iniciando processo para adicionar o usuário '$fullUserName' a grupos locais..."

if (-not $GroupNames) {
    Write-Host "Nenhum grupo foi especificado. Encerrando o script."
    exit 0
}

foreach ($groupName in $GroupNames) {
    try {
        Write-Host "Processando grupo local: '$groupName'..."
        $grupoLocal = Get-LocalGroup -Name $groupName -ErrorAction SilentlyContinue
        if (-not $grupoLocal) { throw "O grupo local '$groupName' não foi encontrado nesta máquina." }
        $members = Get-LocalGroupMember -Group $groupName
        if ($members.Name -contains $fullUserName) {
            Write-Host "O usuário '$fullUserName' já é membro do grupo '$groupName'. Nenhuma ação necessária."
            continue
        }
        Write-Host "Adicionando '$fullUserName' ao grupo '$groupName'..."
        Add-LocalGroupMember -Group $groupName -Member $fullUserName -ErrorAction Stop
        Write-Host "Usuário adicionado com sucesso."
    } catch {
        Write-Warning "AVISO: Falha ao adicionar usuário ao grupo '$groupName'. Erro: $($_.Exception.Message)"
    }
}

Write-Host "Processo de adição a grupos locais concluído."
exit 0
