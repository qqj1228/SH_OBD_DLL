using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Dyno_OBD_DLL {
    public static class Utility {
        /// <summary>
        /// 反序列化
        /// </summary>
        public static T Deserializer<T>(string strXML) where T : class {
            try {
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(strXML))) {
                    using (XmlReader xr = XmlReader.Create(ms)) {
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        return serializer.Deserialize(xr) as T;
                    }
                }
            } catch (Exception) {
                return null;
            }
        }

        /// <summary>
        /// 序列化
        /// </summary>
        public static string XmlSerialize<T>(T obj, ref string indentChars) {
            using (MemoryStream ms = new MemoryStream()) {
                XmlWriterSettings setting = new XmlWriterSettings() {
                    Encoding = new UTF8Encoding(false),
                    Indent = true,
                };
                using (XmlWriter writer = XmlWriter.Create(ms, setting)) {
                    XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                    namespaces.Add(string.Empty, string.Empty);
                    XmlSerializer xmlSearializer = new XmlSerializer(obj.GetType(), string.Empty);
                    xmlSearializer.Serialize(writer, obj, namespaces);
                    if (indentChars != null) {
                        indentChars = writer.Settings.IndentChars;
                    }
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }

        public static List<T> LoadXmlFile<T>(string fileName) {
            try {
                if (File.Exists(fileName)) {
                    Type[] extraTypes = new Type[] { typeof(T) };
                    List<T> xmls = new XmlSerializer(typeof(List<T>), extraTypes).Deserialize(new FileStream(fileName, FileMode.Open)) as List<T>;
                    return xmls;
                } else {
                    throw new ApplicationException("Failed to locate the file: " + fileName + ", reason: it doesn't exist.");
                }
            } catch (Exception ex) {
                throw new ApplicationException("Failed to load parameters from: " + fileName + ", reason: " + ex.Message);
            }
        }

    }

    /// <summary>
    /// 获取dll版本类，需要传入dll主class数据类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class DllVersion<T> {
        public static Version AssemblyVersion {
            get { return Assembly.GetAssembly(typeof(T)).GetName().Version; }
        }
    }

}
