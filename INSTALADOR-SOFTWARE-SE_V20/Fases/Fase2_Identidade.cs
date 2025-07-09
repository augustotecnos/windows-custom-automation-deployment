// =====================================================================================
// Fases/Fase2_Identidade.cs
// Define o nome da máquina (fase 2) e agenda a próxima etapa
// =====================================================================================
using INSTALADOR_SOFTWARE_SE.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace INSTALADOR_SOFTWARE_SE.Fases
{
    public class Fase2_Identidade
    {
        private readonly GerenciadorDeEstado _estadoMgr;
        private readonly Dictionary<string, string> _estado;
        private readonly Action<string> _log;

        public Fase2_Identidade(GerenciadorDeEstado estadoMgr,
                                Dictionary<string, string> estado,
                                Action<string> logCallback)
        {
            _estadoMgr = estadoMgr;
            _estado    = estado;
            _log       = logCallback;
        }

        public bool Executar()
        {
            _log("───────────────────────────────────────────────────────────────");
            _log("FASE 2 ▸ Definição da Identidade da Máquina");

            var nome = GerarNomeComputador();
            if (string.IsNullOrWhiteSpace(nome))
            {
                _log("ERRO: não foi possível gerar o nome do computador.");
                return false;
            }

            _log($"Nome gerado: {nome}");
            _estado["NomeComputadorDefinido"] = nome;
            _estado["EtapaAtual"]             = "PósRename_IngressarDominio";
            _estadoMgr.SalvarEstadoCompleto(_estado);

            // Renomeia usando PowerShell para evitar reboot imediato,
            // pois precisaremos reiniciar de qualquer forma após ingresso no domínio.
            var p = Process.Start("powershell",
                $"-Command \"Rename-Computer -NewName '{nome}' -Force\"");
            p?.WaitForExit();

            if (p?.ExitCode != 0)
            {
                _log($"ERRO: Rename-Computer saiu com código {p?.ExitCode}");
                return false;
            }

            _log("Nome alterado com sucesso. Reinício será feito após a Fase 3.");
            return true;
        }

        // ---------------------------------------------------------------------
        // Helpers privados
        // ---------------------------------------------------------------------

        private string GerarNomeComputador()
        {
            var unidade = _estado.GetValueOrDefault("PerfilId") ?? "matriz";
            var setor   = _estado.GetValueOrDefault("SetorId")  ?? "geral";
            var usuario = _estado.GetValueOrDefault("UsuarioFinal") ?? "";

            var modo = _estado.GetValueOrDefault("ModoNomenclatura") ?? "Novo";
            if (modo.Equals("ManterExistente", StringComparison.OrdinalIgnoreCase))
                return _estado.GetValueOrDefault("NomeComputadorSelecionado")!;

            // Delegue a lógica a um script AD se disponível
            try
            {
                var psi = new ProcessStartInfo("powershell",
                    $"-ExecutionPolicy Bypass -File \"{AppConfig.DeploymentSharePath}\\Scripts\\Manage-ComputerIdentity.ps1\" " +
                    $"-Mode GetNewName -PerfilId '{unidade}'")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using var proc = Process.Start(psi);
                var newName = proc?.StandardOutput.ReadLine();
                proc?.WaitForExit();
                if (!string.IsNullOrWhiteSpace(newName)) return newName.Trim();
            }
            catch { /* fallback */ }

            // fallback local: unidade-setor-RAND4
            var rand = Random.Shared.Next(1000, 9999);
            return $"{unidade}-{setor}-{rand}";
        }
    }
}
