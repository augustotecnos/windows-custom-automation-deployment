namespace INSTALADOR_SOFTWARE_SE
{
    partial class Form_Tela_Principal
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pnlConfiguracao = new System.Windows.Forms.Panel();
            this.gbNomenclatura = new System.Windows.Forms.GroupBox();
            this.lblNomeManual = new System.Windows.Forms.Label();
            this.txtNomeManual = new System.Windows.Forms.TextBox();
            this.rbNomeManual = new System.Windows.Forms.RadioButton();
            this.cmbNomesExistentes = new System.Windows.Forms.ComboBox();
            this.lblNomeExistente = new System.Windows.Forms.Label();
            this.rbNomeExistente = new System.Windows.Forms.RadioButton();
            this.rbNomeNovo = new System.Windows.Forms.RadioButton();
            this.btnIniciarConfiguracao = new System.Windows.Forms.Button();
            this.chkExecutarUpdates = new System.Windows.Forms.CheckBox();
            this.cmbUsuarioFinal = new System.Windows.Forms.ComboBox();
            this.lblUsuarioFinal = new System.Windows.Forms.Label();
            this.cmbSetor = new System.Windows.Forms.ComboBox();
            this.lblSetor = new System.Windows.Forms.Label();
            this.cmbUnidade = new System.Windows.Forms.ComboBox();
            this.lblUnidade = new System.Windows.Forms.Label();
            this.lblTituloConfig = new System.Windows.Forms.Label();
            this.pnlProgresso = new System.Windows.Forms.Panel();
            this.txtLogCompleto = new System.Windows.Forms.TextBox();
            this.lblStatusProgresso = new System.Windows.Forms.Label();
            this.pictureBoxProgresso = new System.Windows.Forms.PictureBox();
            this.pnlAlerta = new System.Windows.Forms.Panel();
            this.lblMensagemAlerta = new System.Windows.Forms.Label();
            this.lblTituloAlerta = new System.Windows.Forms.Label();
            this.timerAlerta = new System.Windows.Forms.Timer(this.components);
            this.pnlConfiguracao.SuspendLayout();
            this.gbNomenclatura.SuspendLayout();
            this.pnlProgresso.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxProgresso)).BeginInit();
            this.pnlAlerta.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlConfiguracao
            // 
            this.pnlConfiguracao.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pnlConfiguracao.Controls.Add(this.gbNomenclatura);
            this.pnlConfiguracao.Controls.Add(this.btnIniciarConfiguracao);
            this.pnlConfiguracao.Controls.Add(this.chkExecutarUpdates);
            this.pnlConfiguracao.Controls.Add(this.cmbUsuarioFinal);
            this.pnlConfiguracao.Controls.Add(this.lblUsuarioFinal);
            this.pnlConfiguracao.Controls.Add(this.cmbSetor);
            this.pnlConfiguracao.Controls.Add(this.lblSetor);
            this.pnlConfiguracao.Controls.Add(this.cmbUnidade);
            this.pnlConfiguracao.Controls.Add(this.lblUnidade);
            this.pnlConfiguracao.Controls.Add(this.lblTituloConfig);
            this.pnlConfiguracao.Location = new System.Drawing.Point(12, 12);
            this.pnlConfiguracao.Name = "pnlConfiguracao";
            this.pnlConfiguracao.Size = new System.Drawing.Size(984, 537);
            this.pnlConfiguracao.TabIndex = 0;
            // 
            // gbNomenclatura
            // 
            this.gbNomenclatura.Controls.Add(this.lblNomeManual);
            this.gbNomenclatura.Controls.Add(this.txtNomeManual);
            this.gbNomenclatura.Controls.Add(this.rbNomeManual);
            this.gbNomenclatura.Controls.Add(this.cmbNomesExistentes);
            this.gbNomenclatura.Controls.Add(this.lblNomeExistente);
            this.gbNomenclatura.Controls.Add(this.rbNomeExistente);
            this.gbNomenclatura.Controls.Add(this.rbNomeNovo);
            this.gbNomenclatura.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.gbNomenclatura.Location = new System.Drawing.Point(52, 275);
            this.gbNomenclatura.Name = "gbNomenclatura";
            this.gbNomenclatura.Size = new System.Drawing.Size(880, 150);
            this.gbNomenclatura.TabIndex = 10;
            this.gbNomenclatura.TabStop = false;
            this.gbNomenclatura.Text = "Nome da Máquina";
            // 
            // lblNomeManual
            // 
            this.lblNomeManual.AutoSize = true;
            this.lblNomeManual.Enabled = false;
            this.lblNomeManual.Location = new System.Drawing.Point(360, 104);
            this.lblNomeManual.Name = "lblNomeManual";
            this.lblNomeManual.Size = new System.Drawing.Size(114, 21);
            this.lblNomeManual.TabIndex = 6;
            this.lblNomeManual.Text = "Nome Desejado:";
            // 
            // txtNomeManual
            // 
            this.txtNomeManual.Enabled = false;
            this.txtNomeManual.Location = new System.Drawing.Point(480, 101);
            this.txtNomeManual.Name = "txtNomeManual";
            this.txtNomeManual.Size = new System.Drawing.Size(380, 29);
            this.txtNomeManual.TabIndex = 5;
            // 
            // rbNomeManual
            // 
            this.rbNomeManual.AutoSize = true;
            this.rbNomeManual.Location = new System.Drawing.Point(20, 102);
            this.rbNomeManual.Name = "rbNomeManual";
            this.rbNomeManual.Size = new System.Drawing.Size(228, 25);
            this.rbNomeManual.TabIndex = 4;
            this.rbNomeManual.Text = "Digitar um nome manualmente";
            this.rbNomeManual.UseVisualStyleBackColor = true;
            this.rbNomeManual.CheckedChanged += new System.EventHandler(this.rbNomenclatura_CheckedChanged);
            // 
            // cmbNomesExistentes
            // 
            this.cmbNomesExistentes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbNomesExistentes.Enabled = false;
            this.cmbNomesExistentes.FormattingEnabled = true;
            this.cmbNomesExistentes.Location = new System.Drawing.Point(480, 70);
            this.cmbNomesExistentes.Name = "cmbNomesExistentes";
            this.cmbNomesExistentes.Size = new System.Drawing.Size(380, 29);
            this.cmbNomesExistentes.TabIndex = 3;
            // 
            // lblNomeExistente
            // 
            this.lblNomeExistente.AutoSize = true;
            this.lblNomeExistente.Enabled = false;
            this.lblNomeExistente.Location = new System.Drawing.Point(476, 46);
            this.lblNomeExistente.Name = "lblNomeExistente";
            this.lblNomeExistente.Size = new System.Drawing.Size(271, 21);
            this.lblNomeExistente.TabIndex = 2;
            this.lblNomeExistente.Text = "Selecione a máquina a ser formatada:";
            // 
            // rbNomeExistente
            // 
            this.rbNomeExistente.AutoSize = true;
            this.rbNomeExistente.Location = new System.Drawing.Point(20, 71);
            this.rbNomeExistente.Name = "rbNomeExistente";
            this.rbNomeExistente.Size = new System.Drawing.Size(325, 25);
            this.rbNomeExistente.TabIndex = 1;
            this.rbNomeExistente.Text = "Reformatar uma máquina (manter o nome)";
            this.rbNomeExistente.UseVisualStyleBackColor = true;
            this.rbNomeExistente.CheckedChanged += new System.EventHandler(this.rbNomenclatura_CheckedChanged);
            // 
            // rbNomeNovo
            // 
            this.rbNomeNovo.AutoSize = true;
            this.rbNomeNovo.Checked = true;
            this.rbNomeNovo.Location = new System.Drawing.Point(20, 40);
            this.rbNomeNovo.Name = "rbNomeNovo";
            this.rbNomeNovo.Size = new System.Drawing.Size(287, 25);
            this.rbNomeNovo.TabIndex = 0;
            this.rbNomeNovo.TabStop = true;
            this.rbNomeNovo.Text = "Atribuir um novo nome para máquina";
            this.rbNomeNovo.UseVisualStyleBackColor = true;
            this.rbNomeNovo.CheckedChanged += new System.EventHandler(this.rbNomenclatura_CheckedChanged);
            // 
            // btnIniciarConfiguracao
            // 
            this.btnIniciarConfiguracao.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btnIniciarConfiguracao.Location = new System.Drawing.Point(365, 470);
            this.btnIniciarConfiguracao.Name = "btnIniciarConfiguracao";
            this.btnIniciarConfiguracao.Size = new System.Drawing.Size(255, 55);
            this.btnIniciarConfiguracao.TabIndex = 9;
            this.btnIniciarConfiguracao.Text = "INICIAR CONFIGURAÇÃO";
            this.btnIniciarConfiguracao.UseVisualStyleBackColor = true;
            this.btnIniciarConfiguracao.Click += new System.EventHandler(this.btnIniciarConfiguracao_Click);
            // 
            // chkExecutarUpdates
            // 
            this.chkExecutarUpdates.AutoSize = true;
            this.chkExecutarUpdates.Checked = true;
            this.chkExecutarUpdates.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkExecutarUpdates.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.chkExecutarUpdates.Location = new System.Drawing.Point(52, 431);
            this.chkExecutarUpdates.Name = "chkExecutarUpdates";
            this.chkExecutarUpdates.Size = new System.Drawing.Size(439, 25);
            this.chkExecutarUpdates.TabIndex = 8;
            this.chkExecutarUpdates.Text = "Executar busca completa por atualizações (Windows Update)";
            this.chkExecutarUpdates.UseVisualStyleBackColor = true;
            // 
            // cmbUsuarioFinal
            // 
            this.cmbUsuarioFinal.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbUsuarioFinal.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.cmbUsuarioFinal.FormattingEnabled = true;
            this.cmbUsuarioFinal.Location = new System.Drawing.Point(52, 230);
            this.cmbUsuarioFinal.Name = "cmbUsuarioFinal";
            this.cmbUsuarioFinal.Size = new System.Drawing.Size(420, 29);
            this.cmbUsuarioFinal.TabIndex = 7;
            // 
            // lblUsuarioFinal
            // 
            this.lblUsuarioFinal.AutoSize = true;
            this.lblUsuarioFinal.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblUsuarioFinal.Location = new System.Drawing.Point(48, 206);
            this.lblUsuarioFinal.Name = "lblUsuarioFinal";
            this.lblUsuarioFinal.Size = new System.Drawing.Size(99, 21);
            this.lblUsuarioFinal.TabIndex = 6;
            this.lblUsuarioFinal.Text = "Usuário Final";
            // 
            // cmbSetor
            // 
            this.cmbSetor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSetor.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.cmbSetor.FormattingEnabled = true;
            this.cmbSetor.Location = new System.Drawing.Point(52, 164);
            this.cmbSetor.Name = "cmbSetor";
            this.cmbSetor.Size = new System.Drawing.Size(420, 29);
            this.cmbSetor.TabIndex = 5;
            // 
            // lblSetor
            // 
            this.lblSetor.AutoSize = true;
            this.lblSetor.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblSetor.Location = new System.Drawing.Point(48, 140);
            this.lblSetor.Name = "lblSetor";
            this.lblSetor.Size = new System.Drawing.Size(47, 21);
            this.lblSetor.TabIndex = 4;
            this.lblSetor.Text = "Setor";
            // 
            // cmbUnidade
            // 
            this.cmbUnidade.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbUnidade.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.cmbUnidade.FormattingEnabled = true;
            this.cmbUnidade.Location = new System.Drawing.Point(52, 98);
            this.cmbUnidade.Name = "cmbUnidade";
            this.cmbUnidade.Size = new System.Drawing.Size(420, 29);
            this.cmbUnidade.TabIndex = 3;
            // 
            // lblUnidade
            // 
            this.lblUnidade.AutoSize = true;
            this.lblUnidade.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblUnidade.Location = new System.Drawing.Point(48, 74);
            this.lblUnidade.Name = "lblUnidade";
            this.lblUnidade.Size = new System.Drawing.Size(113, 21);
            this.lblUnidade.TabIndex = 2;
            this.lblUnidade.Text = "Perfil / Unidade";
            // 
            // lblTituloConfig
            // 
            this.lblTituloConfig.AutoSize = true;
            this.lblTituloConfig.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblTituloConfig.Location = new System.Drawing.Point(45, 20);
            this.lblTituloConfig.Name = "lblTituloConfig";
            this.lblTituloConfig.Size = new System.Drawing.Size(499, 37);
            this.lblTituloConfig.TabIndex = 1;
            this.lblTituloConfig.Text = "Configuração de Provisionamento";
            // 
            // pnlProgresso
            // 
            this.pnlProgresso.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.pnlProgresso.Controls.Add(this.txtLogCompleto);
            this.pnlProgresso.Controls.Add(this.lblStatusProgresso);
            this.pnlProgresso.Controls.Add(this.pictureBoxProgresso);
            this.pnlProgresso.Location = new System.Drawing.Point(0, 0);
            this.pnlProgresso.Name = "pnlProgresso";
            this.pnlProgresso.Size = new System.Drawing.Size(1008, 561);
            this.pnlProgresso.TabIndex = 11;
            // 
            // txtLogCompleto
            // 
            this.txtLogCompleto.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLogCompleto.BackColor = System.Drawing.Color.Black;
            this.txtLogCompleto.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLogCompleto.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.txtLogCompleto.Location = new System.Drawing.Point(12, 180);
            this.txtLogCompleto.Multiline = true;
            this.txtLogCompleto.Name = "txtLogCompleto";
            this.txtLogCompleto.ReadOnly = true;
            this.txtLogCompleto.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLogCompleto.Size = new System.Drawing.Size(984, 369);
            this.txtLogCompleto.TabIndex = 2;
            // 
            // lblStatusProgresso
            // 
            this.lblStatusProgresso.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatusProgresso.Font = new System.Drawing.Font("Segoe UI", 16F);
            this.lblStatusProgresso.ForeColor = System.Drawing.Color.White;
            this.lblStatusProgresso.Location = new System.Drawing.Point(12, 130);
            this.lblStatusProgresso.Name = "lblStatusProgresso";
            this.lblStatusProgresso.Size = new System.Drawing.Size(984, 37);
            this.lblStatusProgresso.TabIndex = 1;
            this.lblStatusProgresso.Text = "Iniciando...";
            this.lblStatusProgresso.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pictureBoxProgresso
            // 
            this.pictureBoxProgresso.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.pictureBoxProgresso.Location = new System.Drawing.Point(454, 32);
            this.pictureBoxProgresso.Name = "pictureBoxProgresso";
            this.pictureBoxProgresso.Size = new System.Drawing.Size(100, 80);
            this.pictureBoxProgresso.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxProgresso.TabIndex = 0;
            this.pictureBoxProgresso.TabStop = false;
            // 
            // pnlAlerta
            // 
            this.pnlAlerta.BackColor = System.Drawing.Color.DarkRed;
            this.pnlAlerta.Controls.Add(this.lblMensagemAlerta);
            this.pnlAlerta.Controls.Add(this.lblTituloAlerta);
            this.pnlAlerta.Location = new System.Drawing.Point(0, 0);
            this.pnlAlerta.Name = "pnlAlerta";
            this.pnlAlerta.Size = new System.Drawing.Size(1008, 561);
            this.pnlAlerta.TabIndex = 12;
            // 
            // lblMensagemAlerta
            // 
            this.lblMensagemAlerta.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMensagemAlerta.Font = new System.Drawing.Font("Segoe UI", 18F);
            this.lblMensagemAlerta.ForeColor = System.Drawing.Color.White;
            this.lblMensagemAlerta.Location = new System.Drawing.Point(54, 281);
            this.lblMensagemAlerta.Name = "lblMensagemAlerta";
            this.lblMensagemAlerta.Size = new System.Drawing.Size(900, 100);
            this.lblMensagemAlerta.TabIndex = 1;
            this.lblMensagemAlerta.Text = "Mensagem de erro detalhada.";
            this.lblMensagemAlerta.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblTituloAlerta
            // 
            this.lblTituloAlerta.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTituloAlerta.Font = new System.Drawing.Font("Segoe UI", 36F, System.Drawing.FontStyle.Bold);
            this.lblTituloAlerta.ForeColor = System.Drawing.Color.White;
            this.lblTituloAlerta.Location = new System.Drawing.Point(54, 181);
            this.lblTituloAlerta.Name = "lblTituloAlerta";
            this.lblTituloAlerta.Size = new System.Drawing.Size(900, 80);
            this.lblTituloAlerta.TabIndex = 0;
            this.lblTituloAlerta.Text = "TÍTULO DO ALERTA";
            this.lblTituloAlerta.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // timerAlerta
            // 
            this.timerAlerta.Interval = 10000;
            this.timerAlerta.Tick += new System.EventHandler(this.timerAlerta_Tick);
            // 
            // Form_Tela_Principal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 561);
            this.Controls.Add(this.pnlConfiguracao);
            this.Controls.Add(this.pnlAlerta);
            this.Controls.Add(this.pnlProgresso);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form_Tela_Principal";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SE Promotora - Assistente de Provisionamento";
            this.Load += new System.EventHandler(this.Form_Tela_Principal_Load);
            this.pnlConfiguracao.ResumeLayout(false);
            this.pnlConfiguracao.PerformLayout();
            this.gbNomenclatura.ResumeLayout(false);
            this.gbNomenclatura.PerformLayout();
            this.pnlProgresso.ResumeLayout(false);
            this.pnlProgresso.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxProgresso)).EndInit();
            this.pnlAlerta.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlConfiguracao;
        private System.Windows.Forms.Label lblTituloConfig;
        private System.Windows.Forms.Label lblUnidade;
        private System.Windows.Forms.ComboBox cmbUnidade;
        private System.Windows.Forms.Label lblSetor;
        private System.Windows.Forms.ComboBox cmbSetor;
        private System.Windows.Forms.Label lblUsuarioFinal;
        private System.Windows.Forms.ComboBox cmbUsuarioFinal;
        private System.Windows.Forms.CheckBox chkExecutarUpdates;
        private System.Windows.Forms.Button btnIniciarConfiguracao;
        private System.Windows.Forms.GroupBox gbNomenclatura;
        private System.Windows.Forms.RadioButton rbNomeNovo;
        private System.Windows.Forms.RadioButton rbNomeExistente;
        private System.Windows.Forms.ComboBox cmbNomesExistentes;
        private System.Windows.Forms.Label lblNomeExistente;
        private System.Windows.Forms.Panel pnlProgresso;
        private System.Windows.Forms.PictureBox pictureBoxProgresso;
        private System.Windows.Forms.Label lblStatusProgresso;
        private System.Windows.Forms.TextBox txtLogCompleto;
        private System.Windows.Forms.Panel pnlAlerta;
        private System.Windows.Forms.Label lblTituloAlerta;
        private System.Windows.Forms.Label lblMensagemAlerta;
        private System.Windows.Forms.Timer timerAlerta;
        private System.Windows.Forms.Label lblNomeManual;
        private System.Windows.Forms.TextBox txtNomeManual;
        private System.Windows.Forms.RadioButton rbNomeManual;
    }
}