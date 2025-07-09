[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string[]]$PrinterPaths
)

Write-Host "Iniciando processo de adição de impressoras de rede..."

if (-not $PrinterPaths) {
    Write-Host "Nenhum caminho de impressora foi fornecido. Encerrando o script."
    exit 0
}

foreach ($path in $PrinterPaths) {
    try {
        if (-not ($path.StartsWith("\\"))) { throw "O caminho '$path' não parece ser um caminho de rede UNC válido (deve começar com \\)." }
        Write-Host "Tentando conectar e instalar a impressora em: $path"
        Add-Printer -ConnectionName $path -ErrorAction Stop
        Write-Host "Impressora '$path' adicionada com sucesso."
    } catch {
        Write-Warning "AVISO: Falha ao adicionar a impressora '$path'. Erro: $($_.Exception.Message)"
    }
}

Write-Host "Processo de adição de impressoras concluído. Verifique os avisos acima para qualquer falha."
exit 0
