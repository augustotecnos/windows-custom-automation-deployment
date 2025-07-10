using INSTALADOR_SOFTWARE_SE.Fases;
using INSTALADOR_SOFTWARE_SE.Helpers;
using INSTALADOR_SOFTWARE_SE.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

 namespace INSTALADOR_SOFTWARE_SE
{
    /// <summary>
    /// Tela principal do "Maestro C#". Responsável por exibir UI ao técnico
    /// e orquestrar todas as fases de provisionamento.
    /// </summary>
    public partial class Form_Tela_Principal : Form
    {
        // ------------------------------------------------------------------
        // Constantes
        // ------------------------------------------------------------------
        //private const string CaminhoDeploymentShare     = @"\\seu-servidor\DeploymentShare$";
        //private const string CaminhoScriptIdentidade    = @"\\seu-servidor\DeploymentShare$\Scripts\Manage-ComputerIdentity.ps1";

        // ------------------------------------------------------------------
        // Campos
        // ------------------------------------------------------------------
        private readonly GerenciadorDeEstado _gerenciadorDeEstado; 
        private readonly GerenciadorDeRede   _gerenciadorDeRede;
        private readonly BackgroundWorker    _tarefa;

        // ------------------------------------------------------------------
        // Construtor / Load
        // ------------------------------------------------------------------
        public Form_Tela_Principal()
        {
            InitializeComponent();

            _gerenciadorDeEstado = new GerenciadorDeEstado();
            _gerenciadorDeRede   = new GerenciadorDeRede();

            _tarefa = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _tarefa.DoWork             += Tarefa_DoWork;
            _tarefa.ProgressChanged    += Tarefa_ProgressChanged;
            _tarefa.RunWorkerCompleted += Tarefa_RunWorkerCompleted;
        }

        private void Form_Tela_Principal_Load(object sender, EventArgs e)
        {
            pnlConfiguracao.Visible = false;
            pnlAlerta.Visible       = false;
            pnlProgresso.Visible    = true;
            lblStatusProgresso.Text = "Inicializando e verificando estado...";

            var estado = _gerenciadorDeEstado.CarregarEstadoCompleto();
            if (estado != null)
            {
                ContinuarProcesso(estado);
            }
            else
            {
                Task.Run(() => IniciarNovoProcesso());
            }
        }

        // ------------------------------------------------------------------
        // Fase 1 – Preparação da UI / Rede
        // ------------------------------------------------------------------
        private void IniciarNovoProcesso()
        {
//            AtualizarStatusProgresso("Configurando interface de rede...");
//            if (!_gerenciadorDeRede.AtribuirIpDisponivel(out var ip))
//            {
//                AtivarModoDeAlerta("FALHA DE REDE", "Não foi possível atribuir um IP válido. Verifique o cabo e o arquivo 'network_config.json'.");
//                return;
//            }
//
//            AtualizarStatusProgresso($"IP {ip} atribuído. Verificando acesso ao servidor...");
//            if (!Directory.Exists(AppConfig.DeploymentSharePath))
//            {
//                AtivarModoDeAlerta("FALHA DE CONEXÃO", $"Não foi possível acessar '{AppConfig.DeploymentSharePath}'.");
//                return;
//            }

            AtualizarStatusProgresso("Conectado ao servidor. Carregando opções...");
            var masterConfig = CarregarConfiguracoesDaRede();
            if (masterConfig == null)
            {
                AtivarModoDeAlerta("FALHA DE CONFIGURAÇÃO", "Arquivo 'master_config.json' não encontrado ou inválido.");
                return;
            }

            Invoke((MethodInvoker)delegate
            {
                PopularControlesDaUI(masterConfig);
                pnlProgresso.Visible    = false;
                pnlConfiguracao.Visible = true;
            });
        }

        private MasterConfig? CarregarConfiguracoesDaRede()
        {
            try
            {
                var file = Path.Combine(AppConfig.DeploymentSharePath, "Config", "master_config.json");
                return JsonSerializer.Deserialize<MasterConfig>(File.ReadAllText(file));
            }
            catch (Exception ex)
            {
                AtualizarStatusProgresso($"ERRO master_config: {ex.Message}");
                return null;
            }
        }

        private void PopularControlesDaUI(MasterConfig cfg)
        {
            cmbUnidade.DataSource    = cfg.Unidades;
            cmbUnidade.DisplayMember = "NomeExibicao";
            cmbUnidade.ValueMember   = "Id";

            cmbSetor.DataSource    = cfg.Setores;
            cmbSetor.DisplayMember = "NomeExibicao";
            cmbSetor.ValueMember   = "Id";

            cmbUsuarioFinal.DataSource    = cfg.UsuariosFinais;
            cmbUsuarioFinal.DisplayMember = "NomeExibicao";
            cmbUsuarioFinal.ValueMember   = "LoginName";

            cmbUnidade.SelectedIndex      = -1;
            cmbSetor.SelectedIndex        = -1;
            cmbUsuarioFinal.SelectedIndex = -1;
        }

        // ------------------------------------------------------------------
        // UI – Eventos
        // ------------------------------------------------------------------
        private void rbNomenclatura_CheckedChanged(object sender, EventArgs e)
        {
           // Converte o sender para RadioButton para saber qual foi ativado
            var rb = sender as RadioButton;
            if (rb == null || !rb.Checked) return;

            // Habilita/Desabilita a opção de manter nome existente
            bool manterExistente = (rb == rbNomeExistente);
            lblNomeExistente.Enabled = manterExistente;
            cmbNomesExistentes.Enabled = manterExistente;
            if (manterExistente)
            {
                CarregarNomesDeMaquinasExistentes();
            }
            else
            {
                cmbNomesExistentes.DataSource = null;
            }

            // Habilita/Desabilita a opção de digitar nome manual
            bool manual = (rb == rbNomeManual);
            lblNomeManual.Enabled = manual;
            txtNomeManual.Enabled = manual;
            if (manual)
            {
                txtNomeManual.Focus();
            }
            else
            {
                txtNomeManual.Text = string.Empty;
            }
        }

        private void CarregarNomesDeMaquinasExistentes()
        {
            if (cmbUnidade.SelectedValue == null)
            {
                MessageBox.Show("Selecione primeiro um Perfil / Unidade.", "Ação Necessária", MessageBoxButtons.OK, MessageBoxIcon.Information);
                rbNomeNovo.Checked = true;
                return;
            }

            lblNomeExistente.Text = "Carregando lista de máquinas...";
            cmbNomesExistentes.DataSource = null;
            Application.DoEvents();

            try
            {
                var perfilId = cmbUnidade.SelectedValue.ToString();

                // 1. Defina o caminho para o arquivo de credencial
                string credentialFilePath = Path.Combine(AppConfig.DeploymentSharePath, "Config", "secure_credential.xml");

                var psi = new ProcessStartInfo("powershell")
                {
                    Arguments = $"-ExecutionPolicy Bypass -File \"{AppConfig.CaminhoScriptIdentidade}\" -Mode GetExistingNames -PerfilId \"{perfilId}\" -CredentialFilePath \"{credentialFilePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                var nomes = p!.StandardOutput.ReadToEnd()
                            .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                            .ToList();
                p.WaitForExit();
                cmbNomesExistentes.DataSource = nomes;
                lblNomeExistente.Text = "Selecione a máquina:";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao buscar máquinas: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblNomeExistente.Text = "Falha ao carregar.";
            }
        }

        private void btnIniciarConfiguracao_Click(object sender, EventArgs e)
        {
            
                    // --- VALIDAÇÃO ---
            if (cmbUnidade.SelectedValue == null ||
                cmbSetor.SelectedValue == null ||
                cmbUsuarioFinal.SelectedValue == null)
            {
                MessageBox.Show("Preencha todas as opções de Perfil, Setor e Usuário.", "Campos obrigatórios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        
            if (rbNomeExistente.Checked && cmbNomesExistentes.SelectedValue == null)
            {
                MessageBox.Show("Se você escolheu reformatar, precisa selecionar uma máquina da lista.", "Campos obrigatórios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        
            // **NOVA VALIDAÇÃO**
            if (rbNomeManual.Checked && string.IsNullOrWhiteSpace(txtNomeManual.Text))
            {
                MessageBox.Show("Se você escolheu digitar o nome, o campo não pode estar vazio.", "Campos obrigatórios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        
            // --- COLETA DE DADOS ---
            string modoNomenclatura = "Novo"; // Padrão
            if (rbNomeExistente.Checked) modoNomenclatura = "ManterExistente";
            if (rbNomeManual.Checked) modoNomenclatura = "Manual"; // **NOVO MODO**
        
            var estadoInicial = new Dictionary<string, string>
            {
                ["PerfilId"]                  = cmbUnidade.SelectedValue.ToString()!,
                ["SetorId"]                   = cmbSetor.SelectedValue.ToString()!,
                ["UsuarioFinal"]              = cmbUsuarioFinal.SelectedValue.ToString()!,
                ["ExecutarUpdates"]           = chkExecutarUpdates.Checked.ToString(),
                ["ModoNomenclatura"]          = modoNomenclatura, // Atualizado
                ["NomeComputadorSelecionado"] = rbNomeExistente.Checked && cmbNomesExistentes.SelectedValue != null ? cmbNomesExistentes.SelectedValue.ToString()! : string.Empty,
                ["NomeComputadorManual"]      = txtNomeManual.Text, // **NOVO ESTADO**
                ["EtapaAtual"]                = "Iniciar_Nomenclatura"
            };
        
            _gerenciadorDeEstado.SalvarEstadoCompleto(estadoInicial);
            pnlConfiguracao.Visible = false;
            ContinuarProcesso(estadoInicial);

        }

        // ------------------------------------------------------------------
        // Orquestração – BackgroundWorker
        // ------------------------------------------------------------------
        private void ContinuarProcesso(Dictionary<string,string> estado)
        {
            pnlProgresso.Visible = true;
            AtualizarStatusProgresso($"Iniciando etapa: {estado["EtapaAtual"]}");
            if (!_tarefa.IsBusy) _tarefa.RunWorkerAsync(estado);
        }

        private void Tarefa_DoWork(object? sender, DoWorkEventArgs e)
        {
            var estado = (Dictionary<string,string>)e.Argument!;
            bool sucesso = false;
            Action<string> log = msg => _tarefa.ReportProgress(0, msg);

            try
            {
                switch (estado["EtapaAtual"])
                {
                    case "Iniciar_Nomenclatura":
                        sucesso = new Fase2_Identidade(_gerenciadorDeEstado, estado, log).Executar();
                        break;

                    case "PósRename_IngressarDominio":
                        sucesso = new Fase3_Dominio(_gerenciadorDeEstado, estado, log).Executar();
                        break;

                    case "Iniciar_WindowsUpdate":
                        sucesso = new Fase4_Update(_gerenciadorDeEstado, estado, log).Executar();
                        break;

                    case "Iniciar_InstalacaoSoftware":
                        sucesso = new Fase5_SoftwareE_Drivers(_gerenciadorDeEstado, estado, log).Executar();
                        break;

                    case "Iniciar_ConfigsFinais":
                        sucesso = new Fase6_ConfigsFinais(_gerenciadorDeEstado, estado, log).Executar();
                        break;

                    case "Iniciar_Limpeza":
                        sucesso = new Fase7_Limpeza(_gerenciadorDeEstado, log).Executar();
                        break;
                }
            }
            catch (Exception ex)
            {
                log($"ERRO FATAL NO WORKER: {ex.Message}");
            }
            e.Result = sucesso;
        }

        private void Tarefa_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is string msg) AtualizarStatusProgresso(msg);
        }

        private void Tarefa_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                AtivarModoDeAlerta("ERRO INESPERADO", e.Error.Message);
            }
            else if (e.Cancelled)
            {
                AtivarModoDeAlerta("CANCELADO", "A operação foi cancelada.");
            }
            else if (e.Result is bool ok && !ok)
            {
                AtivarModoDeAlerta("FALHA", "A etapa atual não foi concluída com sucesso.");
            }
        }

        // ------------------------------------------------------------------
        // UI helpers
        // ------------------------------------------------------------------
        private void AtualizarStatusProgresso(string status)
        {
            if (InvokeRequired) { Invoke((MethodInvoker)(() => AtualizarStatusProgresso(status))); return; }
            lblStatusProgresso.Text = status;
            txtLogCompleto.AppendText($"[{DateTime.Now:HH:mm:ss}] {status}{Environment.NewLine}");
        }

        private void AtivarModoDeAlerta(string titulo, string mensagem)
        {
            if (InvokeRequired) { Invoke((MethodInvoker)(() => AtivarModoDeAlerta(titulo, mensagem))); return; }

            pnlConfiguracao.Visible = false;
            pnlProgresso.Visible    = false;
            pnlAlerta.Visible       = true;
            lblTituloAlerta.Text    = titulo;
            lblMensagemAlerta.Text  = mensagem;
            System.Media.SystemSounds.Exclamation.Play();
            timerAlerta.Start();
        }

        private void timerAlerta_Tick(object sender, EventArgs e)
        {
            timerAlerta.Stop();
            lblTituloAlerta.Text   = "TENTANDO NOVAMENTE";
            lblMensagemAlerta.Text = "Tentando restabelecer a conexão...";
            Application.DoEvents();
            Task.Run(() => IniciarNovoProcesso());
        }
    }
}
