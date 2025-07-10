namespace INSTALADOR_SOFTWARE_SE
{
    /// <summary>
    /// Configurações globais da aplicação.
    /// Ajuste a constante DeploymentSharePath para apontar para o
    /// compartilhamento de rede contendo os scripts e arquivos de suporte.
    /// </summary>
    public static class AppConfig
    {
        //public const string DeploymentSharePath = @"\\seu-servidor\DeploymentShare$";
        public const string DeploymentSharePath = @"\\10.113.11.4\DeploymentShare$";
        public const string CaminhoScriptIdentidade    = @"\\10.113.11.4\DeploymentShare$\Scripts\Manage-ComputerIdentity.ps1";
        
    }
}
