// =====================================================================================
// Fases/Fase6_ConfigsFinais.cs
// Configura impressoras, hosts, VPN, grupos locais
// =====================================================================================
using INSTALADOR_SOFTWARE_SE.Helpers;
using INSTALADOR_SOFTWARE_SE.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace INSTALADOR_SOFTWARE_SE.Fases
{
    public class Fase6_ConfigsFinais
    {
        private readonly GerenciadorDeEstado _estadoMgr;
        private readonly Dictionary<string,string> _estado;
        private readonly Action<string> _log;

        public Fase6_ConfigsFinais(GerenciadorDeEstado mgr,
                                   Dictionary<string,string> estado,
                                   Action<string> logCallback)
        {
            _estadoMgr = mgr;
            _estado    = estado;
            _log       = logCallback;
        }

        public bool Executar()
        {
            _log("───────────────────────────────────────────────────────────────");
            _log("FASE 6 ▸ Configurações Finais (impressoras, hosts, VPN)");

            var perfil = CarregarPerfil();
            if (perfil == null)
            {
                _log("Aviso: perfil não encontrado – pulando Fase 6.");
                _estado["EtapaAtual"] = "Iniciar_Limpeza";
                _estadoMgr.SalvarEstadoCompleto(_estado);
                return true;
            }

            // 1) Impressoras ------------------------------------------------------------
            if (perfil.Printers.Count > 0)
            {
                var addPrinter = Path.Combine(AppConfig.DeploymentSharePath, "Scripts", "Add-NetworkPrinters.ps1");
                var args = string.Join(",", perfil.Printers);
                Process.Start("powershell", $"-ExecutionPolicy Bypass -File \"{addPrinter}\" -PrinterPaths {args}")?.WaitForExit();
            }

            // 2) Hosts ------------------------------------------------------------------
            if (perfil.HostsFile is not null)
            {
                var setHosts = Path.Combine(AppConfig.DeploymentSharePath, "Scripts", "Set-HostsFile.ps1");
                Process.Start("powershell", $"-ExecutionPolicy Bypass -File \"{setHosts}\" -HostsFileName \"{perfil.HostsFile}\"")?.WaitForExit();
            }

            // 3) VPN --------------------------------------------------------------------
            if (perfil.VpnConfig is not null)
            {
                var vpnScript = Path.Combine(AppConfig.DeploymentSharePath, "Scripts", "Install-VPN.ps1");
                Process.Start("powershell",
                    $"-ExecutionPolicy Bypass -File \"{vpnScript}\" -InstallerPath \"{perfil.VpnConfig.InstallerPath}\" -ConfigFilePath \"{perfil.VpnConfig.ConfigFilePath}\"")?.WaitForExit();
            }

            // 4) Grupos locais ----------------------------------------------------------
            if (perfil.LocalGroups.Count > 0)
            {
                var addGroup = Path.Combine(AppConfig.DeploymentSharePath, "Scripts", "Add-UserToLocalGroups.ps1");
                var user     = _estado.GetValueOrDefault("UsuarioFinal") ?? "SEPROMOTORA";
                var groups   = string.Join(",", perfil.LocalGroups);
                Process.Start("powershell",
                    $"-ExecutionPolicy Bypass -File \"{addGroup}\" -UserName \"{user}\" -GroupNames {groups}")?.WaitForExit();
            }

            // Agenda próxima fase
            _estado["EtapaAtual"] = "Iniciar_Limpeza";
            _estadoMgr.SalvarEstadoCompleto(_estado);
            return true;
        }

        // ---------------------------------------------------------------------

        private PerfilSoftware? CarregarPerfil()
        {
            var unidade = _estado.GetValueOrDefault("PerfilId");
            var setor   = _estado.GetValueOrDefault("SetorId");
            if (string.IsNullOrWhiteSpace(unidade) || string.IsNullOrWhiteSpace(setor))
                return null;

            var file = Path.Combine(AppConfig.DeploymentSharePath,
                                    "Config", $"{unidade}_{setor}.json").ToLower();
            if (!File.Exists(file)) return null;

            try
            {
                return JsonSerializer.Deserialize<PerfilSoftware>(File.ReadAllText(file));
            }
            catch (Exception ex)
            {
                _log($"Erro JSON perfil: {ex.Message}");
                return null;
            }
        }
    }
}
