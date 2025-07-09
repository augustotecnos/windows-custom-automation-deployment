using INSTALADOR_SOFTWARE_SE.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using INSTALADOR_SOFTWARE_SE;

namespace INSTALADOR_SOFTWARE_SE.Fases
{
    /// <summary>
    /// Gerencia todas as operações da Fase 4: Atualização Massiva do SO e Drivers.
    /// Implementa um loop de reinicialização controlado baseado nos códigos de saída do script PowerShell.
    /// </summary>
    public class Fase4_Update
    {
        private readonly GerenciadorDeEstado _gerenciadorDeEstado;
        private readonly Dictionary<string, string> _estadoAtual;
        private readonly Action<string> _logCallback;
        private static readonly string CaminhoScriptUpdate =
            Path.Combine(AppConfig.DeploymentSharePath, "Scripts", "Invoke-WindowsUpdate.ps1");

        public Fase4_Update(GerenciadorDeEstado gerenciadorDeEstado, Dictionary<string, string> estadoAtual, Action<string> logCallback)
        {
            _gerenciadorDeEstado = gerenciadorDeEstado;
            _estadoAtual = estadoAtual;
            _logCallback = logCallback;
        }

        /// <summary>
        /// Ponto de entrada principal que executa todas as ações da Fase 4.
        /// </summary>
        /// <returns>True se a fase foi concluída ou se uma reinicialização foi iniciada, False se falhou.</returns>
        public bool Executar()
        {
            _logCallback("FASE 4: Atualização Massiva de SO e Drivers Iniciada.");

            // --- ETAPA 1: VERIFICAR SE A FASE DEVE SER EXECUTADA ---
            // Lê a decisão do técnico salva na Fase 1.
            if (_estadoAtual["ExecutarUpdates"].Equals("False", StringComparison.OrdinalIgnoreCase))
            {
                _logCallback("Opção de não executar updates selecionada pelo técnico. Pulando esta fase.");
                
                // Prepara o estado para a próxima fase.
                _estadoAtual["EtapaAtual"] = "Iniciar_InstalacaoSoftware";
                _gerenciadorDeEstado.SalvarEstadoCompleto(_estadoAtual);
                
                // Retorna sucesso, pois a ação de pular foi a correta.
                return true;
            }

            try
            {
                // --- ETAPA 2: VERIFICAR SCRIPT E EXECUTAR O CICLO DE UPDATE ---
                _logCallback($"Verificando a existência do script em {CaminhoScriptUpdate}...");
                if (!File.Exists(CaminhoScriptUpdate))
                {
                    throw new FileNotFoundException("O script de Windows Update não foi encontrado no DeploymentShare.", CaminhoScriptUpdate);
                }
                _logCallback("Script encontrado. Invocando ciclo de verificação de atualizações...");

                int exitCode = ExecutarScriptDeUpdate();
                _logCallback($"Ciclo de atualização concluído com o código de saída: {exitCode}");

                // --- ETAPA 3: ANALISAR O RESULTADO E DECIDIR A PRÓXIMA AÇÃO ---
                switch (exitCode)
                {
                    // CASO 1: SUCESSO, SEM NECESSIDADE DE REBOOT.
                    // O script rodou e não encontrou mais nada para instalar ou o que instalou não exige reboot.
                    case 0:
                        _logCallback("SUCESSO: O sistema está totalmente atualizado. Concluindo a Fase 4.");
                        // Define o estado para a próxima fase.
                        _estadoAtual["EtapaAtual"] = "Iniciar_InstalacaoSoftware";
                        _gerenciadorDeEstado.SalvarEstadoCompleto(_estadoAtual);
                        // Não configura RunOnce pois não haverá reboot. O worker continuará para o próximo 'case'.
                        return true;

                    // CASO 2: SUCESSO, MAS UMA REINICIALIZAÇÃO É NECESSÁRIA.
                    // O script instalou atualizações que exigem um reboot para serem finalizadas.
                    case 3010:
                        _logCallback("REINICIALIZAÇÃO NECESSÁRIA. Preparando para reiniciar e continuar o ciclo de updates...");
                        
                        // Mantém a etapa atual como "Iniciar_WindowsUpdate".
                        // Assim, após o reboot, o app entrará nesta mesma fase para verificar se há mais atualizações.
                        _estadoAtual["EtapaAtual"] = "Iniciar_WindowsUpdate"; 
                        _gerenciadorDeEstado.SalvarEstadoCompleto(_estadoAtual);
                        _gerenciadorDeEstado.ConfigurarRunOnce(); // Configura o "despertador".

                        _logCallback("Configurações salvas. A máquina será reiniciada em 20 segundos...");
                        Process.Start("shutdown.exe", "/r /t 20 /c \"Atualizações instaladas. Reiniciando para continuar a verificação.\"");
                        return true;

                    // CASO 3: FALHA.
                    // O script retornou um código de erro inesperado.
                    default:
                        throw new Exception($"O script de atualização falhou com um código de erro desconhecido: {exitCode}.");
                }
            }
            catch (Exception ex)
            {
                _logCallback($"ERRO FATAL NA FASE 4: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executa o script PowerShell de atualização e retorna seu código de saída.
        /// </summary>
        private int ExecutarScriptDeUpdate()
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{CaminhoScriptUpdate}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true, // Captura a saída para log
                RedirectStandardError = true   // Captura erros para log
            };

            using (var process = Process.Start(startInfo))
            {
                // Loga a saída do script em tempo real para um melhor diagnóstico
                process.OutputDataReceived += (sender, args) => { if (args.Data != null) _logCallback($"  [PS-LOG]: {args.Data}"); };
                process.ErrorDataReceived += (sender, args) => { if (args.Data != null) _logCallback($"  [PS-ERR]: {args.Data}"); };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
                return process.ExitCode;
            }
        }
    }
}