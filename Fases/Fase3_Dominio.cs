using INSTALADOR_SOFTWARE_SE.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace INSTALADOR_SOFTWARE_SE.Fases
{
    /// <summary>
    /// Gerencia todas as operações da Fase 3: Ingresso no Domínio.
    /// Decide se a ação é necessária e orquestra a chamada ao script PowerShell seguro.
    /// </summary>
    public class Fase3_Dominio
    {
        private readonly GerenciadorDeEstado _gerenciadorDeEstado;
        private readonly Dictionary<string, string> _estadoAtual;
        private readonly Action<string> _logCallback;
        private const string CaminhoScriptDominio = @"\\seu-servidor\DeploymentShare$\Scripts\Invoke-SecureDomainJoin.ps1";

        public Fase3_Dominio(GerenciadorDeEstado gerenciadorDeEstado, Dictionary<string, string> estadoAtual, Action<string> logCallback)
        {
            _gerenciadorDeEstado = gerenciadorDeEstado;
            _estadoAtual = estadoAtual;
            _logCallback = logCallback;
        }

        /// <summary>
        /// Ponto de entrada principal que executa todas as ações da Fase 3.
        /// </summary>
        /// <returns>True se a fase foi concluída com sucesso (ou pulada corretamente), False se falhou.</returns>
        public bool Executar()
        {
            _logCallback("FASE 3: Integração ao Ambiente (Ingresso no Domínio) Iniciada.");

            // --- ETAPA 1: DECIDIR SE A FASE DEVE SER EXECUTADA ---
            // Verifica o PerfilId salvo no registro. Apenas o perfil "matriz" requer ingresso no domínio.
            if (_estadoAtual["PerfilId"] != "matriz")
            {
                _logCallback($"Perfil '{_estadoAtual["PerfilId"]}' selecionado. Pulando ingresso no domínio.");
                
                // Prepara para a próxima fase imediatamente, sem reiniciar.
                _estadoAtual["EtapaAtual"] = "Iniciar_WindowsUpdate";
                _gerenciadorDeEstado.SalvarEstadoCompleto(_estadoAtual);
                
                // Retorna sucesso, pois pular a fase foi a ação correta e intencional.
                return true;
            }

            try
            {
                // --- ETAPA 2: VERIFICAR SE O SCRIPT DE SUPORTE EXISTE ---
                _logCallback($"Verificando a existência do script em {CaminhoScriptDominio}...");
                if (!File.Exists(CaminhoScriptDominio))
                {
                    // Lança uma exceção se o script não for encontrado, pois a operação não pode continuar.
                    throw new FileNotFoundException("O script de ingresso no domínio não foi encontrado no DeploymentShare.", CaminhoScriptDominio);
                }
                _logCallback("Script encontrado.");

                // --- ETAPA 3: EXECUTAR O SCRIPT POWERSHELL DE FORMA SEGURA ---
                _logCallback("Invocando script de ingresso no domínio. Aguardando credenciais do técnico...");
                
                // Chama o método que executa o script e retorna o código de saída.
                int exitCode = ExecutarScriptDeDominio();

                // O script foi projetado para retornar 0 em caso de sucesso.
                if (exitCode != 0)
                {
                    // Se o código de saída for diferente de 0, o script falhou ou foi cancelado.
                    throw new Exception($"O script de ingresso no domínio falhou ou foi cancelado. Código de saída: {exitCode}.");
                }

                _logCallback("Script de ingresso no domínio executado com sucesso.");

                // --- ETAPA 4: PREPARAR PARA A PRÓXIMA FASE E REINICIAR ---
                _logCallback("Preparando para a próxima fase (Windows Update) e reinicialização...");

                // Atualiza o estado para que, no próximo boot, o app saiba que deve iniciar a Fase 4.
                _estadoAtual["EtapaAtual"] = "Iniciar_WindowsUpdate";
                _gerenciadorDeEstado.SalvarEstadoCompleto(_estadoAtual);
                _gerenciadorDeEstado.ConfigurarRunOnce();

                _logCallback("Configurações salvas. A máquina será reiniciada em 20 segundos...");
                Process.Start("shutdown.exe", "/r /t 20 /c \"Ingresso no domínio concluído. Reiniciando para a Fase 4.\"");
                
                return true; // Fase concluída com sucesso.
            }
            catch (Exception ex)
            {
                _logCallback($"ERRO FATAL NA FASE 3: {ex.Message}");
                // Em caso de erro, o processo para aqui para que o técnico possa investigar.
                return false;
            }
        }

        /// <summary>
        /// Constrói e executa o processo PowerShell para ingressar no domínio, aguardando sua conclusão.
        /// </summary>
        /// <returns>O código de saída do processo PowerShell.</returns>
        private int ExecutarScriptDeDominio()
        {
            // Recupera os dados necessários do estado salvo para passar como parâmetros para o script.
            string nomeComputador = _estadoAtual["NomeComputadorDefinido"];
            string perfilId = _estadoAtual["PerfilId"];
            string setorId = _estadoAtual["SetorId"];

            // Constrói a string de argumentos de forma segura, com aspas para tratar caminhos com espaços.
            string arguments = $"-ExecutionPolicy Bypass -File \"{CaminhoScriptDominio}\" " +
                               $"-ComputerNameToJoin \"{nomeComputador}\" " +
                               $"-PerfilId \"{perfilId}\" " +
                               $"-SetorId \"{setorId}\"";

            var startInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = arguments,
                // É FUNDAMENTAL que UseShellExecute seja 'true' e o Verbo seja 'runas'.
                // Isso garante que o PowerShell seja executado em sua própria janela e
                // com os privilégios corretos para que o prompt de credenciais (Get-Credential)
                // possa ser exibido para o técnico.
                UseShellExecute = true,
                Verb = "runas"
            };

            using (var process = Process.Start(startInfo))
            {
                // Pausa a execução do C# e aguarda o processo PowerShell (e a interação do técnico) terminar.
                process.WaitForExit();
                // Retorna o código de saída para análise.
                return process.ExitCode;
            }
        }
    }
}