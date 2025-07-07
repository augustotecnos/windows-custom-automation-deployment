using INSTALADOR_SOFTWARE_SE.Helpers;
using INSTALADOR_SOFTWARE_SE.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using INSTALADOR_SOFTWARE_SE;

namespace INSTALADOR_SOFTWARE_SE.Fases
{
    /// <summary>
    /// Gerencia todas as operações da Fase 5: Instalação de todo o software
    /// de produtividade e verificação/instalação final de drivers.
    /// </summary>
    public class Fase5_SoftwareE_Drivers
    {
        private readonly GerenciadorDeEstado _gerenciadorDeEstado;
        private readonly Dictionary<string, string> _estadoAtual;
        private readonly Action<string> _logCallback;

        public Fase5_SoftwareE_Drivers(GerenciadorDeEstado gerenciadorDeEstado, Dictionary<string, string> estadoAtual, Action<string> logCallback)
        {
            _gerenciadorDeEstado = gerenciadorDeEstado;
            _estadoAtual = estadoAtual;
            _logCallback = logCallback;
        }

        /// <summary>
        /// Ponto de entrada principal que executa todas as ações da Fase 5.
        /// </summary>
        /// <returns>True se a fase foi concluída com sucesso, False se falhou.</returns>
        public bool Executar()
        {
            _logCallback("FASE 5: Instalação de Software e Verificação de Drivers Iniciada.");
            try
            {
                // --- ETAPA 1: CARREGAR O PERFIL DE SOFTWARE ESPECÍFICO ---
                SoftwareProfile perfil = CarregarPerfilDeSoftware();
                if (perfil == null)
                {
                    throw new Exception("Não foi possível carregar o perfil de software. A instalação não pode continuar.");
                }
                _logCallback($"Perfil '{perfil.NomePerfil}' carregado com sucesso.");

                // --- ETAPA 2: INSTALAR PACOTES VIA WINGET ---
                InstalarPacotesWinget(perfil.WingetPackages);

                // --- ETAPA 3: INSTALAR APLICATIVOS LEGADOS (.MSI, .EXE, .BAT) ---
                InstalarAplicativosLegados(perfil.LegacyInstallers);

                // --- ETAPA 4: VERIFICAR E INSTALAR DRIVERS (MECANISMO DE FALLBACK) ---
                VerificarEInstalarDrivers();

                // --- ETAPA 5: FINALIZAR A FASE E PREPARAR PARA A PRÓXIMA ---
                _logCallback("SUCESSO: Fase 5 concluída.");
                _estadoAtual["EtapaAtual"] = "Iniciar_ConfigsFinais"; // Define a próxima fase
                _gerenciadorDeEstado.SalvarEstadoCompleto(_estadoAtual);
                return true;
            }
            catch (Exception ex)
            {
                _logCallback($"ERRO FATAL NA FASE 5: {ex.Message}");
                return false;
            }
        }

        private SoftwareProfile CarregarPerfilDeSoftware()
        {
            string perfilId = _estadoAtual["PerfilId"];
            string setorId = _estadoAtual["SetorId"];
            string nomeArquivoPerfil = $"{perfilId}_{setorId}.json";
            string caminhoCompletoPerfil = Path.Combine(AppConfig.DeploymentSharePath, "Config", nomeArquivoPerfil);

            _logCallback($"Carregando perfil de software de: {caminhoCompletoPerfil}");
            if (!File.Exists(caminhoCompletoPerfil))
            {
                _logCallback($"AVISO: Arquivo de perfil '{nomeArquivoPerfil}' não encontrado. Pulando instalação de software customizado.");
                return new SoftwareProfile { NomePerfil = "Padrão (Vazio)", WingetPackages = new List<string>(), LegacyInstallers = new List<LegacyInstaller>() };
            }

            string jsonContent = File.ReadAllText(caminhoCompletoPerfil);
            return JsonSerializer.Deserialize<SoftwareProfile>(jsonContent);
        }

        private void InstalarPacotesWinget(List<string> pacotes)
        {
            _logCallback("--- Iniciando instalação de pacotes via Winget ---");
            if (pacotes == null || pacotes.Count == 0)
            {
                _logCallback("Nenhum pacote Winget definido no perfil.");
                return;
            }

            foreach (var pacoteId in pacotes)
            {
                _logCallback($"Instalando Winget pacote: {pacoteId}");
                string argumentos = $"install --id \"{pacoteId}\" --source winget --accept-package-agreements --accept-source-agreements --silent";
                ExecutarComandoProcesso("winget.exe", argumentos, $"Falha ao instalar {pacoteId} via Winget");
            }
        }

        private void InstalarAplicativosLegados(List<LegacyInstaller> instaladores)
        {
            _logCallback("--- Iniciando instalação de aplicativos legados ---");
            if (instaladores == null || instaladores.Count == 0)
            {
                _logCallback("Nenhum aplicativo legado definido no perfil.");
                return;
            }

            string pastaTempLocal = Path.Combine(Path.GetTempPath(), "InstaladorSE_Temp");
            Directory.CreateDirectory(pastaTempLocal);

            foreach (var app in instaladores)
            {
                _logCallback($"Processando instalador: {app.Nome}");
                string nomeArquivo = Path.GetFileName(app.Path);
                string caminhoLocal = Path.Combine(pastaTempLocal, nomeArquivo);

                try
                {
                    _logCallback($"Copiando '{nomeArquivo}' do servidor para a pasta temporária local...");
                    File.Copy(app.Path, caminhoLocal, true); // O 'true' permite sobrescrever o arquivo se ele já existir

                    string comando;
                    string argumentos;

                    switch (app.Tipo.ToLower())
                    {
                        case "msi":
                            comando = "msiexec.exe";
                            argumentos = $"/i \"{caminhoLocal}\" {app.Argumentos}";
                            break;
                        case "exe":
                        case "bat":
                            comando = caminhoLocal;
                            argumentos = app.Argumentos;
                            break;
                        default:
                            _logCallback($"AVISO: Tipo de instalador desconhecido ('{app.Tipo}') para '{app.Nome}'. Pulando.");
                            continue; // Pula para o próximo item do loop
                    }

                    _logCallback($"Executando instalação de '{app.Nome}' com o comando: {comando} {argumentos}");
                    ExecutarComandoProcesso(comando, argumentos, $"Falha ao instalar '{app.Nome}'");
                }
                catch (Exception ex)
                {
                     _logCallback($"ERRO ao processar o instalador '{app.Nome}': {ex.Message}. Pulando para o próximo.");
                }
            }
        }

        private void VerificarEInstalarDrivers()
        {
            _logCallback("--- Iniciando verificação final de drivers ---");
            string scriptPath = Path.Combine(AppConfig.DeploymentSharePath, "Scripts", "Test-DriverStatus.ps1");
            int exitCode = ExecutarComandoProcesso("powershell.exe", $"-ExecutionPolicy Bypass -File \"{scriptPath}\"");

            if (exitCode == 1) // Código '1' que definimos para "problemas encontrados"
            {
                _logCallback("Problemas de driver detectados. Acionando Lenovo System Update como fallback...");
                string scriptLenovoPath = Path.Combine(AppConfig.DeploymentSharePath, "Scripts", "Invoke-LenovoSystemUpdate.ps1");
                ExecutarComandoProcesso("powershell.exe", $"-ExecutionPolicy Bypass -File \"{scriptLenovoPath}\"");
                _logCallback("Lenovo System Update concluído. Recomenda-se uma verificação manual se os problemas persistirem.");
            }
            else if (exitCode == 0)
            {
                _logCallback("Verificação de drivers concluída. Nenhum problema encontrado.");
            }
            else
            {
                _logCallback($"AVISO: O script de verificação de drivers terminou com um código de erro inesperado: {exitCode}.");
            }
        }

        /// <summary>
        /// Um método helper genérico para executar processos externos e logar sua saída.
        /// </summary>
        private int ExecutarComandoProcesso(string comando, string argumentos, string mensagemErro = "O comando falhou")
        {
            var startInfo = new ProcessStartInfo(comando, argumentos)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = Process.Start(startInfo))
            {
                process.OutputDataReceived += (sender, args) => { if (args.Data != null) _logCallback($"  [log]: {args.Data}"); };
                process.ErrorDataReceived += (sender, args) => { if (args.Data != null) _logCallback($"  [err]: {args.Data}"); };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    _logCallback($"AVISO: {mensagemErro}. O processo '{Path.GetFileName(comando)}' terminou com o código de saída: {process.ExitCode}");
                }
                return process.ExitCode;
            }
        }
    }
}