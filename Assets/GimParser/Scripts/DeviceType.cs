using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace cn.cssoftstudio.gimParser
{
    public enum DeviceType
    {
        //导线
        wire,
        //杆塔
        tower,
        //绝缘子串
        strings,
        //基础
        bases,
        //间隔棒
        spacer,
        //防振锤
        damper,
        //金具
        fittings,
        //盘形绝缘子
        insulator,
        //棒形绝缘子
        rodininsulator,
        //其他设备
        equipment
    }

    public enum GimFileFlag
    {
        //变电工程文件标识
        GIMPKGS,
        //线路工程文件标识
        GIMPKGT,
        //电缆工程文件标识
        GIMPKEC
    }

    public enum ProjectType
    {
        //变电站
        TS,
        //串补站
        SS,
        //换流站
        CS
    }
}
