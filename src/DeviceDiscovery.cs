// Copyright (c) 2026 the Receiver Rescue project owner. All rights reserved. Proprietary; see LICENSE.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace ReceiverRescue {
 public sealed class DeviceObservation {
  // PhysicalId stays in memory only; never show or log instance IDs/serials.
  public string PhysicalId; public ushort Vendor,Product,UsagePage,Usage; public bool Hid,CapabilitiesRead;
 }
 public sealed class ReceiverState {
  internal string Identity="";
  public int BootloaderInterfaces,BootloaderDevices,NormalDevices,HeadsetDevices,UnsupportedDevices,ReadableHidInterfaces;public bool DescriptorUnavailable;
  public bool NormalReceiver {get{return NormalDevices>0;}}
  public bool HeadsetUsb {get{return HeadsetDevices>0;}}
  public string Code {get{return HeadsetUsb?"disconnect-headset":NormalReceiver?"normal-receiver":UnsupportedDevices>0?"unsupported":BootloaderDevices>1?"multiple-receivers":BootloaderDevices==1&&BootloaderInterfaces>0?"ready":BootloaderDevices==1?"check-interface":"not-found";}}
  public bool CanLaunch {get{return Code=="ready";}}
 }
 public static class DeviceDiscovery {
  [StructLayout(LayoutKind.Sequential)]struct DeviceInterface{public int Size;public Guid Guid;public int Flags;public IntPtr Reserved;}
  [StructLayout(LayoutKind.Sequential)]struct DeviceInfo{public int Size;public Guid ClassGuid;public uint DevInst;public IntPtr Reserved;}
  [StructLayout(LayoutKind.Sequential)]struct HidCaps{public ushort Usage,UsagePage,InputLength,OutputLength,FeatureLength;[MarshalAs(UnmanagedType.ByValArray,SizeConst=17)]public ushort[] Reserved;public ushort LinkNodes,InputButtons,InputValues,InputData,OutputButtons,OutputValues,OutputData,FeatureButtons,FeatureValues,FeatureData;}
  [DllImport("hid.dll")]static extern void HidD_GetHidGuid(out Guid g);
  [DllImport("hid.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.U1)]static extern bool HidD_GetPreparsedData(SafeFileHandle h,out IntPtr p);
  [DllImport("hid.dll")][return:MarshalAs(UnmanagedType.U1)]static extern bool HidD_FreePreparsedData(IntPtr p);
  [DllImport("hid.dll")]static extern int HidP_GetCaps(IntPtr p,out HidCaps caps);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern SafeFileHandle CreateFile(string path,uint access,uint share,IntPtr security,uint creation,uint flags,IntPtr template);
  [DllImport("setupapi.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr SetupDiGetClassDevs(ref Guid g,string en,IntPtr w,uint flags);
  [DllImport("setupapi.dll",SetLastError=true)]static extern bool SetupDiEnumDeviceInterfaces(IntPtr h,IntPtr dev,ref Guid g,uint i,ref DeviceInterface d);
  [DllImport("setupapi.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr h,ref DeviceInterface d,IntPtr detail,uint n,out uint need,ref DeviceInfo info);
  [DllImport("setupapi.dll")]static extern bool SetupDiDestroyDeviceInfoList(IntPtr h);
  [DllImport("cfgmgr32.dll",CharSet=CharSet.Unicode)]static extern uint CM_Get_Device_ID(uint dev,StringBuilder text,uint length,uint flags);
  [DllImport("cfgmgr32.dll")]static extern uint CM_Get_Parent(out uint parent,uint dev,uint flags);
  static string PhysicalId(uint dev,string fallback){
   // Group HID collections and USB interfaces at their physical USB devnode,
   // retaining the instance suffix to distinguish separate receivers.
   for(int i=0;i<16;i++){var id=new StringBuilder(512);if(CM_Get_Device_ID(dev,id,512,0)==0){string s=id.ToString();if(s.StartsWith("USB\\VID_",StringComparison.OrdinalIgnoreCase)&&s.IndexOf("&MI_",StringComparison.OrdinalIgnoreCase)<0)return s.ToLowerInvariant();}uint parent;if(CM_Get_Parent(out parent,dev,0)!=0)break;dev=parent;}
   return fallback.ToLowerInvariant(); // Never merge devices with unknown ancestry.
  }
  static bool UsbId(string path,string key,out ushort value){value=0;int i=path.IndexOf(key,StringComparison.OrdinalIgnoreCase);return i>=0&&path.Length>=i+key.Length+4&&ushort.TryParse(path.Substring(i+key.Length,4),System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture,out value);}
  public static ReceiverState Classify(IEnumerable<DeviceObservation> observations){
   var groups=observations.GroupBy(o=>o.PhysicalId,StringComparer.OrdinalIgnoreCase).ToArray();var state=new ReceiverState();
   foreach(var group in groups){
    if(group.Any(o=>o.Vendor==0x046d&&o.Product==0x0aba))state.NormalDevices++;
    if(group.Any(o=>(o.Vendor==0x046d&&o.Product==0x0abb)||(o.Vendor==0x170d&&o.Product==0x0101)))state.HeadsetDevices++;
    if(group.Any(o=>o.Vendor==0x170d&&o.Product!=0x0100&&o.Product!=0x0101))state.UnsupportedDevices++;
    var boot=group.Where(o=>o.Vendor==0x170d&&o.Product==0x0100).ToArray();
    if(boot.Length>0){state.BootloaderDevices++;state.BootloaderInterfaces+=boot.Count(o=>o.Hid&&o.CapabilitiesRead&&o.UsagePage==0xff00&&o.Usage==1);state.DescriptorUnavailable|=boot.Any(o=>o.Hid&&!o.CapabilitiesRead);}
   }
   state.ReadableHidInterfaces=observations.Count(o=>o.Hid&&o.CapabilitiesRead);state.Identity=string.Join("|",groups.Select(g=>g.Key).OrderBy(k=>k,StringComparer.OrdinalIgnoreCase));return state;
  }
  static void Enumerate(Guid guid,bool hid,List<DeviceObservation> found){
   IntPtr h=SetupDiGetClassDevs(ref guid,null,IntPtr.Zero,0x12);if(h==new IntPtr(-1))throw new InvalidOperationException("Windows device enumeration failed ("+Marshal.GetLastWin32Error()+").");
   try{for(uint i=0;;i++){
    var d=new DeviceInterface{Size=Marshal.SizeOf(typeof(DeviceInterface))};if(!SetupDiEnumDeviceInterfaces(h,IntPtr.Zero,ref guid,i,ref d)){int error=Marshal.GetLastWin32Error();if(error==259)break;throw new InvalidOperationException("Windows device interface enumeration failed ("+error+").");}
    var info=new DeviceInfo{Size=Marshal.SizeOf(typeof(DeviceInfo))};uint n;SetupDiGetDeviceInterfaceDetail(h,ref d,IntPtr.Zero,0,out n,ref info);if(n<8)throw new InvalidOperationException("Windows returned an incomplete device descriptor.");IntPtr p=Marshal.AllocHGlobal((int)n);
    try{Marshal.WriteInt32(p,IntPtr.Size==8?8:6);if(!SetupDiGetDeviceInterfaceDetail(h,ref d,p,n,out n,ref info))throw new InvalidOperationException("Windows device details unavailable ("+Marshal.GetLastWin32Error()+").");string path=Marshal.PtrToStringUni(IntPtr.Add(p,4));ushort vendor,product;if(!UsbId(path,"vid_",out vendor)||!UsbId(path,"pid_",out product))continue;
     if(vendor!=0x170d&&!(vendor==0x046d&&(product==0x0aba||product==0x0abb)))continue;
     var o=new DeviceObservation{PhysicalId=PhysicalId(info.DevInst,path),Vendor=vendor,Product=product,Hid=hid};
     if(hid){
      // Zero desired access: cached descriptor queries only. No HID reports,
      // bootloader commands, mode changes or firmware transfers are sent.
      using(var handle=CreateFile(path,0,3,IntPtr.Zero,3,0,IntPtr.Zero)){IntPtr data;if(!handle.IsInvalid&&HidD_GetPreparsedData(handle,out data)){try{HidCaps caps;if(HidP_GetCaps(data,out caps)==0x110000){o.CapabilitiesRead=true;o.UsagePage=caps.UsagePage;o.Usage=caps.Usage;}}finally{HidD_FreePreparsedData(data);}}}
     }
     found.Add(o);
    }finally{Marshal.FreeHGlobal(p);}
   }}finally{SetupDiDestroyDeviceInfoList(h);}
  }
  public static ReceiverState Scan(){Guid hid;HidD_GetHidGuid(out hid);var found=new List<DeviceObservation>();Enumerate(new Guid("a5dcbf10-6530-11d2-901f-00c04fb951ed"),false,found);Enumerate(hid,true,found);return Classify(found);}
 }
}
