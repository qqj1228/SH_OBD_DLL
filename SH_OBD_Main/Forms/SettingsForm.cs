using SH_OBD_DLL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace SH_OBD_Main {
    public partial class SettingsForm : Form {
        private readonly DllSettings _dllSettings;
        private readonly MainSettings _mainSettings;
        private readonly Model _db;

        public SettingsForm(DllSettings dllSettings, MainSettings mainSettings, Model db) {
            InitializeComponent();
            _dllSettings = dllSettings;
            _mainSettings = mainSettings;
            _db = db;
        }

        private void SettingsForm_Load(object sender, EventArgs e) {
            try {
                string[] serialPorts = SerialPort.GetPortNames();
                foreach (string serialPort in serialPorts) {
                    comboPorts.Items.Add(serialPort);
                    cmbBoxScannerPort.Items.Add(serialPort);
                }

                if (CommBase.IsPortAvailable(_dllSettings.ComPort)) {
                    comboPorts.SelectedItem = _dllSettings.ComPortName;
                } else if (comboPorts.Items.Count > 0) {
                    comboPorts.SelectedIndex = 0;
                }

                // 打开设置窗口时扫码枪串口已经被打开了，故无需判断串口是否可用
                cmbBoxScannerPort.SelectedItem = _mainSettings.ScannerPortName;

                comboHardware.SelectedIndex = _dllSettings.HardwareIndexInt;
                comboBaud.SelectedIndex = _dllSettings.BaudRateIndex;
                cmbBoxScannerBaud.SelectedIndex = _mainSettings.ScannerBaudRateIndex;

                chkBoxUseSerialScanner.Checked = _mainSettings.UseSerialScanner;
                cmbBoxScannerPort.Enabled = chkBoxUseSerialScanner.Checked;
                cmbBoxScannerBaud.Enabled = chkBoxUseSerialScanner.Checked;

                foreach (string item in DllSettings.ProtocolNames) {
                    comboProtocol.Items.Add(item);
                }
                comboProtocol.SelectedIndex = _dllSettings.ProtocolIndexInt;

                comboInitialize.SelectedIndex = !_dllSettings.DoInitialization ? 1 : 0;

                foreach (string item in DllSettings.StandardNames) {
                    comboStandard.Items.Add(item);
                }
                comboStandard.SelectedIndex = _dllSettings.StandardIndexInt;

                if (_dllSettings.AutoDetect) {
                    checkBoxAutoDetect.Checked = true;
                } else {
                    checkBoxAutoDetect.Checked = false;
                }

                txtTesterName.Text = _mainSettings.TesterName;
            } catch (Exception ex) {
                MessageBox.Show(ex.ToString());
            }
        }

        private void BtnOK_Click(object sender, EventArgs e) {
            _dllSettings.AutoDetect = checkBoxAutoDetect.Checked;
            if (comboPorts.SelectedItem != null && comboPorts.SelectedItem.ToString().Length > 3) {
                _dllSettings.ComPort = Convert.ToInt32(comboPorts.SelectedItem.ToString().Remove(0, 3));
            }
            _mainSettings.UseSerialScanner = chkBoxUseSerialScanner.Checked;
            if (cmbBoxScannerPort.SelectedItem != null && cmbBoxScannerPort.SelectedItem.ToString().Length > 3) {
                _mainSettings.ScannerPort = Convert.ToInt32(cmbBoxScannerPort.SelectedItem.ToString().Remove(0, 3));
            }
            _mainSettings.ScannerBaudRateIndex = cmbBoxScannerBaud.SelectedIndex;
            _dllSettings.BaudRateIndex = comboBaud.SelectedIndex;
            _dllSettings.HardwareIndexInt = comboHardware.SelectedIndex;
            _dllSettings.ProtocolIndexInt = comboProtocol.SelectedIndex;
            _dllSettings.StandardIndexInt = comboStandard.SelectedIndex;
            _dllSettings.DoInitialization = (comboInitialize.SelectedIndex == 0);

            Close();
        }

        private void CheckBoxAutoDetect_CheckedChanged(object sender, EventArgs e) {
            if (checkBoxAutoDetect.Checked) {
                comboPorts.Enabled = false;
                comboHardware.Enabled = false;
                comboBaud.Enabled = false;
                comboProtocol.Enabled = false;
                comboInitialize.Enabled = false;
                comboStandard.Enabled = false;
            } else {
                comboPorts.Enabled = true;
                comboHardware.Enabled = true;
                comboBaud.Enabled = true;
                comboProtocol.Enabled = true;
                comboInitialize.Enabled = true;
                comboStandard.Enabled = true;
            }
        }

        private void ChkBoxUseSerialScanner_CheckedChanged(object sender, EventArgs e) {
            if (chkBoxUseSerialScanner.Checked) {
                cmbBoxScannerPort.Enabled = true;
                cmbBoxScannerBaud.Enabled = true;
            } else {
                cmbBoxScannerPort.Enabled = false;
                cmbBoxScannerBaud.Enabled = false;
            }
        }

        private void ComboProtocol_SelectedIndexChanged(object sender, EventArgs e) {
            if (comboStandard.Items.Count > 3) {
                switch (comboProtocol.SelectedIndex) {
                case 0:
                    comboStandard.Enabled = true;
                    break;
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                    comboStandard.Enabled = false;
                    comboStandard.SelectedIndex = 1;
                    break;
                case 6:
                case 7:
                case 8:
                case 9:
                    comboStandard.Enabled = true;
                    comboStandard.SelectedIndex = 2;
                    break;
                case 10:
                    comboStandard.Enabled = false;
                    comboStandard.SelectedIndex = 3;
                    break;
                default:
                    comboStandard.Enabled = true;
                    break;
                }
            }
        }

        private void BtnPwd_Click(object sender, EventArgs e) {
            _mainSettings.TesterName = txtTesterName.Text;
            bool CanClose = true;
            if (this.txtBoxOriPwd.Text.Length > 0 && this.txtBoxNewPwd1.Text.Length > 0 && this.txtBoxNewPwd2.Text.Length > 0) {
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] output = md5.ComputeHash(Encoding.Default.GetBytes(this.txtBoxOriPwd.Text.Trim()));
                string strValue = BitConverter.ToString(output).Replace("-", "");
                if (strValue == _db.GetPassWord()) {
                    if (this.txtBoxNewPwd1.Text != this.txtBoxNewPwd2.Text) {
                        CanClose = false;
                        MessageBox.Show("两次输入的新密码不一致！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    } else {
                        output = md5.ComputeHash(Encoding.Default.GetBytes(this.txtBoxNewPwd1.Text.Trim()));
                        strValue = BitConverter.ToString(output).Replace("-", "");
                        if (_db.SetPassWord(strValue) == 1) {
                            MessageBox.Show("修改管理员密码成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                } else {
                    CanClose = false;
                    MessageBox.Show("原密码不正确！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                md5.Dispose();
            }
            if (CanClose) {
                Close();
            }
        }
    }
}
