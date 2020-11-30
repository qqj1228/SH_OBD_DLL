using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBCParser.DBCObj {
    public class Node : DBCObjBase {
        public Dictionary<uint, Message> Messages { get; private set; }
        public string Name { get; set; }

        public Node(string name) {
            Messages = new Dictionary<uint, Message>();
            Name = name;
        }

        public void AddMessage(Message message) {
            if (message.Sender != null) {
                throw new ApplicationException("Message already belongs to node: " + message.Sender.Name);
            }
            AddChild(message);
            message.Sender = this;
            Messages.Add(message.ID, message);
        }
    }
}
