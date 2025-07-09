using System;
using System.IO;
using System.Windows.Forms;

namespace INSTALADOR_SOFTWARE_SE.Helpers
{
    /// <summary>
    /// Utilitário simples para registrar mensagens de log em um arquivo de texto
    /// localizado no mesmo diretório da aplicação.
    /// </summary>
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(Application.StartupPath, "instalador.log");

        /// <summary>
        /// Adiciona uma mensagem ao arquivo de log com carimbo de data e hora.
        /// Qualquer falha ao registrar é ignorada para não interromper a aplicação.
        /// </summary>
        public static void Log(string message)
        {
            try
            {
                string linha = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFilePath, linha);
            }
            catch
            {
                // Intencionalmente ignorado para evitar exceções não tratadas durante o log
            }
        }
    }
}
