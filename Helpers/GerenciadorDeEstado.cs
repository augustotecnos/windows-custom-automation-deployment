using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace INSTALADOR_SOFTWARE_SE.Helpers
{
    /// <summary>
    /// Classe responsável por gerenciar a persistência do estado da automação
    /// através do Registro do Windows. Garante que o processo possa continuar
    /// após múltiplas reinicializações.
    /// </summary>
    public class GerenciadorDeEstado
    {
        /// <summary>
        /// O caminho completo no registro onde o estado da aplicação será salvo.
        /// Usar HKEY_LOCAL_MACHINE (HKLM) garante que a chave seja para toda a máquina e
        /// exige privilégios de administrador para ser escrita, o que nosso app já terá.
        /// </summary>
        private const string CaminhoChaveRaiz = @"SOFTWARE\SEPromotora";
        private const string CaminhoChaveApp = @"SOFTWARE\SEPromotora\Instalador";

        /// <summary>
        /// Salva um conjunto completo de valores de estado no registro.
        /// Este método cria a chave e todos os valores necessários. Se a chave já existe,
        /// os valores são sobrescritos.
        /// </summary>
        /// <param name="estado">Um dicionário contendo os pares de chave/valor a serem salvos.</param>
        public void SalvarEstadoCompleto(Dictionary<string, string> estado)
        {
            try
            {
                // Abre a chave base HKLM e cria a subchave do nosso app.
                // O using garante que o objeto RegistryKey seja fechado e liberado corretamente.
                using (RegistryKey chaveBase = Registry.LocalMachine.CreateSubKey(CaminhoChaveApp))
                {
                    if (chaveBase == null)
                    {
                        throw new Exception("Não foi possível criar ou acessar a chave do registro. Verifique as permissões.");
                    }

                    // Itera sobre cada item do dicionário e o salva como um valor no registro.
                    foreach (var par in estado)
                    {
                        // SetValue cria o valor se ele não existe, ou o atualiza se já existir.
                        chaveBase.SetValue(par.Key, par.Value, RegistryValueKind.String);
                    }
                }
            }
            catch (Exception ex)
            {
                // Em uma aplicação real, aqui seria o local ideal para logar o erro em um arquivo.
                Console.WriteLine($"ERRO CRÍTICO ao salvar estado no registro: {ex.Message}");
                // Propaga a exceção para que a thread principal saiba que algo deu errado.
                throw;
            }
        }

        /// <summary>
        /// Carrega todos os valores salvos na chave de estado do aplicativo.
        /// </summary>
        /// <returns>Um dicionário com o estado carregado, ou null se a chave não existir (indicando primeira execução).</returns>
        public Dictionary<string, string> CarregarEstadoCompleto()
        {
            try
            {
                using (RegistryKey chaveBase = Registry.LocalMachine.OpenSubKey(CaminhoChaveApp))
                {
                    // Se a chave não puder ser aberta (retorna null), significa que é a primeira
                    // vez que o app roda, ou que ela já foi limpa.
                    if (chaveBase == null)
                    {
                        return null;
                    }

                    var estado = new Dictionary<string, string>();
                    // Pega o nome de todos os valores dentro da nossa chave.
                    foreach (var nomeValor in chaveBase.GetValueNames())
                    {
                        // Adiciona cada par nome/valor ao dicionário.
                        estado.Add(nomeValor, chaveBase.GetValue(nomeValor).ToString());
                    }
                    return estado;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO ao carregar estado do registro: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Configura a aplicação para iniciar automaticamente uma única vez após o próximo reboot.
        /// Esta é a chave para o nosso loop de reinicialização.
        /// </summary>
        public void ConfigurarRunOnce()
        {
            const string runOnceKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";
            try
            {
                // Abre a chave RunOnce com permissão de escrita.
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(runOnceKeyPath, true))
                {
                    // Define um valor com um nome único ("InstaladorSE") e o caminho completo para o nosso .exe.
                    // O Windows executará este caminho após o próximo logon e depois excluirá a entrada automaticamente.
                    key?.SetValue("InstaladorSE", Application.ExecutablePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO CRÍTICO ao configurar a chave RunOnce: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Remove permanentemente toda a chave de estado da aplicação do registro.
        /// Esta é uma ação de limpeza final para impedir que o app seja executado novamente
        /// e para não deixar lixo no registro do cliente.
        /// </summary>
        public void RemoverChaveDeEstado()
        {
            try
            {
                // Tenta abrir a chave pai.
                using (RegistryKey chavePai = Registry.LocalMachine.OpenSubKey(CaminhoChaveRaiz, true))
                {
                    if (chavePai != null)
                    {
                        // Se a chave pai existe, deleta a subchave "Instalador" e tudo dentro dela.
                        chavePai.DeleteSubKeyTree("Instalador", false); // 'false' para não lançar exceção se não existir
                    }
                }
            }
            catch (Exception ex)
            {
                // Um erro aqui não é crítico para o usuário final, mas deve ser logado.
                Console.WriteLine($"AVISO: Erro não crítico ao remover a chave do registro: {ex.Message}");
            }
        }
    }
}