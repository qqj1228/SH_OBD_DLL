using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBCParser.DBCObj {
    public class Message : DBCObjBase {
        public Dictionary<string, Signal> Signals { get; private set; }
        public uint ID { get; set; }
        public string Name { get; set; }
        public int Length { get; set; } // 以字节为单位
        public Node Sender { get; set; }

        public Message(uint ID, string name, int length) {
            Signals = new Dictionary<string, Signal>();
            this.ID = ID;
            this.Name = name;
            this.Length = length;
        }

        public void AddSignal(Signal signalIn) {
            // 特殊ID表示该报文作为未使用信号的容器，不必检查新信号的合法性
            if (ID != 0xC0000000) {
                if (signalIn.Message != null) {
                    throw new ApplicationException("Signal already belongs to message: " + signalIn.Message.Name);
                }
                if (!CheckSignalPosition(signalIn)) {
                    throw new ApplicationException("Signal position doesn't fit in message layout");
                }
            }
            AddChild(signalIn);
            Signals.Add(signalIn.Name, signalIn);
        }

        /// <summary>
        /// 简单检查新信号在报文中的位置是否合法
        /// </summary>
        /// <param name="signalIn"></param>
        /// <returns></returns>
        private bool CheckSignalPosition(Signal signalIn) {
            List<int[]> posListIn = signalIn.GetRawPosition();
            if (posListIn[posListIn.Count - 1][1] >= this.Length * 8) {
                return false;
            }
            // 若是扩展复用信号的话，SG_MUL_VAL_会在文件最后才读入，故无法判断其位置是否正确
            //foreach (Signal signal in Signals.Values) {
            //    List<int[]> posList = signal.GetRawPosition();
            //    if (signal.MultiplexedID == signalIn.MultiplexedID || signal.MultiplexedID < 0 || signalIn.MultiplexedID < 0) {
            //        foreach (int[] pos in posList) {
            //            foreach (int[] posIn in posListIn) {
            //                if (!(posIn[1] < pos[0] || pos[1] < posIn[0])) {
            //                    return false;
            //                }
            //            }
            //        }
            //    }
            //}
            return true;
        }

        /// <summary>
        /// 加载完dbc文件后，对报文的后处理函数。目前仅包含对信号的处理
        /// </summary>
        public void PostProcess() {
            List<Signal> MUXors = new List<Signal>();
            foreach (Signal signal in Signals.Values) {
                if (signal.Multiplexor) {
                    MUXors.Add(signal);
                }
            }
            foreach (Signal signal in Signals.Values) {
                if (signal.MultiplexedID >= 0 && (signal.MultiplexorValue.Key == null)) {
                    int[] iIDs = new int[] { signal.MultiplexedID, signal.MultiplexedID };
                    List<int[]> MUXedIDs = new List<int[]> { iIDs };
                    signal.MultiplexorValue = new KeyValuePair<Signal, List<int[]>>(MUXors[0], MUXedIDs);
                }
            }
        }

        /// <summary>
        /// 用于dbc文件读入后的纯数据解析，必须在dbc文件全部解析完成后才能执行
        /// </summary>
        /// <param name="strData"></param>
        /// <returns></returns>
        public bool SetSignalRawValue(string strData) {
            // 判断输入的数据长度是否正确
            bool wrongLen = true;
            if (Attributes.ContainsKey("DLCMin")) {
                DBCAttribute attribute = Attributes["DLCMin"];
                if (attribute.AttriValue.iValue != 0) {
                    if (strData.Length >= attribute.AttriValue.iValue * 2) {
                        wrongLen = false;
                    }
                }
            }
            if (strData.Length < Length * 2 && wrongLen) {
                return false;
            }

            // 计算信号原始数值
            foreach (Signal signal in Signals.Values) {
                List<int[]> rawPos = signal.GetRawPosition(); // 信号在报文中的原始位置List
                List<byte> parts = new List<byte>(); // 将输入数据的字符串转成16进制后得到的字节List
                foreach (int[] pos in rawPos) {
                    int index = pos[0] / 8;
                    if (index * 2 + 1 < strData.Length) {
                        string strPart = strData.Substring(index * 2, 2);
                        byte part = Convert.ToByte(strPart, 16);
                        part = (byte)(part << (7 - pos[1] % 8));
                        part = (byte)(part >> (7 - pos[1] % 8 + pos[0] % 8));
                        parts.Add(part);
                    }
                }
                if (parts.Count == 0) {
                    continue;
                }

                signal.ListString.Clear();
                if (signal.Unit == "ASCII") {
                    if (signal.Length > 8) {
                        string strVal = string.Empty;
                        foreach (byte part in parts) {
                            if (0x20 <= part && part <= 0x7E) {
                                strVal += Convert.ToChar(part);
                            }
                        }
                        signal.ListString.Add(strVal);
                    } else if (signal.Length == 8) {
                        int index = rawPos[0][0] / 8;
                        string strVal = HexStringToASCIIString(index, Length, strData);
                        signal.ListString.Add(strVal);
                        if (Attributes.ContainsKey("MultipleItem")) {
                            DBCAttribute attribute = Attributes["MultipleItem"];
                            if (attribute.AttriValue.iValue > 0) {
                                int start = Length;
                                int scale = 2;
                                while (strData.Length / 2 >= Length * scale) {
                                    strVal = HexStringToASCIIString(start, Length * scale, strData);
                                    signal.ListString.Add(strVal);
                                    start += Length;
                                    ++scale;
                                }
                            }
                        }
                    } else {
                        return false;
                    }
                } else if (signal.Unit == "HEX") {
                    string strVal = strData.Substring(0, Length * 2);
                    signal.ListString.Add(strVal);
                    if (Attributes.ContainsKey("MultipleItem")) {
                        DBCAttribute attribute = Attributes["MultipleItem"];
                        if (attribute.AttriValue.iValue > 0) {
                            int start = Length;
                            int scale = 2;
                            while (strData.Length / 2 >= Length * scale) {
                                strVal = strData.Substring(start * 2, Length * 2);
                                signal.ListString.Add(strVal);
                                start += Length;
                                ++scale;
                            }
                        }
                    }
                } else {
                    uint rawValue = parts[0];
                    for (int i = 1; i < parts.Count; i++) {
                        rawValue <<= rawPos[i][1] - rawPos[i][0] + 1;
                        rawValue += parts[i];
                    }
                    if (signal.Signed) {
                        signal.RawValue = (int)rawValue;
                    } else {
                        signal.RawValue = rawValue;
                    }
                }
            }
            // 根据信号的直接MUXor判断信号在报文中是否被使用
            // 并不判断MUXor是否也可用（目前阶段无法判断，需全部信号处理完后才行）
            foreach (Signal signal in Signals.Values) {
                if (signal.MultiplexedID < 0) {
                    signal.UsedInMessage = true;
                } else {
                    Signal MUXor = signal.MultiplexorValue.Key;
                    foreach (int[] MUXedIDs in signal.MultiplexorValue.Value) {
                        if (MUXedIDs[0] <= MUXor.RawValue && MUXor.RawValue <= MUXedIDs[1]) {
                            signal.UsedInMessage = true;
                            break;
                        }
                    }
                }
            }
            return true;
        }

        private string HexStringToASCIIString(int start, int length, string strData) {
            string strVal = string.Empty;
            for (int i = start; i < length; i++) {
                string strPart = strData.Substring(i * 2, 2);
                byte part = Convert.ToByte(strPart, 16);
                if (0x20 <= part && part <= 0x7E) {
                    strVal += Convert.ToChar(part);
                }
            }
            return strVal;
        }
    }
}
