﻿using SH_OBD_DLL;
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
        public static bool m_bCanOBDTest;
        private readonly OBDIfEx m_obdIfEx;
        private readonly OBDTest m_obdTest;
        private MainForm f_MainForm;
        private readonly Color m_backColor;
        private float m_lastHeight;
        private string m_serialRecvBuf;
        readonly System.Timers.Timer m_timer;
        CancellationTokenSource m_ctsOBDTestStart;
        CancellationTokenSource m_ctsSetupColumnsDone;
        CancellationTokenSource m_ctsWriteDbStart;
        CancellationTokenSource m_ctsUploadDataStart;

        public OBDStartForm() {
            InitializeComponent();
            this.Text += " Ver(Main/Dll): " + MainFileVersion.AssemblyVersion + "/" + DllVersion<SH_OBD_Dll>.AssemblyVersion;
            m_bCanOBDTest = true;
            m_lastHeight = this.Height;
            m_serialRecvBuf = "";
            m_obdIfEx = new OBDIfEx();
            if (m_obdIfEx.StrLoadConfigResult.Length > 0) {
                m_obdIfEx.StrLoadConfigResult += "是否要以默认配置运行程序？点击\"否\"：将会退出程序。";
                DialogResult result = MessageBox.Show(m_obdIfEx.StrLoadConfigResult, "加载配置文件出错", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No) {
                    Environment.Exit(0);
                }
            }
            m_obdTest = new OBDTest(m_obdIfEx);
            m_backColor = label1.BackColor;
            if (m_obdIfEx.ScannerPortOpened) {
                m_obdIfEx.m_sp.DataReceived += new SerialPortClass.SerialPortDataReceiveEventArgs(SerialDataReceived);
            }
            m_obdTest.OBDTestStart += new Action(OnOBDTestStart);
            m_obdTest.SetupColumnsDone += new Action(OnSetupColumnsDone);
            m_obdTest.WriteDbStart += new Action(OnWriteDbStart);
            m_obdTest.WriteDbDone += new Action(OnWriteDbDone);
            m_obdTest.UploadDataStart += new Action(OnUploadDataStart);
            m_obdTest.UploadDataDone += new Action(OnUploadDataDone);
#if !DEBUG
            // 删除WebService上传接口缓存dll
            string dllPath = ".\\" + m_obdInterface.DBandMES.WebServiceName + ".dll";
            try {
                if (File.Exists(dllPath)) {
                    File.Delete(dllPath);
                }
            } catch (Exception ex) {
                m_obdInterface.m_log.TraceError("Delete WebService dll file failed: " + ex.Message);
            }
#endif
            Task.Factory.StartNew(TestNativeDatabase);
            // 在OBDData表中新增Upload字段，用于存储上传是否成功的标志
            Task.Factory.StartNew(m_obdTest.m_db.AddUploadField);
            // 在OBDUser表中新增SN字段，用于存储检测报表编号中顺序号的特征字符串
            Task.Factory.StartNew(m_obdTest.m_db.AddSNField);
            // 定时上传以前上传失败的数据
            m_timer = new System.Timers.Timer(m_obdIfEx.OBDResultSetting.UploadInterval * 60 * 1000);
            m_timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimeUpload);
            m_timer.AutoReset = true;
            m_timer.Enabled = true;
        }

        private void TestNativeDatabase() {
            try {
                m_obdTest.m_db.ShowDB("OBDUser");
            } catch (Exception ex) {
                m_obdIfEx.Log.TraceError("Access native database failed: " + ex.Message);
                MessageBox.Show("检测到数据库通讯异常，请排查相关故障：\n" + ex.Message, "数据库通讯异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnTimeUpload(object source, System.Timers.ElapsedEventArgs e) {
            m_obdIfEx.Log.TraceInfo("Start UploadDataFromDBOnTime");
            try {
                m_obdTest.UploadDataFromDBOnTime(out string errorMsg);
#if DEBUG
                MessageBox.Show(errorMsg, WSHelper.GetMethodName(0));
#endif
            } catch (Exception ex) {
                m_obdIfEx.Log.TraceError("UploadDataFromDBOnTime fialed：" + ex.Message);
            }

        }

        void OnOBDTestStart() {
            if (!m_obdTest.AdvanceMode) {
                m_ctsOBDTestStart = UpdateUITask("开始OBD检测");
            }
        }

        void OnSetupColumnsDone() {
            if (!m_obdTest.AdvanceMode) {
                m_ctsOBDTestStart.Cancel();
                m_ctsSetupColumnsDone = UpdateUITask("正在读取车辆信息");
            }
        }

        void OnWriteDbStart() {
            if (!m_obdTest.AdvanceMode) {
                m_ctsSetupColumnsDone.Cancel();
                m_ctsWriteDbStart = UpdateUITask("正在写入本地数据库");
            }
        }

        void OnWriteDbDone() {
            if (!m_obdTest.AdvanceMode) {
                m_ctsWriteDbStart.Cancel();
                this.Invoke((EventHandler)delegate {
                    this.labelResult.ForeColor = Color.Black;
                    this.labelResult.Text = "写入本地数据库结束";
                });
            }
        }

        void OnUploadDataStart() {
            if (!m_obdTest.AdvanceMode) {
                m_ctsUploadDataStart = UpdateUITask("正在上传数据");
            }
        }

        void OnUploadDataDone() {
            if (!m_obdTest.AdvanceMode) {
                m_ctsUploadDataStart.Cancel();
                this.Invoke((EventHandler)delegate {
                    this.labelResult.ForeColor = Color.Black;
                    this.labelResult.Text = "上传数据结束";
                });
            }
        }

        void SerialDataReceived(object sender, SerialDataReceivedEventArgs e, byte[] bits) {
            // 以回车符作为输入结束标志，处理串口输入的VIN号，串口数据可能会有断包问题需要处理
            Control con = this.ActiveControl;
            if (con is TextBox tb) {
                m_serialRecvBuf += Encoding.Default.GetString(bits);
                if (m_serialRecvBuf.Contains("\n")) {
                    if (!m_bCanOBDTest) {
                        this.Invoke((EventHandler)delegate {
                            this.txtBoxVIN.SelectAll();
                            MessageBox.Show("上一辆车还未完全结束检测过程，请稍后再试", "OBD检测出错", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        });
                        m_serialRecvBuf = "";
                        return;
                    }
                    m_serialRecvBuf = m_serialRecvBuf.Trim().ToUpper();
                    this.Invoke((EventHandler)delegate {
                        this.txtBoxVIN.Text = m_serialRecvBuf;
                    });
                    if (m_serialRecvBuf.Length >= 17) {
                        m_bCanOBDTest = false;
                        m_obdTest.StrVIN_IN = m_serialRecvBuf;
                        m_serialRecvBuf = "";
                        m_obdIfEx.Log.TraceInfo("Get scanned VIN: " + m_obdTest.StrVIN_IN + " by serial port scanner");
                        if (!m_obdTest.AdvanceMode) {
                            Task.Factory.StartNew(StartOBDTest);
                        }
                    }
                }
            }
        }

        private void StartOBDTest() {
            this.Invoke((EventHandler)delegate {
                this.labelResult.ForeColor = Color.Black;
                this.labelResult.Text = "准备OBD检测";
                this.labelVINError.BackColor = m_backColor;
                this.labelVINError.ForeColor = Color.Gray;
                this.labelCALIDCVN.BackColor = m_backColor;
                this.labelCALIDCVN.ForeColor = Color.Gray;
                this.label3Space.BackColor = m_backColor;
                this.label3Space.ForeColor = Color.Gray;
            });
            m_obdIfEx.Log.TraceInfo(">>>>>>>>>> Start to test vehicle of VIN: " + m_obdTest.StrVIN_IN + " MainVersion: " + MainFileVersion.AssemblyVersion + " <<<<<<<<<<");
            if (m_obdIfEx.OBDIf.ConnectedStatus) {
                m_obdIfEx.OBDIf.Disconnect();
            }
            this.Invoke((EventHandler)delegate {
                this.labelResult.ForeColor = Color.Black;
                this.labelResult.Text = "正在连接车辆。。。";
            });
            CancellationTokenSource tokenSource = UpdateUITask("正在连接车辆");
            if (!m_obdIfEx.OBDDll.ConnectOBD()) {
                tokenSource.Cancel();
                this.Invoke((EventHandler)delegate {
                    this.labelResult.ForeColor = Color.Red;
                    this.labelResult.Text = "连接车辆失败！";
                });
                m_bCanOBDTest = true;
                return;
            }
            tokenSource.Cancel();

            string errorMsg = "";
            int VINCount = 0;
            bool bNoTestRecord = false;
            bool bTestException = false;
            try {
                m_obdTest.StartOBDTest(out errorMsg);
#if DEBUG
                MessageBox.Show(errorMsg, WSHelper.GetMethodName(0));
#endif

                // 江铃股份操作工反应会有少量车辆漏检，故加入二次检查被测车辆是否已经检测过
                Dictionary<string, string> whereDic = new Dictionary<string, string> { { "VIN", m_obdTest.StrVIN_ECU } };
                VINCount = m_obdTest.m_db.GetRecordCount("OBDData", whereDic);
                if (VINCount == 0) {
                    m_obdIfEx.Log.TraceError("No test record of this vehicle: " + m_obdTest.StrVIN_ECU);
                    m_obdTest.OBDResult = false;
                    bNoTestRecord = true;
                }
            } catch (Exception ex) {
                if (m_obdTest.StrVIN_ECU == null || m_obdTest.StrVIN_ECU.Length == 0) {
                    m_obdTest.StrVIN_ECU = m_obdTest.StrVIN_IN;
                }
                m_obdIfEx.Log.TraceError("OBD test occurred error: " + ex.Message + (errorMsg.Length > 0 ? ", " + errorMsg : ""));
                bTestException = true;
                MessageBox.Show(ex.Message + (errorMsg.Length > 0 ? "\n" + errorMsg : ""), "OBD检测出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            this.Invoke((EventHandler)delegate {
                if (m_obdTest.OBDResult) {
                    this.labelResult.ForeColor = Color.GreenYellow;
                    this.labelResult.Text = "被检车辆: " + m_obdTest.StrVIN_ECU + "\nOBD检测结果：合格";
                    this.txtBoxVIN.Text = "";
                } else {
                    if (!m_obdTest.VINResult) {
                        this.labelVINError.BackColor = Color.Red;
                        this.labelVINError.ForeColor = Color.Black;
                    }
                    if (!m_obdTest.CALIDCVNResult || !m_obdTest.CALIDUnmeaningResult) {
                        this.labelCALIDCVN.BackColor = Color.Red;
                        this.labelCALIDCVN.ForeColor = Color.Black;
                    }
                    if (!m_obdTest.OBDSUPResult) {
                        this.label3Space.BackColor = Color.Red;
                        this.label3Space.ForeColor = Color.Black;
                    }

                    this.labelResult.ForeColor = Color.Red;
                    if (bNoTestRecord) {
                        this.labelResult.Text = "被检车辆: " + m_obdTest.StrVIN_ECU + "\n没有本地检测记录";
                    } else if (bTestException) {
                        this.labelResult.Text = "被检车辆: " + m_obdTest.StrVIN_ECU + "\nOBD检测过程发生异常";
                    } else {
                        this.labelResult.Text = "被检车辆: " + m_obdTest.StrVIN_ECU + "\nOBD检测结果：不合格";
                    }
                }
            });
            if (m_obdTest.CALIDCVNAllEmpty) {
                MessageBox.Show("CALID和CVN均为空！请检查OBD线缆接头连接是否牢固。", "OBD检测出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            m_obdTest.StrVIN_IN = "";
            m_obdTest.StrVIN_ECU = "";
            if (m_ctsOBDTestStart != null) {
                m_ctsOBDTestStart.Cancel();
            }
            if (m_ctsSetupColumnsDone != null) {
                m_ctsSetupColumnsDone.Cancel();
            }
            if (m_ctsWriteDbStart != null) {
                m_ctsWriteDbStart.Cancel();
            }
            if (m_ctsUploadDataStart != null) {
                m_ctsUploadDataStart.Cancel();
            }
            m_bCanOBDTest = true;
        }

        private CancellationTokenSource UpdateUITask(string strMsg) {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            Task.Factory.StartNew(() => {
                int count = 0;
                while (!token.IsCancellationRequested) {
                    try {
                        this.Invoke((EventHandler)delegate {
                            this.labelResult.ForeColor = Color.Black;
                            if (count == 0) {
                                this.labelResult.Text = strMsg + "。。。";
                            } else {
                                this.labelResult.Text = strMsg + "，用时" + count.ToString() + "s";
                            }
                        });
                    } catch (ObjectDisposedException ex) {
                        m_obdIfEx.Log.TraceWarning(ex.Message);
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
            if (m_lastHeight == 0) {
                return;
            }
            float scale = this.Height / m_lastHeight;
            ResizeFont(this.txtBoxVIN, scale);
            ResizeFont(this.label1, scale);
            ResizeFont(this.labelResult, scale);
            ResizeFont(this.labelVINError, scale);
            ResizeFont(this.labelCALIDCVN, scale);
            ResizeFont(this.label3Space, scale);
            ResizeFont(this.btnAdvanceMode, scale);
            m_lastHeight = this.Height;
        }

        private void BtnAdvanceMode_Click(object sender, EventArgs e) {
            m_obdTest.AccessAdvanceMode = 0;
            PassWordForm passWordForm = new PassWordForm(m_obdTest);
            passWordForm.ShowDialog();
            if (m_obdTest.AccessAdvanceMode > 0) {
                m_obdTest.AdvanceMode = true;
                f_MainForm = new MainForm(m_obdIfEx, m_obdTest);
                f_MainForm.Show();
            } else if (m_obdTest.AccessAdvanceMode < 0) {
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
            if (m_timer != null) {
                m_timer.Dispose();
            }

            Monitor.Enter(m_obdIfEx);
            if (m_obdIfEx.OBDIf.ConnectedStatus) {
                m_obdIfEx.OBDIf.Disconnect();
            }
            Monitor.Exit(m_obdIfEx);
        }

        private void OBDStartForm_Load(object sender, EventArgs e) {
            this.labelResult.ForeColor = Color.Black;
            this.labelResult.Text = "准备OBD检测";
            this.txtBoxVIN.Focus();
            this.labelVINError.ForeColor = Color.Gray;
            this.labelCALIDCVN.ForeColor = Color.Gray;
            this.label3Space.ForeColor = Color.Gray;
        }

        private void TxtBoxVIN_KeyPress(object sender, KeyPressEventArgs e) {
            // 以回车符作为输入结束标志，处理USB扫码枪扫描的或者人工输入的VIN号
            if (e.KeyChar == (char)Keys.Enter) {
                if (!m_bCanOBDTest) {
                    this.txtBoxVIN.SelectAll();
                    MessageBox.Show("上一辆车还未完全结束检测过程，请稍后再试", "OBD检测出错", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string strTxt = this.txtBoxVIN.Text.Trim();
                if (strTxt.Length >= 17) {
                    m_bCanOBDTest = false;
                    m_obdTest.StrVIN_IN = strTxt.Substring(strTxt.Length - 17, 17);
                    m_obdIfEx.Log.TraceInfo("Get scanned VIN: " + m_obdTest.StrVIN_IN);
                    if (!m_obdTest.AdvanceMode) {
                        Task.Factory.StartNew(StartOBDTest);
                    }
                    this.txtBoxVIN.Text = m_obdTest.StrVIN_IN;
                    this.txtBoxVIN.SelectAll();
                }
            }
        }

        private void OBDStartForm_Activated(object sender, EventArgs e) {
            this.txtBoxVIN.Focus();
        }

        private void MenuItemStat_Click(object sender, EventArgs e) {
            StatisticForm form = new StatisticForm(m_obdTest);
            form.ShowDialog();
            form.Dispose();
        }
    }
}
