[CmdletBinding()]
param()

try {
    Write-Host "Iniciando a reativação do Controle de Conta de Usuário (UAC) para os padrões de segurança..."
    $regPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"
    Set-ItemProperty -Path $regPath -Name "EnableLUA" -Value 1 -Force
    Set-ItemProperty -Path $regPath -Name "ConsentPromptBehaviorAdmin" -Value 5 -Force
    Set-ItemProperty -Path $regPath -Name "PromptOnSecureDesktop" -Value 1 -Force
    Write-Host "UAC reativado com sucesso."
    $setupUser = "SEPROMOTORA"
    Write-Host "Desabilitando a conta de provisionamento local '$setupUser' por segurança..."
    $localUser = Get-LocalUser -Name $setupUser -ErrorAction SilentlyContinue
    if ($localUser) {
        if ($localUser.Enabled) { Disable-LocalUser -Name $setupUser -ErrorAction Stop; Write-Host "Conta '$setupUser' desabilitada com sucesso." }
        else { Write-Host "Conta '$setupUser' já estava desabilitada." }
    } else {
        Write-Warning "AVISO: A conta de provisionamento '$setupUser' não foi encontrada. Nenhuma ação foi tomada."
    }
    Write-Host "Script de finalização do sistema concluído com sucesso."
    exit 0
} catch {
    $errorMessage = "ERRO FATAL durante a finalização do sistema: $($_.Exception.Message)"
    Write-Error $errorMessage
    exit 1
}
