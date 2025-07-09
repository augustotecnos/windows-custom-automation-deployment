[CmdletBinding()]
param()

Write-Host "Iniciando verificação de status de drivers no Gerenciador de Dispositivos..."
try {
    $dispositivosComProblema = Get-PnpDevice | Where-Object { $_.Status -ne 'OK' }
    if ($null -eq $dispositivosComProblema) {
        Write-Host "SUCESSO: Verificação concluída. Nenhum dispositivo com problema foi encontrado."
        Write-Host "Todos os drivers parecem estar instalados e funcionando corretamente."
        exit 0
    } else {
        Write-Warning "AVISO: Um ou mais dispositivos com problemas foram encontrados."
        $dispositivosComProblema | Format-Table -Property Name, Status, Problem, ProblemDescription -AutoSize
        exit 1
    }
} catch {
    $errorMessage = "ERRO FATAL ao tentar verificar os drivers: $($_.Exception.Message)"
    Write-Error $errorMessage
    exit 99
}
