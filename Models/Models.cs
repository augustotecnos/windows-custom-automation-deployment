using System.Collections.Generic;

namespace INSTALADOR_SOFTWARE_SE.Models
{
    // -----------------------------------------------------------------------------------
    // MODELOS PARA O ARQUIVO DE REDE LOCAL (network_config.json)
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Representa a configuração de rede para a atribuição inicial de IP estático.
    /// Lida a partir do arquivo 'network_config.json' local.
    /// </summary>
    public class NetworkConfig
    {
        public string SubnetMask { get; set; }
        public string Gateway { get; set; }
        public string Dns { get; set; }
        public List<string> IpTestRange { get; set; }
    }

    // -----------------------------------------------------------------------------------
    // MODELOS PARA O ARQUIVO DE CONFIGURAÇÃO MESTRE (master_config.json)
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Representa uma Unidade/Filial da empresa.
    /// </summary>
    public class Unidade
    {
        public string NomeExibicao { get; set; }
        public string Id { get; set; }
    }

    /// <summary>
    /// Representa um Setor/Departamento da empresa.
    /// </summary>
    public class Setor
    {
        public string NomeExibicao { get; set; }
        public string Id { get; set; }
    }

    /// <summary>
    /// Representa um usuário final que receberá a máquina.
    /// </summary>
    public class UsuarioFinal
    {
        public string NomeExibicao { get; set; }
        public string LoginName { get; set; }
    }

    /// <summary>
    /// Representa o objeto raiz do arquivo 'master_config.json', contendo as listas
    /// que irão popular os ComboBoxes na interface do técnico.
    /// </summary>
    public class MasterConfig
    {
        public List<Unidade> Unidades { get; set; }
        public List<Setor> Setores { get; set; }
        public List<UsuarioFinal> UsuariosFinais { get; set; }
    }


    // -----------------------------------------------------------------------------------
    // MODELOS PARA OS ARQUIVOS DE PERFIL DE SOFTWARE (ex: matriz_ti.json)
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Representa um instalador que não é do Winget (legado), como .msi, .exe ou .bat.
    /// </summary>
    public class LegacyInstaller
    {
        public string Nome { get; set; }
        public string Path { get; set; }
        public string Tipo { get; set; }
        public string Argumentos { get; set; }
    }

    /// <summary>
    /// Representa a configuração da VPN para um determinado perfil.
    /// </summary>
    public class VpnConfig
    {
        public string InstallerPath { get; set; }
        public string ConfigFilePath { get; set; }
    }

    /// <summary>
    /// Representa o objeto raiz de um arquivo de perfil de software,
    /// detalhando tudo que precisa ser instalado e configurado para um setor específico.
    /// </summary>
    public class SoftwareProfile
    {
        public string NomePerfil { get; set; }
        public List<string> WingetPackages { get; set; }
        public List<LegacyInstaller> LegacyInstallers { get; set; }
        public List<string> Printers { get; set; }
        public string HostsFile { get; set; }
        public VpnConfig VpnConfig { get; set; }
        public List<string> LocalGroups { get; set; }
    }
}