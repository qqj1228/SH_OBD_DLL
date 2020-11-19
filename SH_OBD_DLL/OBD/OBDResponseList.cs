using System;
using System.Collections;
using System.Collections.Generic;

namespace SH_OBD_DLL {
    public class OBDResponse {
        public string Data { get; set; }
        public string Header { get; set; }

        public OBDResponse() {
            Data = "";
        }

        public string GetDataByte(int index) {
            index *= 2;
            if (index + 2 > Data.Length) {
                return "";
            }
            return Data.Substring(index, 2);
        }

        public int GetDataByteCount() {
            return Data.Length / 2;
        }
    }

    public class OBDResponseList {
        private readonly List<OBDResponse> _Responses;
        public bool ErrorDetected { get; set; }
        public string RawResponse { get; set; }
        public bool Pending { get; set; }

        public int ResponseCount {
            get { return _Responses.Count; }
        }

        public OBDResponseList(string response) {
            RawResponse = response;
            ErrorDetected = false;
            Pending = false;
            _Responses = new List<OBDResponse>();
        }

        public void AddOBDResponse(OBDResponse response) {
            _Responses.Add(response);
        }

        public OBDResponse GetOBDResponse(int index) {
            if (index < _Responses.Count) {
                return _Responses[index];
            }
            return null;
        }
    }
}
