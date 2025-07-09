using System.ComponentModel;
using System.Windows.Forms;
using System.Text.Json;
// ADICIONE ESTAS DIRETIVAS PARA QUE O FORMULÁRIO ENCONTRE AS OUTRAS CLASSES
using INSTALADOR_SOFTWARE_SE.Models;
using INSTALADOR_SOFTWARE_SE.Helpers;
using INSTALADOR_SOFTWARE_SE.Fases;

namespace INSTALADOR_SOFTWARE_SE
{
    public partial class Form_Tela_Principal : Form
    {
        // --- PROPRIEDADES E CONSTANTES ---

        private readonly GerenciadorDeEstado _gerenciadorDeEstado;
        private readonly GerenciadorDeRede _gerenciadorDeRede;
        private readonly BackgroundWorker _tarefa;

        private const string CaminhoDeploymentShare = @"\\seu-servidor\DeploymentShare$";
        private const string CaminhoScriptIdentidade = @"\\seu-servidor\DeploymentShare$\Scripts\Manage-ComputerIdentity.ps1";

        // --- CONSTRUTOR E LOAD DO FORMULÁRIO ---

        public Form_Tela_Principal()
        {
            InitializeComponent();
            _gerenciadorDeEstado = new GerenciadorDeEstado();
            _gerenciadorDeRede = new GerenciadorDeRede();
            
            _tarefa = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _tarefa.DoWork += Tarefa_DoWork;
            _tarefa.ProgressChanged += Tarefa_ProgressChanged;
            _tarefa.RunWorkerCompleted += Tarefa_RunWorkerCompleted;
        }

        private void Form_Tela_Principal_Load(object sender, EventArgs e)
        {
            pnlConfiguracao.Visible = false;
            pnlAlerta.Visible = false;
            pnlProgresso.Visible = true;
            lblStatusProgresso.Text = "Inicializando e verificando estado...";

            var estadoAtual = _gerenciadorDeEstado.CarregarEstadoCompleto();
            if (estadoAtual != null)
            {
                ContinuarProcesso(estadoAtual);
            }
            else
            {
                Task.Run(() => IniciarNovoProcesso());
            }
        }

        // --- FLUXO DE INICIALIZAÇÃO E CONFIGURAÇÃO (FASE 1) ---

        private void IniciarNovoProcesso()
        {
            AtualizarStatusProgresso("Configurando interface de rede...");
            if (!_gerenciadorDeRede.AtribuirIpDisponivel(out string ipAtribuido))
            {
                AtivarModoDeAlerta("FALHA DE REDE", "Não foi possível atribuir um IP válido. Verifique o cabo de rede e o arquivo 'network_config.json' local.");
                return;
            }

            AtualizarStatusProgresso($"IP {ipAtribuido} atribuído. Verificando acesso ao servidor...");
            if (!Directory.Exists(CaminhoDeploymentShare))
            {
                AtivarModoDeAlerta("FALHA DE CONEXÃO", $"Não foi possível acessar o compartilhamento '{CaminhoDeploymentShare}'.");
                return;
            }

            AtualizarStatusProgresso("Conectado ao servidor. Carregando opções...");
            var masterConfig = CarregarConfiguracoesDaRede();
            if (masterConfig == null)
            {
                AtivarModoDeAlerta("FALHA DE CONFIGURAÇÃO", "Arquivo 'master_config.json' não encontrado ou inválido no servidor.");
                return;
            }

            this.Invoke((MethodInvoker)delegate
            {
                PopularControlesDaUI(masterConfig);
                pnlProgresso.Visible = false;
                pnlConfiguracao.Visible = true;
            });
        }

        private MasterConfig CarregarConfiguracoesDaRede()
        {
            try
            {
                string caminhoMasterConfig = Path.Combine(CaminhoDeploymentShare, "Config", "master_config.json");
                string jsonContent = File.ReadAllText(caminhoMasterConfig);
                return JsonSerializer.Deserialize<MasterConfig>(jsonContent);
            }
            catch (Exception ex)
            {
                AtualizarStatusProgresso($"ERRO ao ler master_config.json: {ex.Message}");
                return null;
            }
        }

        private void PopularControlesDaUI(MasterConfig config)
        {
            cmbUnidade.DataSource = config.Unidades;
            cmbUnidade.DisplayMember = "NomeExibicao";
            cmbUnidade.ValueMember = "Id";
            cmbSetor.DataSource = config.Setores;
            cmbSetor.DisplayMember = "NomeExibicao";
            cmbSetor.ValueMember = "Id";
            cmbUsuarioFinal.DataSource = config.UsuariosFinais;
            cmbUsuarioFinal.DisplayMember = "NomeExibicao";
            cmbUsuarioFinal.ValueMember = "LoginName";
            
            cmbUnidade.SelectedIndex = -1;
            cmbSetor.SelectedIndex = -1;
            cmbUsuarioFinal.SelectedIndex = -1;
        }

        // --- LÓGICA DE EVENTOS DA UI ---

        private void rbNomenclatura_CheckedChanged(object sender, EventArgs e)
        {
            bool manterNome = rbNomeExistente.Checked;
            lblNomeExistente.Enabled = manterNome;
            cmbNomesExistentes.Enabled = manterNome;

            if (manterNome)
            {
                CarregarNomesDeMaquinasExistentes();
            }
            else
            {
                cmbNomesExistentes.DataSource = null;
            }
        }

        private void CarregarNomesDeMaquinasExistentes()
        {
            if (cmbUnidade.SelectedValue == null)
            {
                MessageBox.Show("Por favor, selecione primeiro um Perfil / Unidade.", "Ação Necessária", MessageBoxButtons.OK, MessageBoxIcon.Information);
                rbNomeNovo.Checked = true;
                return;
            }
            
            lblNomeExistente.Text = "Carregando lista de máquinas...";
            cmbNomesExistentes.DataSource = null;
            Application.DoEvents(); // Força a atualização da UI

            try
            {
                string perfilId = cmbUnidade.SelectedValue.ToString();
                var startInfo = new ProcessStartInfo("powershell.exe")
                {
                    Arguments = $"-ExecutionPolicy Bypass -File \"{CaminhoScriptIdentidade}\" -Mode GetExistingNames -PerfilId \"{perfilId}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    var nomes = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    cmbNomesExistentes.DataSource = nomes;
                    lblNomeExistente.Text = "Selecione a máquina a ser formatada:";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao buscar a lista de máquinas existentes: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblNomeExistente.Text = "Falha ao carregar.";
            }
        }

        private void btnIniciarConfiguracao_Click(object sender, EventArgs e)
        {
            if (cmbUnidade.SelectedValue == null || cmbSetor.SelectedValue == null || cmbUsuarioFinal.SelectedValue == null || (rbNomeExistente.Checked && cmbNomesExistentes.SelectedValue == null))
            {
                MessageBox.Show("Por favor, preencha todas as opções antes de continuar.", "Campos Obrigatórios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var estadoInicial = new Dictionary<string, string>
            {
                { "PerfilId", cmbUnidade.SelectedValue.ToString() },
                { "SetorId", cmbSetor.SelectedValue.ToString() },
                { "UsuarioFinal", cmbUsuarioFinal.SelectedValue.ToString() },
                { "ExecutarUpdates", chkExecutarUpdates.Checked.ToString() },
                { "ModoNomenclatura", rbNomeNovo.Checked ? "Novo" : "ManterExistente" },
                { "NomeComputadorSelecionado", rbNomeExistente.Checked ? cmbNomesExistentes.SelectedValue.ToString() : "" },
                { "EtapaAtual", "Iniciar_Nomenclatura" }
            };

            _gerenciadorDeEstado.SalvarEstadoCompleto(estadoInicial);
            
            pnlConfiguracao.Visible = false;
            ContinuarProcesso(estadoInicial);
        }

        // --- LÓGICA DO BACKGROUNDWORKER E FLUXO CONTÍNUO ---

        private void ContinuarProcesso(Dictionary<string, string> estado)
        {
            pnlProgresso.Visible = true;
            AtualizarStatusProgresso($"Iniciando etapa: {estado["EtapaAtual"]}");
            if (!_tarefa.IsBusy)
            {
                _tarefa.RunWorkerAsync(estado);
            }
        }

        private void Tarefa_DoWork(object? sender, DoWorkEventArgs e)
        {
            var estado = e.Argument as Dictionary<string, string>;
            bool sucessoDaFase = false;
            Action<string> logCallback = (mensagem) => _tarefa.ReportProgress(0, mensagem);

            try
            {
                // A grande máquina de estados da automação
                switch (estado["EtapaAtual"])
                {
                    case "Iniciar_Nomenclatura":
                        var fase2 = new Fase2_Identidade(_gerenciadorDeEstado, estado, logCallback);
                        sucessoDaFase = fase2.Executar();
                        break;
                    case "PósRename_IngressarDominio":
                        var fase3 = new Fase3_Dominio(_gerenciadorDeEstado, estado, logCallback);
                        sucessoDaFase = fase3.Executar();
                        break;
                    case "Iniciar_WindowsUpdate":
                        var fase4 = new Fase4_Update(_gerenciadorDeEstado, estado, logCallback);
                        sucessoDaFase = fase4.Executar();
                        break;
                    case "Iniciar_InstalacaoSoftware":
                        var fase5 = new Fase5_SoftwareE_Drivers(_gerenciadorDeEstado, estado, logCallback);
                        sucessoDaFase = fase5.Executar();
                        break;
                    case "Iniciar_ConfigsFinais":
                        var fase6 = new Fase6_ConfigsFinais(_gerenciadorDeEstado, estado, logCallback);
                        sucessoDaFase = fase6.Executar();
                        break;
                    case "Iniciar_Limpeza":
                        var fase7 = new Fase7_Limpeza(_gerenciadorDeEstado, logCallback);
                        sucessoDaFase = fase7.Executar();
                        break;
                }
            }
            catch (Exception ex)
            {
                logCallback($"ERRO FATAL NO WORKER: {ex.Message}");
                sucessoDaFase = false;
            }
            e.Result = sucessoDaFase;
        }

        private void Tarefa_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            string mensagem = e.UserState as string;
            if (mensagem != null)
            {
                AtualizarStatusProgresso(mensagem);
            }
        }

        private void Tarefa_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                AtivarModoDeAlerta("ERRO INESPERADO", $"A automação falhou com um erro: {e.Error.Message}");
            }
            else if (e.Cancelled)
            {
                AtivarModoDeAlerta("CANCELADO", "A operação foi cancelada.");
            }
            else if (e.Result is bool sucesso && !sucesso)
            {
                AtivarModoDeAlerta("FALHA NA FASE", "A etapa atual não foi concluída com sucesso. Verifique os logs para mais detalhes.");
            }
        }

        // --- MÉTODOS DE UI AUXILIARES ---

        private void AtivarModoDeAlerta(string titulo, string mensagem)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate { AtivarModoDeAlerta(titulo, mensagem); });
            }
            else
            {
                pnlConfiguracao.Visible = false;
                pnlProgresso.Visible = false;
                pnlAlerta.Visible = true;
                lblTituloAlerta.Text = titulo;
                lblMensagemAlerta.Text = mensagem;
                System.Media.SystemSounds.Exclamation.Play();
                timerAlerta.Start();
            }
        }
        
        private void AtualizarStatusProgresso(string status)
        {
             if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate { AtualizarStatusProgresso(status); });
            }
            else
            {
                lblStatusProgresso.Text = status;
                txtLogCompleto.AppendText($"[{DateTime.Now:HH:mm:ss}] {status}{Environment.NewLine}");
            }
        }

        private void timerAlerta_Tick(object sender, EventArgs e)
        {
            timerAlerta.Stop();
            AtivarModoDeAlerta("TENTANDO NOVAMENTE", "Tentando restabelecer a conexão e reiniciar o processo...");
            Application.DoEvents();
            Task.Run(() => IniciarNovoProcesso());
        }
    }
}