using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SH_OBD_Main {
    public partial class StatisticForm : Form {
        private readonly DataTable _dtContent;
        private readonly string[] _columns;
        private readonly OBDTest _obdTest;
        private int _allQty;
        private int _passedQty;
        private int _uploadedQty;
        private readonly int _pageSize;

        public StatisticForm(OBDTest obdTest) {
            InitializeComponent();
            _dtContent = new DataTable();
            _obdTest = obdTest;
            _columns = new string[4];
            _columns[0] = "WriteTime";
            _columns[1] = "VIN";
            _columns[2] = "Result";
            _columns[3] = "Upload";
            _allQty = 0;
            _passedQty = 0;
            _uploadedQty = 0;
            _pageSize = 500;
        }

        private void SetGridViewColumnsSortMode(DataGridView gridView, DataGridViewColumnSortMode sortMode) {
            for (int i = 0; i < gridView.Columns.Count; i++) {
                gridView.Columns[i].SortMode = sortMode;
            }
        }

        private void SetDataTableColumns<T>(DataTable dt, string[] columns) {
            dt.Clear();
            dt.Columns.Clear();
            foreach (string col in columns) {
                dt.Columns.Add(new DataColumn(col, typeof(T)));
            }
        }

        private void SetDataTableRow(DataTable dt, string[] columns) {
            Dictionary<string, string> whereDic = new Dictionary<string, string>();
            if (this.cmbBoxResult.SelectedIndex > 0) {
                if (this.cmbBoxResult.SelectedIndex == 1) {
                    whereDic.Add("Result", "1");
                } else if (this.cmbBoxResult.SelectedIndex == 2) {
                    whereDic.Add("Result", "0");
                }
            }
            if (this.cmbBoxUpload.SelectedIndex > 0) {
                if (this.cmbBoxUpload.SelectedIndex == 1) {
                    whereDic.Add("Upload", "1");
                } else if (this.cmbBoxUpload.SelectedIndex == 2) {
                    whereDic.Add("Upload", "0");
                }
            }
            ModelSQLServer.FilterTime time = ModelSQLServer.FilterTime.NoFilter;
            if (this.radioBtnDay.Checked) {
                time = ModelSQLServer.FilterTime.Day;
            } else if (this.radioBtnWeek.Checked) {
                time = ModelSQLServer.FilterTime.Week;
            } else if (this.radioBtnMonth.Checked) {
                time = ModelSQLServer.FilterTime.Month;
            }
            int max = (_allQty / _pageSize) + (_allQty % _pageSize > 0 ? 1 : 0);
            this.UpDownPage.Maximum = max > 0 ? max : 1;
            this.lblAllPage.Text = "页 / 共 " + this.UpDownPage.Maximum.ToString() + " 页";
            string[,] results = _obdTest.DbNative.GetRecordsFilterTime("OBDData", columns, whereDic, time, decimal.ToInt32(this.UpDownPage.Value), _pageSize);
            if (results == null) {
                return;
            }
            List<string> distinct = new List<string>();
            for (int iRow = 0; iRow < results.GetLength(0); iRow++) {
                if (distinct.Contains(results[iRow, 1])) {
                    int index = distinct.IndexOf(results[iRow, 1]);
                    for (int iCol = 0; iCol < results.GetLength(1); iCol++) {
                        dt.Rows[index][iCol] = results[iRow, iCol];
                    }
                } else {
                    distinct.Add(results[iRow, 1]);
                    DataRow dr = dt.NewRow();
                    for (int iCol = 0; iCol < results.GetLength(1); iCol++) {
                        dr[iCol] = results[iRow, iCol];
                    }
                    dt.Rows.Add(dr);
                }
            }
        }

        private void SetDataTableContent() {
            SetDataTableColumns<string>(_dtContent, _columns);
            SetGridViewColumnsSortMode(this.GridContent, DataGridViewColumnSortMode.Programmatic);
            SetDataTableRow(_dtContent, _columns);
        }

        private void ShowResult(Label lbl, string[,] results, ref int qty) {
            if (results != null && results.GetLength(0) > 0) {
                int.TryParse(results[0, 0].ToString(), out qty);
                if (qty < 10000) {
                    lbl.Text = results[0, 0];
                } else {
                    lbl.Text = (qty / 10000.0).ToString("F2") + "万";
                }
            } else {
                qty = 0;
                lbl.Text = "0";
            }
        }

        private void GetQty() {
            ModelSQLServer.FilterTime time = ModelSQLServer.FilterTime.Day;
            if (this.radioBtnDay.Checked) {
                time = ModelSQLServer.FilterTime.Day;
            } else if (this.radioBtnWeek.Checked) {
                time = ModelSQLServer.FilterTime.Week;
            } else if (this.radioBtnMonth.Checked) {
                time = ModelSQLServer.FilterTime.Month;
            }
            Dictionary<string, string> whereDic = new Dictionary<string, string>();
            string[] columns = { "VIN" };
            string[,] results = _obdTest.DbNative.GetRecordsCount("OBDData", columns, whereDic, time);
            ShowResult(this.lblAllQty, results, ref _allQty);

            whereDic = new Dictionary<string, string> { { "Result", "1" } };
            results = _obdTest.DbNative.GetRecordsCount("OBDData", columns, whereDic, time);
            ShowResult(this.lblPassedQty, results, ref _passedQty);
            this.lblPassedRate.Text = (_passedQty * 100.0 / (float)_allQty).ToString("F2") + "%";

            whereDic = new Dictionary<string, string> { { "Upload", "1" } };
            results = _obdTest.DbNative.GetRecordsCount("OBDData", columns, whereDic, time);
            ShowResult(this.lblUploadedQty, results, ref _uploadedQty);
            this.lblUploadedRate.Text = (_uploadedQty * 100.0 / (float)_allQty).ToString("F2") + "%";
        }

        private void StatisticForm_Load(object sender, EventArgs e) {
            this.GridContent.DataSource = _dtContent;
            this.radioBtnDay.Checked = true;
            this.lblAllQty.Text = _allQty.ToString();
            this.lblPassedQty.Text = _passedQty.ToString();
            this.lblPassedRate.Text = "0%";
            this.lblUploadedQty.Text = _uploadedQty.ToString();
            this.lblUploadedRate.Text = "0%";
            this.cmbBoxResult.SelectedIndex = 1;
            this.cmbBoxUpload.SelectedIndex = 1;
        }

        private void StatisticForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (_dtContent != null) {
                _dtContent.Dispose();
            }
        }

        private void Option_Click(object sender, EventArgs e) {
            GetQty();
            SetDataTableContent();
        }
    }
}
