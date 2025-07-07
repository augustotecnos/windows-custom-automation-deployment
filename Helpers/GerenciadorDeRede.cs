using INSTALADOR_SOFTWARE_SE.Models;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Windows.Forms; // Usado apenas para Application.StartupPath

namespace INSTALADOR_SOFTWARE_SE.Helpers
{
    /// <summary>
    /// Encapsula toda a lógica de rede para a configuração inicial da máquina,
    /// focando na atribuição de um endereço IP estático.
    /// </summary>
    public class GerenciadorDeRede
    {
        private readonly NetworkConfig _networkConfig;

        /// <summary>
        /// Inicializa uma nova instância do GerenciadorDeRede, carregando
        /// imediatamente a configuração de rede do arquivo JSON local.
        /// </summary>
        public GerenciadorDeRede()
        {
            _networkConfig = CarregarConfiguracaoDeRedeLocal();
        }

        /// <summary>
        /// Lê e desserializa o arquivo 'network_config.json' da pasta da aplicação.
        /// </summary>
        /// <returns>Um objeto NetworkConfig preenchido ou null se o arquivo não for encontrado ou for inválido.</returns>
        private NetworkConfig CarregarConfiguracaoDeRedeLocal()
        {
            try
            {
                // Constrói o caminho para o arquivo de configuração que deve estar junto com o .exe
                string configPath = Path.Combine(Application.StartupPath, "network_config.json");
                if (!File.Exists(configPath))
                {
                    // Lançar uma exceção aqui é uma boa prática para indicar um problema de setup.
                    throw new FileNotFoundException("O arquivo 'network_config.json' é essencial e não foi encontrado na pasta da aplicação.", configPath);
                }
                string jsonContent = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<NetworkConfig>(jsonContent);
            }
            catch (Exception ex)
            {
                // Em caso de qualquer erro (arquivo não encontrado, JSON mal formatado), logamos e retornamos null.
                Console.WriteLine($"ERRO CRÍTICO ao carregar network_config.json: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Procura pela primeira interface de rede (Ethernet ou Wi-Fi) que esteja fisicamente conectada e operacional.
        /// </summary>
        /// <returns>O objeto NetworkInterface ou null se nenhuma for encontrada.</returns>
        private NetworkInterface GetActiveInterface()
        {
            return NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(
                a => a.OperationalStatus == OperationalStatus.Up &&
                     (a.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                      a.NetworkInterfaceType == NetworkInterfaceType.Wireless80211));
        }

        /// <summary>
        /// Executa os comandos 'netsh.exe' para configurar um IP estático, gateway e DNS na interface de rede especificada.
        /// </summary>
        /// <returns>True se os comandos foram executados com sucesso, False caso contrário.</returns>
        private bool SetStaticIpAddress(string interfaceName, string ip, string subnetMask, string gateway, string dns)
        {
            // Oculta a janela do console para uma execução limpa e profissional
            var processInfo = new ProcessStartInfo("netsh.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            // Comando para definir o IP estático e o Gateway
            processInfo.Arguments = $"interface ip set address name=\"{interfaceName}\" static {ip} {subnetMask} {gateway}";
            using (var processIp = Process.Start(processInfo))
            {
                processIp.WaitForExit();
                if (processIp.ExitCode != 0) return false; // Falhou em definir o IP
            }

            // Comando para definir o servidor DNS primário
            processInfo.Arguments = $"interface ip set dns name=\"{interfaceName}\" static {dns}";
            using (var processDns = Process.Start(processInfo))
            {
                processDns.WaitForExit();
                return processDns.ExitCode == 0; // Retorna true apenas se o DNS também for definido com sucesso
            }
        }

        /// <summary>
        /// Orquestra o processo de encontrar um IP livre e atribuí-lo à máquina.
        /// Este é o principal método público da classe.
        /// </summary>
        /// <param name="ipAtribuido">Parâmetro de saída que retornará o IP que foi configurado com sucesso.</param>
        /// <returns>True se um IP foi atribuído com sucesso, False caso contrário.</returns>
        public bool AtribuirIpDisponivel(out string ipAtribuido)
        {
            ipAtribuido = null;
            if (_networkConfig == null) return false; // Falha se a configuração não foi carregada

            var activeInterface = GetActiveInterface();
            if (activeInterface == null) return false; // Falha se não há cabo de rede conectado

            // Itera sobre cada IP no range definido no arquivo JSON.
            foreach (var ip in _networkConfig.IpTestRange)
            {
                // Usa um Ping para verificar se o IP já responde na rede.
                using (var ping = new Ping())
                {
                    // Timeout de 1.5 segundos é um bom equilíbrio entre rapidez e confiabilidade.
                    var reply = ping.Send(ip, 1500);
                    if (reply.Status == IPStatus.Success)
                    {
                        Console.WriteLine($"IP em uso: {ip}");
                        continue; // Se o IP responde, pula para o próximo da lista.
                    }
                }
                
                // Se o Ping falhou (o que é bom, significa que o IP está livre), tenta configurar.
                if (SetStaticIpAddress(activeInterface.Name, ip, _networkConfig.SubnetMask, _networkConfig.Gateway, _networkConfig.Dns))
                {
                    ipAtribuido = ip; // Guarda o IP que foi usado.
                    return true; // Sucesso! Termina o método.
                }
            }

            // Se o loop terminar, significa que todos os IPs do range foram testados e estão em uso.
            return false;
        }
    }
}