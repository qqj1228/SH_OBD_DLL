using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace DBCParser.DBCObj {
    public class Signal : DBCObjBase {
        public string Name { get; set; }
        public int StartBit { get; set; }
        public int Length { get; set; } // 以bit为单位
        public bool IntelOrder { get; set; }
        public bool Signed { get; set; }
        public double Factor { get; set; }
        public double Offset { get; set; }
        public double ValueMin { get; set; }
        public double ValueMax { get; set; }
        public string Unit { get; set; }
        public bool Multiplexor { get; set; } // 是否为复用开关信号
        public int MultiplexedID { get; set; } // 为负值时表示不是复用信号
        public List<Node> Receiver { get; private set; }
        public Message Message { get; set; }
        public Dictionary<uint, string> ValueDescs { get; set; }
        public KeyValuePair<Signal, List<int[]>> MultiplexorValue { get; set; } // 扩展复用开关信号值，key：该信号对应的扩展复用开关信号
        public bool UsedInMessage { get; set; } // 在报文中是否被使用
        public List<string> ListString { get; set; } // 存放信号中的ASCII字符串List
        public string DisplayString { get; set; } // 在报文解析后存放用于对外显示的字符串

        private double _rawValue;
        /// <summary>
        /// 未转换为物理值的原始数值
        /// </summary>
        public double RawValue {
            get { return _rawValue; }
            set {
                if (!this.Signed && value < 0) {
                    throw new ApplicationException("Signed value not allowed");
                }
                int usableLen = this.Signed ? this.Length - 1 : this.Length;
                if (Math.Pow(2, usableLen) <= Math.Abs(value)) {
                    throw new ApplicationException("Value exceeds signal length");
                }
                _rawValue = value;
            }
        }

        /// <summary>
        /// 将 raw value 转换为实际的物理值
        /// </summary>
        public double Value {
            get {
                return this.RawValue * this.Factor + this.Offset;
            }
            set {
                double tempV = value;
                if (ValueMin != 0 || ValueMax != 0) {
                    tempV = Math.Max(tempV, ValueMin);
                    tempV = Math.Min(tempV, ValueMax);
                }
                _rawValue = (tempV - this.Offset) / this.Factor;
            }
        }

        /// <summary>
        /// 信号类构造函数
        /// bMultiplexer为true，multiplexerID为-1的话，该信号为复用信号交换机，表示报文中不变的部分
        /// bMultiplexer为true，multiplexerID为大于等于0的值的话，该信号为复用信号，
        /// 不同的ID值表示不同的信号复用情况，相同的ID表示在同一个报文中
        /// </summary>
        /// <param name="name"></param>
        /// <param name="startBit"></param>
        /// <param name="length"></param>
        /// <param name="bIntelOrder"></param>
        /// <param name="bSigned"></param>
        /// <param name="factor"></param>
        /// <param name="offset"></param>
        /// <param name="valueMin"></param>
        /// <param name="valueMax"></param>
        /// <param name="unit"></param>
        /// <param name="bMultiplexor"></param>
        /// <param name="iMultiplexedID"></param>
        public Signal(string name, int startBit, int length, bool bIntelOrder = false, bool bSigned = false,
            double factor = 1.0, double offset = 0.0, double valueMin = 0, double valueMax = 0,
            string unit = "", bool bMultiplexor = false, int iMultiplexedID = -1) {
            Name = name;
            StartBit = startBit;
            Length = length;
            IntelOrder = bIntelOrder;
            Signed = bSigned;
            Factor = factor;
            Offset = offset;
            ValueMin = valueMin;
            ValueMax = valueMax;
            Unit = unit;
            Multiplexor = bMultiplexor;
            MultiplexedID = iMultiplexedID;

            Receiver = new List<Node>();
            Message = null;
            ValueDescs = new Dictionary<uint, string>();
            MultiplexorValue = new KeyValuePair<Signal, List<int[]>>();
            UsedInMessage = false;
            ListString = new List<string>();
            _rawValue = 0;
        }

        public void AddReceiver(Node node) {
            Receiver.Add(node);
        }

        /// <summary>
        /// 返回信号在报文中的原始起止bit位置[start, end]数组的List，
        /// List的大小即为该信号占用报文的字节数，
        /// bit位置从小到大排列，该值已考虑字节顺序，使用时无需再计算，
        /// </summary>
        public List<int[]> GetRawPosition() {
            List<int[]> rets = new List<int[]>();

            int rest = Length;
            if (IntelOrder) {
                int lsb = StartBit;
                while (rest > 0) {
                    int[] pos = new int[2];
                    pos[0] = lsb;
                    pos[1] = (8 * ((lsb + 8) / 8)) - 1;
                    if (rest - (pos[1] - pos[0] + 1) < 0) {
                        pos[1] = pos[0] + rest - 1;
                    }
                    rets.Add(pos);
                    rest -= (pos[1] - pos[0] + 1);
                    lsb = pos[1] + 1;
                }
            } else {
                int msb = StartBit;
                while (rest > 0) {
                    int[] pos = new int[2];
                    pos[1] = msb;
                    int a = msb - 8;
                    if (a < 0) {
                        pos[0] = 0;
                    } else {
                        pos[0] = 8 * (a / 8 + 1);
                    }
                    if (rest - (pos[1] - pos[0] + 1) < 0) {
                        pos[0] = msb - rest + 1;
                    }
                    rets.Add(pos);
                    rest -= (pos[1] - pos[0] + 1);
                    msb = pos[0] + 15;
                }
            }
            return rets;
        }

        /// <summary>
        /// 判断信号是否可用，必须在dbc文件全部解析完成后才能执行，
        /// 返回值 > 0 : 可用，返回值 <= 0 : 不可用，0 - 由当前信号的MUXor判断不可用，-1 - 由上一层MUXor判断不可用，其余负值依次类推
        /// </summary>
        /// <param name="sigIn"></param>
        /// <returns></returns>
        public int TestSignalUesed() {
            int iUsed;
            if (UsedInMessage) {
                iUsed = 1;
            } else {
                iUsed = 0;
            }
            if (MultiplexorValue.Key == null) {
                return iUsed;
            } else {
                int iMUXor = MultiplexorValue.Key.TestSignalUesed();
                if (iMUXor <= 0) {
                    return iMUXor - 1;
                } else {
                    return iUsed;
                }
            }
        }

    }
}
