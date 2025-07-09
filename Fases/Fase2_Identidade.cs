using INSTALADOR_SOFTWARE_SE.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using INSTALADOR_SOFTWARE_SE;

namespace INSTALADOR_SOFTWARE_SE.Fases
{
    /// <summary>
    /// Gerencia todas as operações da Fase 2: Definição da Identidade da Máquina.
    /// Esta classe é responsável por nomear a máquina, seja gerando um novo nome
    /// ou reutilizando um existente após resetar seu objeto no AD.
    /// </summary>
    public class Fase2_Identidade
    {
        private readonly GerenciadorDeEstado _gerenciadorDeEstado;
        private readonly Dictionary<string, string> _estadoAtual;
        private readonly Action<string> _logCallback;
        private static readonly string CaminhoScriptIdentidade =
            Path.Combine(AppConfig.DeploymentSharePath, "Scripts", "Manage-ComputerIdentity.ps1");

        /// <summary>
        /// Inicializa uma nova instância da classe que orquestra a Fase 2.
        /// </summary>
        /// <param name="gerenciadorDeEstado">A instância do helper que interage com o registro.</param>
        /// <param name="estadoAtual">O dicionário contendo as escolhas do técnico salvas na Fase 1.</param>
        /// <param name="logCallback">Uma Action para enviar mensagens de log de volta para a UI.</param>
        public Fase2_Identidade(GerenciadorDeEstado gerenciadorDeEstado, Dictionary<string, string> estadoAtual, Action<string> logCallback)
        {
            _gerenciadorDeEstado = gerenciadorDeEstado;
            _estadoAtual = estadoAtual;
            _logCallback = logCallback;
        }

        /// <summary>
        /// Ponto de entrada principal que executa todas as ações da Fase 2.
        /// </summary>
        /// <returns>True se a fase foi concluída com sucesso e um reboot foi iniciado, False se falhou.</returns>
        public bool Executar()
        {
            _logCallback("FASE 2: Definição de Identidade Iniciada.");
            string nomeFinalDaMaquina = null;

            try
            {
                // Lê a decisão do técnico salva na Fase 1.
                // É crucial que a UI da Fase 1 salve este valor no estado.
                string modoNomenclatura = _estadoAtual["ModoNomenclatura"];

                // --- BIFURCAÇÃO DA LÓGICA: MÁQUINA NOVA VS. REFORMATAÇÃO ---
                if (modoNomenclatura == "ManterExistente")
                {
                    string nomeParaManter = _estadoAtual["NomeComputadorSelecionado"];
                    _logCallback($"Modo de reformatação selecionado para a máquina: {nomeParaManter}.");

                    _logCallback($"Resetando objeto '{nomeParaManter}' no AD e DHCP para evitar conflitos de confiança...");

                    string resetResult = ExecutarScriptIdentidade(
                        "-Mode ResetADObject",
                        $"-ComputerName \"{nomeParaManter}\"",
                        true);

                    if (!string.Equals(resetResult, "Success", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception("Falha ao resetar o objeto do computador no ambiente.");
                    }

                    _logCallback($"Objeto '{nomeParaManter}' resetado com sucesso.");
                    
                    // O nome final é o mesmo que foi selecionado para manter.
                    nomeFinalDaMaquina = nomeParaManter;
                }
                else // modoNomenclatura == "Novo"
                {
                    _logCallback("Modo de nova máquina selecionado. Gerando novo nome...");
                    string perfilId = _estadoAtual["PerfilId"];
                    
                    // Chama o script para obter o próximo nome disponível.
                    nomeFinalDaMaquina = ExecutarScriptIdentidade("-Mode GetNewName", $"-PerfilId \"{perfilId}\"", true);

                    if (string.IsNullOrEmpty(nomeFinalDaMaquina))
                    {
                        throw new Exception("Script não retornou um nome de máquina válido.");
                    }
                }

                // --- CAMINHO COMUM: APLICAÇÃO DO NOME E REBOOT ---
                _logCallback($"Nome final definido como: {nomeFinalDaMaquina}. Aplicando localmente...");
                
                if (!RenomearMaquinaLocal(nomeFinalDaMaquina))
                {
                    throw new Exception("Falha ao executar o comando para renomear a máquina.");
                }

                _logCallback($"Máquina renomeada com sucesso para '{nomeFinalDaMaquina}'.");
                _logCallback("Preparando para a próxima fase e reinicialização...");

                // Persiste o estado final da fase para o próximo reboot
                _estadoAtual["EtapaAtual"] = "PósRename_IngressarDominio"; // Define o próximo passo
                _estadoAtual["NomeComputadorDefinido"] = nomeFinalDaMaquina; // Salva o nome que foi efetivamente usado
                _gerenciadorDeEstado.SalvarEstadoCompleto(_estadoAtual);
                _gerenciadorDeEstado.ConfigurarRunOnce();

                _logCallback("Configurações salvas. A máquina será reiniciada em 20 segundos...");
                Process.Start("shutdown.exe", "/r /t 20 /c \"Configuração de identidade concluída. Reiniciando para a Fase 3.\"");
                
                return true;
            }
            catch (Exception ex)
            {
                _logCallback($"ERRO FATAL NA FASE 2: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Método genérico para executar o script de identidade com diferentes modos e argumentos.
        /// </summary>
        /// <param name="mode">O modo de operação do script (ex: -Mode GetNewName).</param>
        /// <param name="args">Os argumentos adicionais para o modo (ex: -PerfilId "matriz").</param>
        /// <param name="capturaSaida">Se true, o método captura e retorna a saída padrão do script.</param>
        /// <returns>A saída do script se 'capturaSaida' for true, caso contrário null.</returns>
        private string ExecutarScriptIdentidade(string mode, string args, bool capturaSaida = false)
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{CaminhoScriptIdentidade}\" {mode} {args}",
                UseShellExecute = false,
                RedirectStandardOutput = capturaSaida,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                string saida = null;
                if (capturaSaida)
                {
                    // Lê a saída do script, que deve ser o nome do computador.
                    saida = process.StandardOutput.ReadToEnd().Trim();
                }

                string erro = process.StandardError.ReadToEnd().Trim();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Script PowerShell '{CaminhoScriptIdentidade}' falhou com o código {process.ExitCode}. Erro: {erro}");
                }
                
                // Retorna a saída capturada (pode ser o nome do computador ou null).
                return saida;
            }
        }

        /// <summary>
        /// Executa o comando PowerShell local para renomear a máquina.
        /// </summary>
        /// <param name="novoNome">O novo nome a ser aplicado.</param>
        /// <returns>True se o comando foi executado com sucesso (exit code 0).</returns>
        private bool RenomearMaquinaLocal(string novoNome)
        {
             var startInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"Rename-Computer -NewName '{novoNome}' -Force\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                return process.ExitCode == 0;
            }
        }
    }
}