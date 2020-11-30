using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model {
    public class ModelBase {
    }

    public class ModelParameter {
        public string UserName { get; set; }
        public string PassWord { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        public string DBorService { get; set; }
    }
}
