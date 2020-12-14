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
        private readonly OBDIfEx _obdIfEx;
        private readonly OBDTest _obdTest;
        private AdvancedForm f_MainForm;
        private readonly Color _backColor;
        private float _lastHeight;
        private string _serialRecvBuf;
        readonly System.Timers.Timer _timer;
        CancellationTokenSource _ctsOBDTestStart;
        CancellationTokenSource _ctsSetupColumnsDone;
        CancellationTokenSource _ctsWriteDbStart;
        CancellationTokenSource _ctsUploadDataStart;

        public OBDStartForm() {
            InitializeComponent();
            this.Text += " Ver(Main/Dll): " + MainFileVersion.AssemblyVersion + "/" + DllVersion<SH_OBD_Dll>.AssemblyVersion;
            _bCanOBDTest = true;
            _lastHeight = this.Height;
            _serialRecvBuf = "";
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
                _obdIfEx._sp.DataReceived += new SerialPortClass.SerialPortDataReceiveEventArgs(SerialDataReceived);
            }
            _obdTest.OBDTestStart += new Action(OnOBDTestStart);
            _obdTest.SetupColumnsDone += new Action(OnSetupColumnsDone);
            _obdTest.WriteDbStart += new Action(OnWriteDbStart);
            _obdTest.WriteDbDone += new Action(OnWriteDbDone);
            _obdTest.UploadDataStart += new Action(OnUploadDataStart);
            _obdTest.UploadDataDone += new Action(OnUploadDataDone);
#if !DEBUG
            // 删除WebService上传接口缓存dll
            string dllPath = ".\\" + _obdIfEx.DBandMES.WSMES.Name + ".dll";
            try {
                if (File.Exists(dllPath)) {
                    File.Delete(dllPath);
                }
            } catch (Exception ex) {
                _obdIfEx.Log.TraceError("Delete WebService dll file failed: " + ex.Message);
            }
#endif
            // 测试本地数据库连接是否正常
            Task.Factory.StartNew(TestNativeDatabase);

            // 定时上传以前上传失败的数据
            _timer = new System.Timers.Timer(_obdIfEx.OBDResultSetting.UploadInterval * 60 * 1000);
            _timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimeUpload);
            _timer.AutoReset = true;
            _timer.Enabled = true;
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
#if DEBUG
                MessageBox.Show(errorMsg, WSHelper.GetMethodName(0));
#endif
            } catch (Exception ex) {
                _obdIfEx.Log.TraceError("UploadDataFromDBOnTime fialed：" + ex.Message);
            }

        }

        void OnOBDTestStart() {
            if (!_obdTest.AdvanceMode) {
                _ctsOBDTestStart = UpdateUITask("开始OBD检测");
            }
        }

        void OnSetupColumnsDone() {
            if (!_obdTest.AdvanceMode) {
                _ctsOBDTestStart.Cancel();
                _ctsSetupColumnsDone = UpdateUITask("正在读取车辆信息");
            }
        }

        void OnWriteDbStart() {
            if (!_obdTest.AdvanceMode) {
                _ctsSetupColumnsDone.Cancel();
                _ctsWriteDbStart = UpdateUITask("正在写入本地数据库");
            }
        }

        void OnWriteDbDone() {
            if (!_obdTest.AdvanceMode) {
                _ctsWriteDbStart.Cancel();
                this.Invoke((EventHandler)delegate {
                    this.lblResult.ForeColor = Color.Black;
                    this.lblResult.Text = "写入本地数据库结束";
                });
            }
        }

        void OnUploadDataStart() {
            if (!_obdTest.AdvanceMode) {
                _ctsUploadDataStart = UpdateUITask("正在上传数据");
            }
        }

        void OnUploadDataDone() {
            if (!_obdTest.AdvanceMode) {
                _ctsUploadDataStart.Cancel();
                this.Invoke((EventHandler)delegate {
                    this.lblResult.ForeColor = Color.Black;
                    this.lblResult.Text = "上传数据结束";
                });
            }
        }

        void SerialDataReceived(object sender, SerialDataReceivedEventArgs e, byte[] bits) {
            // 以回车符作为输入结束标志，处理串口输入的VIN号，串口数据可能会有断包问题需要处理
            Control con = this.ActiveControl;
            if (con is TextBox tb) {
                _serialRecvBuf += Encoding.Default.GetString(bits);
                if (_serialRecvBuf.Contains("\n")) {
                    if (!_bCanOBDTest) {
                        this.Invoke((EventHandler)delegate {
                            this.txtBoxVIN.SelectAll();
                            MessageBox.Show("上一辆车还未完全结束检测过程，请稍后再试", "OBD检测出错", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        });
                        _serialRecvBuf = "";
                        return;
                    }
                    _serialRecvBuf = _serialRecvBuf.Trim().ToUpper();
                    this.Invoke((EventHandler)delegate {
                        this.txtBoxVIN.Text = _serialRecvBuf;
                    });
                    if (_serialRecvBuf.Length >= 17) {
                        _bCanOBDTest = false;
                        _obdTest.StrVIN_IN = _serialRecvBuf;
                        _serialRecvBuf = "";
                        _obdIfEx.Log.TraceInfo("Get scanned VIN: " + _obdTest.StrVIN_IN + " by serial port scanner");
                        if (!_obdTest.AdvanceMode) {
                            Task.Factory.StartNew(StartOBDTest);
                        }
                    }
                }
            }
        }

        private void StartOBDTest() {
            this.Invoke((EventHandler)delegate {
                this.lblResult.ForeColor = Color.Black;
                this.lblResult.Text = "准备OBD检测";
                this.lblVINError.BackColor = _backColor;
                this.lblVINError.ForeColor = Color.Gray;
                this.lblCALIDCVN.BackColor = _backColor;
                this.lblCALIDCVN.ForeColor = Color.Gray;
                this.lblOBDSUP.BackColor = _backColor;
                this.lblOBDSUP.ForeColor = Color.Gray;
            });
            _obdIfEx.Log.TraceInfo(">>>>>>>>>> Start to test vehicle of VIN: " + _obdTest.StrVIN_IN + " MainVersion: " + MainFileVersion.AssemblyVersion + " <<<<<<<<<<");
            if (_obdIfEx.OBDIf.ConnectedStatus) {
                _obdIfEx.OBDIf.Disconnect();
            }
            this.Invoke((EventHandler)delegate {
                this.lblResult.ForeColor = Color.Black;
                this.lblResult.Text = "正在连接车辆。。。";
            });
            CancellationTokenSource tokenSource = UpdateUITask("正在连接车辆");
            if (!_obdIfEx.OBDDll.ConnectOBD()) {
                tokenSource.Cancel();
                this.Invoke((EventHandler)delegate {
                    this.lblResult.ForeColor = Color.Red;
                    this.lblResult.Text = "连接车辆失败！";
                });
                _bCanOBDTest = true;
                return;
            }
            tokenSource.Cancel();

            string errorMsg = "";
            bool bNoTestRecord = false;
            bool bTestException = false;
            try {
                _obdTest.StartOBDTest(out errorMsg);

                // 江铃股份操作工反应会有少量车辆漏检，故加入二次检查被测车辆是否已经检测过
                Dictionary<string, string> whereDic = new Dictionary<string, string> { { "VIN", _obdTest.StrVIN_ECU } };
                DataTable dt = new DataTable("OBDData");
                _obdTest.DbNative.GetRecords(dt, whereDic);
                if (dt.Rows.Count <= 0) {
                    _obdIfEx.Log.TraceError("No test record of this vehicle: " + _obdTest.StrVIN_ECU);
                    _obdTest.OBDResult = false;
                    bNoTestRecord = true;
                }
            } catch (Exception ex) {
                if (_obdTest.StrVIN_ECU == null || _obdTest.StrVIN_ECU.Length == 0) {
                    _obdTest.StrVIN_ECU = _obdTest.StrVIN_IN;
                }
                _obdIfEx.Log.TraceError("OBD test occurred error: " + ex.Message + (errorMsg.Length > 0 ? ", " + errorMsg : ""));
                bTestException = true;
                MessageBox.Show(ex.Message + (errorMsg.Length > 0 ? "\n" + errorMsg : ""), "OBD检测出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            this.Invoke((EventHandler)delegate {
                if (_obdTest.OBDResult) {
                    this.lblResult.ForeColor = Color.GreenYellow;
                    this.lblResult.Text = "被检车辆: " + _obdTest.StrVIN_ECU + "\nOBD检测结果：合格";
                    this.txtBoxVIN.Text = "";
                } else {
                    if (!_obdTest.VINResult) {
                        this.lblVINError.BackColor = Color.Red;
                        this.lblVINError.ForeColor = Color.Black;
                    }
                    if (!_obdTest.CALIDCVNResult || !_obdTest.CALIDUnmeaningResult) {
                        this.lblCALIDCVN.BackColor = Color.Red;
                        this.lblCALIDCVN.ForeColor = Color.Black;
                    }
                    if (!_obdTest.OBDSUPResult) {
                        this.lblOBDSUP.BackColor = Color.Red;
                        this.lblOBDSUP.ForeColor = Color.Black;
                    }

                    this.lblResult.ForeColor = Color.Red;
                    if (bNoTestRecord) {
                        this.lblResult.Text = "被检车辆: " + _obdTest.StrVIN_ECU + "\n没有本地检测记录";
                    } else if (bTestException) {
                        this.lblResult.Text = "被检车辆: " + _obdTest.StrVIN_ECU + "\nOBD检测过程发生异常";
                    } else {
                        this.lblResult.Text = "被检车辆: " + _obdTest.StrVIN_ECU + "\nOBD检测结果：不合格";
                    }
                }
            });
            if (_obdTest.CALIDCVNAllEmpty) {
                MessageBox.Show("CALID和CVN均为空！请检查OBD线缆接头连接是否牢固。", "OBD检测出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            _obdTest.StrVIN_IN = "";
            _obdTest.StrVIN_ECU = "";
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

        private CancellationTokenSource UpdateUITask(string strMsg) {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            Task.Factory.StartNew(() => {
                int count = 0;
                while (!token.IsCancellationRequested) {
                    try {
                        this.Invoke((EventHandler)delegate {
                            this.lblResult.ForeColor = Color.Black;
                            if (count == 0) {
                                this.lblResult.Text = strMsg + "。。。";
                            } else {
                                this.lblResult.Text = strMsg + "，用时" + count.ToString() + "s";
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
            float scale = this.Height / _lastHeight;
            ResizeFont(this.lblLogo, scale);
            ResizeFont(this.txtBoxVIN, scale);
            ResizeFont(this.lblVIN, scale);
            ResizeFont(this.lblResult, scale);
            ResizeFont(this.lblVINError, scale);
            ResizeFont(this.lblCALIDCVN, scale);
            ResizeFont(this.lblOBDSUP, scale);
            ResizeFont(this.btnAdvanceMode, scale);
            _lastHeight = this.Height;
        }

        private void BtnAdvanceMode_Click(object sender, EventArgs e) {
            _obdTest.AccessAdvanceMode = 0;
            PassWordForm passWordForm = new PassWordForm(_obdTest);
            passWordForm.ShowDialog();
            if (_obdTest.AccessAdvanceMode > 0) {
                _obdTest.AdvanceMode = true;
                f_MainForm = new AdvancedForm(_obdIfEx, _obdTest);
                f_MainForm.Show();
            } else if (_obdTest.AccessAdvanceMode < 0) {
                MessageBox.Show("密码错误！", "拒绝访问", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.txtBoxVIN.Focus();
            } else {
                this.txtBoxVIN.Focus();
            }
            passWordForm.Dispose();
        }

        private void OBDStartForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (f_MainForm != null) {
                f_MainForm.Close();
            }
            if (_timer != null) {
                _timer.Dispose();
            }

            Monitor.Enter(_obdIfEx);
            if (_obdIfEx.OBDIf.ConnectedStatus) {
                _obdIfEx.OBDIf.Disconnect();
            }
            Monitor.Exit(_obdIfEx);
        }

        private void OBDStartForm_Load(object sender, EventArgs e) {
            this.lblResult.ForeColor = Color.Black;
            this.lblResult.Text = "准备OBD检测";
            this.txtBoxVIN.Focus();
            this.lblVINError.ForeColor = Color.Gray;
            this.lblCALIDCVN.ForeColor = Color.Gray;
            this.lblOBDSUP.ForeColor = Color.Gray;
        }

        private void TxtBoxVIN_KeyPress(object sender, KeyPressEventArgs e) {
            // 以回车符作为输入结束标志，处理USB扫码枪扫描的或者人工输入的VIN号
            if (e.KeyChar == (char)Keys.Enter) {
                if (!_bCanOBDTest) {
                    this.txtBoxVIN.SelectAll();
                    MessageBox.Show("上一辆车还未完全结束检测过程，请稍后再试", "OBD检测出错", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string strTxt = this.txtBoxVIN.Text.Trim();
                if (strTxt.Length >= 17) {
                    _bCanOBDTest = false;
                    _obdTest.StrVIN_IN = strTxt.Substring(strTxt.Length - 17, 17);
                    _obdIfEx.Log.TraceInfo("Get scanned VIN: " + _obdTest.StrVIN_IN);
                    if (!_obdTest.AdvanceMode) {
                        Task.Factory.StartNew(StartOBDTest);
                    }
                    this.txtBoxVIN.Text = _obdTest.StrVIN_IN;
                    this.txtBoxVIN.SelectAll();
                }
            }
        }

        private void OBDStartForm_Activated(object sender, EventArgs e) {
            this.txtBoxVIN.Focus();
        }

        private void MenuItemStat_Click(object sender, EventArgs e) {
            StatisticForm form = new StatisticForm(_obdTest);
            form.ShowDialog();
            form.Dispose();
        }
    }
}
