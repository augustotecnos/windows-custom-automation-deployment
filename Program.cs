namespace INSTALADOR_SOFTWARE_SE
{
    /// <summary>
    /// A classe principal que contém o ponto de entrada da aplicação.
    /// Esta classe é estática pois não precisa ser instanciada; ela apenas serve como um container para o método Main.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// O ponto de entrada principal para a aplicação.
        /// Este é o primeiro método a ser executado quando o .exe é iniciado.
        /// </summary>
        [STAThread] // O atributo STAThread é obrigatório para aplicações Windows Forms. Ele define o modelo de threading do COM para "Single-Threaded Apartment".
        static void Main()
        {
            // ApplicationConfiguration.Initialize() é um método de inicialização para aplicações .NET modernas.
            // Ele configura definições padrão da aplicação, como o suporte a alta resolução (High DPI), fontes padrão, etc.
            // Isso garante que sua aplicação tenha uma aparência moderna e nítida em diferentes configurações de tela.
            ApplicationConfiguration.Initialize();

            // Aqui, criamos uma nova instância do nosso formulário principal.
            // É neste momento que o seu 'Form_Tela_Principal' "nasce" na memória.
            var formularioPrincipal = new Form_Tela_Principal();

            // Application.Run() inicia o loop de mensagens da aplicação e exibe o formulário especificado.
            // A aplicação permanecerá em execução, respondendo a eventos (cliques, etc.), até que este formulário seja fechado.
            Application.Run(formularioPrincipal);
        }
    }
}