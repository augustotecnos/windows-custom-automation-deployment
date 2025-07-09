/*
 * =====================================================================================
 * ARQUIVO: Models/Models.cs (CORRIGIDO)
 * INSTRUÇÃO: Substitua todo o conteúdo do seu arquivo por este.
 * =====================================================================================
 */
namespace INSTALADOR_SOFTWARE_SE.Models
{
    public class NetworkConfig
    {
        public string? SubnetMask { get; set; }
        public string? Gateway { get; set; }
        public string? Dns { get; set; }
        public List<string> IpTestRange { get; set; } = new();
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
        public List<Unidade> Unidades { get; set; } = new();
        public List<Setor> Setores { get; set; } = new();
        public List<UsuarioFinal> UsuariosFinais { get; set; } = new();
    }

    public class LegacyInstaller
    {
        public string? Nome { get; set; }
        public string? Path { get; set; }
        public string? Tipo { get; set; }
        public string? Argumentos { get; set; }
    }
    
    public class VpnConfig
    {
        public string? InstallerPath { get; set; }
        public string? ConfigFilePath { get; set; }
    }

    public class PerfilSoftware
    {
        public string? NomePerfil { get; set; }
        public List<string> WingetPackages { get; set; } = new();
        public List<LegacyInstaller> LegacyInstallers { get; set; } = new();
        public List<string> Printers { get; set; } = new();
        public Dictionary<string, string>? HostsFile { get; set; }
        public VpnConfig? VpnConfig { get; set; }
        public List<string> LocalGroups { get; set; } = new();
    }
}
