namespace SH_OBD_Main {
    partial class MainForm {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.StatusLabelConnStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatusLabelDeviceName = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatusLabelAppProtocol = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatusLabelCommProtocol = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatusLabelDeviceType = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatusLabelPort = new System.Windows.Forms.ToolStripStatusLabel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.buttonOBDTest = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.picDiagram = new System.Windows.Forms.PictureBox();
            this.lblInstruction1 = new System.Windows.Forms.Label();
            this.buttonDefaultFontStyle = new System.Windows.Forms.Button();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripBtnConnect = new System.Windows.Forms.ToolStripButton();
            this.toolStripBtnDisconnect = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripBtnUserPrefs = new System.Windows.Forms.ToolStripButton();
            this.toolStripBtnSettings = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.statusStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDiagram)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusLabelConnStatus,
            this.StatusLabelDeviceName,
            this.StatusLabelAppProtocol,
            this.StatusLabelCommProtocol,
            this.StatusLabelDeviceType,
            this.StatusLabelPort});
            this.statusStrip1.Location = new System.Drawing.Point(0, 605);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.statusStrip1.Size = new System.Drawing.Size(934, 26);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // StatusLabelConnStatus
            // 
            this.StatusLabelConnStatus.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.StatusLabelConnStatus.Name = "StatusLabelConnStatus";
            this.StatusLabelConnStatus.Size = new System.Drawing.Size(305, 21);
            this.StatusLabelConnStatus.Spring = true;
            this.StatusLabelConnStatus.Text = "OBD通讯接口状态";
            // 
            // StatusLabelDeviceName
            // 
            this.StatusLabelDeviceName.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.StatusLabelDeviceName.Name = "StatusLabelDeviceName";
            this.StatusLabelDeviceName.Size = new System.Drawing.Size(305, 21);
            this.StatusLabelDeviceName.Spring = true;
            this.StatusLabelDeviceName.Text = "OBD设备名称";
            // 
            // StatusLabelAppProtocol
            // 
            this.StatusLabelAppProtocol.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.StatusLabelAppProtocol.Name = "StatusLabelAppProtocol";
            this.StatusLabelAppProtocol.Size = new System.Drawing.Size(99, 21);
            this.StatusLabelAppProtocol.Text = "OBD应用层协议";
            // 
            // StatusLabelCommProtocol
            // 
            this.StatusLabelCommProtocol.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.StatusLabelCommProtocol.Name = "StatusLabelCommProtocol";
            this.StatusLabelCommProtocol.Size = new System.Drawing.Size(87, 21);
            this.StatusLabelCommProtocol.Text = "OBD连接协议";
            // 
            // StatusLabelDeviceType
            // 
            this.StatusLabelDeviceType.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.StatusLabelDeviceType.Name = "StatusLabelDeviceType";
            this.StatusLabelDeviceType.Size = new System.Drawing.Size(87, 21);
            this.StatusLabelDeviceType.Text = "OBD设备类型";
            // 
            // StatusLabelPort
            // 
            this.StatusLabelPort.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.StatusLabelPort.Name = "StatusLabelPort";
            this.StatusLabelPort.Size = new System.Drawing.Size(36, 21);
            this.StatusLabelPort.Text = "端口";
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.buttonOBDTest);
            this.panel1.Location = new System.Drawing.Point(0, 55);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(120, 551);
            this.panel1.TabIndex = 1;
            // 
            // buttonOBDTest
            // 
            this.buttonOBDTest.Location = new System.Drawing.Point(3, 3);
            this.buttonOBDTest.Name = "buttonOBDTest";
            this.buttonOBDTest.Size = new System.Drawing.Size(110, 23);
            this.buttonOBDTest.TabIndex = 12;
            this.buttonOBDTest.Text = "buttonOBDTest";
            this.buttonOBDTest.UseVisualStyleBackColor = true;
            this.buttonOBDTest.Click += new System.EventHandler(this.Button_Click);
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.pictureBox1);
            this.panel2.Controls.Add(this.picDiagram);
            this.panel2.Controls.Add(this.lblInstruction1);
            this.panel2.Controls.Add(this.buttonDefaultFontStyle);
            this.panel2.Location = new System.Drawing.Point(120, 55);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(814, 551);
            this.panel2.TabIndex = 3;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(260, 62);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(280, 140);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 22;
            this.pictureBox1.TabStop = false;
            // 
            // picDiagram
            // 
            this.picDiagram.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.picDiagram.Image = ((System.Drawing.Image)(resources.GetObject("picDiagram.Image")));
            this.picDiagram.Location = new System.Drawing.Point(143, 208);
            this.picDiagram.Name = "picDiagram";
            this.picDiagram.Size = new System.Drawing.Size(528, 72);
            this.picDiagram.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picDiagram.TabIndex = 20;
            this.picDiagram.TabStop = false;
            // 
            // lblInstruction1
            // 
            this.lblInstruction1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lblInstruction1.Location = new System.Drawing.Point(219, 297);
            this.lblInstruction1.Name = "lblInstruction1";
            this.lblInstruction1.Size = new System.Drawing.Size(386, 120);
            this.lblInstruction1.TabIndex = 16;
            this.lblInstruction1.Text = "1、请确认已使用了正确的通讯设置。\r\n\r\n2、请确认已使用了正确的车辆设置。\r\n\r\n3、使用OBD设备连接车辆OBD-II接口与电脑主机。\r\n\r\n4、打开车辆点火" +
    "按钮至“ON”位置，或者发动引擎使其处于运转状态。\r\n\r\n5、点击工具栏“建立连接”按钮。";
            // 
            // buttonDefaultFontStyle
            // 
            this.buttonDefaultFontStyle.Location = new System.Drawing.Point(221, 433);
            this.buttonDefaultFontStyle.Name = "buttonDefaultFontStyle";
            this.buttonDefaultFontStyle.Size = new System.Drawing.Size(75, 23);
            this.buttonDefaultFontStyle.TabIndex = 21;
            this.buttonDefaultFontStyle.Text = "DefaultFontStyle";
            this.buttonDefaultFontStyle.UseVisualStyleBackColor = true;
            this.buttonDefaultFontStyle.Visible = false;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(48, 48);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripBtnConnect,
            this.toolStripBtnDisconnect,
            this.toolStripSeparator1,
            this.toolStripBtnUserPrefs,
            this.toolStripBtnSettings,
            this.toolStripSeparator2});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(934, 55);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.TabStop = true;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripBtnConnect
            // 
            this.toolStripBtnConnect.Image = ((System.Drawing.Image)(resources.GetObject("toolStripBtnConnect.Image")));
            this.toolStripBtnConnect.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripBtnConnect.Name = "toolStripBtnConnect";
            this.toolStripBtnConnect.Size = new System.Drawing.Size(108, 52);
            this.toolStripBtnConnect.Text = "建立连接";
            this.toolStripBtnConnect.Click += new System.EventHandler(this.ToolStripBtnConnect_Click);
            // 
            // toolStripBtnDisconnect
            // 
            this.toolStripBtnDisconnect.Image = ((System.Drawing.Image)(resources.GetObject("toolStripBtnDisconnect.Image")));
            this.toolStripBtnDisconnect.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripBtnDisconnect.Name = "toolStripBtnDisconnect";
            this.toolStripBtnDisconnect.Size = new System.Drawing.Size(108, 52);
            this.toolStripBtnDisconnect.Text = "断开连接";
            this.toolStripBtnDisconnect.Click += new System.EventHandler(this.ToolStripBtnDisconnect_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 55);
            // 
            // toolStripBtnUserPrefs
            // 
            this.toolStripBtnUserPrefs.Image = ((System.Drawing.Image)(resources.GetObject("toolStripBtnUserPrefs.Image")));
            this.toolStripBtnUserPrefs.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripBtnUserPrefs.Name = "toolStripBtnUserPrefs";
            this.toolStripBtnUserPrefs.Size = new System.Drawing.Size(108, 52);
            this.toolStripBtnUserPrefs.Text = "用户设置";
            this.toolStripBtnUserPrefs.Click += new System.EventHandler(this.ToolStripBtnUserPrefs_Click);
            // 
            // toolStripBtnSettings
            // 
            this.toolStripBtnSettings.Image = ((System.Drawing.Image)(resources.GetObject("toolStripBtnSettings.Image")));
            this.toolStripBtnSettings.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripBtnSettings.Name = "toolStripBtnSettings";
            this.toolStripBtnSettings.Size = new System.Drawing.Size(108, 52);
            this.toolStripBtnSettings.Text = "通讯设置";
            this.toolStripBtnSettings.Click += new System.EventHandler(this.ToolStripBtnSettings_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 55);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(934, 631);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.statusStrip1);
            this.MinimumSize = new System.Drawing.Size(920, 640);
            this.Name = "MainForm";
            this.Text = "SH_OBD";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDiagram)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel StatusLabelConnStatus;
        private System.Windows.Forms.ToolStripStatusLabel StatusLabelDeviceName;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.ToolStripStatusLabel StatusLabelCommProtocol;
        private System.Windows.Forms.ToolStripStatusLabel StatusLabelPort;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripBtnConnect;
        private System.Windows.Forms.ToolStripButton toolStripBtnDisconnect;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton toolStripBtnSettings;
        private System.Windows.Forms.PictureBox picDiagram;
        private System.Windows.Forms.Label lblInstruction1;
        private System.Windows.Forms.Button buttonDefaultFontStyle;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ToolStripStatusLabel StatusLabelDeviceType;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.Button buttonOBDTest;
        private System.Windows.Forms.ToolStripStatusLabel StatusLabelAppProtocol;
        private System.Windows.Forms.ToolStripButton toolStripBtnUserPrefs;
    }
}

