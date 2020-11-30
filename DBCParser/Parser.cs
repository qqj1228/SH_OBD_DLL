using DBCParser.DBCObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace DBCParser {
    public class Parser {
        private delegate void ParseFunction(string strLine);

        private readonly NetWork _netWork;
        private readonly Dictionary<string, ParseFunction> _keywordsParser;
        private KeyValuePair<DBCObjType, DBCObjBase> _mode;
        private bool _NSLine;
        public List<ValueDisplay> ValDispalys { get; set; }
        public List<SignalDisplay> SigDisplays { get; set; }


        public Parser(string valFile, string sigFile) {
            _netWork = new NetWork();
            _keywordsParser = new Dictionary<string, ParseFunction> {
                { "VERSION", ParseVersion },
                { "NS_", ParseNewSymbol },
                { "BS_", ParseBaudRate },
                { "BU_", ParseNodes },
                { "VAL_TABLE", ParseValueTable },
                { "BO_", ParseMessage },
                { "SG_", ParseSignal },
                { "CM_", ParseComment },
                { "BA_DEF_", ParseAttributeDefinition },
                { "BA_DEF_DEF_", ParseAttributeDefinitionDefault },
                { "BA_", ParseAttribute },
                { "VAL_", ParseSignalValueDesc },
                { "SG_MUL_VAL_", ParseSignalExtendedMultiplex }
            };
            _mode = new KeyValuePair<DBCObjType, DBCObjBase>(DBCObjType.DBCObjBase, null);
            _NSLine = false;
            ValDispalys = LoadDisplayFile<ValueDisplay>(valFile);
            SigDisplays = LoadDisplayFile<SignalDisplay>(sigFile);
        }

        public NetWork ParseFile(string filePath) {
            StreamReader reader = null;
            string strLine;
            int lineNO = 0;
            try {
                reader = new StreamReader(filePath, Encoding.GetEncoding("GBK"), false);
                while (!reader.EndOfStream) {
                    strLine = reader.ReadLine().Trim();
                    ++lineNO;
                    if (_mode.Key == DBCObjType.DBCObjBase && _mode.Value != null) {
                        ParseMultilineComment(strLine);
                        continue;
                    }
                    if (strLine.Length == 0) {
                        _NSLine = false;
                        continue;
                    }
                    if (_NSLine) {
                        ParseNewSymbol(strLine);
                    } else {
                        ParseLine(strLine);
                    }
                }
            } catch (Exception ex) {
                throw new Exception(string.Format("[Line {0}] {1}", lineNO, ex.Message));
            } finally {
                if (reader != null) {
                    reader.Close();
                }
            }
            _netWork.PostProcess();
            return _netWork;
        }

        private void ParseLine(string strLine) {
            foreach (string key in _keywordsParser.Keys) {
                if (strLine.StartsWith(key)) {
                    _keywordsParser[key](strLine);
                }
            }
        }

        private void ParseVersion(string strLine) {
            Regex re = new Regex(@"VERSION\s+""(?<version>\S+)""");
            _netWork.Version = re.Match(strLine).Groups["version"].Value;
        }

        private void ParseNewSymbol(string strLine) {
            _NSLine = true;
            if (!strLine.StartsWith("NS_")) {
                _netWork.NewSymbols.Add(strLine);
            }
        }

        private void ParseBaudRate(string strLine) {
            Regex re = new Regex(@"BS_\s*:\s*(?<baudRate>\d+)?\s*");
            string strBaudRate = re.Match(strLine).Groups["baudRate"].Value;
            if (strBaudRate.Length > 0) {
                _netWork.BaudRate = int.Parse(strBaudRate);
            }
        }

        private void ParseNodes(string strLine) {
            Regex re = new Regex(@"BU_\s*:\s*(?<nodes>.+)\s*");
            string[] nodes = re.Match(strLine).Groups["nodes"].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string node in nodes) {
                _netWork.AddNode(new Node(node));
            }
        }

        private Dictionary<uint, string> ParseValueDescription(string strValTblDesc) {
            Dictionary<uint, string> ValTblDescs = new Dictionary<uint, string>();

            Regex re = new Regex(@"\s*(\d+\s+""[^""]*"")\s*");
            foreach (Match item in re.Matches(strValTblDesc)) {
                Regex re1 = new Regex(@"\s*(?<num>\d+)\s+""(?<desc>[^""]*)""\s*");
                Match match = re1.Match(item.Value);
                string strNum = match.Groups["num"].Value;
                string desc = match.Groups["desc"].Value.Replace("\"", "");
                ValTblDescs.Add(uint.Parse(strNum), desc);
            }
            return ValTblDescs;
        }

        private void ParseValueTable(string strLine) {
            Regex re = new Regex(@"VAL_TABLE_\s+(?<name>\S+)\s+(?<valDesc>.+)\s*;");
            string name = re.Match(strLine).Groups["name"].Value;
            Dictionary<uint, string> ValTblDescs = ParseValueDescription(re.Match(strLine).Groups["valDesc"].Value);
            _netWork.AddValueTable(name, ValTblDescs);
        }

        private void ParseMessage(string strLine) {
            Regex re = new Regex(@"BO_\s+(?<id>\d+)\s+(?<name>\S+)\s*:\s*(?<length>\d+)\s+(?<sender>\S+)\s*");
            string strID = re.Match(strLine).Groups["id"].Value;
            string name = re.Match(strLine).Groups["name"].Value;
            string strLen = re.Match(strLine).Groups["length"].Value;
            string sender = re.Match(strLine).Groups["sender"].Value.Trim();
            if (_netWork.Nodes.ContainsKey(sender)) {
                Message message = new Message(uint.Parse(strID), name, int.Parse(strLen));
                _netWork.Nodes[sender].AddMessage(message);
                _mode = new KeyValuePair<DBCObjType, DBCObjBase>(DBCObjType.Message, message);
            } else {
                throw new ApplicationException("NetWork not contain the sender: " + sender);
            }
        }

        private void ParseSignal(string strLine) {
            if (strLine.StartsWith("SG_MUL_VAL_")) {
                return;
            }
            if (_mode.Key != DBCObjType.Message) {
                throw new ApplicationException("Signal definition not in message block");
            }
            string pattern = @"SG_\s+(?<name>\S+)\s*(?<multiplexedID>m\d+)?(?<multiplexor>M)?\s*:\s*";
            pattern += @"(?<startBit>\d+)\|(?<length>\d+)@(?<byteOrder>[0|1])(?<sign>[+|-])\s*";
            pattern += @"\(\s*(?<factor>\S+)\s*,\s*(?<offset>\S+)\s*\)\s*";
            pattern += @"\[\s*(?<minValue>\S+)\s*\|\s*(?<maxValue>\S+)\s*\]\s*""(?<unit>.*)""\s+(?<receiverAll>.+)\s*";
            Regex re = new Regex(pattern);
            string name = re.Match(strLine).Groups["name"].Value;
            string strMultiplexedID = re.Match(strLine).Groups["multiplexedID"].Value;
            int iMultiplexedID = -1;
            if (strMultiplexedID.Length > 0) {
                iMultiplexedID = int.Parse(strMultiplexedID.Trim().Substring(1));
            }
            string strMultiplexor = re.Match(strLine).Groups["multiplexor"].Value;
            bool bMultiplexor = strMultiplexor.Length > 0;
            string strStartBit = re.Match(strLine).Groups["startBit"].Value;
            int iStartBit = int.Parse(strStartBit);
            string strLength = re.Match(strLine).Groups["length"].Value;
            int iLen = int.Parse(strLength);
            string strByteOrder = re.Match(strLine).Groups["byteOrder"].Value;
            bool bIntelOrder = strByteOrder.Contains("1");
            string strSign = re.Match(strLine).Groups["sign"].Value;
            bool bSigned = strSign.Contains("-");
            string strFactor = re.Match(strLine).Groups["factor"].Value;
            double dFactor = double.Parse(strFactor);
            if (dFactor == 0) {
                throw new ApplicationException("Factor can not equal zero of signal: " + name);
            }
            string strOffset = re.Match(strLine).Groups["offset"].Value;
            double dOffset = double.Parse(strOffset);
            string strMinValue = re.Match(strLine).Groups["minValue"].Value;
            double dMinValue = double.Parse(strMinValue);
            string strMaxValue = re.Match(strLine).Groups["maxValue"].Value;
            double dMaxValue = double.Parse(strMaxValue);
            string unit = re.Match(strLine).Groups["unit"].Value;
            string receiverAll = re.Match(strLine).Groups["receiverAll"].Value;
            string[] receivers = receiverAll.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            Signal signal = new Signal(
                name,
                iStartBit,
                iLen,
                bIntelOrder,
                bSigned,
                dFactor,
                dOffset,
                dMinValue,
                dMaxValue,
                unit,
                bMultiplexor,
                iMultiplexedID
            );
            foreach (string receiver in receivers) {
                signal.AddReceiver(_netWork.Nodes[receiver.Trim()]);
            }
            if (_mode.Value is Message message) {
                message.AddSignal(signal);
            } else {
                throw new ApplicationException("_mode.Value can not convert to Message");
            }
        }

        private void ParseComment(string strLine) {
            Regex re = new Regex(@"CM_\s+(?<objType>\S+)?\s*(?<id>\d*)?\s*(?<name>\S*)?\s*""(?<comment>.+)");
            string strObjType = re.Match(strLine).Groups["objType"].Value;
            string strID = re.Match(strLine).Groups["id"].Value;
            uint iID = 0;
            if (strID.Length > 0) {
                iID = uint.Parse(strID);
            }
            string name = re.Match(strLine).Groups["name"].Value.Trim();
            string comment = re.Match(strLine).Groups["comment"].Value;

            DBCObjBase commentObj;
            if (strObjType.Contains("BU_")) {
                commentObj = _netWork.Nodes[name];
            } else if (strObjType.Contains("BO_")) {
                commentObj = _netWork.GetMessage(iID);
            } else if (strObjType.Contains("SG_")) {
                commentObj = _netWork.GetSignal(iID, name);
            } else {
                commentObj = _netWork;
            }
            if (commentObj == null) {
                throw new ApplicationException("The ID or/and name of this line comment is error");
            }

            if (comment.EndsWith("\";")) {
                commentObj.Comment = comment.Substring(0, comment.Length - 2);
                _mode = new KeyValuePair<DBCObjType, DBCObjBase>(DBCObjType.DBCObjBase, null);
            } else {
                commentObj.Comment = comment;
                _mode = new KeyValuePair<DBCObjType, DBCObjBase>(DBCObjType.DBCObjBase, commentObj);
            }
        }

        private void ParseMultilineComment(string strLine) {
            if (_mode.Key != DBCObjType.DBCObjBase || _mode.Value == null) {
                throw new ApplicationException("The _mode of multiline comment is illegal");
            }
            if (strLine.EndsWith("\";")) {
                _mode.Value.Comment += Environment.NewLine + strLine.Substring(0, strLine.Length - 2);
                _mode = new KeyValuePair<DBCObjType, DBCObjBase>(DBCObjType.DBCObjBase, null);
            } else {
                _mode.Value.Comment += Environment.NewLine + strLine;
                _mode = new KeyValuePair<DBCObjType, DBCObjBase>(DBCObjType.DBCObjBase, _mode.Value);
            }

        }

        private void ParseAttributeDefinition(string strLine) {
            if (strLine.StartsWith("BA_DEF_DEF_")) {
                return;
            }
            Regex re = new Regex(@"BA_DEF_\s+(?<objType>\S+)?\s*""(?<name>\S+)""\s+(?<attriValType>\S+)\s*(?<attrDef>.+)?\s*;");
            string strObjType = re.Match(strLine).Groups["objType"].Value;
            string name = re.Match(strLine).Groups["name"].Value;
            string strAttriValType = re.Match(strLine).Groups["attriValType"].Value;
            string attrDef = re.Match(strLine).Groups["attrDef"].Value;

            DBCObjType objType = DBCObjType.DBCObjBase;
            if (strObjType.Contains("BU_")) {
                objType = DBCObjType.Node;
            } else if (strObjType.Contains("BO_")) {
                objType = DBCObjType.Message;
            } else if (strObjType.Contains("SG_")) {
                objType = DBCObjType.Signal;
            }

            AttriValType valType = AttriValType.STRING;
            if (strAttriValType.Contains("INT")) {
                valType = AttriValType.INT;
            } else if (strAttriValType.Contains("HEX")) {
                valType = AttriValType.HEX;
            } else if (strAttriValType.Contains("FLOAT")) {
                valType = AttriValType.FLOAT;
            } else if (strAttriValType.Contains("ENUM")) {
                valType = AttriValType.ENUM;
            } else if (strAttriValType.Contains("STRING")) {
                valType = AttriValType.STRING;
            }

            Regex re1;
            string strMin, strMax;
            switch (valType) {
            case AttriValType.INT:
                re1 = new Regex(@"\s*(?<min>\S+)\s*(?<max>\S+)");
                strMin = re1.Match(attrDef).Groups["min"].Value;
                strMax = re1.Match(attrDef).Groups["max"].Value;
                IntDBCAttributeDefinition attributeInt = new IntDBCAttributeDefinition(name, objType, int.Parse(strMin), int.Parse(strMax));
                _netWork.AddAttributeDef(attributeInt);
                break;
            case AttriValType.HEX:
                re1 = new Regex(@"\s*(?<min>\S+)\s*(?<max>\S+)");
                strMin = re1.Match(attrDef).Groups["min"].Value;
                strMax = re1.Match(attrDef).Groups["max"].Value;
                HexDBCAttributeDefinition attributeHex = new HexDBCAttributeDefinition(name, objType, uint.Parse(strMin), uint.Parse(strMax));
                _netWork.AddAttributeDef(attributeHex);
                break;
            case AttriValType.FLOAT:
                re1 = new Regex(@"\s*(?<min>\S+)\s*(?<max>\S+)");
                strMin = re1.Match(attrDef).Groups["min"].Value;
                strMax = re1.Match(attrDef).Groups["max"].Value;
                FloatDBCAttributeDefinition attributeFloat = new FloatDBCAttributeDefinition(name, objType, double.Parse(strMin), double.Parse(strMax));
                _netWork.AddAttributeDef(attributeFloat);
                break;
            case AttriValType.ENUM:
                string[] parts = attrDef.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> partl = new List<string>();
                foreach (string item in parts) {
                    partl.Add(item.Replace("\"", "").Trim());
                }
                EnumDBCAttributeDefinition attributeEnum = new EnumDBCAttributeDefinition(name, objType, partl);
                _netWork.AddAttributeDef(attributeEnum);
                break;
            case AttriValType.STRING:
                StringDBCAttributeDefinition attributeString = new StringDBCAttributeDefinition(name, objType);
                _netWork.AddAttributeDef(attributeString);
                break;
            }
        }

        private void ParseAttributeDefinitionDefault(string strLine) {
            Regex re = new Regex(@"BA_DEF_DEF_\s+""(?<name>\S+)""\s+(?<defaultVal>\S+)\s*;");
            string name = re.Match(strLine).Groups["name"].Value;
            string defaultVal = re.Match(strLine).Groups["defaultVal"].Value;

            switch (_netWork.AttributeDefs[name].ValType) {
            case AttriValType.STRING:
                if (_netWork.AttributeDefs[name] is StringDBCAttributeDefinition attriDefString) {
                    attriDefString.DefaultValue = defaultVal.Replace("\"", "");
                } else {
                    throw new ApplicationException("DBCAttributeBase can not convert to StringDBCAttributeDefinition");
                }
                break;
            case AttriValType.INT:
                if (_netWork.AttributeDefs[name] is IntDBCAttributeDefinition attriDefInt) {
                    attriDefInt.DefaultValue = int.Parse(defaultVal);
                } else {
                    throw new ApplicationException("DBCAttributeBase can not convert to IntDBCAttributeDefinition");
                }
                break;
            case AttriValType.HEX:
                if (_netWork.AttributeDefs[name] is HexDBCAttributeDefinition attriDefHex) {
                    attriDefHex.DefaultValue = uint.Parse(defaultVal);
                } else {
                    throw new ApplicationException("DBCAttributeBase can not convert to HexDBCAttributeDefinition");
                }
                break;
            case AttriValType.FLOAT:
                if (_netWork.AttributeDefs[name] is FloatDBCAttributeDefinition attriDefFloat) {
                    attriDefFloat.DefaultValue = double.Parse(defaultVal);
                } else {
                    throw new ApplicationException("DBCAttributeBase can not convert to FloatDBCAttributeDefinition");
                }
                break;
            case AttriValType.ENUM:
                if (_netWork.AttributeDefs[name] is EnumDBCAttributeDefinition attriDefEnum) {
                    attriDefEnum.DefaultValue = defaultVal.Replace("\"", "");
                } else {
                    throw new ApplicationException("DBCAttributeBase can not convert to EnumDBCAttributeDefinition");
                }
                break;
            }
        }

        private void ParseAttribute(string strLine) {
            if (strLine.StartsWith("BA_DEF_")) {
                return;
            }
            Regex re = new Regex(@"BA_\s+""(?<attriName>\S+)""\s*(?<objType>\S+)?\s*(?<id>\d*)?\s*(?<name>\S*)?\s+(?<value>\S+)\s*;");
            string attriName = re.Match(strLine).Groups["attriName"].Value;
            string strObjType = re.Match(strLine).Groups["objType"].Value;
            string strID = re.Match(strLine).Groups["id"].Value;
            uint iID = 0;
            if (strID.Length > 0) {
                iID = uint.Parse(strID);
            }
            string name = re.Match(strLine).Groups["name"].Value;
            string value = re.Match(strLine).Groups["value"].Value;

            DBCAttributeBase attributeDef = _netWork.AttributeDefs[attriName];
            DBCObjBase attriObj;
            if (strObjType.Contains("BU_")) {
                if (attributeDef.ObjType != DBCObjType.Node) {
                    throw new ApplicationException("The DBCObjType of this line attribute doesn't match");
                }
                attriObj = _netWork.Nodes[name];
            } else if (strObjType.Contains("BO_")) {
                if (attributeDef.ObjType != DBCObjType.Message) {
                    throw new ApplicationException("The DBCObjType of this line attribute doesn't match");
                }
                attriObj = _netWork.GetMessage(iID);
            } else if (strObjType.Contains("SG_")) {
                if (attributeDef.ObjType != DBCObjType.Signal) {
                    throw new ApplicationException("The DBCObjType of this line attribute doesn't match");
                }
                attriObj = _netWork.GetSignal(iID, name);
            } else {
                if (attributeDef.ObjType != DBCObjType.DBCObjBase) {
                    throw new ApplicationException("The DBCObjType of this line attribute doesn't match");
                }
                attriObj = _netWork;
            }
            if (attriObj == null) {
                throw new ApplicationException("The ID or/and name of this line attribute is error");
            }

            switch (attributeDef.ValType) {
            case AttriValType.STRING:
                if (attributeDef is StringDBCAttributeDefinition attriDefString) {
                    DBCAttribute attriString = new DBCAttribute(attriDefString, value.Replace("\"", ""));
                    attriObj.AddAttribute(attriString);
                } else {
                    throw new ApplicationException("DBCAttributeBase can not convert to StringDBCAttributeDefinition");
                }
                break;
            case AttriValType.INT:
                if (attributeDef is IntDBCAttributeDefinition attriDefInt) {
                    DBCAttribute attriInt = new DBCAttribute(attriDefInt, int.Parse(value));
                    attriObj.AddAttribute(attriInt);
                } else {
                    throw new ApplicationException("DBCAttributeBase can not convert to IntDBCAttributeDefinition");
                }
                break;
            case AttriValType.HEX:
                if (attributeDef is HexDBCAttributeDefinition attriDefHex) {
                    DBCAttribute attriHex = new DBCAttribute(attriDefHex, uint.Parse(value));
                    attriObj.AddAttribute(attriHex);
                } else {
                    throw new ApplicationException("DBCAttributeBase can not convert to HexDBCAttributeDefinition");
                }
                break;
            case AttriValType.FLOAT:
                if (attributeDef is FloatDBCAttributeDefinition attriDefFloat) {
                    DBCAttribute attriFloat = new DBCAttribute(attriDefFloat, double.Parse(value));
                    attriObj.AddAttribute(attriFloat);
                } else {
                    throw new ApplicationException("DBCAttributeBase can not convert to FloatDBCAttributeDefinition");
                }
                break;
            case AttriValType.ENUM:
                if (attributeDef is EnumDBCAttributeDefinition attriDefEnum) {
                    DBCAttribute attriEnum = new DBCAttribute(attriDefEnum, int.Parse(value));
                    attriObj.AddAttribute(attriEnum);
                } else {
                    throw new ApplicationException("DBCAttributeBase can not convert to EnumDBCAttributeDefinition");
                }
                break;
            }
        }

        private void ParseSignalValueDesc(string strLine) {
            if (strLine.StartsWith("VAL_TABLE_")) {
                return;
            }
            Regex re = new Regex(@"VAL_\s+(?<id>\d+)\s+(?<name>\S+)\s+(?<valDesc>.+)\s*;");
            string strID = re.Match(strLine).Groups["id"].Value;
            string name = re.Match(strLine).Groups["name"].Value;
            string strValDesc = re.Match(strLine).Groups["valDesc"].Value;
            Dictionary<uint, string> ValTblDescs = ParseValueDescription(strValDesc);
            Signal signal = _netWork.GetSignal(uint.Parse(strID), name);
            signal.ValueDescs = ValTblDescs;
        }

        private List<int[]> ParseExtendedMultiplexValue(string strValDesc) {
            List<int[]> ret = new List<int[]>();
            string[] strGroups = strValDesc.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string strGroup in strGroups) {
                int[] iVals = new int[2];
                string[] strVals = strGroup.Trim().Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                iVals[0] = int.Parse(strVals[0]);
                iVals[1] = int.Parse(strVals[1]);
                ret.Add(iVals);
            }
            return ret;
        }

        private void ParseSignalExtendedMultiplex(string strLine) {
            Regex re = new Regex(@"SG_MUL_VAL_\s+(?<id>\d+)\s+(?<name>\S+)\s+(?<nameMUXor>\S+)\s+(?<valDesc>.+)\s*;");
            string strID = re.Match(strLine).Groups["id"].Value;
            string name = re.Match(strLine).Groups["name"].Value;
            string nameMor = re.Match(strLine).Groups["nameMUXor"].Value;
            string strValDesc = re.Match(strLine).Groups["valDesc"].Value;
            List<int[]> iVals = ParseExtendedMultiplexValue(strValDesc);
            Signal signal = _netWork.GetSignal(uint.Parse(strID), name);
            Signal MUXor = _netWork.GetSignal(uint.Parse(strID), nameMor);
            signal.MultiplexorValue = new KeyValuePair<Signal, List<int[]>>(MUXor, iVals);
        }

        private List<T> LoadDisplayFile<T>(string fileName) {
            try {
                if (File.Exists(fileName)) {
                    Type[] extraTypes = new Type[] { typeof(T) };
                    List<T> displays = new XmlSerializer(typeof(List<T>), extraTypes).Deserialize(new FileStream(fileName, FileMode.Open)) as List<T>;
                    return displays;
                } else {
                    throw new ApplicationException("Failed to locate the file: " + fileName + ", reason: it doesn't exist.");
                }
            } catch (Exception ex) {
                throw new ApplicationException("Failed to load parameters from: " + fileName + ", reason: " + ex.Message);
            }
        }

        public string GetDisplayString(Signal sigIn, string strNotSupport) {
            string ret = string.Empty;
            int iUsed = sigIn.TestSignalUesed();
            if (iUsed > 0) {
                if (sigIn.ValueDescs.ContainsKey((uint)sigIn.RawValue)) {
                    ret = sigIn.ValueDescs[(uint)sigIn.RawValue];
                    foreach (ValueDisplay vd in ValDispalys) {
                        if (sigIn.Parent is Message msg) {
                            if (vd.ID == msg.ID && vd.Name == sigIn.Name) {
                                ret = vd.Values[(uint)sigIn.RawValue];
                            }
                        }
                    }
                } else {
                    if (sigIn.Unit == "ASCII" || sigIn.Unit == "HEX") {
                        foreach (string item in sigIn.ListString) {
                            ret += item + ",";
                        }
                        ret = ret.TrimEnd(',');
                    } else {
                        ret = sigIn.Value.ToString();
                    }
                }
            } else if (iUsed == 0) {
                ret = strNotSupport;
            }
            return ret;
        }

    }

    public class SignalDisplay {
        public uint ID { get; set; } // 报文ID
        public string Name { get; set; } // 信号名称
        public string Display { get; set; } // 显示内容

        public SignalDisplay() { }

        public SignalDisplay(uint id, string name, string display) {
            ID = id;
            Name = name;
            Display = display;
        }

        public override string ToString() {
            return string.Format("{0}, {1}, {2}", ID, Name, Display);
        }
    }

    public class ValueDisplay {
        public uint ID { get; set; } // 报文ID
        public string Name { get; set; } // 信号名称
        public DictionaryEx<uint, string> Values { get; set; } // 信号值内容, DictionaryEx<值, 显示内容>

        public ValueDisplay() { }

        public ValueDisplay(uint id, string name, DictionaryEx<uint, string> values) {
            ID = id;
            Name = name;
            Values = values;
        }
    }
}
