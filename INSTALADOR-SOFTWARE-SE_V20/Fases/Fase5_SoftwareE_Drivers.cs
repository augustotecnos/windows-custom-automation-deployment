// =====================================================================================
// Fases/Fase5_SoftwareE_Drivers.cs
// Instala software (Winget/Legados) e drivers (placeholder)
// Implementa pré-caching e verificação de instaladores.
// =====================================================================================
using INSTALADOR_SOFTWARE_SE.Helpers;
using INSTALADOR_SOFTWARE_SE.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace INSTALADOR_SOFTWARE_SE.Fases
{
    public class Fase5_SoftwareE_Drivers
    {
        private readonly GerenciadorDeEstado _estadoMgr;
        private readonly Dictionary<string, string> _estado;
        private readonly Action<string> _log;
        
        // NOVO: Constante para o diretório de cache local.
        private const string LocalCachePath = @"C:\SUPORTE\apps";

        public Fase5_SoftwareE_Drivers(GerenciadorDeEstado estadoMgr, Dictionary<string, string> estado, Action<string> logCallback)
        {
            _estadoMgr = estadoMgr;
            _estado = estado;
            _log = logCallback;
        }

        public bool Executar()
        {
            _log("───────────────────────────────────────────────────────────────");
            _log("FASE 5 ▸ Instalação de Software e Drivers");

            var perfil = CarregarPerfil();
            if (perfil == null)
            {
                _log("Aviso: perfil de software não encontrado – pulando Fase 5.");
                _estado["EtapaAtual"] = "Iniciar_ConfigsFinais";
                _estadoMgr.SalvarEstadoCompleto(_estado);
                return true;
            }

            // =================================================================================
            // NOVO: Etapa de Pré-Cache e Verificação
            // =================================================================================
            _log("Iniciando etapa de pré-cache de instaladores legados.");

            if (perfil.LegacyInstaller.Any())
            {
                // Garante que o diretório de cache local exista.
                Directory.CreateDirectory(LocalCachePath);

                var arquivosParaCopiar = new List<(string Source, string Destination)>();
                foreach (var inst in perfil.LegacyInstaller)
                {
                    // Ignora se o caminho no JSON estiver vazio
                    if (string.IsNullOrWhiteSpace(inst.Path)) continue;

                    string sourcePath = inst.Path.Replace("%share%", AppConfig.DeploymentSharePath);
                    string destPath = Path.Combine(LocalCachePath, Path.GetFileName(sourcePath));
                    arquivosParaCopiar.Add((sourcePath, destPath));
                }

                // Tenta copiar todos os arquivos
                foreach (var file in arquivosParaCopiar)
                {
                    try
                    {
                        if (!File.Exists(file.Source))
                        {
                            _log($"AVISO: Arquivo de origem não encontrado: {file.Source}. Pulando.");
                            continue;
                        }
                        _log($"Copiando: {Path.GetFileName(file.Source)} para {LocalCachePath}...");
                        File.Copy(file.Source, file.Destination, true); // true para sobrescrever se já existir
                    }
                    catch (Exception ex)
                    {
                        _log($"ERRO ao copiar {Path.GetFileName(file.Source)}: {ex.Message}");
                        // Mesmo com erro, continuamos para a verificação listar todos os que falharam.
                    }
                }
                
                // Agora, a verificação crítica
                _log("Verificando integridade dos arquivos cacheados...");
                var arquivosComFalha = new List<string>();
                foreach (var file in arquivosParaCopiar)
                {
                    if (!File.Exists(file.Source))
                    {
                        // Se o arquivo de origem nem existia, já foi logado. Adiciona à lista de falhas.
                        arquivosComFalha.Add($"{Path.GetFileName(file.Source)} (Origem não encontrada)");
                        continue;
                    }

                    if (!File.Exists(file.Destination))
                    {
                        arquivosComFalha.Add($"{Path.GetFileName(file.Destination)} (Não foi copiado)");
                        continue;
                    }
                    
                    // Verificação de tamanho para garantir cópia completa
                    var sourceInfo = new FileInfo(file.Source);
                    var destInfo = new FileInfo(file.Destination);

                    if (sourceInfo.Length != destInfo.Length)
                    {
                        arquivosComFalha.Add($"{Path.GetFileName(file.Destination)} (Tamanho incorreto! Origem: {sourceInfo.Length} bytes, Destino: {destInfo.Length} bytes)");
                    }
                }

                if (arquivosComFalha.Any())
                {
                    _log("ERRO CRÍTICO: Falha no pré-cache dos seguintes arquivos. A instalação não pode continuar.");
                    foreach (var falha in arquivosComFalha)
                    {
                        _log($" - {falha}");
                    }
                    return false; // Sinaliza para a UI que a fase falhou.
                }

                _log("Verificação concluída. Todos os instaladores foram cacheados com sucesso.");
            }
            else
            {
                _log("Nenhum instalador legado definido no perfil. Pulando etapa de cache.");
            }

            // =================================================================================
            // Fim da Etapa de Pré-Cache
            // =================================================================================

            // 1) Winget -----------------------------------------------------------------
            _log("Iniciando instalações via Winget...");
            foreach (var pkg in perfil.WingetPackages)
            {
                _log($"[Winget] Instalando: {pkg}");
                var p = Process.Start("winget", $"install --id {pkg} -e --accept-package-agreements --accept-source-agreements");
                p?.WaitForExit();
                if (p?.ExitCode != 0) _log($" ⚠️ Winget retornou código de erro: {p?.ExitCode}");
            }

            // 2) Instaladores legados ----------------------------------------------------
            _log("Iniciando instalações de pacotes legados (usando cache local)...");
            foreach (var inst in perfil.LegacyInstaller)
            {
                // ALTERADO: O caminho agora aponta para o arquivo no cache local.
                var localInstallerPath = Path.Combine(LocalCachePath, Path.GetFileName(inst.Path!));

                _log($"[Legacy] {inst.Nome} → {localInstallerPath}");
                if (!File.Exists(localInstallerPath))
                {
                    _log($" ⚠️ Arquivo local inexistente, pulando. (Isso não deveria acontecer após a verificação).");
                    continue;
                }

                Process? p = null;
                switch (inst.Tipo?.ToLower())
                {
                    case "msi":
                        p = Process.Start("msiexec", $"/i \"{localInstallerPath}\" {inst.Argumentos ?? "/qn /norestart"}");
                        break;
                    case "exe":
                        p = Process.Start(localInstallerPath, inst.Argumentos ?? "/S /VERYSILENT /NORESTART");
                        break;
                    case "bat":
                    case "cmd":
                        p = Process.Start("cmd.exe", $"/c \"{localInstallerPath}\" {inst.Argumentos}");
                        break;
                }
                p?.WaitForExit();
                if (p?.ExitCode != 0) _log($" ⚠️ Instalador retornou código de erro: {p?.ExitCode}");
            }

            // 3) Drivers (placeholder) ---------------------------------------------------
            _log("Instalação de drivers dedicados (Lenovo / Dell / HP) ainda não implementada.");

            // Agenda próxima fase
            _estado["EtapaAtual"] = "Iniciar_ConfigsFinais";
            _estadoMgr.SalvarEstadoCompleto(_estado);
            return true;
        }

        private PerfilSoftware? CarregarPerfil()
        {
            var unidade = _estado.GetValueOrDefault("PerfilId");
            var setor = _estado.GetValueOrDefault("SetorId");
            if (string.IsNullOrWhiteSpace(unidade) || string.IsNullOrWhiteSpace(setor)) return null;
            var file = Path.Combine(AppConfig.DeploymentSharePath, "Config", $"{unidade}_{setor}.json").ToLower();
            if (!File.Exists(file))
            {
                _log($"AVISO: Arquivo de perfil '{Path.GetFileName(file)}' não encontrado no compartilhamento.");
                return null;
            }
            try
            {
                return JsonSerializer.Deserialize<PerfilSoftware>(File.ReadAllText(file));
            }
            catch (Exception ex)
            {
                _log($"Erro ao ler JSON do perfil: {ex.Message}");
                return null;
            }
        }
    }
}



/*

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
*/

