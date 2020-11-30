using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBCParser.DBCObj {
    public abstract class DBCObjBase {
        public DBCObjBase Parent { get; set; }
        public string Comment { get; set; }
        public Dictionary<string, DBCAttribute> Attributes { get; private set; }

        protected DBCObjBase() {
            Attributes = new Dictionary<string, DBCAttribute>();
        }

        public void AddChild(DBCObjBase child) {
            child.Parent = this;
        }

        public void AddAttribute(DBCAttribute attribute) {
            Attributes.Add(attribute.Name, attribute);
        }

    }
}
