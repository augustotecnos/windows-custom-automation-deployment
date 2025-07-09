/*
 * =====================================================================================
 * ARQUIVO: Models/Models.cs
 * DESCRIÇÃO: Corrigido para incluir o namespace correto e resolver os avisos de
 * nulidade (CS8618). Propriedades que são carregadas de JSON agora
 * são anuláveis ('?') e listas são inicializadas para evitar erros.
 * =====================================================================================
 */
namespace INSTALADOR_SOFTWARE_SE.Models
{
    // Adicionamos '?' para indicar que as propriedades podem ser nulas,
    // pois elas são preenchidas a partir de arquivos JSON, e não no construtor.
    // Também inicializamos listas para evitar que sejam nulas.

    public class NetworkConfig
    {
        public string? SubnetMask { get; set; }
        public string? Gateway { get; set; }
        public string? Dns { get; set; }
        public List<string> IpTestRange { get; set; } = new List<string>();
    }

    public class Unidade
    {
        public string? NomeExibicao { get; set; }
        public string? Id { get; set; }
    }

    public class Setor
    {
        public string? NomeExibicao { get; set; }
        public string? Id { get; set; }
    }

    public class UsuarioFinal
    {
        public string? NomeExibicao { get; set; }
        public string? LoginName { get; set; }
    }

    public class MasterConfig
    {
        public List<Unidade> Unidades { get; set; } = new List<Unidade>();
        public List<Setor> Setores { get; set; } = new List<Setor>();
        public List<UsuarioFinal> UsuariosFinais { get; set; } = new List<UsuarioFinal>();
    }

    public class LegacyInstaller
    {
        public string? Nome { get; set; }
        public string? Path { get; set; }
        public string? Tipo { get; set; } // 'msi' ou 'exe'
        public string? Argumentos { get; set; }
    }
    
    public class VpnConfig
    {
        public string? InstallerPath { get; set; }
        public string? ConfigFilePath { get; set; }
    }

    // O nome da classe é 'PerfilSoftware' como no repositório.
    public class PerfilSoftware
    {
        public string? NomePerfil { get; set; }
        public List<string> WingetPackages { get; set; } = new List<string>();
        public List<LegacyInstaller> LegacyInstallers { get; set; } = new List<LegacyInstaller>();
        public List<string> Printers { get; set; } = new List<string>();
        public Dictionary<string, string>? HostsFile { get; set; }
        public VpnConfig? VpnConfig { get; set; }
        public List<string> LocalGroups { get; set; } = new List<string>();
    }
}