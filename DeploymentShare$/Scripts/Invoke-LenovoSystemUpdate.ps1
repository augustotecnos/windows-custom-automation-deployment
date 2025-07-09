[CmdletBinding()]
param()

try {
    Write-Host "Iniciando a execução do Lenovo System Update como fallback para instalação de drivers..."
    $caminhoSystemUpdate = "C:\Program Files (x86)\Lenovo\System Update\tvsu.exe"
    Write-Host "Verificando se a ferramenta existe em: $caminhoSystemUpdate"
    if (-not (Test-Path $caminhoSystemUpdate)) { throw "ERRO CRÍTICO: Lenovo System Update não encontrado em '$caminhoSystemUpdate'. Não é possível continuar com a instalação de drivers específicos." }
    Write-Host "Ferramenta encontrada."
    $argumentos = "/CM -search A -action INSTALL -includerebootpackages 1,3,4,5 -noicon -noreboot -nolicense"
    Write-Host "Executando o comando com os seguintes argumentos silenciosos: $argumentos"
    $processo = Start-Process -FilePath $caminhoSystemUpdate -ArgumentList $argumentos -Wait -PassThru
    Write-Host "Execução do Lenovo System Update concluída."
    Write-Host "O processo terminou com o código de saída: $($processo.ExitCode)."
    exit $processo.ExitCode
} catch {
    Write-Error "ERRO FATAL ao executar o Lenovo System Update: $($_.Exception.Message)"
    exit 1
}
