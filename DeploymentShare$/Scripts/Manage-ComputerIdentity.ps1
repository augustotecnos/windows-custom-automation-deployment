[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('GetNewName', 'GetExistingNames', 'ResetADObject')]
    [string]$Mode,

    [string]$PerfilId,

    [string]$ComputerName,

    [string]$ServidorDhcp = "seu-servidor-dhcp.dominio.local"
)

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
            $computadores = Get-ADComputer -Filter "Name -like '$($prefixo)*'" | Select-Object -ExpandProperty Name | Sort-Object
            if ($computadores) { return ($computadores -join [Environment]::NewLine) }
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
            Get-ADComputer -Filter "Name -like '$($prefixo)*'" -ErrorAction SilentlyContinue | ForEach-Object {
                if ($_.Name -match '\d+') { $numerosUsados.Add([int]$matches[0]) }
            }
            Get-DhcpServerv4Reservation -ComputerName $ServidorDhcp -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "$($prefixo)*" } | ForEach-Object {
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
            try {
                if (Get-DhcpServerv4Reservation -ComputerName $ServidorDhcp -Name $ComputerName -ErrorAction SilentlyContinue) {
                    Remove-DhcpServerv4Reservation -ComputerName $ServidorDhcp -Name $ComputerName -Force
                    Write-Host "Reserva DHCP para '$ComputerName' removida com sucesso."
                }
            } catch {
                Write-Warning "AVISO: Não foi possível remover a reserva DHCP para '$ComputerName'. Erro: $($_.Exception.Message)"
            }
            if (Get-ADComputer -Identity $ComputerName -ErrorAction SilentlyContinue) {
                Remove-ADComputer -Identity $ComputerName -Confirm:$false
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
