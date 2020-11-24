using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using SH_OBD_DLL;

namespace SH_OBD_Main {
    public partial class OBDTestForm : Form {
        private readonly OBDIfEx _obdIfEx;
        private readonly OBDTest _obdTest;
        CancellationTokenSource _ctsOBDTestStart;
        CancellationTokenSource _ctsSetupColumnsDone;
        CancellationTokenSource _ctsWriteDbStart;
        CancellationTokenSource _ctsUploadDataStart;

        public OBDTestForm(OBDIfEx obdIfEx, OBDTest obdTest) {
            InitializeComponent();
            _obdIfEx = obdIfEx;
            _obdTest = obdTest;
            btnStartOBDTest.Enabled = false;
        }

        void OnOBDTestStart() {
            _ctsOBDTestStart = UpdateUITask("开始OBD检测");
        }

        void OnSetupColumnsDone() {
            if (_ctsOBDTestStart != null) {
                _ctsOBDTestStart.Cancel();
            }
            _ctsSetupColumnsDone = UpdateUITask("正在读取车辆信息");
            Invoke((EventHandler)delegate {
                GridViewInfo.DataSource = _obdTest.GetDataTable(DataTableType.dtInfo);
                GridViewECUInfo.DataSource = _obdTest.GetDataTable(DataTableType.dtECUInfo);
                GridViewIUPR.DataSource = _obdTest.GetDataTable(DataTableType.dtIUPR);
                if (GridViewInfo.Columns.Count > 1) {
                    GridViewInfo.Columns[0].Width = 30;
                    GridViewInfo.Columns[1].Width = GridViewInfo.Columns[0].Width * 5;
                    SetGridViewColumnsSortMode(GridViewInfo, DataGridViewColumnSortMode.Programmatic);
                }
                if (GridViewECUInfo.Columns.Count > 1) {
                    GridViewECUInfo.Columns[0].Width = GridViewInfo.Columns[0].Width;
                    GridViewECUInfo.Columns[1].Width = GridViewInfo.Columns[1].Width;
                    SetGridViewColumnsSortMode(GridViewECUInfo, DataGridViewColumnSortMode.Programmatic);
                }
                if (GridViewIUPR.Columns.Count > 1) {
                    GridViewIUPR.Columns[0].Width = GridViewInfo.Columns[0].Width;
                    GridViewIUPR.Columns[1].Width = GridViewInfo.Columns[0].Width * 8;
                    SetGridViewColumnsSortMode(GridViewIUPR, DataGridViewColumnSortMode.Programmatic);
                }
            });
        }

        void OnWriteDbStart() {
            _ctsSetupColumnsDone.Cancel();
            _ctsWriteDbStart = UpdateUITask("正在写入本地数据库");
            Invoke((EventHandler)delegate {
                txtBoxVIN.ReadOnly = false;
                txtBoxVehicleType.ReadOnly = false;
                GridViewInfo.Invalidate();
                GridViewECUInfo.Invalidate();
                GridViewIUPR.Invalidate();
            });
        }

        void OnWriteDbDone() {
            _ctsWriteDbStart.Cancel();
            Invoke((EventHandler)delegate {
                labelMESInfo.ForeColor = Color.Black;
                labelMESInfo.Text = "数据库写入完成";
            });
        }

        void OnUploadDataStart() {
            if (_ctsSetupColumnsDone != null) {
                _ctsSetupColumnsDone.Cancel();
            }
            _ctsUploadDataStart = UpdateUITask("正在上传数据");
            Invoke((EventHandler)delegate {
                GridViewInfo.Invalidate();
                GridViewECUInfo.Invalidate();
                GridViewIUPR.Invalidate();
            });
        }

        void OnUploadDataDone() {
            _ctsUploadDataStart.Cancel();
            Invoke((EventHandler)delegate {
                labelMESInfo.ForeColor = Color.ForestGreen;
                labelMESInfo.Text = "上传数据结束";
            });
        }

        void OnNotUploadData() {
            if (_ctsSetupColumnsDone != null) {
                _ctsSetupColumnsDone.Cancel();
            }
            Invoke((EventHandler)delegate {
                if (!chkBoxShowData.Checked) {
                    labelMESInfo.ForeColor = Color.Red;
                    labelMESInfo.Text = "因OBD检测不合格，故数据不上传";
                }
                GridViewInfo.Invalidate();
                GridViewECUInfo.Invalidate();
                GridViewIUPR.Invalidate();
            });
        }

        void OnSetDataTableColumnsError(object sender, SetDataTableColumnsErrorEventArgs e) {
            Invoke((EventHandler)delegate {
                labelMESInfo.ForeColor = Color.Red;
                labelMESInfo.Text = e.ErrorMsg;
            });
        }

        void SerialDataReceived(object sender, SerialDataReceivedEventArgs e, byte[] bits) {
            // 跨UI线程调用UI控件要使用Invoke
            Invoke((EventHandler)delegate {
                txtBoxVIN.Text = Encoding.Default.GetString(bits).Trim();
                if (txtBoxVIN.Text.Length == 17 && !chkBoxManualUpload.Checked) {
                    btnStartOBDTest.PerformClick();
                }
            });
        }

        public void CheckConnection() {
            if (_obdIfEx.OBDIf.ConnectedStatus) {
                btnStartOBDTest.Enabled = true;
                labelInfo.ForeColor = Color.Black;
                labelInfo.Text = "准备OBD检测";
                txtBoxVIN.ReadOnly = false;
                txtBoxVehicleType.ReadOnly = false;
                txtBoxVIN.SelectAll();
                txtBoxVIN.Focus();
            } else {
                btnStartOBDTest.Enabled = false;
                labelInfo.ForeColor = Color.Red;
                labelInfo.Text = "等待连接车辆OBD接口";
                labelMESInfo.ForeColor = Color.Black;
                labelMESInfo.Text = "准备上传数据";
            }
        }

        private void SetGridViewColumnsSortMode(DataGridView gridView, DataGridViewColumnSortMode sortMode) {
            for (int i = 0; i < gridView.Columns.Count; i++) {
                gridView.Columns[i].SortMode = sortMode;
            }
        }

        private void OBDTestForm_Load(object sender, EventArgs e) {
            GridViewInfo.DataSource = _obdTest.GetDataTable(DataTableType.dtInfo);
            GridViewECUInfo.DataSource = _obdTest.GetDataTable(DataTableType.dtECUInfo);
            GridViewIUPR.DataSource = _obdTest.GetDataTable(DataTableType.dtIUPR);
            if (_obdIfEx.ScannerPortOpened) {
                _obdIfEx._sp.DataReceived += new SerialPortClass.SerialPortDataReceiveEventArgs(SerialDataReceived);
            }
            _obdTest.OBDTestStart += new Action(OnOBDTestStart);
            _obdTest.SetupColumnsDone += new Action(OnSetupColumnsDone);
            _obdTest.WriteDbStart += new Action(OnWriteDbStart);
            _obdTest.WriteDbDone += new Action(OnWriteDbDone);
            _obdTest.UploadDataStart += new Action(OnUploadDataStart);
            _obdTest.UploadDataDone += new Action(OnUploadDataDone);
            _obdTest.NotUploadData += new Action(OnNotUploadData);
            _obdTest.SetDataTableColumnsError += OnSetDataTableColumnsError;
            if (GridViewInfo.Columns.Count > 1) {
                GridViewInfo.Columns[0].Width = 30;
                GridViewInfo.Columns[1].Width = GridViewInfo.Columns[0].Width * 5;
                SetGridViewColumnsSortMode(GridViewInfo, DataGridViewColumnSortMode.Programmatic);
            }
            if (GridViewECUInfo.Columns.Count > 1) {
                GridViewECUInfo.Columns[0].Width = GridViewInfo.Columns[0].Width;
                GridViewECUInfo.Columns[1].Width = GridViewInfo.Columns[1].Width;
                SetGridViewColumnsSortMode(GridViewECUInfo, DataGridViewColumnSortMode.Programmatic);
            }
            if (GridViewIUPR.Columns.Count > 1) {
                GridViewIUPR.Columns[0].Width = GridViewInfo.Columns[0].Width;
                GridViewIUPR.Columns[1].Width = GridViewInfo.Columns[0].Width * 8;
                SetGridViewColumnsSortMode(GridViewIUPR, DataGridViewColumnSortMode.Programmatic);
            }
            txtBoxVIN.Text = _obdTest.StrVIN_IN;
            txtBoxVehicleType.Text = _obdTest.StrType_IN;
        }

        private void OBDTestForm_Resize(object sender, EventArgs e) {
            int margin = grpBoxInfo.Location.X;
            grpBoxInfo.Width = (ClientSize.Width - margin * 3) / 2;
            grpBoxInfo.Height = (ClientSize.Height - (btnStartOBDTest.Location.Y + btnStartOBDTest.Height) - margin * 3) * 2 / 3;
            grpBoxECUInfo.Location = new Point(grpBoxInfo.Location.X, grpBoxInfo.Location.Y + grpBoxInfo.Height + margin);
            grpBoxECUInfo.Width = grpBoxInfo.Width;
            grpBoxECUInfo.Height = grpBoxInfo.Height / 2;
            grpBoxIUPR.Location = new Point(grpBoxInfo.Location.X + grpBoxInfo.Width + margin, grpBoxInfo.Location.Y);
            grpBoxIUPR.Width = grpBoxInfo.Width;
            grpBoxIUPR.Height = ClientSize.Height - (btnStartOBDTest.Location.Y + btnStartOBDTest.Height) - margin * 2;
            labelMESInfo.Location = new Point(grpBoxIUPR.Location.X + grpBoxIUPR.Width / 3, labelInfo.Location.Y);
        }

        private void OBDTestForm_VisibleChanged(object sender, EventArgs e) {
            if (Visible) {
                CheckConnection();
            }
        }

        private void TxtBox_KeyPress(object sender, KeyPressEventArgs e) {
            if (e.KeyChar == (char)Keys.Enter) {
                TextBox tb = sender as TextBox;
                string[] codes = tb.Text.Split('*');
                if (codes != null) {
                    if (codes.Length > 2) {
                        _obdTest.StrVIN_IN = codes[2];
                        _obdTest.StrType_IN = codes[0];
                        txtBoxVIN.Text = _obdTest.StrVIN_IN;
                        txtBoxVehicleType.Text = _obdTest.StrType_IN;
                    } else {
                        if (tb.Name == "txtBoxVIN") {
                            _obdTest.StrVIN_IN = codes[0];
                        } else if (tb.Name == "txtBoxVehicleType") {
                            _obdTest.StrType_IN = codes[0];
                        }
                    }
                }
                if (chkBoxManualUpload.Checked || chkBoxShowData.Checked) {
                    if (txtBoxVIN.Text.Length == 17 && _obdTest.StrType_IN.Length >= 10) {
                        ManualUpload();
                    }
                } else {
                    if (_obdTest.StrVIN_IN.Length == 17 && _obdTest.StrType_IN.Length >= 10) {
                        _obdIfEx.Log.TraceInfo("Get VIN: " + txtBoxVIN.Text);
                        if (btnStartOBDTest.Enabled) {
                            txtBoxVIN.ReadOnly = true;
                            txtBoxVehicleType.ReadOnly = true;
                            btnStartOBDTest.PerformClick();
                        }
                    }
                }
            }
        }

        private void ManualUpload() {
            GridViewInfo.DataSource = null;
            GridViewECUInfo.DataSource = null;
            GridViewIUPR.DataSource = null;
            Task.Factory.StartNew(StartManualUpload);
        }

        private void StartManualUpload() {
            if (!_obdTest.AdvancedMode) {
                return;
            }
            _obdIfEx.Log.TraceInfo("Start ManualUpload");
            Invoke((EventHandler)delegate {
                labelInfo.ForeColor = Color.Black;
                labelInfo.Text = "手动读取数据";
                labelMESInfo.ForeColor = Color.Black;
                labelMESInfo.Text = "准备手动上传数据";
            });
            try {
                _obdTest.UploadDataFromDB(txtBoxVIN.Text, out string errorMsg, chkBoxShowData.Checked);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "手动上传数据出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Invoke((EventHandler)delegate {
                labelInfo.ForeColor = Color.Black;
                labelInfo.Text = "结果数据显示完毕";
            });
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
        }

        private void BtnStartOBDTest_Click(object sender, EventArgs e) {
            GridViewInfo.DataSource = null;
            GridViewECUInfo.DataSource = null;
            GridViewIUPR.DataSource = null;
            Task.Factory.StartNew(StartOBDTest);
        }

        private void StartOBDTest() {
            if (!_obdTest.AdvancedMode) {
                return;
            }
            Invoke((EventHandler)delegate {
                labelInfo.ForeColor = Color.Black;
                labelInfo.Text = "准备OBD检测";
                labelMESInfo.ForeColor = Color.Black;
                labelMESInfo.Text = "准备上传数据";
            });
            string errorMsg = "";
            try {
                _obdTest.StartOBDTest(out errorMsg);
            } catch (Exception ex) {
                _obdIfEx.Log.TraceError("OBD test occurred error: " + errorMsg + ", " + ex.Message);
                MessageBox.Show(ex.Message, "OBD检测出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Invoke((EventHandler)delegate {
                if (_obdTest.OBDResult) {
                    labelInfo.ForeColor = Color.ForestGreen;
                    labelInfo.Text = "OBD检测结束，结果：合格";
                } else {
                    string strCat = "";
                    if (!_obdTest.DTCResult) {
                        strCat += "，存在DTC故障码";
                    }
                    if (!_obdTest.ReadinessResult) {
                        strCat += "，就绪状态未完成项超过2项";
                    }
                    if (!_obdTest.VINResult) {
                        strCat += "，VIN号不匹配";
                    }
                    labelInfo.ForeColor = Color.Red;
                    labelInfo.Text = "OBD检测结束，结果：不合格" + strCat;
                }
                txtBoxVIN.ReadOnly = false;
                txtBoxVehicleType.ReadOnly = false;
            });
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
        }

        private CancellationTokenSource UpdateUITask(string strMsg) {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            Task.Factory.StartNew(() => {
                int count = 0;
                while (!token.IsCancellationRequested) {
                    try {
                        Invoke((EventHandler)delegate {
                            labelInfo.ForeColor = Color.Black;
                            if (count == 0) {
                                labelInfo.Text = strMsg + "。。。";
                            } else {
                                labelInfo.Text = strMsg + "，用时" + count.ToString() + "s";
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

        private void OBDTestForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (_obdIfEx.ScannerPortOpened) {
                _obdIfEx._sp.DataReceived -= new SerialPortClass.SerialPortDataReceiveEventArgs(SerialDataReceived);
            }
            _obdTest.AdvancedMode = false;
            _obdTest.OBDTestStart -= new Action(OnOBDTestStart);
            _obdTest.SetupColumnsDone -= new Action(OnSetupColumnsDone);
            _obdTest.WriteDbStart -= new Action(OnWriteDbStart);
            _obdTest.WriteDbDone -= new Action(OnWriteDbDone);
            _obdTest.UploadDataStart -= new Action(OnUploadDataStart);
            _obdTest.UploadDataDone -= new Action(OnUploadDataDone);
            _obdTest.NotUploadData -= new Action(OnNotUploadData);
            _obdTest.SetDataTableColumnsError -= OnSetDataTableColumnsError;
        }
    }

}
