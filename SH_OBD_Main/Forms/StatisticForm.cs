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
        private readonly OBDTest _obdTest;
        private readonly DataTable _dtContent;
        private int _allQty;
        private int _passedQty;
        private int _uploadedQty;
        private readonly int _pageSize;

        public StatisticForm(OBDTest obdTest) {
            InitializeComponent();
            _dtContent = new DataTable("OBDData");
            _dtContent.Columns.Add("WriteTime");
            _dtContent.Columns.Add("VIN");
            _dtContent.Columns.Add("Result");
            _dtContent.Columns.Add("Upload");
            _obdTest = obdTest;
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

        private void SetDataTableRow(DataTable dt) {
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
            ModelSQLite.FilterTime time = ModelSQLite.FilterTime.NoFilter;
            if (this.radioBtnDay.Checked) {
                time = ModelSQLite.FilterTime.Day;
            } else if (this.radioBtnWeek.Checked) {
                time = ModelSQLite.FilterTime.Week;
            } else if (this.radioBtnMonth.Checked) {
                time = ModelSQLite.FilterTime.Month;
            }
            int max = (_allQty / _pageSize) + (_allQty % _pageSize > 0 ? 1 : 0);
            this.UpDownPage.Maximum = max > 0 ? max : 1;
            this.lblAllPage.Text = "页 / 共 " + this.UpDownPage.Maximum.ToString() + " 页";
            _obdTest.DbNative.GetRecordsFilterTime(dt, whereDic, time, decimal.ToInt32(this.UpDownPage.Value), _pageSize);
        }

        private void SetDataTableContent() {
            SetGridViewColumnsSortMode(GridContent, DataGridViewColumnSortMode.Programmatic);
            _dtContent.Clear();
            SetDataTableRow(_dtContent);
        }

        private void ShowResult(Label lbl, object result, ref int qty) {
            if (result != null) {
                int.TryParse(result.ToString(), out qty);
                if (qty < 10000) {
                    lbl.Text = result.ToString();
                } else {
                    lbl.Text = (qty / 10000.0).ToString("F2") + "万";
                }
            } else {
                qty = 0;
                lbl.Text = "0";
            }
        }

        private void GetQty() {
            ModelSQLite.FilterTime time = ModelSQLite.FilterTime.Day;
            if (this.radioBtnDay.Checked) {
                time = ModelSQLite.FilterTime.Day;
            } else if (this.radioBtnWeek.Checked) {
                time = ModelSQLite.FilterTime.Week;
            } else if (this.radioBtnMonth.Checked) {
                time = ModelSQLite.FilterTime.Month;
            }

            Dictionary<string, string> whereDic = new Dictionary<string, string>();
            string[] columns = { "VIN" };
            object result = _obdTest.DbNative.GetRecordsCount("OBDData", columns, whereDic, time);
            ShowResult(this.lblAllQty, result, ref _allQty);

            whereDic = new Dictionary<string, string> { { "Result", "1" } };
            result = _obdTest.DbNative.GetRecordsCount("OBDData", columns, whereDic, time);
            ShowResult(this.lblPassedQty, result, ref _passedQty);
            this.lblPassedRate.Text = (_passedQty * 100.0 / (float)_allQty).ToString("F2") + "%";

            whereDic = new Dictionary<string, string> { { "Upload", "1" } };
            result = _obdTest.DbNative.GetRecordsCount("OBDData", columns, whereDic, time);
            ShowResult(this.lblUploadedQty, result, ref _uploadedQty);
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
            //Task.Factory.StartNew(SetDataTableContent);
            //SetDataTableContent();
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
