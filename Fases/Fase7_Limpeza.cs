using INSTALADOR_SOFTWARE_SE.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using INSTALADOR_SOFTWARE_SE;

namespace INSTALADOR_SOFTWARE_SE.Fases
{
    /// <summary>
    /// Gerencia todas as operações da Fase 7: Limpeza final do sistema e preparação para entrega.
    /// </summary>
    public class Fase7_Limpeza
    {
        private readonly GerenciadorDeEstado _gerenciadorDeEstado;
        private readonly Action<string> _logCallback;
        private static readonly string CaminhoScriptFinalizacao =
            Path.Combine(INSTALADOR_SOFTWARE_SE.AppConfig.DeploymentSharePath, "Scripts", "Finalize-System.ps1");

        public Fase7_Limpeza(GerenciadorDeEstado gerenciadorDeEstado, Action<string> logCallback)
        {
            _gerenciadorDeEstado = gerenciadorDeEstado;
            _logCallback = logCallback;
        }

        /// <summary>
        /// Ponto de entrada principal que executa todas as ações da Fase 7.
        /// </summary>
        /// <returns>True se a fase foi concluída com sucesso, False se falhou.</returns>
        public bool Executar()
        {
            _logCallback("FASE 7: Limpeza e Finalização Iniciada.");
            try
            {
                // --- ETAPA 1: REMOVER O ESTADO DE AUTOMAÇÃO (AUTODESTRUIÇÃO) ---
                // Esta é a primeira ação para garantir que, mesmo que algo falhe daqui para frente,
                // o aplicativo não tentará se executar novamente no próximo boot.
                _logCallback("Removendo chaves de estado do registro para prevenir futuras execuções...");
                _gerenciadorDeEstado.RemoverChaveDeEstado();
                _logCallback("Chaves de estado da automação removidas com sucesso.");

                // --- ETAPA 2: EXECUTAR O SCRIPT DE FINALIZAÇÃO DO SISTEMA ---
                _logCallback("Executando script para reativar UAC e desabilitar conta de setup...");
                if (!File.Exists(CaminhoScriptFinalizacao))
                {
                    throw new FileNotFoundException("Script de finalização não encontrado no DeploymentShare.", CaminhoScriptFinalizacao);
                }
                
                int exitCode = ExecutarScriptFinalizacao();
                if (exitCode != 0)
                {
                    // Mesmo que o script falhe, o processo deve continuar para o reboot,
                    // mas logamos isso como um aviso crítico.
                    _logCallback($"AVISO CRÍTICO: O script de finalização falhou com o código de saída: {exitCode}. Verificação manual recomendada.");
                }
                else
                {
                    _logCallback("Script de finalização do sistema executado com sucesso.");
                }

                // --- ETAPA 3: MENSAGEM FINAL E REINICIALIZAÇÃO CONTROLADA ---
                _logCallback("------------------------------------------------------------------");
                _logCallback("PROCESSO DE PROVISIONAMENTO CONCLUÍDO COM SUCESSO!");
                _logCallback("A máquina está pronta para ser entregue ao usuário final.");
                _logCallback("------------------------------------------------------------------");
                
                // Aguarda 10 segundos para que o técnico possa ler a mensagem de sucesso na tela.
                Task.Delay(TimeSpan.FromSeconds(10)).Wait();

                _logCallback("A máquina será reiniciada em 30 segundos...");
                // Executa o comando de reinicialização final. Após isso, o usuário final poderá fazer logon.
                Process.Start("shutdown.exe", "/r /t 30 /c \"Limpeza final concluída. A máquina está pronta para o usuário final.\"");

                return true;
            }
            catch (Exception ex)
            {
                _logCallback($"ERRO FATAL NA FASE 7: {ex.Message}");
                // Se a fase final falhar, é crucial que o técnico seja informado.
                // O processo para aqui e uma intervenção manual será necessária.
                return false;
            }
        }

        /// <summary>
        /// Executa o script PowerShell de finalização e retorna seu código de saída.
        /// </summary>
        private int ExecutarScriptFinalizacao()
        {
            var startInfo = new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File \"{CaminhoScriptFinalizacao}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                return process.ExitCode;
            }
        }
    }
}