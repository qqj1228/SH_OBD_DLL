using SH_OBD_DLL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SH_OBD_Main {
    public partial class OBDStartForm : Form {
        public static bool _bCanOBDTest;
        private string _serialRecvBuf;
        private readonly OBDIfEx _obdIfEx;
        private readonly OBDTest _obdTest;
        private AdvancedForm f_Advanced;
        private readonly Color _backColor;
        private float _lastHeight;
        readonly System.Timers.Timer _timer;
        CancellationTokenSource _ctsOBDTestStart;
        CancellationTokenSource _ctsSetupColumnsDone;
        CancellationTokenSource _ctsWriteDbStart;
        CancellationTokenSource _ctsUploadDataStart;

        public OBDStartForm() {
            InitializeComponent();
            Text += " Ver(Main/Dll): " + MainFileVersion.AssemblyVersion + "/" + DllVersion<SH_OBD_Dll>.AssemblyVersion;
            _serialRecvBuf = "";
            _bCanOBDTest = true;
            _lastHeight = Height;
            _obdIfEx = new OBDIfEx();
            if (_obdIfEx.StrLoadConfigResult.Length > 0) {
                _obdIfEx.StrLoadConfigResult += "是否要以默认配置运行程序？点击\"否\"：将会退出程序。";
                DialogResult result = MessageBox.Show(_obdIfEx.StrLoadConfigResult, "加载配置文件出错", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No) {
                    Environment.Exit(0);
                }
            }
            _obdTest = new OBDTest(_obdIfEx);
            _backColor = lblVIN.BackColor;
            if (_obdIfEx.ScannerPortOpened) {
                _obdIfEx.ScannerSP.DataReceived += new SerialPortClass.SerialPortDataReceiveEventArgs(SerialDataReceived);
            }
            _obdTest.OBDTestStart += new Action(OnOBDTestStart);
            _obdTest.SetupColumnsDone += new Action(OnSetupColumnsDone);
            _obdTest.WriteDbStart += new Action(OnWriteDbStart);
            _obdTest.WriteDbDone += new Action(OnWriteDbDone);
            _obdTest.UploadDataStart += new Action(OnUploadDataStart);
            _obdTest.UploadDataDone += new Action(OnUploadDataDone);

            // 测试MES数据库连接是否正常
            Task.Factory.StartNew(TestOracleConnect);
            // 测试本地数据库连接是否正常
            Task.Factory.StartNew(TestNativeDatabase);

            // 定时上传以前上传失败的数据
#if DEBUG
            // debug版：使用秒作为单位，结束后不自动开始
            _timer = new System.Timers.Timer(_obdIfEx.OBDResultSetting.UploadInterval * 1000) {
                AutoReset = false
            };
#else
            // release版：正常功能
            _timer = new System.Timers.Timer(_obdIfEx.OBDResultSetting.UploadInterval * 60 * 1000) {
                AutoReset = true
            };
#endif
            _timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimeUpload);
            _timer.Enabled = true;
        }

        ~OBDStartForm() {
            if (f_Advanced != null) {
                f_Advanced.Close();
            }
            if (_timer != null) {
                _timer.Dispose();
            }
        }

        private void TestNativeDatabase() {
            try {
                _obdTest.DbNative.GetPassWord();
            } catch (Exception ex) {
                _obdIfEx.Log.TraceError("Access native database failed: " + ex.Message);
                MessageBox.Show("检测到本地数据库通讯异常，请排查相关故障：\n" + ex.Message, "本地数据库通讯异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnTimeUpload(object source, System.Timers.ElapsedEventArgs e) {
            _obdIfEx.Log.TraceInfo("Start UploadDataFromDBOnTime");
            try {
                _obdTest.UploadDataFromDBOnTime(out string errorMsg);
            } catch (Exception ex) {
                _obdIfEx.Log.TraceError("UploadDataFromDBOnTime fialed：" + ex.Message);
            }
        }

        void OnOBDTestStart() {
            if (!_obdTest.AdvancedMode) {
                _ctsOBDTestStart = UpdateUITask("OBD检测中");
            }
        }

        void OnSetupColumnsDone() {
            if (!_obdTest.AdvancedMode) {
                _ctsOBDTestStart.Cancel();
                _ctsSetupColumnsDone = UpdateUITask("正在读取结果");
            }
        }

        void OnWriteDbStart() {
            if (!_obdTest.AdvancedMode) {
                _ctsSetupColumnsDone.Cancel();
                _ctsWriteDbStart = UpdateUITask("正在写入本地数据库");
            }
        }

        void OnWriteDbDone() {
            if (!_obdTest.AdvancedMode) {
                _ctsWriteDbStart.Cancel();
                Invoke((EventHandler)delegate {
                    lblResult.ForeColor = Color.Black;
                    lblResult.Text = "写入本地数据库结束";
                });
            }
        }

        void OnUploadDataStart() {
            if (!_obdTest.AdvancedMode) {
                _ctsUploadDataStart = UpdateUITask("正在上传数据");
            }
        }

        void OnUploadDataDone() {
            if (!_obdTest.AdvancedMode) {
                _ctsUploadDataStart.Cancel();
                Invoke((EventHandler)delegate {
                    lblResult.ForeColor = Color.Black;
                    lblResult.Text = "上传数据结束";
                });
            }
        }

        void SerialDataReceived(object sender, SerialDataReceivedEventArgs e, byte[] bits) {
            Control con = ActiveControl;
            if (con is TextBox tb) {
                _serialRecvBuf += Encoding.Default.GetString(bits);
                if (_serialRecvBuf.Contains("\n")) {
                    if (!_bCanOBDTest) {
                        Invoke((EventHandler)delegate {
                            txtBoxVIN.SelectAll();
                            MessageBox.Show("上一辆车还未完全结束检测过程，请稍后再试", "OBD检测出错", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        });
                        _serialRecvBuf = "";
                        return;
                    }
                    string strTxt = _serialRecvBuf.Split('\n')[0];
                    _serialRecvBuf = _serialRecvBuf.Split('\n')[1];
                    string[] codes = strTxt.Trim().Split('*');
                    if (codes != null) {
                        if (codes.Length > 2) {
                            _obdTest.StrVIN_IN = codes[2];
                        }
                        _obdTest.StrType_IN = codes[0];
                        // 跨UI线程调用UI控件要使用Invoke
                        Invoke((EventHandler)delegate {
                            txtBoxVIN.Text = _obdTest.StrVIN_IN;
                            txtBoxVehicleType.Text = _obdTest.StrType_IN;
                        });
                    }
                    if (_obdTest.StrVIN_IN.Length == 17 && _obdTest.StrType_IN.Length >= 10) {
                        if (!_obdTest.AdvancedMode) {
                            Task.Factory.StartNew(StartOBDTest);
                        }
                    }
                }
            }
        }

        private void StartOBDTest() {
            _bCanOBDTest = false;
            Invoke((EventHandler)delegate {
                lblResult.ForeColor = Color.Black;
                lblResult.Text = "准备OBD检测";
                lblVINError.BackColor = _backColor;
                lblVINError.ForeColor = Color.Gray;
                lblCALIDCVN.BackColor = _backColor;
                lblCALIDCVN.ForeColor = Color.Gray;
                lblDTC.BackColor = _backColor;
                lblDTC.ForeColor = Color.Gray;
            });
            _obdIfEx.Log.TraceInfo(string.Format(">>>>>>>>>> Start to test vehicle of [VIN: {0}, VehicleType: {1}] MainVersion: {2} <<<<<<<<<<",
                _obdTest.StrVIN_IN, _obdTest.StrType_IN, MainFileVersion.AssemblyVersion));
            if (_obdIfEx.OBDIf.ConnectedStatus) {
                _obdIfEx.OBDIf.Disconnect();
            }
            CancellationTokenSource tokenSource = UpdateUITask("正在连接车辆");
            if (!_obdIfEx.OBDDll.ConnectOBD()) {
                tokenSource.Cancel();
                Invoke((EventHandler)delegate {
                    lblResult.ForeColor = Color.Red;
                    lblResult.Text = "连接车辆失败！";
                });
                _bCanOBDTest = true;
                return;
            }
            tokenSource.Cancel();

            string errorMsg = "";
            try {
                _obdTest.StartOBDTest(out errorMsg);
            } catch (Exception ex) {
                _obdIfEx.Log.TraceError("OBD test occurred error: " + errorMsg + ", " + ex.Message);
                MessageBox.Show(ex.Message + "\n" + errorMsg, "OBD检测出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
            } finally {
                _obdTest.StrVIN_IN = "";
                _obdTest.StrType_IN = "";
                if (_ctsOBDTestStart != null) {
                    _ctsOBDTestStart.Cancel();
                }
                if (_ctsSetupColumnsDone != null) {
                    _ctsSetupColumnsDone.Cancel();
                }
                if (_ctsWriteDbStart != null) {
                    _ctsWriteDbStart.Cancel();
                }
                if (_ctsUploadDataStart != null) {
                    _ctsUploadDataStart.Cancel();
                }
                _bCanOBDTest = true;
            }

            Invoke((EventHandler)delegate {
                if (_obdTest.OBDResult) {
                    lblResult.ForeColor = Color.GreenYellow;
                    lblResult.Text = "OBD检测结果：合格";
                } else {
                    if (!_obdTest.VINResult) {
                        lblVINError.BackColor = Color.Red;
                        lblVINError.ForeColor = Color.Black;
                    }
                    if (!_obdTest.CALIDCVNResult) {
                        lblCALIDCVN.BackColor = Color.Red;
                        lblCALIDCVN.ForeColor = Color.Black;
                    }
                    if (!_obdTest.DTCResult) {
                        lblDTC.BackColor = Color.Red;
                        lblDTC.ForeColor = Color.Black;
                    }
                    lblResult.ForeColor = Color.Red;
                    if (_obdTest.VehicleTypeExist && _obdTest.CALIDCheckResult && _obdTest.CVNCheckResult) {
                        lblResult.Text = "OBD检测结果：不合格";
                    } else {
                        lblResult.Text = "结果：";
                    }
                    if (!_obdTest.VehicleTypeExist) {
                        lblResult.Text += "缺少车型数据";
                    }
                    if (!_obdTest.CALIDCheckResult) {
                        if (lblResult.Text.Length > 3) {
                            lblResult.Text += "，";
                        }
                        lblResult.Text += "CALID校验不合格";
                    }
                    if (!_obdTest.CVNCheckResult) {
                        if (lblResult.Text.Length > 3) {
                            lblResult.Text += "，";
                        }
                        lblResult.Text += "CVN校验不合格";
                    }

                }
            });
        }

        private CancellationTokenSource UpdateUITask(string strMsg) {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            Task.Factory.StartNew(() => {
                int count = 0;
                while (!token.IsCancellationRequested) {
                    try {
                        Invoke((EventHandler)delegate {
                            lblResult.ForeColor = Color.Black;
                            if (count == 0) {
                                lblResult.Text = strMsg + "。。。";
                            } else {
                                lblResult.Text = strMsg + "，用时" + count.ToString() + "s";
                            }
                        });
                    } catch (ObjectDisposedException ex) {
                        _obdIfEx.Log.TraceWarning(ex.Message);
                    }
                    Thread.Sleep(1000);
                    ++count;
                }
            }, token);
            return tokenSource;
        }

        private void ResizeFont(Control control, float scale) {
            control.Font = new Font(control.Font.FontFamily, control.Font.Size * scale, control.Font.Style);
        }

        private void OBDStartForm_Resize(object sender, EventArgs e) {
            if (_lastHeight == 0) {
                return;
            }
            float scale = Height / _lastHeight;
            ResizeFont(txtBoxVIN, scale);
            ResizeFont(lblVIN, scale);
            ResizeFont(lblVehicleType, scale);
            ResizeFont(txtBoxVehicleType, scale);
            ResizeFont(lblResult, scale);
            ResizeFont(lblVINError, scale);
            ResizeFont(lblCALIDCVN, scale);
            ResizeFont(lblDTC, scale);
            ResizeFont(btnAdvanceMode, scale);
            _lastHeight = Height;
        }

        private void BtnAdvancedMode_Click(object sender, EventArgs e) {
            _obdTest.AccessAdvancedMode = 0;
            PassWordForm passWordForm = new PassWordForm(_obdTest);
            passWordForm.ShowDialog();
            if (_obdTest.AccessAdvancedMode > 0) {
                _obdTest.AdvancedMode = true;
                f_Advanced = new AdvancedForm(_obdIfEx, _obdTest);
                f_Advanced.Show();
            } else if (_obdTest.AccessAdvancedMode < 0) {
                MessageBox.Show("密码错误！", "拒绝访问", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtBoxVIN.Focus();
            } else {
                txtBoxVIN.Focus();
            }
            passWordForm.Dispose();
        }

        private void OBDStartForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (f_Advanced != null) {
                f_Advanced.Close();
            }

            Monitor.Enter(_obdIfEx);
            if (_obdIfEx.OBDIf.ConnectedStatus) {
                _obdIfEx.OBDIf.Disconnect();
            }
            Monitor.Exit(_obdIfEx);
        }

        private void OBDStartForm_Load(object sender, EventArgs e) {
            lblResult.ForeColor = Color.Black;
            lblResult.Text = "准备OBD检测";
            txtBoxVIN.Focus();
            lblVINError.ForeColor = Color.Gray;
            lblCALIDCVN.ForeColor = Color.Gray;
            lblDTC.ForeColor = Color.Gray;
        }

        private void OBDStartForm_Activated(object sender, EventArgs e) {
            Control con = ActiveControl;
            if (con is TextBox tb) {
                tb.Focus();
            }
        }

        private void TestOracleConnect() {
            try {
                _obdTest.DbMES.ConnectOracle();
            } catch (Exception ex) {
                MessageBox.Show("检测到与MES通讯异常，数据将无法上传: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtBox_KeyPress(object sender, KeyPressEventArgs e) {
            if (e.KeyChar == (char)Keys.Enter) {
                if (!_bCanOBDTest) {
                    txtBoxVIN.SelectAll();
                    MessageBox.Show("上一辆车还未完全结束检测过程，请稍后再试", "OBD检测出错", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                TextBox tb = sender as TextBox;
                string[] codes = tb.Text.Split('*');
                if (codes != null) {
                    if (codes.Length > 2) {
                        _obdTest.StrVIN_IN = codes[2];
                    }
                    _obdTest.StrType_IN = codes[0];
                    txtBoxVIN.Text = _obdTest.StrVIN_IN;
                    txtBoxVehicleType.Text = _obdTest.StrType_IN;
                }
                if (_obdTest.StrVIN_IN.Length == 17 && _obdTest.StrType_IN.Length >= 10) {
                    if (!_obdTest.AdvancedMode) {
                        Task.Factory.StartNew(StartOBDTest);
                        txtBoxVIN.SelectAll();
                        txtBoxVehicleType.SelectAll();
                    }
                }
            }
        }

        private void MenuItemStat_Click(object sender, EventArgs e) {
            StatisticForm form = new StatisticForm(_obdTest);
            form.ShowDialog();
            form.Dispose();
        }
    }
}
