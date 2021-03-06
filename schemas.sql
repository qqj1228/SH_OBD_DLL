USE master
GO  
CREATE DATABASE SH_OBD
GO

USE SH_OBD
GO
-- 用户密码表
IF OBJECT_ID(N'SH_OBD.dbo.OBDUser') IS NOT NULL
    DROP TABLE SH_OBD.dbo.OBDUser
GO
CREATE TABLE SH_OBD.dbo.OBDUser (
    ID int IDENTITY PRIMARY KEY NOT NULL, -- ID, 自增, 主键
    UserName varchar(20) NOT NULL, -- 用户名
    PassWord varchar(32) NOT NULL, -- 密码
    SN varchar(20) NULL, -- 检测报表编号中顺序号的特征字符串
)
GO

-- 插入字段备注
EXEC sp_addextendedproperty N'MS_Description', N'ID', N'USER', N'dbo', N'TABLE', N'OBDUser', N'COLUMN', N'ID'
EXEC sp_addextendedproperty N'MS_Description', N'用户名', N'USER', N'dbo', N'TABLE', N'OBDUser', N'COLUMN', N'UserName'
EXEC sp_addextendedproperty N'MS_Description', N'密码', N'USER', N'dbo', N'TABLE', N'OBDUser', N'COLUMN', N'PassWord'
EXEC sp_addextendedproperty N'MS_Description', N'特征字符串', N'USER', N'dbo', N'TABLE', N'OBDUser', N'COLUMN', N'SN'
GO

-- 测试: 插入数据
INSERT SH_OBD.dbo.OBDUser
    VALUES (
        'admin',
        '81DC9BDB52D04DC20036DBD8313ED055' -- 默认密码：1234
    )
GO

USE SH_OBD
GO
-- OBD数据表
IF OBJECT_ID(N'SH_OBD.dbo.OBDData') IS NOT NULL
    DROP TABLE SH_OBD.dbo.OBDData
GO
CREATE TABLE SH_OBD.dbo.OBDData (
    ID int IDENTITY PRIMARY KEY NOT NULL, -- ID, 自增, 主键
    WriteTime datetime NOT NULL default(getdate()), -- 写入时间
    VIN varchar(17) NOT NULL, -- 车辆VIN号，0902
    ECU_ID varchar(8) NOT NULL, -- 与排放相关的ECU Response ID
    MIL varchar(50) default('不适用'), -- MIL灯状态，0101
    MIL_DIST varchar(50) default('不适用'), -- MIL亮后行驶里程（km），0121
    OBD_SUP varchar(50) default('不适用'), -- OBD型式检验类型，011C
    ODO varchar(50) default('不适用'), -- 总累计里程ODO（km），01A6
    DTC03 varchar(100) default('--'), -- 存储DTC，03
    DTC07 varchar(100) default('--'), -- 未决DTC，07
    DTC0A varchar(100) default('--'), -- 永久DTC，0A
    MIS_RDY varchar(50) default('不适用'), -- 失火监测，0101
    FUEL_RDY varchar(50) default('不适用'), -- 燃油系统监测，0101
    CCM_RDY varchar(50) default('不适用'), -- 综合组件监测，0101
    CAT_RDY varchar(50) default('不适用'), -- 催化剂监测，0101
    HCAT_RDY varchar(50) default('不适用'), -- 加热催化剂监测，0101
    EVAP_RDY varchar(50) default('不适用'), -- 燃油蒸发系统监测，0101
    AIR_RDY varchar(50) default('不适用'), -- 二次空气系统监测，0101
    ACRF_RDY varchar(50) default('不适用'), -- 空调系统制冷剂监测，0101
    O2S_RDY varchar(50) default('不适用'), -- 氧气传感器监测，0101
    HTR_RDY varchar(50) default('不适用'), -- 加热氧气传感器监测，0101
    EGR_RDY varchar(50) default('不适用'), -- EGR/VVT系统监测，0101
    HCCAT_RDY varchar(50) default('不适用'), -- NMHC催化剂监测，0101
    NCAT_RDY varchar(50) default('不适用'), -- NOx/SCR后处理监测，0101
    BP_RDY varchar(50) default('不适用'), -- 增压系统监测，0101
    EGS_RDY varchar(50) default('不适用'), -- 废气传感器监测，0101
    PM_RDY varchar(50) default('不适用'), -- PM过滤监测，0101
    ECU_NAME varchar(50) default('不适用'), -- ECU名称，090A
    CAL_ID varchar(50) default('不适用'), -- CAL_ID，0904
    CVN varchar(50) default('不适用'), -- CVN，0906
    Result int default(0), -- OBD检测结果，1 - 合格，0 - 不合格
    Upload int default(0), -- 上传数据结果，1 - 成功， 0 - 失败
)
GO

-- 插入字段备注
EXEC sp_addextendedproperty N'MS_Description', N'ID', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'ID'
EXEC sp_addextendedproperty N'MS_Description', N'写入时间', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'WriteTime'
EXEC sp_addextendedproperty N'MS_Description', N'车辆VIN号', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'VIN'
EXEC sp_addextendedproperty N'MS_Description', N'ECU Response ID', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'ECU_ID'
EXEC sp_addextendedproperty N'MS_Description', N'MIL灯状态', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'MIL'
EXEC sp_addextendedproperty N'MS_Description', N'MIL亮后行驶里程（km）', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'MIL_DIST'
EXEC sp_addextendedproperty N'MS_Description', N'OBD型式检验类型', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'OBD_SUP'
EXEC sp_addextendedproperty N'MS_Description', N'总累计里程ODO（km）', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'ODO'
EXEC sp_addextendedproperty N'MS_Description', N'存储DTC', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'DTC03'
EXEC sp_addextendedproperty N'MS_Description', N'未决DTC', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'DTC07'
EXEC sp_addextendedproperty N'MS_Description', N'永久DTC', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'DTC0A'
EXEC sp_addextendedproperty N'MS_Description', N'失火监测', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'MIS_RDY'
EXEC sp_addextendedproperty N'MS_Description', N'燃油系统监测', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'FUEL_RDY'
EXEC sp_addextendedproperty N'MS_Description', N'综合组件监测', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'CCM_RDY'
EXEC sp_addextendedproperty N'MS_Description', N'催化剂监测', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'CAT_RDY'
EXEC sp_addextendedproperty N'MS_Description', N'加热催化剂监测', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'HCAT_RDY'
EXEC sp_addextendedproperty N'MS_Description', N'燃油蒸发系统监测', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'EVAP_RDY'
EXEC sp_addextendedproperty N'MS_Description', N'二次空气系统监测', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'AIR_RDY'
EXEC sp_addextendedproperty N'MS_Description', N'空调系统制冷剂监测', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'ACRF_RDY'
EXEC sp_addextendedproperty N'MS_Description', N'氧气传感器监测', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'O2S_RDY'
EXEC sp_addextendedproperty N'MS_Description', N'加热氧气传感器监测', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'HTR_RDY'
EXEC sp_addextendedproperty N'MS_Description', N'EGR/VVT系统监测', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'EGR_RDY'
EXEC sp_addextendedproperty N'MS_Description', N'NMHC催化剂监测', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'HCCAT_RDY'
EXEC sp_addextendedproperty N'MS_Description', N'NOx/SCR后处理监测', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'NCAT_RDY'
EXEC sp_addextendedproperty N'MS_Description', N'增压系统监测', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'BP_RDY'
EXEC sp_addextendedproperty N'MS_Description', N'废气传感器监测', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'EGS_RDY'
EXEC sp_addextendedproperty N'MS_Description', N'PM过滤监测', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'PM_RDY'
EXEC sp_addextendedproperty N'MS_Description', N'ECU名称', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'ECU_NAME'
EXEC sp_addextendedproperty N'MS_Description', N'CAL_ID', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'CAL_ID'
EXEC sp_addextendedproperty N'MS_Description', N'CVN', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'CVN'
EXEC sp_addextendedproperty N'MS_Description', N'OBD检测结果', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'Result'
EXEC sp_addextendedproperty N'MS_Description', N'上传数据结果', N'USER', N'dbo', N'TABLE', N'OBDData', N'COLUMN', N'Upload'
GO

-- 测试: 插入数据
INSERT SH_OBD.dbo.OBDData
    VALUES (
        '2222-09-16 18:18:18',
        'testvincode012345',
        '7E0',
        'OFF',
        '0',
        '29,CN-OBD-6',
        '0',
        '--',
        '--',
        '--',
        '完成',
        '完成',
        '完成',
        '完成',
        '完成',
        '完成',
        '完成',
        '完成',
        '完成',
        '完成',
        '完成',
        '不适用',
        '不适用',
        '不适用',
        '不适用',
        '不适用',
        'ECM-EngineControl',
        'JMB*36761500,JMB*47872611',
        '1791BC82,16E062BE',
        '1',
        '1'
    )
GO
-- 测试: 修改数据
UPDATE SH_OBD.dbo.OBDData
    SET ECU_ID = '7E8'
    WHERE VIN = 'testvincode012345'
GO
-- 测试: 查询数据
SELECT VIN, ECU_ID
    FROM SH_OBD.dbo.OBDData
GO

USE SH_OBD
GO

-- IUPR表
IF OBJECT_ID(N'SH_OBD.dbo.OBDIUPR') IS NOT NULL
    DROP TABLE SH_OBD.dbo.OBDIUPR
GO
Create TABLE SH_OBD.dbo.OBDIUPR (
    ID int IDENTITY PRIMARY KEY NOT NULL, -- ID, 自增, 主键
    WriteTime datetime NOT NULL default(getdate()), -- 写入时间
    VIN varchar(17) NOT NULL, -- 车辆VIN号
    ECU_ID varchar(8) NOT NULL, -- 与排放相关的ECU Response ID
    -- 0908
    CATCOMP1 int NULL, -- 催化器 组1 监测完成次数
    CATCOND1 int NULL, -- 催化器 组1 符合监测条件次数
    CATCOMP2 int NULL, -- 催化器 组2 监测完成次数
    CATCOND2 int NULL, -- 催化器 组2 符合监测条件次数
    O2SCOMP1 int NULL, -- 前氧传感器 组1 监测完成次数
    O2SCOND1 int NULL, -- 前氧传感器 组1 符合监测条件次数
    O2SCOMP2 int NULL, -- 前氧传感器 组2 监测完成次数
    O2SCOND2 int NULL, -- 前氧传感器 组2 符合监测条件次数
    SO2SCOMP1 int NULL, -- 后氧传感器 组1 监测完成次数
    SO2SCOND1 int NULL, -- 后氧传感器 组1 符合监测条件次数
    SO2SCOMP2 int NULL, -- 后氧传感器 组2 监测完成次数
    SO2SCOND2 int NULL, -- 后氧传感器 组2 符合监测条件次数
    EVAPCOMP int NULL, -- EVAP 监测完成次数
    EVAPCOND int NULL, -- EVAP 符合监测条件次数
    EGRCOMP_08 int NULL, -- EGR和VVT 监测完成次数
    EGRCOND_08 int NULL, -- EGR和VVT 符合监测条件次数
    PFCOMP1 int NULL, -- GPF 组1 监测完成次数
    PFCOND1 int NULL, -- GPF 组1 符合监测条件次数
    PFCOMP2 int NULL, -- GPF 组2 监测完成次数
    PFCOND2 int NULL, -- GPF 组2 符合监测条件次数
    AIRCOMP int NULL, -- 二次空气喷射系统 监测完成次数
    AIRCOND int NULL, -- 二次空气喷射系统 符合监测条件次数
    -- 090B
    HCCATCOMP int NULL, -- NMHC催化器 监测完成次数
    HCCATCOND int NULL, -- NMHC催化器 符合监测条件次数
    NCATCOMP int NULL, -- NOx催化器 监测完成次数
    NCATCOND int NULL, -- NOx催化器 符合监测条件次数
    NADSCOMP int NULL, -- NOx吸附器 监测完成次数
    NADSCOND int NULL, -- NOx吸附器 符合监测条件次数
    PMCOMP int NULL, -- PM捕集器 监测完成次数
    PMCOND int NULL, -- PM捕集器 符合监测条件次数
    EGSCOMP int NULL, -- 废气传感器 监测完成次数
    EGSCOND int NULL, -- 废气传感器 符合监测条件次数
    EGRCOMP_0B int NULL, -- EGR和VVT 监测完成次数
    EGRCOND_0B int NULL, -- EGR和VVT 符合监测条件次数
    BPCOMP int NULL, -- 增压压力 监测完成次数
    BPCOND int NULL, -- 增压压力 符合监测条件次数
)
GO

-- 插入字段备注
EXEC sp_addextendedproperty N'MS_Description', N'ID', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'ID'
EXEC sp_addextendedproperty N'MS_Description', N'写入时间', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'WriteTime'
EXEC sp_addextendedproperty N'MS_Description', N'车辆VIN号', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'VIN'
EXEC sp_addextendedproperty N'MS_Description', N'ECU Response ID', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'ECU_ID'
EXEC sp_addextendedproperty N'MS_Description', N'催化器 组1 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'CATCOMP1'
EXEC sp_addextendedproperty N'MS_Description', N'催化器 组1 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'CATCOND1'
EXEC sp_addextendedproperty N'MS_Description', N'催化器 组2 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'CATCOMP2'
EXEC sp_addextendedproperty N'MS_Description', N'催化器 组2 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'CATCOND2'
EXEC sp_addextendedproperty N'MS_Description', N'前氧传感器 组1 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'O2SCOMP1'
EXEC sp_addextendedproperty N'MS_Description', N'前氧传感器 组1 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'O2SCOND1'
EXEC sp_addextendedproperty N'MS_Description', N'前氧传感器 组2 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'O2SCOMP2'
EXEC sp_addextendedproperty N'MS_Description', N'前氧传感器 组2 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'O2SCOND2'
EXEC sp_addextendedproperty N'MS_Description', N'后氧传感器 组1 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'SO2SCOMP1'
EXEC sp_addextendedproperty N'MS_Description', N'后氧传感器 组1 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'SO2SCOND1'
EXEC sp_addextendedproperty N'MS_Description', N'后氧传感器 组2 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'SO2SCOMP2'
EXEC sp_addextendedproperty N'MS_Description', N'后氧传感器 组2 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'SO2SCOND2'
EXEC sp_addextendedproperty N'MS_Description', N'EVAP 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'EVAPCOMP'
EXEC sp_addextendedproperty N'MS_Description', N'EVAP 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'EVAPCOND'
EXEC sp_addextendedproperty N'MS_Description', N'EGR和VVT 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'EGRCOMP_08'
EXEC sp_addextendedproperty N'MS_Description', N'EGR和VVT 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'EGRCOND_08'
EXEC sp_addextendedproperty N'MS_Description', N'GPF 组1 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'PFCOMP1'
EXEC sp_addextendedproperty N'MS_Description', N'GPF 组1 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'PFCOND1'
EXEC sp_addextendedproperty N'MS_Description', N'GPF 组2 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'PFCOMP2'
EXEC sp_addextendedproperty N'MS_Description', N'GPF 组2 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'PFCOND2'
EXEC sp_addextendedproperty N'MS_Description', N'二次空气喷射系统 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'AIRCOMP'
EXEC sp_addextendedproperty N'MS_Description', N'二次空气喷射系统 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'AIRCOND'
EXEC sp_addextendedproperty N'MS_Description', N'NMHC催化器 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'HCCATCOMP'
EXEC sp_addextendedproperty N'MS_Description', N'NMHC催化器 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'HCCATCOND'
EXEC sp_addextendedproperty N'MS_Description', N'NOx催化器 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'NCATCOMP'
EXEC sp_addextendedproperty N'MS_Description', N'NOx催化器 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'NCATCOND'
EXEC sp_addextendedproperty N'MS_Description', N'NOx吸附器 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'NADSCOMP'
EXEC sp_addextendedproperty N'MS_Description', N'NOx吸附器 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'NADSCOND'
EXEC sp_addextendedproperty N'MS_Description', N'PM捕集器 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'PMCOMP'
EXEC sp_addextendedproperty N'MS_Description', N'PM捕集器 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'PMCOND'
EXEC sp_addextendedproperty N'MS_Description', N'废气传感器 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'EGSCOMP'
EXEC sp_addextendedproperty N'MS_Description', N'废气传感器 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'EGSCOND'
EXEC sp_addextendedproperty N'MS_Description', N'EGR和VVT 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'EGRCOMP_0B'
EXEC sp_addextendedproperty N'MS_Description', N'EGR和VVT 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'EGRCOND_0B'
EXEC sp_addextendedproperty N'MS_Description', N'增压压力 监测完成次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'BPCOMP'
EXEC sp_addextendedproperty N'MS_Description', N'增压压力 符合监测条件次数', N'USER', N'dbo', N'TABLE', N'OBDIUPR', N'COLUMN', N'BPCOND'
GO

USE SH_OBD
GO

-- 车型表用于存储对应车型含有的CAL_ID和CVN
IF OBJECT_ID(N'SH_OBD.dbo.VehicleType') IS NOT NULL
    DROP TABLE SH_OBD.dbo.VehicleType
GO
Create TABLE SH_OBD.dbo.VehicleType (
    ID int IDENTITY PRIMARY KEY NOT NULL, -- ID, 自增, 主键
    Project varchar(20) NOT NULL, -- 项目号
    Type varchar(20) NOT NULL, -- 车型代码
    ECU_ID varchar(8) NOT NULL, -- 与排放相关的ECU Response ID
    CAL_ID varchar(50) NULL,
    CVN varchar(50) NULL,
)
GO

-- 插入字段备注
EXEC sp_addextendedproperty N'MS_Description', N'ID', N'USER', N'dbo', N'TABLE', N'VehicleType', N'COLUMN', N'ID'
EXEC sp_addextendedproperty N'MS_Description', N'项目号', N'USER', N'dbo', N'TABLE', N'VehicleType', N'COLUMN', N'Project'
EXEC sp_addextendedproperty N'MS_Description', N'车型代码', N'USER', N'dbo', N'TABLE', N'VehicleType', N'COLUMN', N'Type'
EXEC sp_addextendedproperty N'MS_Description', N'ECU Response ID', N'USER', N'dbo', N'TABLE', N'VehicleType', N'COLUMN', N'ECU_ID'
EXEC sp_addextendedproperty N'MS_Description', N'CAL_ID', N'USER', N'dbo', N'TABLE', N'VehicleType', N'COLUMN', N'CAL_ID'
EXEC sp_addextendedproperty N'MS_Description', N'CVN', N'USER', N'dbo', N'TABLE', N'VehicleType', N'COLUMN', N'CVN'
GO

-- 插入数据
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA18', 'JXW1032FSA', '7E8', '2564ENF1JM564011', 'B2BA3E6F')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA18', 'JXW1032FSA', '7E8', '2564EMF1JM564021', '31369D5A')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA18', 'JXW1033FSB', '7E8', '2564ENF1JM564011', 'B2BA3E6F')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA18', 'JXW1033FSB', '7E8', '2564EMF1JM564021', '31369D5A')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA18', 'JXW1032FSAA', '7E8', '2564ENF1JM564011', 'B2BA3E6F')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA18', 'JXW1032FSAA', '7E8', '2564EMF1JM564021', '31369D5A')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA18', 'JXW1033FSBA', '7E8', '2564ENF1JM564011', 'B2BA3E6F')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA18', 'JXW1033FSBA', '7E8', '2564EMF1JM564021', '31369D5A')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA18', 'JXW5032XXYFSG', '7E8', '2564ENF1JM564011', 'B2BA3E6F')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA18', 'JXW5032XXYFSG', '7E8', '2564EMF1JM564021', '31369D5A')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA18', 'JXW5033XXYFSG', '7E8', '2564ENF1JM564011', 'B2BA3E6F')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA18', 'JXW5033XXYFSG', '7E8', '2564EMF1JM564021', '31369D5A')
GO
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA19', 'JXW1033CSG', '7E8', 'MD1CS089 01', 'A59F33E8')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA19', 'JXW1034CSG', '7E8', 'MD1CS089 01', 'A59F33E8')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA19', 'JXW5032XXYCSG', '7E8', 'MD1CS089 01', 'A59F33E8')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA19', 'JXW5033XXYCSG', '7E8', 'MD1CS089 01', 'A59F33E8')
INSERT SH_OBD.dbo.VehicleType VALUES ('PT017', 'JXW1030CSGB', '7E8', 'MD1CS089 01', 'A59F33E8')
INSERT SH_OBD.dbo.VehicleType VALUES ('PT017', 'JXW1031CSGB', '7E8', 'MD1CS089 01', 'A59F33E8')
INSERT SH_OBD.dbo.VehicleType VALUES ('PT017', 'JXW5030XXYCSG', '7E8', 'MD1CS089 01', 'A59F33E8')
INSERT SH_OBD.dbo.VehicleType VALUES ('PT017', 'JXW5031XXYCSG', '7E8', 'MD1CS089 01', 'A59F33E8')
GO
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA06', 'JXW1030CSG', '7E8', 'MD1CS089 MT 01', 'A4E1462E')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA06', 'JXW1031CSG', '7E8', 'MD1CS089 MT 01', 'A4E1462E')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA06', 'JXW1031CSG', '7E8', 'MD1CS089 MT 01', 'CAD05AB0')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA06', 'JXW1030CSGA', '7E8', 'MD1CS089 AT 01', '73BF92F7')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA06', 'JXW1030CSGA', '7EA', '99383-07444', '0000F10C')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA06', 'JXW1031CSGA', '7E8', 'MD1CS089 AT 01', '73BF92F7')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA06', 'JXW1031CSGA', '7EA', '99383-07443', '00007320')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA06', 'JXW5030XXYCSGA', '7E8', 'MD1CS089 MT 01', 'A4E1462E')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA06', 'JXW5030XXYCSGB', '7E8', 'MD1CS089 AT 01', '73BF92F7')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA06', 'JXW5030XXYCSGB', '7EA', '99383-07444', '0000F10C')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA06', 'JXW5031XXYCSGA', '7E8', 'MD1CS089 MT 01', 'A4E1462E')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA06', 'JXW5031XXYCSGA', '7E8', 'MD1CS089 MT 01', 'CAD05AB0')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA06', 'JXW5031XXYCSGB', '7E8', 'MD1CS089 AT 01', '73BF92F7')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA06', 'JXW5031XXYCSGB', '7EA', '99383-07443', '00007320')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA06', 'JXW5031XXYCSGC', '7E8', 'MD1CS089 AT 01', '73BF92F7')
INSERT SH_OBD.dbo.VehicleType VALUES ('PPA06', 'JXW5031XXYCSGC', '7EA', '99383-07443', '00007320')
GO
INSERT SH_OBD.dbo.VehicleType VALUES ('PSA07', 'JXW6480CSE', '7E8', 'MD1CS089 AT 01', 'FFB1EF17')
INSERT SH_OBD.dbo.VehicleType VALUES ('PSA07', 'JXW6480CSE', '7EA', '99383-07444', '0000F10C')
INSERT SH_OBD.dbo.VehicleType VALUES ('PSA07', 'JXW6480CSE', '7E8', 'MD1CS089 AT 01', '73BF92F7')
INSERT SH_OBD.dbo.VehicleType VALUES ('PSA07', 'JXW6480CSEA', '7E8', 'MD1CS089 AT 01', 'FFB1EF17')
INSERT SH_OBD.dbo.VehicleType VALUES ('PSA07', 'JXW6480CSEA', '7EA', '99383-07444', '0000F10C')
INSERT SH_OBD.dbo.VehicleType VALUES ('PSA07', 'JXW6480CSEA', '7E8', 'MD1CS089 AT 01', '73BF92F7')
INSERT SH_OBD.dbo.VehicleType VALUES ('PSA07', 'JXW6481CSE', '7E8', 'MD1CS089 AT 01', 'FFB1EF17')
INSERT SH_OBD.dbo.VehicleType VALUES ('PSA07', 'JXW6481CSE', '7EA', '99383-07443', '00007320')
INSERT SH_OBD.dbo.VehicleType VALUES ('PSA07', 'JXW6481CSE', '7E8', 'MD1CS089 AT 01', '73BF92F7')
INSERT SH_OBD.dbo.VehicleType VALUES ('PSA07', 'JXW6481CSEA', '7E8', 'MD1CS089 AT 01', 'FFB1EF17')
INSERT SH_OBD.dbo.VehicleType VALUES ('PSA07', 'JXW6481CSEA', '7EA', '99383-07443', '00007320')
INSERT SH_OBD.dbo.VehicleType VALUES ('PSA07', 'JXW6481CSEA', '7E8', 'MD1CS089 AT 01', '73BF92F7')
GO
