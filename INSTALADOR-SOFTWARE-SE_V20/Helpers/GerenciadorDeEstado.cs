// =====================================================================================
// Helpers/GerenciadorDeEstado.cs
// Persistência de estado entre reinicializações (RunOnce + Registro)
// =====================================================================================
using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace INSTALADOR_SOFTWARE_SE.Helpers
{
    /// <summary>
    /// Armazena e recupera variáveis que indicam em que fase o orquestrador está,
    /// permitindo retomar o processo após múltiplos reboots.
    /// </summary>
    public class GerenciadorDeEstado
    {
        private const string BaseKey = @"SOFTWARE\SEPromotora\Pilot";
        private const string RunOnceKey =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";

        // ---------------------------------------------------------------------
        // APIs públicas
        // ---------------------------------------------------------------------

        public Dictionary<string, string>? CarregarEstadoCompleto()
        {
            using var k = Registry.LocalMachine.OpenSubKey(BaseKey, false);
            if (k == null) return null;

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in k.GetValueNames())
                map[name] = k.GetValue(name)?.ToString() ?? string.Empty;
            return map.Count == 0 ? null : map;
        }

        public void SalvarEstadoCompleto(IDictionary<string, string> dados)
        {
            using var k = Registry.LocalMachine.CreateSubKey(BaseKey, true);
            foreach (var kv in dados) k!.SetValue(kv.Key, kv.Value);
        }

        public void DefinirVariavel(string chave, string valor)
        {
            using var k = Registry.LocalMachine.CreateSubKey(BaseKey, true);
            k!.SetValue(chave, valor);
        }

        public string? ObterVariavel(string chave) =>
            Registry.LocalMachine.OpenSubKey(BaseKey, false)
            ?.GetValue(chave)?.ToString();

        public void RemoverChaveDeEstado() =>
            Registry.LocalMachine.DeleteSubKeyTree(BaseKey, false);

        /// <summary>
        /// Adiciona esta aplicação ao RunOnce para reiniciar do ponto salvo.
        /// </summary>
        public void ConfigurarRunOnce()
        {
            var exe = Environment.ProcessPath ?? "INSTALADOR-SOFTWARE-SE.exe";
            using var k = Registry.LocalMachine.CreateSubKey(RunOnceKey, true);
            k!.SetValue("SE-Pilot", $"\"{exe}\"");
        }
    }
}
