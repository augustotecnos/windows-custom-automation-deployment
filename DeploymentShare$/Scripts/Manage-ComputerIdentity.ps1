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
    [string]$ServidorDhcp = "seu-servidor-dhcp.dominio.local"
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
            $computadores = Get-ADComputer -Filter "Name -like '$($prefixo)*'" -Credential $credential | Select-Object -ExpandProperty Name | Sort-Object
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
                Get-DhcpServerv4Reservation -ScopeId 10.113.11.0  | Where-Object { $_.Name -like $prefixoLike }
            }
            $reservasDhcp = Invoke-Command -ComputerName $ServidorDhcp -Credential $credential -ScriptBlock $sbDhcp -ArgumentList "$($prefixo)*"

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
                if (Get-DhcpServerv4Reservation -Name $NomeComputador -ScopeId 10.113.11.0  -ErrorAction SilentlyContinue) {
                    Remove-DhcpServerv4Reservation -Name $NomeComputador -ScopeId 10.113.11.0  -Force
                    Write-Host "Reserva DHCP para '$NomeComputador' removida com sucesso (executado remotamente)."
                }
            }
            Invoke-Command -ComputerName $ServidorDhcp -Credential $credential -ScriptBlock $sbRemoveDhcp -ArgumentList $ComputerName

            # USA A CREDENCIAL CARREGADA
            if (Get-ADComputer -Identity $ComputerName -Credential $credential -ErrorAction SilentlyContinue) {
                Remove-ADComputer -Identity $ComputerName -Credential $credential -Confirm:$false
                Write-Host "Objeto do computador '$ComputerName' removido do Active Directory com sucesso."
            } else {
                Write-Warning "AVISO: O objeto do computador '$ComputerName' não foi encontrado no AD."
                return ""
            }
            return "Success"
        }
    }
}
catch {
    Write-Error "ERRO FATAL no script Manage-ComputerIdentity.ps1: $($_.Exception.Message)"
    exit 1
}
