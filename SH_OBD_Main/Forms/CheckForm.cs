using OfficeOpenXml;
using OfficeOpenXml.Style;
using SH_OBD_DLL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SH_OBD_Main {
    public partial class CheckForm : Form {
        private readonly ModelSQLite _dbNative;
        private readonly DataTable _dtContent;
        private readonly Logger _log;
        private readonly string _strSort; 

        public CheckForm(ModelSQLite dbNative, Logger log) {
            InitializeComponent();
            _dtContent = new DataTable("VehicleType");
            _dbNative = dbNative;
            _log = log;
            _strSort = "Project ASC,Type ASC,ECU_ID ASC,CAL_ID ASC,CVN ASC";
        }

        private void CheckForm_Resize(object sender, EventArgs e) {
            int margin = grpBoxType.Location.X - (grpBoxProject.Location.X + grpBoxProject.Width);
            grpBoxProject.Width = (btnModify.Location.X - grpBoxProject.Location.X) / 5 - margin;
            grpBoxType.Location = new Point(grpBoxProject.Location.X + grpBoxProject.Width + margin, grpBoxProject.Location.Y);
            grpBoxType.Width = grpBoxProject.Width;
            grpBoxECUID.Location = new Point(grpBoxType.Location.X + grpBoxType.Width + margin, grpBoxProject.Location.Y);
            grpBoxECUID.Width = grpBoxProject.Width;
            grpBoxCALID.Location = new Point(grpBoxECUID.Location.X + grpBoxECUID.Width + margin, grpBoxProject.Location.Y);
            grpBoxCALID.Width = grpBoxProject.Width;
            grpBoxCVN.Location = new Point(grpBoxCALID.Location.X + grpBoxCALID.Width + margin, grpBoxProject.Location.Y);
            grpBoxCVN.Width = grpBoxProject.Width;
        }

        private void SetGridViewColumnsSortMode(DataGridView gridView, DataGridViewColumnSortMode sortMode) {
            for (int i = 0; i < gridView.Columns.Count; i++) {
                gridView.Columns[i].SortMode = sortMode;
            }
        }

        private void SetDataTableContent() {
            _dbNative.GetEmptyTable(_dtContent);
            _dtContent.DefaultView.Sort = "ID ASC";
            if (GridContent.Columns.Count > 0) {
                SetGridViewColumnsSortMode(GridContent, DataGridViewColumnSortMode.NotSortable);
            }
            _dbNative.GetRecords(_dtContent, null);
        }

        private void ArrangeRecords(DataTable dt, string strSort) {
            if (dt.Rows.Count <= 0) {
                return;
            }
            string[] strColumns = strSort.Replace(" ASC", "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            dt.DefaultView.Sort = strSort;
            dt = dt.DefaultView.ToTable(true, strColumns);
            _dbNative.DeleteAllRecords("VehicleType");
            _dbNative.ResetTableID("VehicleType");
            try {
                _dbNative.InsertRecords(dt);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "整理数据出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckForm_Load(object sender, EventArgs e) {
            GridContent.DataSource = _dtContent;
            SetDataTableContent();
        }

        private void GridContent_Click(object sender, EventArgs e) {
            int index = -1;
            if (GridContent.CurrentRow != null) {
                index = GridContent.CurrentRow.Index;
            }
            if (index >= 0 && index < _dtContent.Rows.Count) {
                txtBoxProject.Text = _dtContent.Rows[index]["Project"].ToString();
                txtBoxType.Text = _dtContent.Rows[index]["Type"].ToString();
                txtBoxECUID.Text = _dtContent.Rows[index]["ECU_ID"].ToString();
                txtBoxCALID.Text = _dtContent.Rows[index]["CAL_ID"].ToString();
                txtBoxCVN.Text = _dtContent.Rows[index]["CVN"].ToString();
            }
        }

        private void BtnModify_Click(object sender, EventArgs e) {
            if (txtBoxType.Text.Length > 0 && txtBoxECUID.Text.Length > 0 && txtBoxCALID.Text.Length > 0 && txtBoxCVN.Text.Length > 0) {
                int index = GridContent.CurrentRow.Index;
                DataTable dtModify = new DataTable("VehicleType");
                dtModify.Columns.Add("Project");
                dtModify.Columns.Add("Type");
                dtModify.Columns.Add("ECU_ID");
                dtModify.Columns.Add("CAL_ID");
                dtModify.Columns.Add("CVN");
                DataRow dr = dtModify.NewRow();
                dr["Project"] = txtBoxProject.Text;
                dr["Type"] = txtBoxType.Text;
                dr["ECU_ID"] = txtBoxECUID.Text;
                dr["CAL_ID"] = txtBoxCALID.Text;
                dr["CVN"] = txtBoxCVN.Text;
                dtModify.Rows.Add(dr);
                List<string> whereVals = new List<string>() { _dtContent.Rows[index]["ID"].ToString() };
                try {
                    _dbNative.UpdateRecords(dtModify, "ID", whereVals);
                    SetDataTableContent();
                    GridContent.Rows[index].Selected = true;
                    GridContent.CurrentCell = GridContent.Rows[index].Cells[0];
                } catch (Exception ex) {
                    MessageBox.Show(ex.Message, "修改数据出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                dtModify.Dispose();
            }
        }

        private void BtnInsert_Click(object sender, EventArgs e) {
            if (txtBoxType.Text.Length > 0 && txtBoxECUID.Text.Length > 0 && txtBoxCALID.Text.Length > 0 && txtBoxCVN.Text.Length > 0) {
                int index = GridContent.Rows.Count;
                DataTable dtInsert = new DataTable("VehicleType");
                _dbNative.GetEmptyTable(dtInsert);
                DataRow dr = dtInsert.NewRow();
                dr["Project"] = txtBoxProject.Text;
                dr["Type"] = txtBoxType.Text;
                dr["ECU_ID"] = txtBoxECUID.Text;
                dr["CAL_ID"] = txtBoxCALID.Text;
                dr["CVN"] = txtBoxCVN.Text;
                dtInsert.Rows.Add(dr);
                try {
                    _dbNative.InsertRecords(dtInsert);
                    SetDataTableContent();
                    GridContent.Rows[index].Selected = true;
                    GridContent.CurrentCell = GridContent.Rows[index].Cells[0];
                } catch (Exception ex) {
                    MessageBox.Show(ex.Message, "插入数据出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                dtInsert.Dispose();
            }
        }

        private void BtnRemove_Click(object sender, EventArgs e) {
            int selectedCount = GridContent.SelectedRows.Count;
            if (selectedCount > 0) {
                List<string> IDs = new List<string>(selectedCount);
                for (int i = 0; i < selectedCount; i++) {
                    if ((DataRowView)GridContent.SelectedRows[i].DataBoundItem is DataRowView rowView) {
                        IDs.Add(rowView.Row["ID"].ToString());
                    }
                }
                int deletedCount = _dbNative.DeleteRecords("VehicleType", "ID", IDs);
                SetDataTableContent();
                if (deletedCount != selectedCount) {
                    _log.TraceError("Remove error, removed count: " + deletedCount.ToString() + ", selected item count: " + selectedCount.ToString());
                    MessageBox.Show("删除数据出错，删除行数：" + deletedCount.ToString(), "出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                if (_dtContent.Rows.Count > 0) {
                    ArrangeRecords(_dtContent, _strSort);
                    SetDataTableContent();
                }
            }
        }

        private void MenuItemImport_Click(object sender, EventArgs e) {
            DataTable dtImport = new DataTable("VehicleType");
            _dbNative.GetEmptyTable(dtImport);
            OpenFileDialog openFileDialog = new OpenFileDialog {
                Title = "打开 Excel 导入文件",
                Filter = "Excel 2007 及以上 (*.xlsx)|*.xlsx",
                FilterIndex = 0,
                RestoreDirectory = true
            };
            DialogResult result = openFileDialog.ShowDialog();
            try {
                if (result == DialogResult.OK && openFileDialog.FileName.Length > 0) {
                    FileInfo xlFile = new FileInfo(openFileDialog.FileName);
                    ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                    using (ExcelPackage package = new ExcelPackage(xlFile, true)) {
                        ExcelWorksheet worksheet1 = package.Workbook.Worksheets[0];
                        for (int i = 2; i < worksheet1.Cells.Rows; i++) {
                            if (worksheet1.Cells[i, 1].Value == null || worksheet1.Cells[i, 1].Value.ToString().Length == 0) {
                                break;
                            }
                            DataRow dr = dtImport.NewRow();
                            dr["Project"] = worksheet1.Cells[i, 2].Value.ToString();
                            dr["Type"] = worksheet1.Cells[i, 3].Value.ToString();
                            dr["ECU_ID"] = worksheet1.Cells[i, 4].Value.ToString();
                            dr["CAL_ID"] = worksheet1.Cells[i, 5].Value.ToString();
                            dr["CVN"] = worksheet1.Cells[i, 6].Value.ToString();
                            dtImport.Rows.Add(dr);
                        }
                    }
                    _dbNative.InsertRecords(dtImport);
                    _dbNative.GetRecords(_dtContent, null);
                    if (_dtContent.Rows.Count > 0) {
                        ArrangeRecords(_dtContent, _strSort);
                        SetDataTableContent();
                        MessageBox.Show("导入Excel数据完成", "导入数据");
                    }
                }
            } finally {
                openFileDialog.Dispose();
                dtImport.Dispose();
            }
        }

        private void MenuItemExport_Click(object sender, EventArgs e) {
            SaveFileDialog saveFileDialog = new SaveFileDialog {
                Title = "保存 Excel 导出文件",
                Filter = "Excel 2007 及以上 (*.xlsx)|*.xlsx",
                FilterIndex = 0,
                RestoreDirectory = true,
                OverwritePrompt = true,
                FileName = DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd") + "_Export"
            };
            DialogResult result = saveFileDialog.ShowDialog();
            try {
                if (result == DialogResult.OK) {
                    using (ExcelPackage package = new ExcelPackage()) {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("车型校验文件");
                        // 标题
                        for (int i = 0; i < _dtContent.Columns.Count; i++) {
                            worksheet.Cells[1, i + 1].Value = _dtContent.Columns[i].ColumnName;
                            // 边框
                            worksheet.Cells[1, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
                        }
                        // 格式化标题
                        using (var range = worksheet.Cells[1, 1, 1, _dtContent.Columns.Count]) {
                            range.Style.Font.Bold = true;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        // 记录
                        for (int iRow = 0; iRow < _dtContent.Rows.Count; iRow++) {
                            for (int iCol = 0; iCol < _dtContent.Columns.Count; iCol++) {
                                worksheet.Cells[iRow + 2, iCol + 1].Value = _dtContent.Rows[iRow][iCol].ToString();
                                // 边框
                                worksheet.Cells[iRow + 2, iCol + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
                            }
                        }
                        // 格式化记录
                        using (var range = worksheet.Cells[2, 1, _dtContent.Rows.Count + 1, _dtContent.Columns.Count]) {
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }
                        // 自适应列宽
                        worksheet.Cells.AutoFitColumns(0);
                        // 保存文件
                        FileInfo xlFile = new FileInfo(saveFileDialog.FileName);
                        package.SaveAs(xlFile);
                    }
                    MessageBox.Show("导出Excel数据完成", "导出数据");
                }
            } finally {
                saveFileDialog.Dispose();
            }
        }

        private void MenuItemRefresh_Click(object sender, EventArgs e) {
            int index = 0;
            if (GridContent.Rows.Count > 0) {
                index = GridContent.CurrentRow.Index;
            }
            SetDataTableContent();
            if (GridContent.Rows.Count > index) {
                GridContent.Rows[index].Selected = true;
                GridContent.CurrentCell = GridContent.Rows[index].Cells[0];
            }
        }

        private void MenuItemArrange_Click(object sender, EventArgs e) {
            ArrangeRecords(_dtContent, _strSort);
            SetDataTableContent();
        }
    }
}
