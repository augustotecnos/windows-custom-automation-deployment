using INSTALADOR_SOFTWARE_SE.Helpers;
using INSTALADOR_SOFTWARE_SE.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using INSTALADOR_SOFTWARE_SE;

namespace INSTALADOR_SOFTWARE_SE.Fases
{
    /// <summary>
    /// Gerencia todas as operações da Fase 6: Aplicação de configurações finais
    /// e personalizações de ambiente.
    /// </summary>
    public class Fase6_ConfigsFinais
    {
        private readonly GerenciadorDeEstado _gerenciadorDeEstado;
        private readonly Dictionary<string, string> _estadoAtual;
        private readonly Action<string> _logCallback;
        private static readonly string CaminhoScripts =
            Path.Combine(INSTALADOR_SOFTWARE_SE.AppConfig.DeploymentSharePath, "Scripts");

        public Fase6_ConfigsFinais(GerenciadorDeEstado gerenciadorDeEstado, Dictionary<string, string> estadoAtual, Action<string> logCallback)
        {
            _gerenciadorDeEstado = gerenciadorDeEstado;
            _estadoAtual = estadoAtual;
            _logCallback = logCallback;
        }

        /// <summary>
        /// Ponto de entrada principal que executa todas as ações da Fase 6.
        /// </summary>
        /// <returns>True se a fase foi concluída com sucesso, False se falhou.</returns>
        public bool Executar()
        {
            _logCallback("FASE 6: Configurações Finais e Personalização Iniciada.");
            try
            {
                SoftwareProfile perfil = CarregarPerfilDeSoftware();
                if (perfil == null)
                {
                    _logCallback("AVISO: Não foi possível carregar o perfil de software. Pulando todas as configurações finais.");
                }
                else
                {
                    // Executa cada sub-tarefa de configuração em sequência.
                    ConfigurarImpressoras(perfil.Printers);
                    ConfigurarArquivoHosts(perfil.HostsFile);
                    InstalarVPN(perfil.VpnConfig);
                    AdicionarUsuarioAGruposLocais(perfil.LocalGroups);
                }

                _logCallback("SUCESSO: Fase 6 concluída.");
                _estadoAtual["EtapaAtual"] = "Iniciar_Limpeza"; // Prepara para a fase final.
                _gerenciadorDeEstado.SalvarEstadoCompleto(_estadoAtual);
                return true;
            }
            catch (Exception ex)
            {
                _logCallback($"ERRO FATAL NA FASE 6: {ex.Message}");
                return false;
            }
        }

        private SoftwareProfile CarregarPerfilDeSoftware()
        {
            string perfilId = _estadoAtual["PerfilId"];
            string setorId = _estadoAtual["SetorId"];
            string nomeArquivoPerfil = $"{perfilId}_{setorId}.json";
            string caminhoCompletoPerfil = Path.Combine(INSTALADOR_SOFTWARE_SE.AppConfig.DeploymentSharePath, "Config", nomeArquivoPerfil);
            
            _logCallback($"Carregando perfil de software de: {caminhoCompletoPerfil} para configurações finais.");
            if (!File.Exists(caminhoCompletoPerfil)) return null;

            string jsonContent = File.ReadAllText(caminhoCompletoPerfil);
            return JsonSerializer.Deserialize<SoftwareProfile>(jsonContent);
        }

        private void ConfigurarImpressoras(List<string> printers)
        {
            if (printers == null || !printers.Any())
            {
                _logCallback("Nenhuma impressora definida no perfil. Pulando.");
                return;
            }
            _logCallback("--- Configurando Impressoras de Rede ---");
            string scriptPath = Path.Combine(CaminhoScripts, "Add-NetworkPrinters.ps1");
            // Converte a lista C# em uma string formatada para o array PowerShell: "'path1','path2','path3'"
            string printerArgs = string.Join(",", printers.Select(p => $"'{p}'"));
            ExecutarScriptPowerShell(scriptPath, $"-PrinterPaths @({printerArgs})");
        }

        private void ConfigurarArquivoHosts(string hostsFileName)
        {
            if (string.IsNullOrWhiteSpace(hostsFileName))
            {
                _logCallback("Nenhum arquivo hosts customizado definido no perfil. Pulando.");
                return;
            }
            _logCallback("--- Configurando Arquivo Hosts ---");
            string scriptPath = Path.Combine(CaminhoScripts, "Set-HostsFile.ps1");
            ExecutarScriptPowerShell(scriptPath, $"-HostsFileName '{hostsFileName}'");
        }

        private void InstalarVPN(VpnConfig vpnConfig)
        {
            if (vpnConfig == null || string.IsNullOrWhiteSpace(vpnConfig.InstallerPath))
            {
                _logCallback("Nenhuma configuração de VPN definida no perfil. Pulando.");
                return;
            }
            _logCallback("--- Configurando VPN ---");
            string scriptPath = Path.Combine(CaminhoScripts, "Install-VPN.ps1");
            string arguments = $"-InstallerPath \"{vpnConfig.InstallerPath}\" -ConfigFilePath \"{vpnConfig.ConfigFilePath}\"";
            ExecutarScriptPowerShell(scriptPath, arguments);
        }

        private void AdicionarUsuarioAGruposLocais(List<string> groupNames)
        {
            if (groupNames == null || !groupNames.Any())
            {
                _logCallback("Nenhuma adição a grupos locais definida no perfil. Pulando.");
                return;
            }
            _logCallback("--- Adicionando Usuário a Grupos Locais ---");
            string userName = _estadoAtual["UsuarioFinal"];
            string scriptPath = Path.Combine(CaminhoScripts, "Add-UserToLocalGroups.ps1");
            string groupArgs = string.Join(",", groupNames.Select(g => $"'{g}'"));
            ExecutarScriptPowerShell(scriptPath, $"-UserName '{userName}' -GroupNames @({groupArgs})");
        }

        /// <summary>
        /// Método helper para executar um script PowerShell e aguardar sua conclusão.
        /// </summary>
        private int ExecutarScriptPowerShell(string scriptPath, string arguments)
        {
            _logCallback($"Executando script: {Path.GetFileName(scriptPath)} com argumentos: {arguments}");
            var startInfo = new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File \"{scriptPath}\" {arguments}")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    _logCallback($"AVISO: O script '{Path.GetFileName(scriptPath)}' terminou com o código de saída: {process.ExitCode}");
                }
                return process.ExitCode;
            }
        }
    }
}