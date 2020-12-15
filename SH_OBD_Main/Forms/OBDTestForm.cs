using OfficeOpenXml;
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
    public partial class OBDTestForm : Form {
        private readonly OBDIfEx _obdIfEx;
        private readonly OBDTest _obdTest;
        private string[] _fileNames;
        private string _serialRecvBuf;
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
                    GridViewInfo.Columns[1].Width = 150;
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
            // 以回车符作为输入结束标志，处理串口输入的VIN号，串口数据可能会有断包问题需要处理
            Control con = ActiveControl;
            if (con is TextBox tb) {
                _serialRecvBuf += Encoding.Default.GetString(bits);
                if (_serialRecvBuf.Contains("\n")) {
                    _serialRecvBuf = _serialRecvBuf.Trim().ToUpper();
                    if (_serialRecvBuf.Length >= 17) {
                        Invoke((EventHandler)delegate {
                            _obdTest.StrVIN_IN = _serialRecvBuf.Substring(_serialRecvBuf.Length - 17, 17);
                            txtBoxVIN.Text = _obdTest.StrVIN_IN;
                            _serialRecvBuf = "";
                        });
                        if (chkBoxManualUpload.Checked || chkBoxShowData.Checked) {
                            Invoke((EventHandler)delegate {
                                ManualUpload();
                            });
                        } else {
                            _obdIfEx.Log.TraceInfo("Get scanned VIN: " + _obdTest.StrVIN_IN + " by serial port scanner in advance mode");
                            Invoke((EventHandler)delegate {
                                btnStartOBDTest.PerformClick();
                            });
                        }
                    }
                }
            }

        }

        public void CheckConnection() {
            if (_obdIfEx.OBDIf.ConnectedStatus) {
                btnStartOBDTest.Enabled = true;
                labelInfo.ForeColor = Color.Black;
                labelInfo.Text = "准备OBD检测";
                txtBoxVIN.ReadOnly = false;
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
                GridViewInfo.Columns[1].Width = 150;
                SetGridViewColumnsSortMode(GridViewInfo, DataGridViewColumnSortMode.Programmatic);
            }
            if (GridViewECUInfo.Columns.Count > 1) {
                GridViewECUInfo.Columns[0].Width = GridViewInfo.Columns[0].Width;
                SetGridViewColumnsSortMode(GridViewECUInfo, DataGridViewColumnSortMode.Programmatic);
            }
            if (GridViewIUPR.Columns.Count > 1) {
                GridViewIUPR.Columns[0].Width = GridViewInfo.Columns[0].Width;
                GridViewIUPR.Columns[1].Width = GridViewInfo.Columns[0].Width * 8;
                SetGridViewColumnsSortMode(GridViewIUPR, DataGridViewColumnSortMode.Programmatic);
            }
            txtBoxVIN.Text = _obdTest.StrVIN_IN;
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

        private void StartManualUpload() {
            if (!_obdTest.AdvanceMode) {
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
                _obdTest.UploadDataFromDB(_obdTest.StrVIN_IN, out string errorMsg, chkBoxShowData.Checked);
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

        private void ManualUpload() {
            GridViewInfo.DataSource = null;
            GridViewECUInfo.DataSource = null;
            GridViewIUPR.DataSource = null;
            Task.Factory.StartNew(StartManualUpload);
        }

        private void BtnStartOBDTest_Click(object sender, EventArgs e) {
            GridViewInfo.DataSource = null;
            GridViewECUInfo.DataSource = null;
            GridViewIUPR.DataSource = null;
            Task.Factory.StartNew(StartOBDTest);
        }

        private void StartOBDTest() {
            if (!_obdTest.AdvanceMode) {
                return;
            }
            _obdIfEx.Log.TraceInfo("Start OBD test in advance mode");
            Invoke((EventHandler)delegate {
                labelInfo.ForeColor = Color.Black;
                labelInfo.Text = "准备OBD检测";
                labelMESInfo.ForeColor = Color.Black;
                labelMESInfo.Text = "准备上传数据";
            });
            _obdIfEx.Log.TraceInfo(">>>>>>>>>> Start to test vehicle of VIN: " + _obdTest.StrVIN_IN + " <<<<<<<<<<");
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
                        strCat += "，存在故障码DTC";
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
            });
            if (_obdTest.CALIDCVNAllEmpty) {
                MessageBox.Show("CALID和CVN均为空！请检查OBD线缆接头连接是否牢固。", "OBD检测出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            _obdTest.AdvanceMode = false;
            _obdTest.OBDTestStart -= new Action(OnOBDTestStart);
            _obdTest.SetupColumnsDone -= new Action(OnSetupColumnsDone);
            _obdTest.WriteDbStart -= new Action(OnWriteDbStart);
            _obdTest.WriteDbDone -= new Action(OnWriteDbDone);
            _obdTest.UploadDataStart -= new Action(OnUploadDataStart);
            _obdTest.UploadDataDone -= new Action(OnUploadDataDone);
            _obdTest.NotUploadData -= new Action(OnNotUploadData);
            _obdTest.SetDataTableColumnsError -= OnSetDataTableColumnsError;
        }

        private void TxtBoxVIN_KeyPress(object sender, KeyPressEventArgs e) {
            // 以回车符作为输入结束标志，处理USB扫码枪扫描的或者人工输入的VIN号
            if (e.KeyChar == (char)Keys.Enter) {
                string strTxt = txtBoxVIN.Text.Trim();
                if (strTxt.Length >= 17) {
                    _obdTest.StrVIN_IN = strTxt.Substring(strTxt.Length - 17, 17);
                    txtBoxVIN.Text = _obdTest.StrVIN_IN;
                    txtBoxVIN.SelectAll();
                    if (chkBoxManualUpload.Checked || chkBoxShowData.Checked) {
                        ManualUpload();
                    } else {
                        _obdIfEx.Log.TraceInfo("Get scanned VIN: " + txtBoxVIN.Text + "in advance mode");
                        txtBoxVIN.ReadOnly = true;
                        btnStartOBDTest.PerformClick();
                    }
                }
            }
        }

        private void BtnImport_Click(object sender, EventArgs e) {
            labelMESInfo.ForeColor = Color.Black;
            labelMESInfo.Text = "Excel报表数据导入中。。。";
            OpenFileDialog openFileDialog = new OpenFileDialog {
                Title = "打开 Excel 报表文件",
                Filter = "Excel 2007 及以上 (*.xlsx)|*.xlsx",
                FilterIndex = 0,
                RestoreDirectory = true,
                Multiselect = true
            };
            try {
                openFileDialog.ShowDialog();
                if (openFileDialog.FileNames.Length <= 0) {
                    return;
                }
                _fileNames = openFileDialog.FileNames;
                Task.Factory.StartNew(ImportExcel);
            } finally {
                openFileDialog.Dispose();
            }
        }

        private void ImportExcel() {
            DataTable dtImport = new DataTable("OBDData");
            dtImport.Columns.Add("VIN");
            dtImport.Columns.Add("ECU_ID");
            dtImport.Columns.Add("OBD_SUP");
            dtImport.Columns.Add("ODO");
            dtImport.Columns.Add("CAL_ID");
            dtImport.Columns.Add("CVN");
            dtImport.Columns.Add("Result");
            try {
                if (_fileNames == null) {
                    Invoke((EventHandler)delegate {
                        labelMESInfo.ForeColor = Color.Red;
                        labelMESInfo.Text = "无Excel报表文件";
                    });
                    return;
                }
                foreach (string file in _fileNames) {
                    FileInfo fileInfo = new FileInfo(file);
                    using (ExcelPackage package = new ExcelPackage(fileInfo, true)) {
                        ExcelWorksheet worksheet1 = package.Workbook.Worksheets[1];
                        DataRow dr = dtImport.NewRow();
                        dr["VIN"] = worksheet1.Cells["B2"].Value;
                        dr["ECU_ID"] = worksheet1.Cells["E3"].Value;
                        dr["OBD_SUP"] = worksheet1.Cells["B9"].Value;
                        dr["ODO"] = worksheet1.Cells["B10"].Value;
                        string CALID;
                        string CVN;
                        CALID = worksheet1.Cells["B3"].Value == null ? "" : worksheet1.Cells["B3"].Value.ToString();
                        CVN = worksheet1.Cells["D3"].Value == null ? "" : worksheet1.Cells["D3"].Value.ToString();
                        if (worksheet1.Cells["B4"].Value != null && worksheet1.Cells["B4"].Value.ToString().Length > 0) {
                            CALID += "," + worksheet1.Cells["B4"].Value.ToString();
                        }
                        if (worksheet1.Cells["D4"].Value != null && worksheet1.Cells["D4"].Value.ToString().Length > 0) {
                            CVN += "," + worksheet1.Cells["D4"].Value.ToString();
                        }
                        dr["CAL_ID"] = CALID;
                        dr["CVN"] = CVN;
                        if (worksheet1.Cells["B12"].Value != null && worksheet1.Cells["B12"].Value.ToString().Length > 0) {
                            dr["Result"] = worksheet1.Cells["B12"].Value.ToString().Contains("不合格") ? "0" : "1";
                        }
                        dtImport.Rows.Add(dr);

                        if (worksheet1.Cells["E5"].Value != null) {
                            dr = dtImport.NewRow();
                            dr["VIN"] = worksheet1.Cells["B2"].Value;
                            dr["ECU_ID"] = worksheet1.Cells["E5"].Value;
                            dr["OBD_SUP"] = worksheet1.Cells["B9"].Value;
                            dr["ODO"] = worksheet1.Cells["B10"].Value;
                            dr["CAL_ID"] = worksheet1.Cells["B5"].Value == null ? "" : worksheet1.Cells["B5"].Value.ToString();
                            dr["CVN"] = worksheet1.Cells["D5"].Value == null ? "" : worksheet1.Cells["D5"].Value.ToString();
                            if (worksheet1.Cells["B12"].Value != null && worksheet1.Cells["B12"].Value.ToString().Length > 0) {
                                dr["Result"] = worksheet1.Cells["B12"].Value.ToString().Contains("不合格") ? "0" : "1";
                            }
                            dtImport.Rows.Add(dr);
                        }
                        _obdTest.DbLocal.ModifyRecords(dtImport);
                    }
                }
                Invoke((EventHandler)delegate {
                    labelMESInfo.ForeColor = Color.ForestGreen;
                    labelMESInfo.Text = "Excel报表数据导入完成";
                });
            } catch (Exception ex) {
                _obdIfEx.Log.TraceError("Import excel report file data error: " + ex.Message);
                Invoke((EventHandler)delegate {
                    labelMESInfo.ForeColor = Color.Red;
                    labelMESInfo.Text = "Excel报表数据导入失败";
                });
                MessageBox.Show(ex.Message, "导入数据出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
            } finally {
                dtImport.Dispose();
            }
        }
    }
}
