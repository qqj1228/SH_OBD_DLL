using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace SH_OBD_Main {
    public partial class PassWordForm : Form {
        private readonly OBDTest _obdTest;

        public PassWordForm(OBDTest obdTest) {
            InitializeComponent();
            _obdTest = obdTest;
        }

        private void BtnOK_Click(object sender, EventArgs e) {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] output = md5.ComputeHash(Encoding.Default.GetBytes(this.txtBoxPassWord.Text.Trim()));
            string strValue = BitConverter.ToString(output).Replace("-", "");
            if (strValue == _obdTest.DbNative.GetPassWord()) {
                _obdTest.AccessAdvancedMode = 1;
            } else {
                _obdTest.AccessAdvancedMode = -1;
            }
            md5.Dispose();
            this.Close();
        }
    }
}
