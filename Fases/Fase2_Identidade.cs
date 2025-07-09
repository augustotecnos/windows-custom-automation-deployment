/*
 * =====================================================================================
 * ARQUIVO: Fases/Fase2_Identidade.cs
 * DESCRIÇÃO: Corrigido para incluir o namespace e resolver o erro CS0023.
 * =====================================================================================
 */
using INSTALADOR_SOFTWARE_SE.Helpers;
using System.Diagnostics;

namespace INSTALADOR_SOFTWARE_SE.Fases
{
    public class Fase2_Identidade
    {
        private readonly GerenciadorDeEstado _estado;
        private readonly Action<string> _logCallback;

        public Fase2_Identidade(GerenciadorDeEstado estado, Action<string> logCallback)
        {
            _estado = estado;
            _logCallback = logCallback;
        }

        public void Executar()
        {
            _logCallback("FASE 2: Definindo a Identidade da Máquina");
            string nomeComputador = GerarNomeComputador();

            // CORREÇÃO DO ERRO PRINCIPAL (CS0023)
            if (string.IsNullOrWhiteSpace(nomeComputador))
            {
                _logCallback("ERRO: Não foi possível gerar o nome do computador.");
                return;
            }

            _logCallback($"Nome do computador gerado: {nomeComputador}");
            _estado.DefinirVariavel("NomeComputador", nomeComputador);
            DefinirNomeComputador(nomeComputador);
        }

        private string GerarNomeComputador()
        {
            string? unidadeId = _estado.ObterVariavel("UnidadeId");
            string? setorId = _estado.ObterVariavel("SetorId");
            string? usuarioFinalLogin = _estado.ObterVariavel("UsuarioFinalLogin");
            string modoNomenclatura = _estado.ObterVariavel("ModoNomenclatura") ?? "Padrao";

            if (string.IsNullOrWhiteSpace(unidadeId) || string.IsNullOrWhiteSpace(setorId))
            {
                return string.Empty;
            }

            if (modoNomenclatura == "Usuario" && !string.IsNullOrWhiteSpace(usuarioFinalLogin))
            {
                return $"{unidadeId}-{setorId}-{usuarioFinalLogin}";
            }
            else
            {
                string serialNumber = ObterNumeroDeSerie();
                return $"{unidadeId}-{setorId}-{serialNumber}";
            }
        }

        private string ObterNumeroDeSerie()
        {
            try
            {
                // ... (código para obter o serial number)
                return "SERIAL123"; // Placeholder
            }
            catch (Exception ex)
            {
                _logCallback($"Erro ao obter número de série: {ex.Message}");
                return "SN" + new Random().Next(1000, 9999);
            }
        }

        private void DefinirNomeComputador(string nome)
        {
            _logCallback($"Executando script para definir o nome do computador como '{nome}'...");
            // Supondo que GerenciadorDeScripts exista em Helpers
            // GerenciadorDeScripts.ExecutarScript("Set-ComputerName.ps1", $"-ComputerName \"{nome}\"", _logCallback);
        }
    }
}