using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBCParser.DBCObj {
    public class NetWork : DBCObjBase {
        public Dictionary<string, Node> Nodes { get; private set; }
        public Dictionary<string, Dictionary<uint, string>> ValueTables { get; private set; }
        public Dictionary<string, DBCAttributeBase> AttributeDefs { get; private set; }
        public string Version { get; set; }
        public List<string> NewSymbols { get; set; }
        public int BaudRate { get; set; }

        public NetWork() {
            Nodes = new Dictionary<string, Node> {
                { "Vector__XXX", new Node("Vector__XXX") }
            };
            ValueTables = new Dictionary<string, Dictionary<uint, string>>();
            AttributeDefs = new Dictionary<string, DBCAttributeBase>();

            Version = string.Empty;
            NewSymbols = new List<string>();
            BaudRate = -1;
        }

        public void AddNode(Node node) {
            AddChild(node);
            Nodes.Add(node.Name, node);
        }

        public void AddValueTable(string name, Dictionary<uint, string> valTblDess) {
            ValueTables.Add(name, valTblDess);
        }

        public void AddAttributeDef(DBCAttributeBase attriDef) {
            AttributeDefs.Add(attriDef.Name, attriDef);
        }

        public Message GetMessage(uint ID) {
            foreach (Node node in Nodes.Values) {
                foreach (Message message in node.Messages.Values) {
                    // 在解析dbc文件阶段，属性值可能还未读入，故不进行是否是扩展ID判断
                    // 报文ID严格按照vector定义执行，即大于0x7FF需加上0x80000000
                    if (message.ID == ID) {
                        return message;
                    }
                }
            }
            return null;
        }

        public Signal GetSignal(uint msgID, string name) {
            Message msg = GetMessage(msgID);
            if (msg == null) {
                return null;
            }
            foreach (Signal signal in msg.Signals.Values) {
                if (signal.Name == name) {
                    return signal;
                }
            }
            return null;
        }

        public List<Message> GetConsumedMessage(Node nodeIn) {
            List<Message> msgs = new List<Message>();
            foreach (Node node in Nodes.Values) {
                foreach (Message msg in node.Messages.Values) {
                    foreach (Signal sig in msg.Signals.Values) {
                        if (sig.Receiver.Contains(nodeIn)) {
                            msgs.Add(msg);
                        }
                    }
                }
            }
            return msgs;
        }

        /// <summary>
        /// 加载完dbc文件后，需要进行的后处理函数，目前仅包含对报文内信号的处理
        /// </summary>
        public void PostProcess() {
            foreach (Node node in Nodes.Values) {
                foreach (Message message in node.Messages.Values) {
                    message.PostProcess();
                }
            }
        }
    }
}
