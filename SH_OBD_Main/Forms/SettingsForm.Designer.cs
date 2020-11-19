namespace SH_OBD_Main {
    partial class SettingsForm {
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
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.comboHardware = new System.Windows.Forms.ComboBox();
            this.comboPorts = new System.Windows.Forms.ComboBox();
            this.checkBoxAutoDetect = new System.Windows.Forms.CheckBox();
            this.groupELM = new System.Windows.Forms.GroupBox();
            this.comboStandard = new System.Windows.Forms.ComboBox();
            this.label18 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.comboInitialize = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.comboProtocol = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBaud = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupScanner = new System.Windows.Forms.GroupBox();
            this.chkBoxUseSerialScanner = new System.Windows.Forms.CheckBox();
            this.label16 = new System.Windows.Forms.Label();
            this.cmbBoxScannerBaud = new System.Windows.Forms.ComboBox();
            this.cmbBoxScannerPort = new System.Windows.Forms.ComboBox();
            this.label17 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnPwd = new System.Windows.Forms.Button();
            this.txtBoxNewPwd2 = new System.Windows.Forms.TextBox();
            this.txtBoxOriPwd = new System.Windows.Forms.TextBox();
            this.txtBoxNewPwd1 = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.groupCompany = new System.Windows.Forms.GroupBox();
            this.txtTesterName = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.groupELM.SuspendLayout();
            this.groupScanner.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupCompany.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(502, 315);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(90, 25);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "取消(&C)";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(406, 315);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(90, 25);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "保存(&S)";
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // comboHardware
            // 
            this.comboHardware.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboHardware.Items.AddRange(new object[] {
            "自动探测",
            "ELM327/SH-VCI-302U"});
            this.comboHardware.Location = new System.Drawing.Point(82, 68);
            this.comboHardware.Name = "comboHardware";
            this.comboHardware.Size = new System.Drawing.Size(115, 20);
            this.comboHardware.TabIndex = 4;
            // 
            // comboPorts
            // 
            this.comboPorts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboPorts.Location = new System.Drawing.Point(82, 42);
            this.comboPorts.Name = "comboPorts";
            this.comboPorts.Size = new System.Drawing.Size(115, 20);
            this.comboPorts.TabIndex = 2;
            // 
            // checkBoxAutoDetect
            // 
            this.checkBoxAutoDetect.AutoSize = true;
            this.checkBoxAutoDetect.Location = new System.Drawing.Point(9, 20);
            this.checkBoxAutoDetect.Name = "checkBoxAutoDetect";
            this.checkBoxAutoDetect.Size = new System.Drawing.Size(138, 16);
            this.checkBoxAutoDetect.TabIndex = 0;
            this.checkBoxAutoDetect.Text = "自动探测OBD连接设置";
            this.checkBoxAutoDetect.CheckedChanged += new System.EventHandler(this.CheckBoxAutoDetect_CheckedChanged);
            // 
            // groupELM
            // 
            this.groupELM.Controls.Add(this.comboStandard);
            this.groupELM.Controls.Add(this.label18);
            this.groupELM.Controls.Add(this.label15);
            this.groupELM.Controls.Add(this.label13);
            this.groupELM.Controls.Add(this.comboHardware);
            this.groupELM.Controls.Add(this.comboPorts);
            this.groupELM.Controls.Add(this.comboInitialize);
            this.groupELM.Controls.Add(this.label3);
            this.groupELM.Controls.Add(this.checkBoxAutoDetect);
            this.groupELM.Controls.Add(this.comboProtocol);
            this.groupELM.Controls.Add(this.label2);
            this.groupELM.Controls.Add(this.comboBaud);
            this.groupELM.Controls.Add(this.label1);
            this.groupELM.Location = new System.Drawing.Point(12, 12);
            this.groupELM.Name = "groupELM";
            this.groupELM.Size = new System.Drawing.Size(580, 125);
            this.groupELM.TabIndex = 0;
            this.groupELM.TabStop = false;
            this.groupELM.Text = "ELM327 设置(&C)";
            // 
            // comboStandard
            // 
            this.comboStandard.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboStandard.Location = new System.Drawing.Point(294, 94);
            this.comboStandard.Name = "comboStandard";
            this.comboStandard.Size = new System.Drawing.Size(280, 20);
            this.comboStandard.TabIndex = 12;
            // 
            // label18
            // 
            this.label18.Location = new System.Drawing.Point(198, 93);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(90, 21);
            this.label18.TabIndex = 11;
            this.label18.Text = "上层协议(&S):";
            this.label18.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label15
            // 
            this.label15.Location = new System.Drawing.Point(6, 70);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(70, 15);
            this.label15.TabIndex = 3;
            this.label15.Text = "VCI设备:";
            this.label15.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label13
            // 
            this.label13.Location = new System.Drawing.Point(6, 44);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(70, 15);
            this.label13.TabIndex = 1;
            this.label13.Text = "串口:";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // comboInitialize
            // 
            this.comboInitialize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboInitialize.Items.AddRange(new object[] {
            "初始化",
            "旁路初始化"});
            this.comboInitialize.Location = new System.Drawing.Point(294, 68);
            this.comboInitialize.Name = "comboInitialize";
            this.comboInitialize.Size = new System.Drawing.Size(280, 20);
            this.comboInitialize.TabIndex = 10;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(213, 69);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(75, 21);
            this.label3.TabIndex = 9;
            this.label3.Text = "初始化(&I):";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // comboProtocol
            // 
            this.comboProtocol.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboProtocol.Location = new System.Drawing.Point(294, 42);
            this.comboProtocol.Name = "comboProtocol";
            this.comboProtocol.Size = new System.Drawing.Size(280, 20);
            this.comboProtocol.TabIndex = 8;
            this.comboProtocol.SelectedIndexChanged += new System.EventHandler(this.ComboProtocol_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(213, 43);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 22);
            this.label2.TabIndex = 7;
            this.label2.Text = "OBD协议(&r):";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // comboBaud
            // 
            this.comboBaud.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBaud.Items.AddRange(new object[] {
            "9600",
            "38400"});
            this.comboBaud.Location = new System.Drawing.Point(82, 94);
            this.comboBaud.Name = "comboBaud";
            this.comboBaud.Size = new System.Drawing.Size(115, 20);
            this.comboBaud.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(6, 92);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 22);
            this.label1.TabIndex = 5;
            this.label1.Text = "波特率(&B):";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // groupScanner
            // 
            this.groupScanner.Controls.Add(this.chkBoxUseSerialScanner);
            this.groupScanner.Controls.Add(this.label16);
            this.groupScanner.Controls.Add(this.cmbBoxScannerBaud);
            this.groupScanner.Controls.Add(this.cmbBoxScannerPort);
            this.groupScanner.Controls.Add(this.label17);
            this.groupScanner.Location = new System.Drawing.Point(305, 145);
            this.groupScanner.Name = "groupScanner";
            this.groupScanner.Size = new System.Drawing.Size(287, 106);
            this.groupScanner.TabIndex = 3;
            this.groupScanner.TabStop = false;
            this.groupScanner.Text = "串口扫码枪设置";
            // 
            // chkBoxUseSerialScanner
            // 
            this.chkBoxUseSerialScanner.AutoSize = true;
            this.chkBoxUseSerialScanner.Location = new System.Drawing.Point(81, 24);
            this.chkBoxUseSerialScanner.Name = "chkBoxUseSerialScanner";
            this.chkBoxUseSerialScanner.Size = new System.Drawing.Size(108, 16);
            this.chkBoxUseSerialScanner.TabIndex = 0;
            this.chkBoxUseSerialScanner.Text = "使用串口扫码枪";
            this.chkBoxUseSerialScanner.UseVisualStyleBackColor = true;
            this.chkBoxUseSerialScanner.CheckedChanged += new System.EventHandler(this.ChkBoxUseSerialScanner_CheckedChanged);
            // 
            // label16
            // 
            this.label16.Location = new System.Drawing.Point(4, 49);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(70, 15);
            this.label16.TabIndex = 1;
            this.label16.Text = "串口:";
            this.label16.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbBoxScannerBaud
            // 
            this.cmbBoxScannerBaud.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBoxScannerBaud.Items.AddRange(new object[] {
            "9600",
            "38400",
            "115200"});
            this.cmbBoxScannerBaud.Location = new System.Drawing.Point(81, 71);
            this.cmbBoxScannerBaud.Name = "cmbBoxScannerBaud";
            this.cmbBoxScannerBaud.Size = new System.Drawing.Size(200, 20);
            this.cmbBoxScannerBaud.TabIndex = 4;
            // 
            // cmbBoxScannerPort
            // 
            this.cmbBoxScannerPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBoxScannerPort.Location = new System.Drawing.Point(81, 45);
            this.cmbBoxScannerPort.Name = "cmbBoxScannerPort";
            this.cmbBoxScannerPort.Size = new System.Drawing.Size(200, 20);
            this.cmbBoxScannerPort.TabIndex = 2;
            // 
            // label17
            // 
            this.label17.Location = new System.Drawing.Point(6, 69);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(70, 22);
            this.label17.TabIndex = 3;
            this.label17.Text = "波特率(&B):";
            this.label17.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnPwd);
            this.groupBox1.Controls.Add(this.txtBoxNewPwd2);
            this.groupBox1.Controls.Add(this.txtBoxOriPwd);
            this.groupBox1.Controls.Add(this.txtBoxNewPwd1);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.label10);
            this.groupBox1.Controls.Add(this.label11);
            this.groupBox1.Location = new System.Drawing.Point(12, 145);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(287, 164);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "修改管理员密码";
            // 
            // btnPwd
            // 
            this.btnPwd.Location = new System.Drawing.Point(191, 98);
            this.btnPwd.Name = "btnPwd";
            this.btnPwd.Size = new System.Drawing.Size(90, 25);
            this.btnPwd.TabIndex = 9;
            this.btnPwd.Text = "修改密码(&M)";
            this.btnPwd.Click += new System.EventHandler(this.BtnPwd_Click);
            // 
            // txtBoxNewPwd2
            // 
            this.txtBoxNewPwd2.Location = new System.Drawing.Point(83, 71);
            this.txtBoxNewPwd2.Name = "txtBoxNewPwd2";
            this.txtBoxNewPwd2.PasswordChar = '*';
            this.txtBoxNewPwd2.Size = new System.Drawing.Size(198, 21);
            this.txtBoxNewPwd2.TabIndex = 12;
            // 
            // txtBoxOriPwd
            // 
            this.txtBoxOriPwd.Location = new System.Drawing.Point(83, 20);
            this.txtBoxOriPwd.Name = "txtBoxOriPwd";
            this.txtBoxOriPwd.PasswordChar = '*';
            this.txtBoxOriPwd.Size = new System.Drawing.Size(198, 21);
            this.txtBoxOriPwd.TabIndex = 8;
            // 
            // txtBoxNewPwd1
            // 
            this.txtBoxNewPwd1.Location = new System.Drawing.Point(83, 45);
            this.txtBoxNewPwd1.Name = "txtBoxNewPwd1";
            this.txtBoxNewPwd1.PasswordChar = '*';
            this.txtBoxNewPwd1.Size = new System.Drawing.Size(198, 21);
            this.txtBoxNewPwd1.TabIndex = 10;
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(15, 20);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(62, 22);
            this.label8.TabIndex = 7;
            this.label8.Text = "原密码:";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label10
            // 
            this.label10.Location = new System.Drawing.Point(15, 45);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(62, 22);
            this.label10.TabIndex = 9;
            this.label10.Text = "新密码:";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label11
            // 
            this.label11.Location = new System.Drawing.Point(15, 70);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(62, 21);
            this.label11.TabIndex = 11;
            this.label11.Text = "再次输入:";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // groupCompany
            // 
            this.groupCompany.Controls.Add(this.txtTesterName);
            this.groupCompany.Controls.Add(this.label12);
            this.groupCompany.Location = new System.Drawing.Point(305, 257);
            this.groupCompany.Name = "groupCompany";
            this.groupCompany.Size = new System.Drawing.Size(288, 52);
            this.groupCompany.TabIndex = 7;
            this.groupCompany.TabStop = false;
            this.groupCompany.Text = "用户情况";
            // 
            // txtTesterName
            // 
            this.txtTesterName.Location = new System.Drawing.Point(82, 18);
            this.txtTesterName.Name = "txtTesterName";
            this.txtTesterName.Size = new System.Drawing.Size(199, 21);
            this.txtTesterName.TabIndex = 1;
            // 
            // label12
            // 
            this.label12.Location = new System.Drawing.Point(6, 17);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(71, 22);
            this.label12.TabIndex = 0;
            this.label12.Text = "操作员(&N):";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // SettingsForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(604, 351);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupCompany);
            this.Controls.Add(this.groupScanner);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.groupELM);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "通讯参数设置";
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.groupELM.ResumeLayout(false);
            this.groupELM.PerformLayout();
            this.groupScanner.ResumeLayout(false);
            this.groupScanner.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupCompany.ResumeLayout(false);
            this.groupCompany.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.ComboBox comboHardware;
        private System.Windows.Forms.ComboBox comboPorts;
        private System.Windows.Forms.CheckBox checkBoxAutoDetect;
        private System.Windows.Forms.GroupBox groupELM;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.ComboBox comboInitialize;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboProtocol;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBaud;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupScanner;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.ComboBox cmbBoxScannerBaud;
        private System.Windows.Forms.ComboBox cmbBoxScannerPort;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.CheckBox chkBoxUseSerialScanner;
        private System.Windows.Forms.ComboBox comboStandard;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnPwd;
        private System.Windows.Forms.TextBox txtBoxNewPwd2;
        private System.Windows.Forms.TextBox txtBoxOriPwd;
        private System.Windows.Forms.TextBox txtBoxNewPwd1;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.GroupBox groupCompany;
        private System.Windows.Forms.TextBox txtTesterName;
        private System.Windows.Forms.Label label12;
    }
}