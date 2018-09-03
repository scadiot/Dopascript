namespace DemoGame
{
    partial class Form1
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.pictureBoxScreen = new System.Windows.Forms.PictureBox();
            this.comboBoxGames = new System.Windows.Forms.ComboBox();
            this.buttonPlay = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxScreen)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxScreen
            // 
            this.pictureBoxScreen.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxScreen.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBoxScreen.Location = new System.Drawing.Point(12, 39);
            this.pictureBoxScreen.Name = "pictureBoxScreen";
            this.pictureBoxScreen.Size = new System.Drawing.Size(553, 370);
            this.pictureBoxScreen.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxScreen.TabIndex = 0;
            this.pictureBoxScreen.TabStop = false;
            this.pictureBoxScreen.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBoxScreen_Paint);
            // 
            // comboBoxGames
            // 
            this.comboBoxGames.DisplayMember = "Name";
            this.comboBoxGames.FormattingEnabled = true;
            this.comboBoxGames.Location = new System.Drawing.Point(125, 12);
            this.comboBoxGames.Name = "comboBoxGames";
            this.comboBoxGames.Size = new System.Drawing.Size(156, 21);
            this.comboBoxGames.TabIndex = 1;
            this.comboBoxGames.ValueMember = "Name";
            // 
            // buttonPlay
            // 
            this.buttonPlay.Location = new System.Drawing.Point(287, 11);
            this.buttonPlay.Name = "buttonPlay";
            this.buttonPlay.Size = new System.Drawing.Size(75, 23);
            this.buttonPlay.TabIndex = 2;
            this.buttonPlay.Text = "Play";
            this.buttonPlay.UseVisualStyleBackColor = true;
            this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(577, 421);
            this.Controls.Add(this.buttonPlay);
            this.Controls.Add(this.comboBoxGames);
            this.Controls.Add(this.pictureBoxScreen);
            this.KeyPreview = true;
            this.Name = "Form1";
            this.Text = "Form1";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyUp);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxScreen)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBoxScreen;
        private System.Windows.Forms.ComboBox comboBoxGames;
        private System.Windows.Forms.Button buttonPlay;
    }
}

