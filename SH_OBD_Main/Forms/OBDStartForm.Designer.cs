namespace SH_OBD_Main {
    partial class OBDStartForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OBDStartForm));
            this.tblLayoutMain = new System.Windows.Forms.TableLayoutPanel();
            this.tblLayoutTop = new System.Windows.Forms.TableLayoutPanel();
            this.lblVIN = new System.Windows.Forms.Label();
            this.txtBoxVIN = new System.Windows.Forms.TextBox();
            this.tblLayoutBottom = new System.Windows.Forms.TableLayoutPanel();
            this.btnAdvanceMode = new System.Windows.Forms.Button();
            this.lblVINError = new System.Windows.Forms.Label();
            this.lblCALIDCVN = new System.Windows.Forms.Label();
            this.lblOBDSUP = new System.Windows.Forms.Label();
            this.tblLayoutLogo = new System.Windows.Forms.TableLayoutPanel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.lblLogo = new System.Windows.Forms.Label();
            this.lblResult = new System.Windows.Forms.Label();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.MenuItemStatistic = new System.Windows.Forms.ToolStripMenuItem();
            this.tblLayoutMain.SuspendLayout();
            this.tblLayoutTop.SuspendLayout();
            this.tblLayoutBottom.SuspendLayout();
            this.tblLayoutLogo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tblLayoutMain
            // 
            this.tblLayoutMain.ColumnCount = 1;
            this.tblLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblLayoutMain.Controls.Add(this.tblLayoutTop, 0, 1);
            this.tblLayoutMain.Controls.Add(this.tblLayoutBottom, 0, 3);
            this.tblLayoutMain.Controls.Add(this.tblLayoutLogo, 0, 0);
            this.tblLayoutMain.Controls.Add(this.lblResult, 0, 2);
            this.tblLayoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblLayoutMain.Location = new System.Drawing.Point(0, 0);
            this.tblLayoutMain.Name = "tblLayoutMain";
            this.tblLayoutMain.RowCount = 4;
            this.tblLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tblLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 17F));
            this.tblLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 53F));
            this.tblLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tblLayoutMain.Size = new System.Drawing.Size(784, 561);
            this.tblLayoutMain.TabIndex = 0;
            // 
            // tblLayoutTop
            // 
            this.tblLayoutTop.ColumnCount = 2;
            this.tblLayoutTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 23F));
            this.tblLayoutTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 77F));
            this.tblLayoutTop.Controls.Add(this.lblVIN, 0, 0);
            this.tblLayoutTop.Controls.Add(this.txtBoxVIN, 1, 0);
            this.tblLayoutTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblLayoutTop.Location = new System.Drawing.Point(3, 87);
            this.tblLayoutTop.Name = "tblLayoutTop";
            this.tblLayoutTop.RowCount = 1;
            this.tblLayoutTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblLayoutTop.Size = new System.Drawing.Size(778, 89);
            this.tblLayoutTop.TabIndex = 0;
            // 
            // lblVIN
            // 
            this.lblVIN.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVIN.Font = new System.Drawing.Font("宋体", 50F, System.Drawing.FontStyle.Bold);
            this.lblVIN.Location = new System.Drawing.Point(3, 0);
            this.lblVIN.Name = "lblVIN";
            this.lblVIN.Size = new System.Drawing.Size(172, 89);
            this.lblVIN.TabIndex = 0;
            this.lblVIN.Text = "VIN:";
            this.lblVIN.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtBoxVIN
            // 
            this.txtBoxVIN.BackColor = System.Drawing.Color.SteelBlue;
            this.txtBoxVIN.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.txtBoxVIN.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtBoxVIN.Font = new System.Drawing.Font("宋体", 50F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtBoxVIN.Location = new System.Drawing.Point(181, 3);
            this.txtBoxVIN.Name = "txtBoxVIN";
            this.txtBoxVIN.Size = new System.Drawing.Size(594, 84);
            this.txtBoxVIN.TabIndex = 1;
            this.txtBoxVIN.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtBoxVIN_KeyPress);
            // 
            // tblLayoutBottom
            // 
            this.tblLayoutBottom.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.OutsetDouble;
            this.tblLayoutBottom.ColumnCount = 4;
            this.tblLayoutBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tblLayoutBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tblLayoutBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tblLayoutBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tblLayoutBottom.Controls.Add(this.btnAdvanceMode, 3, 0);
            this.tblLayoutBottom.Controls.Add(this.lblVINError, 0, 0);
            this.tblLayoutBottom.Controls.Add(this.lblCALIDCVN, 1, 0);
            this.tblLayoutBottom.Controls.Add(this.lblOBDSUP, 2, 0);
            this.tblLayoutBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblLayoutBottom.Location = new System.Drawing.Point(3, 479);
            this.tblLayoutBottom.Name = "tblLayoutBottom";
            this.tblLayoutBottom.RowCount = 1;
            this.tblLayoutBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblLayoutBottom.Size = new System.Drawing.Size(778, 79);
            this.tblLayoutBottom.TabIndex = 1;
            // 
            // btnAdvanceMode
            // 
            this.btnAdvanceMode.BackColor = System.Drawing.Color.SkyBlue;
            this.btnAdvanceMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAdvanceMode.Font = new System.Drawing.Font("宋体", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnAdvanceMode.Location = new System.Drawing.Point(585, 6);
            this.btnAdvanceMode.Name = "btnAdvanceMode";
            this.btnAdvanceMode.Size = new System.Drawing.Size(187, 67);
            this.btnAdvanceMode.TabIndex = 0;
            this.btnAdvanceMode.Text = "高级模式(&A)";
            this.btnAdvanceMode.UseVisualStyleBackColor = false;
            this.btnAdvanceMode.Click += new System.EventHandler(this.BtnAdvanceMode_Click);
            // 
            // lblVINError
            // 
            this.lblVINError.AutoSize = true;
            this.lblVINError.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVINError.Font = new System.Drawing.Font("宋体", 20F, System.Drawing.FontStyle.Bold);
            this.lblVINError.Location = new System.Drawing.Point(6, 3);
            this.lblVINError.Name = "lblVINError";
            this.lblVINError.Size = new System.Drawing.Size(184, 73);
            this.lblVINError.TabIndex = 3;
            this.lblVINError.Text = "VIN号不匹配";
            this.lblVINError.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblCALIDCVN
            // 
            this.lblCALIDCVN.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCALIDCVN.Font = new System.Drawing.Font("宋体", 20F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblCALIDCVN.Location = new System.Drawing.Point(199, 3);
            this.lblCALIDCVN.Name = "lblCALIDCVN";
            this.lblCALIDCVN.Size = new System.Drawing.Size(184, 73);
            this.lblCALIDCVN.TabIndex = 4;
            this.lblCALIDCVN.Text = "CALID或CVN数据异常";
            this.lblCALIDCVN.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblOBDSUP
            // 
            this.lblOBDSUP.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOBDSUP.Font = new System.Drawing.Font("宋体", 20F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblOBDSUP.Location = new System.Drawing.Point(392, 3);
            this.lblOBDSUP.Name = "lblOBDSUP";
            this.lblOBDSUP.Size = new System.Drawing.Size(184, 73);
            this.lblOBDSUP.TabIndex = 5;
            this.lblOBDSUP.Text = "OBD型式不适用或异常";
            this.lblOBDSUP.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tblLayoutLogo
            // 
            this.tblLayoutLogo.ColumnCount = 2;
            this.tblLayoutLogo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 23F));
            this.tblLayoutLogo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 77F));
            this.tblLayoutLogo.Controls.Add(this.pictureBox1, 0, 0);
            this.tblLayoutLogo.Controls.Add(this.lblLogo, 1, 0);
            this.tblLayoutLogo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblLayoutLogo.Location = new System.Drawing.Point(3, 3);
            this.tblLayoutLogo.Name = "tblLayoutLogo";
            this.tblLayoutLogo.RowCount = 1;
            this.tblLayoutLogo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblLayoutLogo.Size = new System.Drawing.Size(778, 78);
            this.tblLayoutLogo.TabIndex = 2;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(3, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(172, 72);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // lblLogo
            // 
            this.lblLogo.AutoSize = true;
            this.lblLogo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLogo.Font = new System.Drawing.Font("宋体", 30F, System.Drawing.FontStyle.Bold);
            this.lblLogo.Location = new System.Drawing.Point(181, 0);
            this.lblLogo.Name = "lblLogo";
            this.lblLogo.Size = new System.Drawing.Size(594, 78);
            this.lblLogo.TabIndex = 1;
            this.lblLogo.Text = "新车下线OBD检测系统";
            this.lblLogo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblResult
            // 
            this.lblResult.AutoSize = true;
            this.lblResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblResult.Font = new System.Drawing.Font("宋体", 50F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblResult.Location = new System.Drawing.Point(3, 179);
            this.lblResult.Name = "lblResult";
            this.lblResult.Size = new System.Drawing.Size(778, 297);
            this.lblResult.TabIndex = 3;
            this.lblResult.Text = "OBD检测结果";
            this.lblResult.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemStatistic});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(173, 26);
            // 
            // MenuItemStatistic
            // 
            this.MenuItemStatistic.Name = "MenuItemStatistic";
            this.MenuItemStatistic.Size = new System.Drawing.Size(172, 22);
            this.MenuItemStatistic.Text = "显示统计信息...(&S)";
            this.MenuItemStatistic.Click += new System.EventHandler(this.MenuItemStat_Click);
            // 
            // OBDStartForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.SteelBlue;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.ContextMenuStrip = this.contextMenuStrip1;
            this.Controls.Add(this.tblLayoutMain);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "OBDStartForm";
            this.Text = "新车下线OBD检测系统";
            this.Activated += new System.EventHandler(this.OBDStartForm_Activated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OBDStartForm_FormClosing);
            this.Load += new System.EventHandler(this.OBDStartForm_Load);
            this.Resize += new System.EventHandler(this.OBDStartForm_Resize);
            this.tblLayoutMain.ResumeLayout(false);
            this.tblLayoutMain.PerformLayout();
            this.tblLayoutTop.ResumeLayout(false);
            this.tblLayoutTop.PerformLayout();
            this.tblLayoutBottom.ResumeLayout(false);
            this.tblLayoutBottom.PerformLayout();
            this.tblLayoutLogo.ResumeLayout(false);
            this.tblLayoutLogo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tblLayoutMain;
        private System.Windows.Forms.TableLayoutPanel tblLayoutTop;
        private System.Windows.Forms.Label lblVIN;
        private System.Windows.Forms.TableLayoutPanel tblLayoutBottom;
        private System.Windows.Forms.Button btnAdvanceMode;
        private System.Windows.Forms.TextBox txtBoxVIN;
        private System.Windows.Forms.Label lblVINError;
        private System.Windows.Forms.Label lblCALIDCVN;
        private System.Windows.Forms.Label lblOBDSUP;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem MenuItemStatistic;
        private System.Windows.Forms.TableLayoutPanel tblLayoutLogo;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lblLogo;
        private System.Windows.Forms.Label lblResult;
    }
}