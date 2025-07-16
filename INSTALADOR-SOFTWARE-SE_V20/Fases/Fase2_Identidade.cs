// Substitua o conteúdo do seu arquivo Fases/Fase2_Identidade.cs por este.
// Ele adiciona verificações para evitar referências nulas.

using INSTALADOR_SOFTWARE_SE.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace INSTALADOR_SOFTWARE_SE.Fases
{
    public class Fase2_Identidade
    {
        private readonly GerenciadorDeEstado _estadoMgr;
        private readonly Dictionary<string, string> _estado;
        private readonly Action<string> _log;

        public Fase2_Identidade(GerenciadorDeEstado estadoMgr, Dictionary<string, string> estado, Action<string> logCallback)
        {
            _estadoMgr = estadoMgr;
            _estado    = estado;
            _log       = logCallback;
        }

        public bool Executar()
        {
            _log("───────────────────────────────────────────────────────────────");
            _log("FASE 2 ▸ Definição da Identidade da Máquina");

            var nomeFinal = GerarNomeComputador();
            if (string.IsNullOrWhiteSpace(nomeFinal))
            {
                _log("ERRO: Falha crítica ao determinar o nome do computador. Verifique os logs acima.");
                return false;
            }

            _log($"O nome definitivo da máquina será: {nomeFinal}");
            
            _log($"Executando: Rename-Computer -NewName '{nomeFinal}'");
            var p = Process.Start("powershell", $"-NoProfile -Command \"Rename-Computer -NewName '{nomeFinal}' -Force\"");
            
            // CORREÇÃO (CS8602): Verifica se o processo foi iniciado antes de usá-lo.
            if (p == null)
            {
                _log("ERRO: Não foi possível iniciar o processo 'powershell' para renomear o computador.");
                return false;
            }
            p.WaitForExit();

            if (p.ExitCode != 0)
            {
                _log($"ERRO: O processo Rename-Computer falhou com o código de saída {p.ExitCode}. A automação não pode continuar.");
                return false;
            }

            try
            {
                _log("Nome da máquina agendado para alteração na próxima reinicialização.");

                _estado["NomeComputadorDefinido"] = nomeFinal;
                _estado["EtapaAtual"]             = "PósRename_IngressarDominio";
                _estadoMgr.SalvarEstadoCompleto(_estado);
                _estadoMgr.ConfigurarRunOnce();
    
                _log("Configurações salvas. A máquina será reiniciada em 20 segundos...");
                    Process.Start("shutdown.exe", "/r /t 20 /c \"Ingresso no domínio concluído. Reiniciando para a Fase 3.\"");
    
                return true;
                
            }
            catch (Exception ex)
            {
                _log($"ERRO FATAL NA FASE 2: {ex.Message}");
                // Em caso de erro, o processo para aqui para que o técnico possa investigar.
                return false;
            }
            
        }

        private string? GerarNomeComputador()
        {
            var modo = _estado.GetValueOrDefault("ModoNomenclatura", "Novo");
            _log($"Modo de nomenclatura selecionado: {modo}");

            switch (modo)
            {
                case "ManterExistente":
                    var nomeSelecionado = _estado.GetValueOrDefault("NomeComputadorSelecionado");
                    // CORREÇÃO (CS8604): Verifica se o nome é válido antes de passar para o método.
                    if (string.IsNullOrWhiteSpace(nomeSelecionado))
                    {
                        _log("ERRO: Modo 'ManterExistente' selecionado, mas nenhum nome de computador foi encontrado no estado.");
                        return null;
                    }
                    _log($"Reutilizando o nome existente: {nomeSelecionado}. Limpando objeto antigo do AD/DHCP...");
                    if (!ResetarObjetoAD(nomeSelecionado)) return null;
                    return nomeSelecionado;

                case "Manual":
                    var nomeManual = _estado.GetValueOrDefault("NomeComputadorManual");
                    // CORREÇÃO (CS8604): Verifica se o nome é válido antes de passar para o método.
                    if (string.IsNullOrWhiteSpace(nomeManual))
                    {
                        _log("ERRO: Modo 'Manual' selecionado, mas nenhum nome foi digitado.");
                        return null;
                    }
                    _log($"Utilizando nome digitado manualmente: {nomeManual}. Limpando objeto antigo do AD/DHCP por segurança...");
                    if (!ResetarObjetoAD(nomeManual)) return null;
                    return nomeManual;

                case "Novo":
                default:
                    _log("Tentando obter um novo nome dinamicamente via script PowerShell...");
                    try
                    {
                        var perfilId = _estado.GetValueOrDefault("PerfilId", "matriz");
                        string credentialFilePath = Path.Combine(AppConfig.DeploymentSharePath, "Config", "secure_credential.xml");
                        
                        var psi = new ProcessStartInfo("powershell",
                            $"-ExecutionPolicy Bypass -File \"{AppConfig.CaminhoScriptIdentidade}\" " +
                            $"-Mode GetNewName -PerfilId '{perfilId}' -CredentialFilePath \"{credentialFilePath}\"")
                        {
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        };
                        
                        using var proc = Process.Start(psi);

                        // CORREÇÃO (CS8602): Verifica se o processo foi iniciado.
                        if (proc == null)
                        {
                             _log("ERRO: Falha ao iniciar o processo do script 'GetNewName'. Usando fallback.");
                             break; // Sai do 'try' e vai para o fallback.
                        }

                        var newName = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit();
                        
                        if (proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(newName))
                        {
                            _log("Script executado com sucesso.");
                            return newName.Trim();
                        }
                        
                        _log($"AVISO: O script de geração de nome falhou (código: {proc.ExitCode}). Usando fallback.");
                    }
                    catch (Exception ex) 
                    {
                        _log($"AVISO: Ocorreu uma exceção ao executar o script: {ex.Message}. Usando fallback.");
                    }

                    _log("Gerando nome local como fallback...");
                    var unidadeFallback = _estado.GetValueOrDefault("PerfilId", "matriz");
                    var setorFallback = _estado.GetValueOrDefault("SetorId", "geral");
                    var rand = Random.Shared.Next(1000, 9999);
                    return $"{unidadeFallback}-{setorFallback}-{rand}";
            }
            return null; // Retorno para o caso de fallback falhar.
        }
        
        private bool ResetarObjetoAD(string nomeComputador)
        {
            if (string.IsNullOrWhiteSpace(nomeComputador))
            {
                _log("AVISO: Nome de computador para resetar está vazio. Ignorando a limpeza.");
                return true;
            }

            _log($"Executando script de limpeza para o computador '{nomeComputador}'...");
            try
            {
                string credentialFilePath = Path.Combine(AppConfig.DeploymentSharePath, "Config", "secure_credential.xml");

                var psi = new ProcessStartInfo("powershell",
                    $"-ExecutionPolicy Bypass -File \"{AppConfig.CaminhoScriptIdentidade}\" " +
                    $"-Mode ResetADObject -ComputerName '{nomeComputador}' -CredentialFilePath \"{credentialFilePath}\"")
                {
                    UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
                };

                using var proc = Process.Start(psi);

                // CORREÇÃO (CS8602): Verifica se o processo foi iniciado.
                if (proc == null)
                {
                    _log("ERRO: Falha ao iniciar o processo do script 'ResetADObject'.");
                    return false;
                }

                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (!string.IsNullOrWhiteSpace(output)) _log($"Saída do script de limpeza: {output.Trim()}");
                
                if (proc.ExitCode != 0)
                {
                    _log($"ERRO: O script de limpeza para '{nomeComputador}' falhou com código {proc.ExitCode}.");
                    if (!string.IsNullOrWhiteSpace(error)) _log($"Detalhes do erro: {error.Trim()}");
                    return false;
                }

                _log($"Limpeza para '{nomeComputador}' concluída com sucesso.");
                return true;
            }
            catch (Exception ex)
            {
                _log($"ERRO CRÍTICO ao tentar executar o script de limpeza: {ex.Message}");
                return false;
            }
        }
    }
}