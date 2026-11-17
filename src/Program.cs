// Copyright (c) 2026 the Receiver Rescue project owner. All rights reserved. Proprietary; see LICENSE.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

// All native imports in this assembly are Windows system libraries.
[assembly: System.Runtime.InteropServices.DefaultDllImportSearchPaths(System.Runtime.InteropServices.DllImportSearchPath.System32)]
[assembly: AssemblyTitle("Receiver Rescue")]
[assembly: AssemblyDescription("Community launcher and reproducible patcher for a specific PRO X Wireless receiver recovery")]
[assembly: AssemblyVersion("0.5.0.0")]
[assembly: AssemblyFileVersion("0.5.0.0")]
[assembly: AssemblyInformationalVersion("0.5.0")]

namespace ReceiverRescue {
 static class Program {
  [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool SetProcessDPIAware();
  static string Manifests {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"manifests");}}
  static void Assert(bool value,string message){if(!value)throw new Exception(message);}
  static DeviceObservation Device(string id,ushort vendor,ushort product,bool hid,bool readable,ushort page,ushort usage){return new DeviceObservation{PhysicalId=id,Vendor=vendor,Product=product,Hid=hid,CapabilitiesRead=readable,UsagePage=page,Usage=usage};}
  static DeviceObservation Boot(string id){return Device(id,0x170d,0x0100,true,true,0xff00,1);}
  static void SelfTest(){
   ProtectedLauncher.PolicySelfTest();
   Assert(DeviceDiscovery.Classify(new[]{Boot("one")}).CanLaunch,"Matching bootloader should be eligible");
   Assert(!DeviceDiscovery.Classify(new DeviceObservation[0]).CanLaunch,"Missing receiver must be rejected");
   var normal=Device("normal",0x046d,0x0aba,false,false,0,0);var headset=Device("headset",0x046d,0x0abb,false,false,0,0);
   Assert(DeviceDiscovery.Classify(new[]{normal}).Code=="normal-receiver","USB-only normal receiver must be detected");
   Assert(DeviceDiscovery.Classify(new[]{Boot("one"),headset}).Code=="disconnect-headset","USB-only headset must block launch");
   Assert(DeviceDiscovery.Classify(new[]{Boot("one"),Boot("two")}).Code=="multiple-receivers","Separate bootloaders must block launch");
   Assert(!DeviceDiscovery.Classify(new[]{Boot("one"),normal}).CanLaunch,"Normal receiver alongside bootloader must block launch");
   var multi=DeviceDiscovery.Classify(new[]{Boot("one"),Boot("ONE"),Device("one",0x170d,0x0100,false,false,0,0)});
   Assert(multi.CanLaunch&&multi.BootloaderDevices==1,"Collections on one physical receiver must not count as separate dongles");
   Assert(DeviceDiscovery.Classify(new[]{Device("one",0x170d,0x0100,false,false,0,0)}).Code=="check-interface","USB bootloader without HID must be reported but not launched");
   Assert(!DeviceDiscovery.Classify(new[]{Device("one",0x170d,0x0100,true,true,0xff43,1)}).CanLaunch,"Wrong HID usage page must be rejected");
   Assert(!DeviceDiscovery.Classify(new[]{Device("one",0x170d,0x0100,true,true,0xff00,2)}).CanLaunch,"Wrong HID usage must be rejected");
   Assert(!DeviceDiscovery.Classify(new[]{Device("one",0x170d,0x0100,true,false,0xff00,1)}).CanLaunch,"Unreadable descriptor must be rejected");
   Assert(DeviceDiscovery.Classify(new[]{Boot("one"),Device("headset-boot",0x170d,0x0101,false,false,0,0)}).Code=="disconnect-headset","Headset bootloader must block launch");
   Assert(DeviceDiscovery.Classify(new[]{Boot("one"),Device("other",0x170d,0x0102,false,false,0,0)}).Code=="unsupported","Unknown Avnera target must block launch");
   foreach(string mode in new[]{"probe","recovery"}){var manifest=VerifiedPatcher.LoadManifest(Manifests,mode);PatchReport report;bool refused=false;try{VerifiedPatcher.Apply(new byte[1024],manifest,mode,out report);}catch(InvalidDataException){refused=true;}Assert(refused,"Unsupported updater was not rejected");Assert(manifest.sourceSHA256==VerifiedPatcher.SourceSha,"Manifest source hash");Assert(manifest.changes.Count==(mode=="probe"?3:9),"Patch count");}
   Assert(Support.FormatElapsed(TimeSpan.Zero)=="00:00"&&Support.FormatElapsed(TimeSpan.FromSeconds(59))=="00:59"&&Support.FormatElapsed(TimeSpan.FromSeconds(61))=="01:01"&&Support.FormatElapsed(TimeSpan.FromSeconds(3661))=="1:01:01"&&Support.FormatElapsed(TimeSpan.FromSeconds(-5))=="00:00","Elapsed time formatting");
   string profile=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),masked=Support.MaskPersonal("a "+profile+"\\x b C:\\Users\\SomeoneElse\\y c "+Environment.UserName+" d "+Environment.MachineName);
   Assert(!masked.Contains(profile)&&!masked.Contains("SomeoneElse")&&masked.Contains("%USERPROFILE%")&&masked.Contains("<user>"),"Personal data masking");
   string rest=masked.Replace("%USERPROFILE%","").Replace("<user>","").Replace("<machine>","");
   if(Environment.UserName.Length>2)Assert(rest.IndexOf(Environment.UserName,StringComparison.OrdinalIgnoreCase)<0,"User name must be masked");
   string reportText=Support.BuildReport("9.9.9",true,"ready",new[]{"00:00:00  ready"},true,true,AudioStatus.Found,true,false,true,"log "+profile+"\\x");
   Assert(reportText.Contains("Receiver Rescue 9.9.9")&&reportText.Contains("State: ready")&&reportText.Contains("00:00:00  ready")&&reportText.Contains("Windows audio device (automatic): Found")&&!reportText.Contains(profile),"Report content and masking");
   string tmpDir=Path.Combine(Path.GetTempPath(),"rr-selftest-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(tmpDir);
   try{
    string exe=Path.Combine(tmpDir,"cmd.exe");File.Copy(Path.Combine(Environment.SystemDirectory,"cmd.exe"),exe);string good=VerifiedPatcher.Hash(File.ReadAllBytes(exe));
    bool wrote=false,renamed=false,deleted=false;
    var child=VerifiedPatcher.LaunchLocked(exe,good,tmpDir,"/c exit 7",false,()=>{try{File.WriteAllBytes(exe,new byte[]{0});wrote=true;}catch(Exception){}try{File.Move(exe,exe+".moved");renamed=true;}catch(Exception){}try{File.Delete(exe);deleted=true;}catch(Exception){}});
    Assert(!wrote&&!renamed&&!deleted&&File.Exists(exe)&&VerifiedPatcher.Hash(File.ReadAllBytes(exe))==good,"The prepared file must be locked against write, rename and delete during launch");
    child.WaitForExit();Assert(child.ExitCode==7,"Locked launch must start the verified file");child.Dispose();
    bool refused=false;try{VerifiedPatcher.LaunchLocked(exe,new string('0',64),tmpDir,"/c exit 0",false,null);}catch(InvalidDataException){refused=true;}
    Assert(refused,"A file that does not match the expected hash must not be launched");
    File.WriteAllBytes(exe,new byte[]{1,2,3});refused=false;try{VerifiedPatcher.LaunchLocked(exe,good,tmpDir,"/c exit 0",false,null);}catch(InvalidDataException){refused=true;}
    Assert(refused,"A file changed after preparation must not be launched");
    string exe2=Path.Combine(tmpDir,"tool.exe");File.Copy(Path.Combine(Environment.SystemDirectory,"cmd.exe"),exe2);string good2=VerifiedPatcher.Hash(File.ReadAllBytes(exe2));
    string runRoot=Path.Combine(tmpDir,"runs"),runDir;
    var iso=VerifiedPatcher.LaunchIsolated(exe2,good2,runRoot,"/c exit 9",false,null,out runDir);
    Assert(runDir.StartsWith(runRoot,StringComparison.OrdinalIgnoreCase)&&Directory.GetFileSystemEntries(runDir).Length==1,"The tool must start from a private folder that contains only the tool");
    iso.WaitForExit();Assert(iso.ExitCode==9,"Isolated launch must start the verified file");iso.Dispose();
    refused=false;try{VerifiedPatcher.LaunchIsolated(exe2,good2,runRoot,"/c exit 0",false,d=>File.WriteAllBytes(Path.Combine(d,"hid.dll"),new byte[]{0}),out runDir);}catch(InvalidDataException){refused=true;}
    Assert(refused,"A DLL planted beside the tool must block the launch");
    Assert(VerifiedPatcher.HasForeignLibrary(runDir),"A planted DLL must be detected");
    refused=false;try{VerifiedPatcher.LaunchIsolated(exe2,new string('0',64),runRoot,"/c exit 0",false,null,out runDir);}catch(InvalidDataException){refused=true;}
    Assert(refused,"Isolated launch must reject a file with the wrong hash");
    VerifiedPatcher.CleanupRuns(runRoot);Assert(Directory.GetDirectories(runRoot,"run-*").Length==0,"Private launch folders must be removed");
   }finally{try{Directory.Delete(tmpDir,true);}catch{}}
   Console.WriteLine("PASS: physical-device grouping, USB-only detection, HID descriptor guards, ambiguous/normal/headset/unknown target rejection, unsupported-file rejection, pinned manifests, locked verified launch, private launch folder with planted-DLL refusal, elapsed-time formatting, report content and personal-data masking.");
  }
  [STAThread]static int Main(string[] args){
   try{Console.OutputEncoding=new UTF8Encoding(false);}catch{}
   try{
    if(args.Length==1&&args[0]=="--protected-self-test"){
     try{ProtectedLauncher.AdministratorSelfTest();MessageBox.Show("PASS: protected directory ACLs, directory rename lock, source/staging reparse rejection, and restricted-token DLL planting / overwrite / delete / ACL-change rejection. No USB device or firmware tool was accessed.","Receiver Rescue - administrator security test");return 0;}
     catch(Exception e){MessageBox.Show(Support.MaskPersonal(e.ToString()),"Receiver Rescue - administrator security test FAILED",MessageBoxButtons.OK,MessageBoxIcon.Error);return 10;}
    }
    if(args.Length>0&&args[0]=="--protected-launch"){
     try{if(args.Length!=3)throw new InvalidDataException("Invalid protected launcher arguments.");return ProtectedLauncher.Run(args[1],args[2]);}
     catch(Exception e){MessageBox.Show(Support.MaskPersonal(e.Message),"Receiver Rescue - protected launch refused",MessageBoxButtons.OK,MessageBoxIcon.Error);return 10;}
    }
    if(args.Length>0&&args[0]=="--self-test"){SelfTest();return 0;}
    if(args.Length==1&&args[0]=="--scan"){var s=DeviceDiscovery.Scan();Console.WriteLine("State: "+s.Code+"; normal receivers: "+s.NormalDevices+"; bootloader devices: "+s.BootloaderDevices+"; matching bootloader HID interfaces: "+s.BootloaderInterfaces+"; readable HID descriptors: "+s.ReadableHidInterfaces+"; headset USB devices: "+s.HeadsetDevices+"; unknown Avnera devices: "+s.UnsupportedDevices+"; PRO X Windows audio devices: "+Support.CountMatchingAudioDevices());return 0;}
    if(args.Length==3&&args[0]=="--verify-updater"){
     SelfTest();string input=Path.GetFullPath(args[1]),output=Path.GetFullPath(args[2]);var files=VerifiedPatcher.Prepare(input,output,Manifests);
     Assert(VerifiedPatcher.MatchesFile(files.Probe,VerifiedPatcher.ProbeSha),"Probe digest");Assert(VerifiedPatcher.MatchesFile(files.Recovery,VerifiedPatcher.RecoverySha),"Recovery digest");
     var mutated=File.ReadAllBytes(input);mutated[100]^=1;PatchReport report;bool refused=false;try{VerifiedPatcher.Apply(mutated,VerifiedPatcher.LoadManifest(Manifests,"recovery"),"recovery",out report);}catch(InvalidDataException){refused=true;}Assert(refused,"Mutated original accepted");
     var badManifest=VerifiedPatcher.LoadManifest(Manifests,"recovery");badManifest.changes[0].before="00";refused=false;try{VerifiedPatcher.Apply(File.ReadAllBytes(input),badManifest,"recovery",out report);}catch(InvalidDataException){refused=true;}Assert(refused,"Changed preimage accepted");
     Console.WriteLine("PASS: output hashes exactly match the previously successful recovery and detection builds; resources and transfer code preserved; tampered input/manifest rejected. No device was accessed.");return 0;
    }
    if(args.Length==3&&args[0]=="--patch"){VerifiedPatcher.Prepare(Path.GetFullPath(args[1]),Path.GetFullPath(args[2]),Manifests);Console.WriteLine("Prepared locally. No firmware tool was launched.");return 0;}
    try{SetProcessDPIAware();}catch{}
    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
    if(args.Length==1&&args[0]=="--ui-test"){
     int bad=0,runs=0;
     foreach(bool turkish in new[]{true,false})foreach(int width in new[]{760,980,1300}){
      using(var f=new RecoveryForm(Manifests,true,turkish)){f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-30000,-30000);f.ShowInTaskbar=false;f.Show();f.Width=Math.Max(f.MinimumSize.Width,width);f.Height=900;Application.DoEvents();Application.DoEvents();
       foreach(string problem in f.CheckLayout()){Console.WriteLine(problem);bad++;}runs++;f.Close();}
     }
     Console.WriteLine(bad==0?"PASS: layout self-check ("+runs+" windows, TR/EN, 3 widths, all device states): no clipped text, overflow or overlapping cards.":"FAIL: "+bad+" layout problem(s).");return bad==0?0:1;
    }
    if(args.Length==2&&(args[0]=="--preview"||args[0]=="--preview-en")){using(var f=new RecoveryForm(Manifests,true,args[0]=="--preview")){f.Show();f.Height=1520;Application.DoEvents();Application.DoEvents();using(var image=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(image,new Rectangle(Point.Empty,f.Size));image.Save(Path.GetFullPath(args[1]));}f.Close();}return 0;}
    Application.Run(new RecoveryForm(Manifests,false));return 0;
   }catch(Exception e){if(args.Length>0)Console.Error.WriteLine(e.Message);else MessageBox.Show(e.Message,"Receiver Rescue",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
  }
 }
}
