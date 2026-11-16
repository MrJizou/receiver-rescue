// Copyright (c) 2026 the Receiver Rescue project owner. All rights reserved. Proprietary; see LICENSE.
using System;
using System.Collections.Generic;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace ReceiverRescue {
 public enum AudioStatus { Unknown, Found, NotFound }

 // Small, side-effect-free helpers. Nothing here writes to a device or sends data anywhere.
 public static class Support {
  public static string FormatElapsed(TimeSpan t){
   if(t<TimeSpan.Zero)t=TimeSpan.Zero;
   int hours=(int)t.TotalHours;
   return hours>0?string.Format("{0}:{1:00}:{2:00}",hours,t.Minutes,t.Seconds):string.Format("{0:00}:{1:00}",t.Minutes,t.Seconds);
  }

  // Masks the Windows user name, machine name and user-profile paths so a report can be shared.
  public static string MaskPersonal(string text){
   if(string.IsNullOrEmpty(text))return text;
   // Placeholders are inserted as control-character tokens first, so a short or generic user name
   // (for example "user") cannot corrupt an already inserted placeholder.
   const string TokenProfile="\u0001P\u0001",TokenUser="\u0001U\u0001",TokenMachine="\u0001M\u0001";
   string r=text;
   string profile=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
   if(!string.IsNullOrEmpty(profile))r=Regex.Replace(r,Regex.Escape(profile),TokenProfile,RegexOptions.IgnoreCase);
   r=Regex.Replace(r,@"(?i)([A-Za-z]:\\Users\\)[^\\\s""'<>|\u0001]+","${1}"+TokenUser);
   if(!string.IsNullOrEmpty(Environment.UserName)&&Environment.UserName.Length>2)r=Regex.Replace(r,Regex.Escape(Environment.UserName),TokenUser,RegexOptions.IgnoreCase);
   if(!string.IsNullOrEmpty(Environment.MachineName)&&Environment.MachineName.Length>2)r=Regex.Replace(r,Regex.Escape(Environment.MachineName),TokenMachine,RegexOptions.IgnoreCase);
   return r.Replace(TokenProfile,"%USERPROFILE%").Replace(TokenUser,"<user>").Replace(TokenMachine,"<machine>");
  }

  static string RegString(string path,string name){
   try{using(var key=Registry.LocalMachine.OpenSubKey(path)){if(key==null)return "";object v=key.GetValue(name);return v==null?"":Convert.ToString(v);}}catch{return "";}
  }

  public static string WindowsInfo(){
   const string p=@"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
   string name=RegString(p,"ProductName"),ver=RegString(p,"DisplayVersion"),build=RegString(p,"CurrentBuildNumber"),ubr=RegString(p,"UBR");
   var sb=new StringBuilder(string.IsNullOrEmpty(name)?"Windows":name);
   if(!string.IsNullOrEmpty(ver))sb.Append(" ").Append(ver);
   if(!string.IsNullOrEmpty(build))sb.Append(" (build ").Append(build).Append(string.IsNullOrEmpty(ubr)?"":"."+ubr).Append(")");
   sb.Append(Environment.Is64BitOperatingSystem?", 64-bit":", 32-bit");
   return sb.ToString();
  }

  public static string DotNetInfo(){
   string rel=RegString(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full","Release");
   int n;if(!int.TryParse(rel,out n))return ".NET Framework (unknown)";
   string v=n>=533320?"4.8.1":n>=528040?"4.8":n>=461808?"4.7.2":n>=461308?"4.7.1":n>=460798?"4.7":"older than 4.7";
   return ".NET Framework "+v+" (release "+n+")";
  }

  // Read-only check: is a Windows sound device belonging to the PRO X receiver present and reporting OK?
  // Only a status is returned; no device names, IDs or serial numbers leave this method.
  public static AudioStatus CheckAudioDevice(){
   try{
    bool any=false,ok=false;
    using(var searcher=new ManagementObjectSearcher("SELECT Name,Status,PNPDeviceID FROM Win32_SoundDevice"))
    using(var results=searcher.Get()){
     foreach(ManagementBaseObject o in results){
      using(o){
       string name=Convert.ToString(o["Name"]),pnp=Convert.ToString(o["PNPDeviceID"]),status=Convert.ToString(o["Status"]);
       bool match=(pnp!=null&&pnp.StartsWith(@"USB\VID_046D&PID_0ABA",StringComparison.OrdinalIgnoreCase))||(name!=null&&name.IndexOf("PRO X",StringComparison.OrdinalIgnoreCase)>=0);
       if(match){any=true;if(string.Equals(status,"OK",StringComparison.OrdinalIgnoreCase))ok=true;}
      }
     }
    }
    return any&&ok?AudioStatus.Found:AudioStatus.NotFound;
   }catch{return AudioStatus.Unknown;}
  }

  public static int CountMatchingAudioDevices(){
   try{
    int n=0;
    using(var searcher=new ManagementObjectSearcher("SELECT Name,PNPDeviceID FROM Win32_SoundDevice"))
    using(var results=searcher.Get()){
     foreach(ManagementBaseObject o in results){
      using(o){
       string name=Convert.ToString(o["Name"]),pnp=Convert.ToString(o["PNPDeviceID"]);
       if((pnp!=null&&pnp.StartsWith(@"USB\VID_046D&PID_0ABA",StringComparison.OrdinalIgnoreCase))||(name!=null&&name.IndexOf("PRO X",StringComparison.OrdinalIgnoreCase)>=0))n++;
      }
     }
    }
    return n;
   }catch{return -1;}
  }

  public static string BuildReport(string appVersion,bool turkish,string stateCode,IList<string> history,bool prepared,bool recovered,AudioStatus audio,bool pairingOk,bool audioOk,bool micOk,string log){
   var sb=new StringBuilder();
   sb.AppendLine("Receiver Rescue "+appVersion);
   sb.AppendLine("Windows: "+WindowsInfo());
   sb.AppendLine(DotNetInfo());
   sb.AppendLine("Interface language: "+(turkish?"Turkish":"English"));
   sb.AppendLine();
   sb.AppendLine("State: "+stateCode);
   sb.AppendLine("Files prepared: "+prepared);
   sb.AppendLine("SOURCE SHA256: "+VerifiedPatcher.SourceSha);
   sb.AppendLine("PROBE SHA256: "+VerifiedPatcher.ProbeSha);
   sb.AppendLine("RECOVERY SHA256: "+VerifiedPatcher.RecoverySha);
   sb.AppendLine("Recovery tool finished with normal receiver seen: "+recovered);
   sb.AppendLine("Windows audio device (automatic): "+audio);
   sb.AppendLine("Checklist (manual) - pairing: "+pairingOk+", audio: "+audioOk+", microphone: "+micOk);
   sb.AppendLine();
   sb.AppendLine("State history:");
   if(history!=null&&history.Count>0){foreach(string h in history)sb.AppendLine("  "+h);}else sb.AppendLine("  (none)");
   sb.AppendLine();
   sb.AppendLine("Log:");
   sb.Append(log);
   return MaskPersonal(sb.ToString());
  }
 }
}
