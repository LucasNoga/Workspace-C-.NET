namespace HylaNOTIFY
{
    partial class HylaNotify
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HylaNotify));
            this.icone = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.SuspendLayout();
            // 
            // icone
            // 
            this.icone.ContextMenuStrip = this.contextMenuStrip;
            this.icone.Icon = ((System.Drawing.Icon)(resources.GetObject("icone.Icon")));
            this.icone.Text = "HylaNotify";
            this.icone.Visible = true;
            this.icone.DoubleClick += new System.EventHandler(this.icone_DoubleClick);
            this.icone.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.icone_MouseDoubleClick);
            this.icone.MouseMove += new System.Windows.Forms.MouseEventHandler(this.icone_MouseMove);
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Name = "contextMenuStrip1";
            this.contextMenuStrip.Size = new System.Drawing.Size(61, 4);
            this.contextMenuStrip.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.contextMenuStrip_ItemClicked);
            // 
            // HylaNotify
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(116, 0);
            this.Location = new System.Drawing.Point(1000, 0);
            this.Name = "HylaNotify";
            this.Text = "Logs";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon icone;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;

    }
}

