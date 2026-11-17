// Copyright (c) 2026 the Receiver Rescue project owner. All rights reserved. Proprietary; see LICENSE.
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace ReceiverRescue {
 // No service, installation, arbitrary command, destination, hash or updater arguments are accepted.
 public static class ProtectedLauncher {
  const string AdminSid="S-1-5-32-544",SystemSid="S-1-5-18";
  const string InstallerSid="S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464";
  [StructLayout(LayoutKind.Sequential)]struct FileInfoNative {
   public uint Attributes;public System.Runtime.InteropServices.ComTypes.FILETIME Creation,Access,Write;
   public uint Volume,SizeHigh,SizeLow,Links,IndexHigh,IndexLow;
  }
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern SafeFileHandle CreateFile(string path,uint access,uint share,IntPtr security,uint disposition,uint flags,IntPtr template);
  [DllImport("kernel32.dll",SetLastError=true)]static extern bool GetFileInformationByHandle(SafeFileHandle file,out FileInfoNative info);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern uint GetFinalPathNameByHandle(SafeFileHandle file,StringBuilder path,uint size,uint flags);
  [StructLayout(LayoutKind.Sequential)]struct SidAttributes{public IntPtr Sid;public uint Attributes;}
  [DllImport("kernel32.dll")]static extern IntPtr GetCurrentProcess();
  [DllImport("advapi32.dll",SetLastError=true)]static extern bool OpenProcessToken(IntPtr process,uint access,out SafeFileHandle token);
  [DllImport("advapi32.dll",SetLastError=true)]static extern bool CreateRestrictedToken(SafeFileHandle token,uint flags,uint count,ref SidAttributes disabled,uint privileges,IntPtr deleted,uint restrictedCount,IntPtr restricted,out SafeFileHandle result);
  [StructLayout(LayoutKind.Sequential)]struct UnicodeString{public ushort Length,MaximumLength;public IntPtr Buffer;}
  [StructLayout(LayoutKind.Sequential)]struct ObjectAttributes{public int Length;public IntPtr RootDirectory,ObjectName;public uint Attributes;public IntPtr SecurityDescriptor,SecurityQos;}
  [StructLayout(LayoutKind.Sequential)]struct IoStatus{public IntPtr Status,Information;}
  [DllImport("ntdll.dll")]static extern int NtCreateFile(out SafeFileHandle file,uint access,ref ObjectAttributes attributes,out IoStatus status,IntPtr allocation,uint fileAttributes,uint share,uint disposition,uint options,IntPtr ea,uint eaLength);
  [DllImport("ntdll.dll")]static extern uint RtlNtStatusToDosError(int status);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)][return:MarshalAs(UnmanagedType.U1)]static extern bool CreateSymbolicLink(string link,string target,uint flags);

  public static bool IsAdministrator(){using(var id=WindowsIdentity.GetCurrent())return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);}
  public static string ExpectedHash(string mode){
   if(mode=="probe")return VerifiedPatcher.ProbeSha;
   if(mode=="recovery")return VerifiedPatcher.RecoverySha;
   throw new InvalidDataException("Only probe and recovery modes are allowed.");
  }
  internal static string LocalFile(string path){
   if(string.IsNullOrEmpty(path)||path.Length>260||path.Length<3||!char.IsLetter(path[0])||path[1]!=':'||path[2]!='\\'||path.IndexOf(':',2)>=0||path.IndexOfAny(new[]{'"','\r','\n','\0'})>=0)
    throw new InvalidDataException("A local absolute file path is required.");
   string full=Path.GetFullPath(path);
   if(!string.Equals(full,path,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Noncanonical source path.");
   return full;
  }
  public static Process Request(string source,string mode){
   ExpectedHash(mode);source=LocalFile(Path.GetFullPath(source));
   string encoded=Convert.ToBase64String(Encoding.UTF8.GetBytes(source));
   return Process.Start(new ProcessStartInfo {
    FileName=Process.GetCurrentProcess().MainModule.FileName,
    Arguments="--protected-launch "+mode+" "+encoded,
    WorkingDirectory=Environment.SystemDirectory,UseShellExecute=true,Verb="runas"
   });
  }
  internal static DirectorySecurity NewSecurity(){
   var s=new DirectorySecurity();s.SetAccessRuleProtection(true,false);
   var admin=new SecurityIdentifier(AdminSid);s.SetOwner(admin);
   foreach(string sid in new[]{AdminSid,SystemSid})s.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(sid),FileSystemRights.FullControl,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));
   return s;
  }
  internal static void ValidateSecurity(DirectorySecurity security,bool staging){
   var descriptor=new RawSecurityDescriptor(security.GetSecurityDescriptorBinaryForm(),0);
   if(descriptor.DiscretionaryAcl==null)throw new InvalidDataException("A null directory DACL is not permitted.");
   string owner=security.GetOwner(typeof(SecurityIdentifier)).Value;
   if(owner!=AdminSid&&owner!=SystemSid&&(staging||owner!=InstallerSid))throw new InvalidDataException("Untrusted protected-directory owner.");
   if(staging&&!security.AreAccessRulesProtected)throw new InvalidDataException("Staging permissions must not be inherited.");
   const FileSystemRights writes=FileSystemRights.Write|FileSystemRights.Delete|FileSystemRights.DeleteSubdirectoriesAndFiles|FileSystemRights.ChangePermissions|FileSystemRights.TakeOwnership;
   bool admin=false,system=false;
   foreach(FileSystemAccessRule rule in security.GetAccessRules(true,true,typeof(SecurityIdentifier))){
    if((rule.PropagationFlags&PropagationFlags.InheritOnly)!=0)continue;
    string sid=rule.IdentityReference.Value;
    if(rule.AccessControlType!=AccessControlType.Allow)continue;
    if(sid==AdminSid&&(rule.FileSystemRights&FileSystemRights.FullControl)==FileSystemRights.FullControl)admin=true;
    if(sid==SystemSid&&(rule.FileSystemRights&FileSystemRights.FullControl)==FileSystemRights.FullControl)system=true;
    if((rule.FileSystemRights&writes)!=0&&sid!=AdminSid&&sid!=SystemSid&&!(sid==InstallerSid&&!staging))throw new InvalidDataException("An unprivileged account can modify the protected directory.");
   }
   if(staging&&(!admin||!system))throw new InvalidDataException("Missing administrator/SYSTEM permissions.");
  }
  internal static SafeFileHandle LockDirectory(string directory){
   // Open the directory itself, without following reparse points; deny renames/deletes while held.
   var h=CreateFile(directory,0x80000000,3,IntPtr.Zero,3,0x02200000,IntPtr.Zero);
   try{
    if(h.IsInvalid)throw new Win32Exception(Marshal.GetLastWin32Error());
    FileInfoNative info;if(!GetFileInformationByHandle(h,out info))throw new Win32Exception(Marshal.GetLastWin32Error());
    if((info.Attributes&0x400)!=0||(info.Attributes&0x10)==0)throw new InvalidDataException("Reparse points are not permitted in the staging path.");
    var name=new StringBuilder(1024);uint n=GetFinalPathNameByHandle(h,name,(uint)name.Capacity,0);
    if(n==0||n>=name.Capacity||!string.Equals(name.ToString(),@"\\?\"+Path.GetFullPath(directory).TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("The staging path resolves to another location.");
    return h;
   }catch{h.Dispose();throw;}
  }
  internal static byte[] ReadPinned(string source,string expected){
   using(var h=OpenSourceWithoutReparse(LocalFile(source)))using(var f=new FileStream(h,FileAccess.Read)){
    if(f.Length!=6259024)throw new InvalidDataException("Unsupported updater size.");
    var data=new byte[6259024];int offset=0,n;
    while(offset<data.Length&&(n=f.Read(data,offset,data.Length-offset))>0)offset+=n;
    if(offset!=data.Length||VerifiedPatcher.Hash(data)!=expected)throw new InvalidDataException("Updater hash verification failed.");
    return data;
   }
  }
  static SafeFileHandle OpenSourceWithoutReparse(string source){
   if(new DriveInfo(Path.GetPathRoot(source)).DriveType!=DriveType.Fixed)throw new InvalidDataException("Source must be on a fixed local drive.");
   // OBJ_DONT_REPARSE rejects junctions/symlinks anywhere in the path before following them.
   string ntPath=@"\??\"+source;IntPtr buffer=Marshal.StringToHGlobalUni(ntPath),name=IntPtr.Zero;
   try{
    var unicode=new UnicodeString{Length=checked((ushort)(ntPath.Length*2)),MaximumLength=checked((ushort)((ntPath.Length+1)*2)),Buffer=buffer};
    name=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(UnicodeString)));Marshal.StructureToPtr(unicode,name,false);
    var attributes=new ObjectAttributes{Length=Marshal.SizeOf(typeof(ObjectAttributes)),ObjectName=name,Attributes=0x1040};IoStatus io;SafeFileHandle handle;
    int status=NtCreateFile(out handle,0x80100000,ref attributes,out io,IntPtr.Zero,0,1,1,0x60,IntPtr.Zero,0);
    if(status<0){if(handle!=null)handle.Dispose();throw new Win32Exception((int)RtlNtStatusToDosError(status));}
    return handle;
   }finally{if(name!=IntPtr.Zero)Marshal.FreeHGlobal(name);Marshal.FreeHGlobal(buffer);}
  }
  static string ProtectedRoot {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"ReceiverRescue.SecureRuns");}}
  static void CreateProtected(string path){new DirectoryInfo(path).Create(NewSecurity());}
  // Called only by the elevated command mode. The parent UI waits for this process to finish.
  public static int Run(string mode,string encodedSource){
   string expected=ExpectedHash(mode);
   if(!IsAdministrator())throw new UnauthorizedAccessException("Administrator permission is required; no tool was started.");
   if(encodedSource==null||encodedSource.Length>2048)throw new InvalidDataException("Invalid source argument.");
   string source=LocalFile(new UTF8Encoding(false,true).GetString(Convert.FromBase64String(encodedSource)));
   byte[] data=ReadPinned(source,expected);
   InProtectedDirectory(run=>{
    string target=Path.Combine(run,mode=="probe"?"Receiver-Probe.exe":"Receiver-Recovery.exe");
    using(var output=new FileStream(target,FileMode.CreateNew,FileAccess.Write,FileShare.None))output.Write(data,0,data.Length);
    // All security checks and a fresh device guard run after the UAC prompt.
    if(!DeviceDiscovery.Scan().CanLaunch)throw new InvalidDataException("Device state changed or is not eligible; no tool was started.");
    if(Directory.GetFileSystemEntries(run).Length!=1)throw new InvalidDataException("Unexpected staging contents.");
    using(var guard=new FileStream(target,FileMode.Open,FileAccess.Read,FileShare.Read)){
     if(guard.Length!=6259024||VerifiedPatcher.Hash(ReadStream(guard))!=expected)throw new InvalidDataException("Protected copy changed.");
     var info=new ProcessStartInfo{FileName=target,WorkingDirectory=run,Arguments="/LOG /VERBOSE",UseShellExecute=false};
     info.EnvironmentVariables["PATH"]=Environment.SystemDirectory;
     info.EnvironmentVariables["TEMP"]=run;info.EnvironmentVariables["TMP"]=run;
     using(var child=Process.Start(info)){if(child==null)throw new IOException("Updater start failed.");child.WaitForExit();}
    }
   });
   return 0; // Completion is not proof of successful firmware recovery.
  }
  internal static void InProtectedDirectory(Action<string> action){
   if(!IsAdministrator())throw new UnauthorizedAccessException("Administrator permission is required.");
   string parent=Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),root=ProtectedRoot;
   using(var parentGuard=LockDirectory(parent)){
    ValidateSecurity(Directory.GetAccessControl(parent),false);
    CreateProtected(root);
    using(var rootGuard=LockDirectory(root)){
     ValidateSecurity(Directory.GetAccessControl(root),true);
     string run=Path.Combine(root,"run-"+Guid.NewGuid().ToString("N"));CreateProtected(run);
     try{
      using(var runGuard=LockDirectory(run)){
       ValidateSecurity(Directory.GetAccessControl(run),true);
       action(run);
      }
     }finally{
      // Remove only this freshly created, access-controlled run directory, never arbitrary caller paths.
      try{Directory.Delete(run,true);}catch{}
     }
    }
   }
  }
  static byte[] ReadStream(Stream stream){var data=new byte[6259024];int o=0,n;while(o<data.Length&&(n=stream.Read(data,o,data.Length-o))>0)o+=n;if(o!=data.Length)throw new InvalidDataException("Incomplete protected copy.");return data;}

  // No device access or updater launch. Actual administrator/filesystem checks are deliberately separate.
  public static void PolicySelfTest(){
   ValidateSecurity(NewSecurity(),true);
   var badOwner=NewSecurity();badOwner.SetOwner(new SecurityIdentifier("S-1-5-32-545"));bool ownerRefused=false;try{ValidateSecurity(badOwner,true);}catch(InvalidDataException){ownerRefused=true;}if(!ownerRefused)throw new Exception("Untrusted owner accepted.");
   foreach(string mode in new[]{"","PROBE","--scan","other"}){bool refused=false;try{ExpectedHash(mode);}catch(InvalidDataException){refused=true;}if(!refused)throw new Exception("Unknown broker mode accepted.");}
   foreach(string path in new[]{@"\\server\file.exe",@"\\?\C:\file.exe",@"C:\file.exe:stream",@"relative.exe",@"C:\a\..\file.exe"}){bool refused=false;try{LocalFile(path);}catch(InvalidDataException){refused=true;}if(!refused)throw new Exception("Unsafe broker source accepted.");}
   foreach(string sid in new[]{"S-1-1-0","S-1-5-32-545","S-1-5-11"}){
    var insecure=NewSecurity();insecure.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(sid),FileSystemRights.Write,AccessControlType.Allow));bool refused=false;
    try{ValidateSecurity(insecure,true);}catch(InvalidDataException){refused=true;}if(!refused)throw new Exception("Writable staging policy accepted.");
   }
  }
  // This integration test writes only benign test data. It never scans USB or starts firmware tools.
  public static void AdministratorSelfTest(){
   PolicySelfTest();
   InProtectedDirectory(run=>{
    string sentinel=Path.Combine(run,"sentinel.txt");File.WriteAllText(sentinel,"benign security test");
    string payload=Path.Combine(run,"dummy.bin"),link=Path.Combine(run,"redirect");var dummy=new byte[6259024];File.WriteAllBytes(payload,dummy);string dummyHash=VerifiedPatcher.Hash(dummy);
    if(ReadPinned(payload,dummyHash).Length!=dummy.Length)throw new Exception("Valid protected source was rejected.");
    if(!CreateSymbolicLink(link,run,1))throw new Win32Exception(Marshal.GetLastWin32Error());
    try{
     bool readRefused=false,stageRefused=false;
     try{ReadPinned(Path.Combine(link,"dummy.bin"),dummyHash);}catch(Win32Exception){readRefused=true;}
     try{using(var h=LockDirectory(link)){}}catch(InvalidDataException){stageRefused=true;}
     if(!readRefused||!stageRefused)throw new Exception("A reparse point was accepted.");
    }finally{Directory.Delete(link);File.Delete(payload);}
    bool renamed=false;try{Directory.Move(run,run+"-moved");renamed=true;}catch(IOException){}
    if(renamed)throw new Exception("Directory guard allowed a rename.");
    SafeFileHandle token;if(!OpenProcessToken(GetCurrentProcess(),14,out token))throw new Win32Exception(Marshal.GetLastWin32Error());
    using(token){
     byte[] sidBytes=new byte[new SecurityIdentifier(AdminSid).BinaryLength];new SecurityIdentifier(AdminSid).GetBinaryForm(sidBytes,0);
     IntPtr sid=Marshal.AllocHGlobal(sidBytes.Length);
     try{Marshal.Copy(sidBytes,0,sid,sidBytes.Length);var disabled=new SidAttributes{Sid=sid};SafeFileHandle restricted;
      if(!CreateRestrictedToken(token,1,1,ref disabled,0,IntPtr.Zero,0,IntPtr.Zero,out restricted))throw new Win32Exception(Marshal.GetLastWin32Error());
      using(restricted)using(WindowsIdentity.Impersonate(restricted.DangerousGetHandle())){
       if(IsAdministrator())throw new Exception("Restricted test token still has administrator rights.");
       var attackAcl=NewSecurity();attackAcl.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier("S-1-1-0"),FileSystemRights.FullControl,AccessControlType.Allow));
       foreach(Action attack in new Action[]{()=>File.WriteAllText(Path.Combine(run,"hid.dll"),"benign"),()=>File.WriteAllText(sentinel,"changed"),()=>File.Delete(sentinel),()=>Directory.SetAccessControl(run,attackAcl)}){
        bool denied=false;try{attack();}catch(UnauthorizedAccessException){denied=true;}
        if(!denied)throw new Exception("Restricted token modified the protected staging directory.");
       }
      }
     }finally{Marshal.FreeHGlobal(sid);}
    }
    if(File.ReadAllText(sentinel)!="benign security test"||Directory.GetFiles(run).Length!=1)throw new Exception("Protected test contents changed.");
   });
  }
 }
}
