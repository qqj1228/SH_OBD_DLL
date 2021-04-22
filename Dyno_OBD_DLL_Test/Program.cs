using Dyno_OBD_DLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dyno_OBD_DLL_Test {
    class Program {
        static void Main(string[] args) {
            string strXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><Request><Cmd>{CMD}</Cmd></Request>";
            OBDDiagnosis OBD = new OBDDiagnosis();
            if (OBD.OBDDiagnosisInit()) {
                // 每次连接车辆OBD时均需要执行ConnectOBD()函数至少一次
                // 执行时会先断开当前VCI和车辆OBD的连接，然后再连接VCI和车辆OBD
                if (OBD.ConnectOBD()) {
                    // OBD 诊断必须以此命令开始
                    string strResponse = OBD.RequestByXMLString(strXml.Replace("{CMD}", "StartTest"));
                    Console.WriteLine();
                    Console.WriteLine(strResponse);

                    strResponse = OBD.RequestByXMLString(strXml.Replace("{CMD}", "GetCarInfo"));
                    Console.WriteLine();
                    Console.WriteLine(strResponse);

                    strResponse = OBD.RequestByXMLString(strXml.Replace("{CMD}", "GetOBDInfo"));
                    Console.WriteLine();
                    Console.WriteLine(strResponse);

                    strResponse = OBD.RequestByXMLString(strXml.Replace("{CMD}", "GetSpeedInfo"));
                    Console.WriteLine();
                    Console.WriteLine(strResponse);

                    strResponse = OBD.RequestByXMLString(strXml.Replace("{CMD}", "GetOilInfo"));
                    Console.WriteLine();
                    Console.WriteLine(strResponse);

                } else {
                    Console.WriteLine("ConnectOBD() failed");
                }
            } else {
                Console.WriteLine("OBDDiagnosisInit() failed");
            }
            Console.ReadKey();
        }
    }

}
