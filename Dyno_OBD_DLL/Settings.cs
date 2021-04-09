using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Dyno_OBD_DLL {
    public class DTC {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
    }

    public class Request {
        public string Cmd { get; set; }
        public Request() {
            Cmd = string.Empty;
        }
    }

    public class Response {
        public int Code { get; set; }
        public string Msg { get; set; }
        public Response() {
            Code = 1;
            Msg = string.Empty;
        }
    }

    public class GetOBDStateResponse : Response {
        public class CData {
            public string State { get; set; }
        }
        public CData Data { get; set; }
    }

    public class GetVCIInfoResponse : Response {
        public class CData {
            public string DllVersion { get; set; }
            public string VCIModel { get; set; }
        }
        public CData Data { get; set; }
    }

    public class StartTestResponse : Response {
        public class CData {
            public string Protocol { get; set; }
            public string AppSTD { get; set; }
            public string FuelType { get; set; }
        }
        public CData Data { get; set; }
    }

    public class GetCarInfoResponse : Response {
        public class CData {
            public string VIN { get; set; }
            public string OBDType { get; set; }
            public string ODO { get; set; }
            public string MIL { get; set; }
            public string MIL_DIST { get; set; }
        }
        public CData Data { get; set; }
    }

    public class GetOBDInfoResponse : Response {
        public class CData {
            public string EngineCVN { get; set; }
            public string EngineCALID { get; set; }
            public string Post_ProcCVN { get; set; }
            public string Post_ProcCALID { get; set; }
            public string OtherCVN { get; set; }
            public string OtherCALID { get; set; }
            public string MODULEECUID { get; set; }
            public string MODULESCRID { get; set; }
            public string MODULEOtherID { get; set; }
        }
        public CData Data { get; set; }
    }

    public class GetDTCResponse : Response {
        public class CData {
            public int ConfirmedNUM { get; set; }
            public string ConfirmedDTC { get; set; }
            public int PendingNUM { get; set; }
            public string PendingDTC { get; set; }
            public int PermanentNUM { get; set; }
            public string PermanentDTC { get; set; }
        }
        public CData Data { get; set; }
    }

    public class GetReadinessResponse : Response {
        public class CData {
            public string MIS_RDY { get; set; }
            public string FUEL_RDY { get; set; }
            public string CCM_RDY { get; set; }
            // 以下为火花点火
            public string CAT_RDY { get; set; }
            public string HCAT_RDY { get; set; }
            public string EVAP_RDY { get; set; }
            public string AIR_RDY { get; set; }
            public string O2S_RDY { get; set; }
            public string HTR_RDY { get; set; }
            // 以下为压缩点火
            public string HCCATRDY { get; set; }
            public string NCAT_RDY { get; set; }
            public string BP_RDY { get; set; }
            public string EGS_RDY { get; set; }
            public string PM_RDY { get; set; }
            // 该项为火花/压缩点火同名项
            public string EGR_RDY { get; set; }
        }
        public CData Data { get; set; }
    }

    public class GetIUPRResponse : Response {
        public class CData {
            // 以下为火花/压缩点火同名项
            public string OBDCOND { get; set; }
            public string IGNCNTR { get; set; }
            // 以下为火花点火
            public string CATCOMP1 { get; set; }
            public string CATCOND1 { get; set; }
            public string CATIUPR1 { get; set; }
            public string CATCOMP2 { get; set; }
            public string CATCOND2 { get; set; }
            public string CATIUPR2 { get; set; }
            public string O2SCOMP1 { get; set; }
            public string O2SCOND1 { get; set; }
            public string O2SIUPR1 { get; set; }
            public string O2SCOMP2 { get; set; }
            public string O2SCOND2 { get; set; }
            public string O2SIUPR2 { get; set; }
            public string EGRCOMP_spark { get; set; }
            public string EGRCOND_spark { get; set; }
            public string EGRIUPR_spark { get; set; }
            public string AIRCOMP { get; set; }
            public string AIRCOND { get; set; }
            public string AIRIUPR { get; set; }
            public string EVAPCOMP { get; set; }
            public string EVAPCOND { get; set; }
            public string EVAPIUPR { get; set; }
            public string SO2SCOMP1 { get; set; }
            public string SO2SCOND1 { get; set; }
            public string SO2SIUPR1 { get; set; }
            public string SO2SCOMP2 { get; set; }
            public string SO2SCOND2 { get; set; }
            public string SO2SIUPR2 { get; set; }
            public string AFRICOMP1 { get; set; }
            public string AFRICOND1 { get; set; }
            public string AFRIIUPR1 { get; set; }
            public string AFRICOMP2 { get; set; }
            public string AFRICOND2 { get; set; }
            public string AFRIIUPR2 { get; set; }
            public string PFCOMP1 { get; set; }
            public string PFCOND1 { get; set; }
            public string PFIUPR1 { get; set; }
            public string PFCOMP2 { get; set; }
            public string PFCOND2 { get; set; }
            public string PFIUPR2 { get; set; }
            // 以下为压缩点火
            public string HCCATCOMP { get; set; }
            public string HCCATCOND { get; set; }
            public string HCCATIUPR { get; set; }
            public string NCATCOMP { get; set; }
            public string NCATCOND { get; set; }
            public string NCATIUPR { get; set; }
            public string NADSCOMP { get; set; }
            public string NADSCOND { get; set; }
            public string NADSIUPR { get; set; }
            public string PMCOMP { get; set; }
            public string PMCOND { get; set; }
            public string PMIUPR { get; set; }
            public string EGSCOMP { get; set; }
            public string EGSCOND { get; set; }
            public string EGSIUPR { get; set; }
            public string EGRCOMP_compression { get; set; }
            public string EGRCOND_compression { get; set; }
            public string EGRIUPR_compression { get; set; }
            public string BPCOMP { get; set; }
            public string BPCOND { get; set; }
            public string BPIUPR { get; set; }
            public string FUELCOMP { get; set; }
            public string FUELCOND { get; set; }
            public string FUELIUPR { get; set; }
        }
        public CData Data { get; set; }
    }

    public class GetRTDataResponse : Response {
        public class CData {
            // 以下为火花/压缩点火同名项
            public string VSS { get; set; }
            public string RPM { get; set; }
            public string ECT { get; set; }
            public string EOT { get; set; }
            public string TP_R { get; set; }
            public string MAF { get; set; }
            public string MAP { get; set; }
            // 以下为火花点火
            public string LOAD_PCT { get; set; }
            public string LAMBDA { get; set; }
            public string SVPOS { get; set; }
            public string SCPOS { get; set; }
            // 以下为压缩点火
            public string ENG_POWER { get; set; }
            public string EGR_ACT { get; set; }
            public string BPA_ACT { get; set; }
            public string BPB_ACT { get; set; }
            public string FUEL_CONSUM { get; set; }
            public string NOXC { get; set; }
            public string EGT { get; set; }
            public string DPF_DP { get; set; }
            public string REAG_RATE { get; set; }
            public string FRP_G { get; set; }
        }
        public CData Data { get; set; }
    }

}
