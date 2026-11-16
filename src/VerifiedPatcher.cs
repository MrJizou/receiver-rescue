// Copyright (c) 2026 the Receiver Rescue project owner. All rights reserved. Proprietary; see LICENSE.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

namespace ReceiverRescue {
 public sealed class PatchChange { public string va,label,before,after; }
 public sealed class PatchSpec { public string purpose,sourceSHA256,outputSHA256; public bool resourcesIdentical; public int removedRelocations; public List<PatchChange> changes; }
 public sealed class PatchReport {public string Mode,SourceSha256,OutputSha256,ResourceSha256;public bool ResourcesUnchanged;public bool? TransferCodeUnchanged;public int RemovedRelocations;public List<PatchChange> Changes;}
 public sealed class PreparedFiles { public string Original,Probe,Recovery,ReportDirectory; }
 public static class VerifiedPatcher {
  public const string SourceSha="e0e0a4e31e7d67c090e293ef600e1ab2df4f993914ff15b7a1a2e21d4dccd36d";
  public const string RecoverySha="7522c65eb9bd30f38e3300cff015c5b8dd43b98356cfb102ac55477668337e85";
  public const string ProbeSha="f9d38820c504f0f053fbda0cb6d85125b265ed771b0e380bbd4317cacaeee7dd";
  public const string VendorUrl="https://download01.logi.com/web/ftp/pub/techsupport/gaming/ProXHeadsetUpdate_v212.exe";
  public static string Hash(byte[] data){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").ToLowerInvariant();}
  public static bool MatchesFile(string path,string sha){return File.Exists(path)&&new FileInfo(path).Length==6259024&&Hash(File.ReadAllBytes(path))==sha;}
  // Re-verifies the file and starts it while this process holds a handle that denies writes, renames and deletes.
  // This closes the gap between the integrity check and process creation: the file cannot be swapped in between.
  public static System.Diagnostics.Process LaunchLocked(string file,string expectedSha,string workingDirectory,string arguments,bool elevate,Action duringLock){
   using(var guard=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read)){
    string actual;using(var sha=SHA256.Create())actual=BitConverter.ToString(sha.ComputeHash(guard)).Replace("-","").ToLowerInvariant();
    if(actual!=expectedSha)throw new InvalidDataException("Prepared file changed. No tool was launched.");
    if(duringLock!=null)duringLock();
    var info=new System.Diagnostics.ProcessStartInfo{FileName=file,WorkingDirectory=workingDirectory,Arguments=arguments??"",UseShellExecute=elevate};
    if(elevate)info.Verb="runas";
    return System.Diagnostics.Process.Start(info);
   }
  }
  // Starts the verified tool from a fresh, private folder that contains nothing but the tool itself.
  // An elevated program loads several system DLLs from its own folder first, so a long-lived, user-writable
  // folder would let another program running in the same account plant a DLL there in advance.
  // The launch is refused if anything else is found beside the tool.
  public static System.Diagnostics.Process LaunchIsolated(string file,string expectedSha,string runRoot,string arguments,bool elevate,Action<string> beforeStart,out string runDirectory){
   byte[] data=File.ReadAllBytes(file);
   if(Hash(data)!=expectedSha)throw new InvalidDataException("Prepared file changed. No tool was launched.");
   Directory.CreateDirectory(runRoot);
   string dir=Path.Combine(runRoot,"run-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);runDirectory=dir;
   string target=Path.Combine(dir,Path.GetFileName(file));
   using(var w=new FileStream(target,FileMode.CreateNew,FileAccess.Write,FileShare.None))w.Write(data,0,data.Length);
   return LaunchLocked(target,expectedSha,dir,arguments,elevate,()=>{
    if(beforeStart!=null)beforeStart(dir);
    string[] entries=Directory.GetFileSystemEntries(dir);
    if(entries.Length!=1||!string.Equals(Path.GetFullPath(entries[0]),Path.GetFullPath(target),StringComparison.OrdinalIgnoreCase))
     throw new InvalidDataException("Unexpected files appeared beside the prepared tool. No tool was launched.");
   });
  }
  public static bool HasForeignLibrary(string runDirectory){
   try{return Directory.Exists(runDirectory)&&Directory.GetFiles(runDirectory,"*.dll").Length>0;}catch{return false;}
  }
  // Best effort: removes leftover private launch folders.
  public static void CleanupRuns(string runRoot){
   try{if(!Directory.Exists(runRoot))return;foreach(string d in Directory.GetDirectories(runRoot,"run-*")){try{Directory.Delete(d,true);}catch{}}}catch{}
  }
  static byte[] Hex(string s){if(s==null||s.Length%2!=0)throw new InvalidDataException("Invalid patch bytes");var bytes=new byte[s.Length/2];for(int i=0;i<bytes.Length;i++)bytes[i]=Convert.ToByte(s.Substring(i*2,2),16);return bytes;}
  sealed class Section { public string Name;public int Rva,Size,Raw; }
  sealed class Pe {
   public byte[] Data;public int Optional,Base;public List<Section> Sections=new List<Section>();
   public Pe(byte[] data){
    Data=data;if(data.Length<256||data[0]!='M'||data[1]!='Z')throw new InvalidDataException("Not a PE executable");int pe=I32(60);Bounds(pe,24);
    if(I32(pe)!=0x4550||U16(pe+4)!=0x14c)throw new InvalidDataException("Expected x86 PE");Optional=pe+24;Bounds(Optional,U16(pe+20));if(U16(Optional)!=0x10b)throw new InvalidDataException("Expected PE32");Base=I32(Optional+28);
    int table=Optional+U16(pe+20);for(int i=0;i<U16(pe+6);i++){int o=table+i*40;Bounds(o,40);var s=new Section{Name=System.Text.Encoding.ASCII.GetString(data,o,8).TrimEnd('\0'),Rva=I32(o+12),Size=I32(o+16),Raw=I32(o+20)};Bounds(s.Raw,s.Size);Sections.Add(s);}
   }
   public void Bounds(int o,int n){if(o<0||n<0||o>Data.Length-n)throw new InvalidDataException("PE range outside file");}
   public int I32(int o){Bounds(o,4);return BitConverter.ToInt32(Data,o);}public ushort U16(int o){Bounds(o,2);return BitConverter.ToUInt16(Data,o);}
   public int Offset(int va){long r=(long)va-Base;var s=Sections.FirstOrDefault(x=>r>=x.Rva&&r<(long)x.Rva+x.Size);if(s==null)throw new InvalidDataException("Unmapped patch address");return checked(s.Raw+(int)(r-s.Rva));}
   public byte[] Slice(int va,int n){int o=Offset(va);Bounds(o,n);var a=new byte[n];Buffer.BlockCopy(Data,o,a,0,n);return a;}
  }
  public static PatchSpec LoadManifest(string directory,string mode){if(mode!="probe"&&mode!="recovery")throw new ArgumentException("Unknown mode");return new JavaScriptSerializer().Deserialize<PatchSpec>(File.ReadAllText(Path.Combine(directory,mode+"-112.2.212.json")));}
  public static byte[] Apply(byte[] original,PatchSpec spec,string mode,out PatchReport report){
   // Fixed digests are the trust root. A changed manifest cannot authorize another binary.
   if(original.Length!=6259024||Hash(original)!=SourceSha)throw new InvalidDataException("Unsupported Logitech updater SHA-256. No patch or launch was performed.");
   string expected=mode=="probe"?ProbeSha:mode=="recovery"?RecoverySha:null;
   if(expected==null||spec==null||spec.sourceSHA256!=SourceSha||spec.outputSHA256!=expected)throw new InvalidDataException("Unrecognized patch manifest");
   if(spec.changes==null||spec.changes.Count!=(mode=="probe"?3:9))throw new InvalidDataException("Unexpected patch count");
   var output=(byte[])original.Clone();var sourcePe=new Pe(original);var pe=new Pe(output);
   var ranges=new List<Tuple<int,int>>();
   foreach(var change in spec.changes){int va=Convert.ToInt32(change.va.Substring(2),16),o=pe.Offset(va);byte[] before=Hex(change.before),after=Hex(change.after);if(before.Length!=after.Length)throw new InvalidDataException("Patch changes file size");pe.Bounds(o,before.Length);
    if(!original.Skip(o).Take(before.Length).SequenceEqual(before))throw new InvalidDataException("Original instructions differ at "+change.va);
    if(ranges.Any(r=>o<r.Item2&&o+after.Length>r.Item1))throw new InvalidDataException("Overlapping patch ranges");ranges.Add(Tuple.Create(o,o+after.Length));Buffer.BlockCopy(after,0,output,o,after.Length);
   }
   int relocationDirectory=pe.Optional+96+5*8,relocationRva=pe.I32(relocationDirectory),relocationSize=pe.I32(relocationDirectory+4),removed=0;
   int cursor=pe.Offset(checked(pe.Base+relocationRva)),end=checked(cursor+relocationSize);pe.Bounds(cursor,relocationSize);
   while(cursor<end){int page=pe.I32(cursor),length=pe.I32(cursor+4);if(length<8||length%2!=0||cursor>end-length)throw new InvalidDataException("Malformed relocation block");
    for(int e=cursor+8;e<cursor+length;e+=2){int entry=pe.U16(e),va=checked(pe.Base+page+(entry&4095));if((entry>>12)!=0&&va>=0x411890&&va<0x4119a1){output[e]=output[e+1]=0;removed++;}}
    cursor+=length;
   }
   if(removed!=3||removed!=spec.removedRelocations)throw new InvalidDataException("Unexpected relocation fixups");
   // Modified code must not claim Logitech's original Authenticode signature.
   Array.Clear(output,pe.Optional+96+4*8,8);Array.Clear(output,pe.Optional+64,4);
   var resource=pe.Sections.Single(s=>s.Name==".rsrc");var sourceResource=sourcePe.Sections.Single(s=>s.Name==".rsrc");
   bool sameResource=output.Skip(resource.Raw).Take(resource.Size).SequenceEqual(original.Skip(sourceResource.Raw).Take(sourceResource.Size));
   if(!sameResource)throw new InvalidDataException("Embedded firmware resources changed");
   bool sameTransfer=true;if(mode=="recovery")foreach(var range in new[]{Tuple.Create(0x405490,0x150),Tuple.Create(0x40e7a0,0x140),Tuple.Create(0x40fd60,0x30),Tuple.Create(0x40fd90,0x1420)})sameTransfer&=pe.Slice(range.Item1,range.Item2).SequenceEqual(sourcePe.Slice(range.Item1,range.Item2));
   if(!sameTransfer)throw new InvalidDataException("Original receiver transfer code changed");
   if(Hash(output)!=expected)throw new InvalidDataException("Result differs from the reviewed receiver-recovery build");
   report=new PatchReport{Mode=mode,SourceSha256=SourceSha,OutputSha256=expected,ResourceSha256=Hash(original.Skip(sourceResource.Raw).Take(sourceResource.Size).ToArray()),ResourcesUnchanged=sameResource,TransferCodeUnchanged=mode=="recovery"?(bool?)sameTransfer:null,RemovedRelocations=removed,Changes=spec.changes};return output;
  }
  public static PreparedFiles Prepare(string originalPath,string outputDirectory,string manifestDirectory){
   if(new FileInfo(originalPath).Length!=6259024)throw new InvalidDataException("Unsupported updater size; expected the pinned 112.2.212 file.");
   var original=File.ReadAllBytes(originalPath);PatchReport probeReport,recoveryReport;
   byte[] probe=Apply(original,LoadManifest(manifestDirectory,"probe"),"probe",out probeReport),recovery=Apply(original,LoadManifest(manifestDirectory,"recovery"),"recovery",out recoveryReport);
   Directory.CreateDirectory(outputDirectory);var paths=new PreparedFiles{Original=originalPath,Probe=Path.Combine(outputDirectory,"Receiver-Probe.exe"),Recovery=Path.Combine(outputDirectory,"Receiver-Recovery.exe"),ReportDirectory=outputDirectory};
   File.WriteAllBytes(paths.Probe,probe);File.WriteAllBytes(paths.Recovery,recovery);var serializer=new JavaScriptSerializer();File.WriteAllText(Path.Combine(outputDirectory,"probe-report.json"),serializer.Serialize(probeReport));File.WriteAllText(Path.Combine(outputDirectory,"recovery-report.json"),serializer.Serialize(recoveryReport));return paths;
  }
 }
}
