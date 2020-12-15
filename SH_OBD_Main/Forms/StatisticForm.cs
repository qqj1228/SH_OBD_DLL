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
            if (cmbBoxResult.SelectedIndex > 0) {
                if (cmbBoxResult.SelectedIndex == 1) {
                    whereDic.Add("Result", "1");
                } else if (cmbBoxResult.SelectedIndex == 2) {
                    whereDic.Add("Result", "0");
                }
            }
            if (cmbBoxUpload.SelectedIndex > 0) {
                if (cmbBoxUpload.SelectedIndex == 1) {
                    whereDic.Add("Upload", "1");
                } else if (cmbBoxUpload.SelectedIndex == 2) {
                    whereDic.Add("Upload", "0");
                }
            }
            FilterTime time = FilterTime.NoFilter;
            if (radioBtnDay.Checked) {
                time = FilterTime.Day;
            } else if (radioBtnWeek.Checked) {
                time = FilterTime.Week;
            } else if (radioBtnMonth.Checked) {
                time = FilterTime.Month;
            }
            int max = (_allQty / _pageSize) + (_allQty % _pageSize > 0 ? 1 : 0);
            UpDownPage.Maximum = max > 0 ? max : 1;
            lblAllPage.Text = "页 / 共 " + UpDownPage.Maximum.ToString() + " 页";
            _obdTest.DbLocal.GetRecordsFilterTime(dt, whereDic, time, decimal.ToInt32(UpDownPage.Value), _pageSize);
        }

        private void SetDataTableContent() {
            SetGridViewColumnsSortMode(GridContent, DataGridViewColumnSortMode.Programmatic);
            _dtContent.Clear();
            SetDataTableRow(_dtContent);
        }

        private void ShowResult(Label lbl, object results, ref int qty) {
            if (results != null) {
                int.TryParse(results.ToString(), out qty);
                if (qty < 10000) {
                    lbl.Text = results.ToString();
                } else {
                    lbl.Text = (qty / 10000.0).ToString("F2") + "万";
                }
            } else {
                qty = 0;
                lbl.Text = "0";
            }
        }

        private void GetQty() {
            FilterTime time = FilterTime.Day;
            if (radioBtnDay.Checked) {
                time = FilterTime.Day;
            } else if (radioBtnWeek.Checked) {
                time = FilterTime.Week;
            } else if (radioBtnMonth.Checked) {
                time = FilterTime.Month;
            }
            Dictionary<string, string> whereDic = new Dictionary<string, string>();
            string[] columns = { "VIN" };
            object result = _obdTest.DbLocal.GetRecordsCount("OBDData", columns, whereDic, time);
            ShowResult(lblAllQty, result, ref _allQty);

            whereDic = new Dictionary<string, string> { { "Result", "1" } };
            result = _obdTest.DbLocal.GetRecordsCount("OBDData", columns, whereDic, time);
            ShowResult(lblPassedQty, result, ref _passedQty);
            lblPassedRate.Text = (_passedQty * 100.0 / (float)_allQty).ToString("F2") + "%";

            whereDic = new Dictionary<string, string> { { "Upload", "1" } };
            result = _obdTest.DbLocal.GetRecordsCount("OBDData", columns, whereDic, time);
            ShowResult(lblUploadedQty, result, ref _uploadedQty);
            lblUploadedRate.Text = (_uploadedQty * 100.0 / (float)_allQty).ToString("F2") + "%";
        }

        private void StatisticForm_Load(object sender, EventArgs e) {
            GridContent.DataSource = _dtContent;
            radioBtnDay.Checked = true;
            lblAllQty.Text = _allQty.ToString();
            lblPassedQty.Text = _passedQty.ToString();
            lblPassedRate.Text = "0%";
            lblUploadedQty.Text = _uploadedQty.ToString();
            lblUploadedRate.Text = "0%";
            cmbBoxResult.SelectedIndex = 1;
            cmbBoxUpload.SelectedIndex = 1;
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
