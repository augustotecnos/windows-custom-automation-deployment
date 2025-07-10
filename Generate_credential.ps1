<#
.SYNOPSIS
    Cria um arquivo de credencial de usuário criptografado para uso em scripts de automação.
.DESCRIPTION
    Este script solicita um nome de usuário e senha e os salva em um arquivo XML
    criptografado usando a API de Proteção de Dados do Windows (DPAPI).
    Execute-o uma vez para gerar o arquivo que será usado pela automação.
#>

# --- CONFIGURAÇÃO ---
# Defina o caminho completo onde o arquivo de credencial seguro será salvo.
# Recomenda-se colocá-lo em uma pasta segura dentro do seu DeploymentShare.
$CaminhoDoArquivo = "C:\temp\secure_credential.xml"

# --- EXECUÇÃO ---
try {
    # Solicita as credenciais de uma conta que tenha permissão para consultar o AD e gerenciar o DHCP.
    # Exemplo de usuário: SEPROMOTORA\svc_automacao
    $Credencial = Get-Credential -Message "Digite as credenciais da conta de serviço para automação"

    if ($Credencial) {
        # Exporta o objeto de credencial para um arquivo XML, criptografando a parte da senha.
        $Credencial | Export-CliXml -Path $CaminhoDoArquivo
        Write-Host "Arquivo de credencial seguro foi criado com sucesso em:" -ForegroundColor Green
        Write-Host $CaminhoDoArquivo -ForegroundColor Yellow
    } else {
        Write-Warning "Operação cancelada. Nenhum arquivo foi criado."
    }
}
catch {
    Write-Error "Ocorreu um erro ao criar o arquivo de credencial: $_"
}
