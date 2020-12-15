using SH_OBD_DLL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SH_OBD_Main {
    public partial class AdvancedForm : Form {
        private Dictionary<string, Form> dicSubForms;
        private OBDTestForm f_OBDTest;
        private readonly OBDIfEx _obdIfEx;
        private readonly OBDTest _obdTest;
        private readonly Font _boldFont;
        private readonly Font _originFont;

        public AdvancedForm(OBDIfEx obdIfEX, OBDTest obdTest) {
            InitializeComponent();
            _obdIfEx = obdIfEX;
            _obdTest = obdTest;
            _obdIfEx.OBDIf.OnConnect += new OBDInterface.__Delegate_OnConnect(On_OBD_Connect);
            _obdIfEx.OBDIf.OnDisconnect += new OBDInterface.__Delegate_OnDisconnect(On_OBD_Disconnect);

            _originFont = buttonDefaultFontStyle.Font;
            _boldFont = new Font(_originFont, FontStyle.Bold);

            StatusLabelConnStatus.ForeColor = Color.Red;
            StatusLabelConnStatus.Text = "OBD通讯接口未连接";
            StatusLabelDeviceName.Text = "未获取到设备名";
            StatusLabelCommProtocol.Text = _obdIfEx.OBDIf.GetProtocol().ToString();
            StatusLabelDeviceType.Text = _obdIfEx.OBDIf.GetDevice().ToString().Replace("ELM327", "SH-VCI-302U");
            if (_obdIfEx.OBDIf.DllSettings != null) {
                if (_obdIfEx.OBDIf.DllSettings.AutoDetect) {
                    StatusLabelPort.Text = "自动探测";
                    StatusLabelAppProtocol.Text = "自动探测";
                } else {
                    StatusLabelPort.Text = _obdIfEx.OBDIf.DllSettings.ComPortName;
                    StatusLabelAppProtocol.Text = _obdIfEx.OBDIf.DllSettings.StandardName;
                }
            }

            InitSubForm();
            this.Text = "SH_OBD - Ver " + MainFileVersion.AssemblyVersion;
        }

        void InitSubForm() {
            dicSubForms = new Dictionary<string, Form>();

            f_OBDTest = new OBDTestForm(_obdIfEx, _obdTest);

            buttonOBDTest.Text = Properties.Resources.buttonName_OBDTest;

            dicSubForms.Add(Properties.Resources.buttonName_OBDTest, f_OBDTest);
        }

        private void BroadcastConnectionUpdate() {
            foreach (var key in dicSubForms.Keys) {
                if (key == Properties.Resources.buttonName_OBDTest) {
                    (dicSubForms[key] as OBDTestForm).CheckConnection();
                }
            }
        }

        private void On_OBD_Connect() {
            if (InvokeRequired) {
                this.Invoke((EventHandler)delegate {
                    On_OBD_Connect();
                });
            } else {
                StatusLabelConnStatus.Text = "OBD通讯接口已连接";
                StatusLabelConnStatus.ForeColor = Color.Green;
                StatusLabelDeviceName.Text = _obdIfEx.OBDIf.GetDeviceIDString();
                StatusLabelCommProtocol.Text = _obdIfEx.OBDIf.GetProtocol().ToString();
                toolStripBtnSettings.Enabled = false;
                BroadcastConnectionUpdate();
            }
        }

        private void On_OBD_Disconnect() {
            StatusLabelConnStatus.Text = "OBD通讯接口未连接";
            StatusLabelConnStatus.ForeColor = Color.Red;
            StatusLabelDeviceName.Text = "未获取到设备名";
            toolStripBtnSettings.Enabled = true;
            BroadcastConnectionUpdate();
        }

        private void Button_Click(object sender, EventArgs e) {
            if (sender is Button button) {
                if (panel2.Controls.Count > 0 && panel2.Controls[0] is Form activeForm && activeForm != dicSubForms[button.Text]) {
                    activeForm.Hide();
                }
                foreach (var item in panel1.Controls) {
                    if (item is Button btn) {
                        btn.Font = _originFont;
                        btn.ForeColor = Color.Black;
                    }
                }
                button.Font = _boldFont;
                button.ForeColor = Color.Red;

                Form form = dicSubForms[button.Text];
                if (panel2.Controls.IndexOf(form) < 0) {
                    panel2.Controls.Clear();
                    form.TopLevel = false;
                    panel2.Controls.Add(form);
                    panel2.Resize += new EventHandler(Panel2_Resize);
                    //form.MdiParent = this; // 指定当前窗体为顶级Mdi窗体
                    //form.Parent = this.panel2; // 指定子窗体的父容器为
                    form.FormBorderStyle = FormBorderStyle.None;
                    form.Size = this.panel2.Size;
                    form.Show();
                }
            }
        }

        private void Panel2_Resize(object sender, EventArgs e) {
            if (panel2.Controls.Count > 0) {
                if (panel2.Controls[0] is Form form) {
                    if (form != null) {
                        form.Size = panel2.Size;
                    }
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e) {
            if (_obdIfEx.OBDIf.ConnectedStatus) {
                toolStripBtnConnect.Enabled = false;
                toolStripBtnDisconnect.Enabled = true;
                ShowConnectedLabel();
                StatusLabelDeviceName.Text = _obdIfEx.OBDIf.GetDeviceIDString();
            } else {
                toolStripBtnConnect.Enabled = true;
                toolStripBtnDisconnect.Enabled = false;
                ShowDisconnectedLabel();
                StatusLabelDeviceName.Text = "未获取到设备名";
            }
        }

        private void ToolStripBtnSettings_Click(object sender, EventArgs e) {
            DllSettings dllSettings = _obdIfEx.OBDIf.DllSettings;
            MainSettings mainSettings = _obdIfEx.MainSettings;
            SettingsForm settingsForm = new SettingsForm(dllSettings, mainSettings, _obdTest.DbLocal);
            settingsForm.ShowDialog();
            _obdIfEx.SaveDllSettings(dllSettings);
            _obdIfEx.SaveMainSettings(mainSettings);
            StatusLabelCommProtocol.Text = _obdIfEx.OBDIf.GetProtocol().ToString();
            StatusLabelAppProtocol.Text = _obdIfEx.OBDIf.GetStandard().ToString();
            StatusLabelDeviceType.Text = _obdIfEx.OBDIf.GetDevice().ToString().Replace("ELM327", "SH-VCI-302U");
            if (dllSettings.AutoDetect) {
                StatusLabelPort.Text = "自动探测";
            } else {
                StatusLabelPort.Text = dllSettings.ComPortName;
            }
            settingsForm.Dispose();
        }

        private void ToolStripBtnConnect_Click(object sender, EventArgs e) {
            toolStripBtnConnect.Enabled = false;
            toolStripBtnDisconnect.Enabled = true;
            _obdIfEx.OBDDll.LogCommSettingInfo();

            Task.Factory.StartNew(ConnectThreadNew);
        }

        private void ToolStripBtnDisconnect_Click(object sender, EventArgs e) {
            ShowDisconnectedLabel();
            _obdIfEx.OBDIf.Disconnect();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            f_OBDTest.Close();
            _boldFont.Dispose();
            _obdIfEx.OBDIf.OnConnect -= new OBDInterface.__Delegate_OnConnect(On_OBD_Connect);
            _obdIfEx.OBDIf.OnDisconnect -= new OBDInterface.__Delegate_OnDisconnect(On_OBD_Disconnect);
            _obdTest.AdvanceMode = false;
        }

        private void ConnectThreadNew() {
            ShowConnectingLabel();
            if (_obdIfEx.OBDDll.ConnectOBD()) {
                ShowConnectedLabel();
            } else {
                ShowDisconnectedLabel();
                _obdIfEx.OBDIf.Disconnect();
                MessageBox.Show(
                    "无法找到与本机相连的兼容的OBD-II硬件设备。\r\n请确认没有其他软件正在使用所需端口。",
                    "连接车辆失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }
        }

        private void ShowConnectedLabel() {
            this.Invoke((EventHandler)delegate {
                StatusLabelConnStatus.ForeColor = Color.Green;
                StatusLabelConnStatus.Text = "OBD通讯接口已连接";
                switch (_obdIfEx.OBDIf.STDType) {
                case StandardType.Automatic:
                    StatusLabelAppProtocol.Text = "Automatic";
                    break;
                case StandardType.ISO_15031:
                    StatusLabelAppProtocol.Text = "ISO_15031";
                    break;
                case StandardType.ISO_27145:
                    StatusLabelAppProtocol.Text = "ISO_27145";
                    break;
                case StandardType.SAE_J1939:
                    StatusLabelAppProtocol.Text = "SAE_J1939";
                    break;
                default:
                    StatusLabelAppProtocol.Text = "Automatic";
                    break;
                }
            });
        }

        private void ShowConnectingLabel() {
            if (InvokeRequired) {
                BeginInvoke((EventHandler)delegate { ShowConnectingLabel(); });
            } else {
                StatusLabelConnStatus.ForeColor = Color.Black;
                StatusLabelConnStatus.Text = "OBD通讯接口连接中...";
            }
        }

        private void ShowDisconnectedLabel() {
            if (InvokeRequired) {
                BeginInvoke((EventHandler)delegate { ShowDisconnectedLabel(); });
            } else {
                StatusLabelConnStatus.ForeColor = Color.Red;
                StatusLabelConnStatus.Text = "OBD通讯接口未连接";
                StatusLabelAppProtocol.Text = "自动探测";
                toolStripBtnConnect.Enabled = true;
                toolStripBtnDisconnect.Enabled = false;
            }
        }

    }
}
