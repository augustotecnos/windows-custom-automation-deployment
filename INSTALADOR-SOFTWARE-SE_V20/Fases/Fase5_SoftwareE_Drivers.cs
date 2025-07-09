// =====================================================================================
// Fases/Fase5_SoftwareE_Drivers.cs
// Instala software (Winget/Legados) e drivers (placeholder)
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
    public class Fase5_SoftwareE_Drivers
    {
        private readonly GerenciadorDeEstado _estadoMgr;
        private readonly Dictionary<string,string> _estado;
        private readonly Action<string> _log;

        public Fase5_SoftwareE_Drivers(GerenciadorDeEstado estadoMgr,
                                       Dictionary<string,string> estado,
                                       Action<string> logCallback)
        {
            _estadoMgr = estadoMgr;
            _estado    = estado;
            _log       = logCallback;
        }

        public bool Executar()
        {
            _log("───────────────────────────────────────────────────────────────");
            _log("FASE 5 ▸ Instalação de Software e Drivers");

            var perfil = CarregarPerfil();
            if (perfil == null)
            {
                _log("Aviso: perfil não encontrado – pulando Fase 5.");
                _estado["EtapaAtual"] = "Iniciar_ConfigsFinais";
                _estadoMgr.SalvarEstadoCompleto(_estado);
                return true;
            }

            // 1) Winget -----------------------------------------------------------------
            foreach (var pkg in perfil.WingetPackages)
            {
                _log($"[Winget] Instalando: {pkg}");
                var p = Process.Start("winget", $"install --id {pkg} -e --accept-package-agreements --accept-source-agreements");
                p?.WaitForExit();
                if (p?.ExitCode != 0) _log($"  ⚠️ Winget retorno {p?.ExitCode}");
            }

            // 2) Instaladores legados ----------------------------------------------------
            foreach (var inst in perfil.LegacyInstallers)
            {
                var path = inst.Path!.Replace("%share%", AppConfig.DeploymentSharePath);
                _log($"[Legacy] {inst.Nome} → {path}");
                if (!File.Exists(path))
                {
                    _log($"  ⚠️ Arquivo inexistente, pulando.");
                    continue;
                }

                Process? p = null;
                switch (inst.Tipo?.ToLower())
                {
                    case "msi":
                        p = Process.Start("msiexec",
                            $"/i \"{path}\" {inst.Argumentos ?? "/qn /norestart"}");
                        break;
                    case "exe":
                        p = Process.Start(path, inst.Argumentos ?? "/S /VERYSILENT /NORESTART");
                        break;
                    case "bat":
                    case "cmd":
                        p = Process.Start("cmd.exe", $"/c \"{path}\" {inst.Argumentos}");
                        break;
                }
                p?.WaitForExit();
                if (p?.ExitCode != 0) _log($"  ⚠️ Retorno {p?.ExitCode}");
            }

            // 3) Drivers (placeholder) ---------------------------------------------------
            _log("Instalação de drivers dedicados (Lenovo / Dell / HP) ainda não implementada.");

            // Agenda próxima fase
            _estado["EtapaAtual"] = "Iniciar_ConfigsFinais";
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
