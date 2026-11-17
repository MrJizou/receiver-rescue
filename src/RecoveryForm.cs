// Copyright (c) 2026 the Receiver Rescue project owner. All rights reserved. Proprietary; see LICENSE.
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReceiverRescue {
 // ---------- Design tokens (dark, navy + teal; text contrast >= 4.5:1 on every surface) ----------
 static class UI {
  public static float K=1f;
  public static int P(int v){return (int)Math.Round(v*K);}
  public static readonly Color Bg=Color.FromArgb(11,18,32),Surface=Color.FromArgb(18,27,46),SurfaceHi=Color.FromArgb(30,44,72),Border=Color.FromArgb(52,70,102),
   Text=Color.FromArgb(232,238,246),Muted=Color.FromArgb(169,184,204),Dim=Color.FromArgb(112,128,153),
   Accent=Color.FromArgb(45,212,191),OnAccent=Color.FromArgb(4,33,29),Caution=Color.FromArgb(251,191,36),OnCaution=Color.FromArgb(31,21,0),CautionTint=Color.FromArgb(40,34,20),
   Success=Color.FromArgb(52,211,153),Danger=Color.FromArgb(248,113,113),Info=Color.FromArgb(96,165,250);
  public static readonly Font Body=new Font("Segoe UI",10f),Small=new Font("Segoe UI",9f),Semi=new Font("Segoe UI Semibold",10f),Section=new Font("Segoe UI Semibold",11.5f),Title=new Font("Segoe UI Semibold",18f),Mono=new Font("Consolas",9.5f);
  public static GraphicsPath Round(Rectangle r,int rad){var p=new GraphicsPath();int d=Math.Max(1,rad*2);p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
  public static void Check(Graphics g,Color c,float cx,float cy,float s){using(var pen=new Pen(c,Math.Max(1.6f,s*0.16f)){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round})g.DrawLines(pen,new[]{new PointF(cx-s*0.32f,cy+s*0.02f),new PointF(cx-s*0.08f,cy+s*0.27f),new PointF(cx+s*0.34f,cy-s*0.22f)});}
 }
 public enum BtnKind{Primary,Secondary,Caution,Ghost}
 public enum IconKind{Neutral,Success,Warning,Danger,Info}
 public enum StepState{Active,Done,Locked}

 public sealed class RescueButton:Button {
  public BtnKind Style=BtnKind.Secondary;bool hover,down;
  public RescueButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);FlatStyle=FlatStyle.Flat;Cursor=Cursors.Hand;Font=UI.Semi;}
  protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){hover=false;down=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e){down=true;Invalidate();base.OnMouseDown(e);}
  protected override void OnMouseUp(MouseEventArgs e){down=false;Invalidate();base.OnMouseUp(e);}
  protected override void OnEnabledChanged(EventArgs e){Cursor=Enabled?Cursors.Hand:Cursors.Default;Invalidate();base.OnEnabledChanged(e);}
  protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}
  protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
  static Color Shift(Color c,int d){return Color.FromArgb(Math.Max(0,Math.Min(255,c.R+d)),Math.Max(0,Math.Min(255,c.G+d)),Math.Max(0,Math.Min(255,c.B+d)));}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Parent!=null?Parent.BackColor:UI.Bg);
   Color fill,border,text;
   if(!Enabled){fill=Color.FromArgb(22,32,53);border=Color.FromArgb(36,50,76);text=UI.Dim;}
   else switch(Style){
    case BtnKind.Primary:fill=UI.Accent;border=fill;text=UI.OnAccent;break;
    case BtnKind.Caution:fill=UI.Caution;border=fill;text=UI.OnCaution;break;
    case BtnKind.Ghost:fill=Parent!=null?Parent.BackColor:UI.Bg;border=UI.Border;text=hover?UI.Text:UI.Muted;break;
    default:fill=UI.SurfaceHi;border=UI.Border;text=UI.Text;break;
   }
   if(Enabled&&hover&&Style!=BtnKind.Ghost)fill=Shift(fill,Style==BtnKind.Secondary?10:18);
   if(Enabled&&down)fill=Shift(fill,-14);
   var r=new Rectangle(0,0,Width-1,Height-1);int rad=UI.P(7);
   using(var path=UI.Round(r,rad))using(var b=new SolidBrush(fill))using(var pen=new Pen(border)){g.FillPath(b,path);g.DrawPath(pen,path);}
   TextRenderer.DrawText(g,Text,Font,new Rectangle(UI.P(10),0,Width-UI.P(20),Height),text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
   if(Focused&&ShowFocusCues){var f=new Rectangle(UI.P(2),UI.P(2),Width-UI.P(5),Height-UI.P(5));using(var path=UI.Round(f,Math.Max(2,rad-2)))using(var pen=new Pen(UI.Text,Math.Max(2f,UI.P(2))))g.DrawPath(pen,path);}
  }
 }
 public sealed class RescueCheck:CheckBox {
  bool hover;
  public RescueCheck(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);AutoSize=false;Cursor=Cursors.Hand;Font=UI.Body;}
  protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnEnabledChanged(EventArgs e){Invalidate();base.OnEnabledChanged(e);}
  protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}
  protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
  protected override void OnCheckedChanged(EventArgs e){Invalidate();base.OnCheckedChanged(e);}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(BackColor);int bs=UI.P(20),by=UI.P(4);var box=new Rectangle(UI.P(1),by,bs,bs);
   using(var path=UI.Round(box,UI.P(4))){
    if(Checked&&Enabled){using(var b=new SolidBrush(UI.Accent))g.FillPath(b,path);UI.Check(g,UI.OnAccent,box.X+bs/2f,box.Y+bs/2f,bs);}
    else{using(var b=new SolidBrush(Enabled?UI.Bg:Color.FromArgb(22,32,53)))g.FillPath(b,path);if(Checked)UI.Check(g,UI.Dim,box.X+bs/2f,box.Y+bs/2f,bs);}
    using(var pen=new Pen(Enabled?(hover?UI.Accent:UI.Muted):UI.Dim,Math.Max(1.5f,UI.P(1)*1.5f)))g.DrawPath(pen,path);
   }
   var tr=new Rectangle(UI.P(30),0,Width-UI.P(30),Height);
   TextRenderer.DrawText(g,Text,Font,tr,Enabled?UI.Text:UI.Dim,TextFormatFlags.Left|TextFormatFlags.Top|TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix|TextFormatFlags.NoPadding);
   if(Focused&&ShowFocusCues){using(var path=UI.Round(new Rectangle(0,by-UI.P(2),bs+UI.P(4),bs+UI.P(4)),UI.P(5)))using(var pen=new Pen(UI.Text,2f))g.DrawPath(pen,path);}
  }
 }
 public sealed class CardPanel:Panel {
  public Color Fill=UI.Surface,Stroke=UI.Border,Bar=Color.Empty;
  public CardPanel(){DoubleBuffered=true;ResizeRedraw=true;BackColor=UI.Surface;}
  protected override void OnPaintBackground(PaintEventArgs e){}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Parent!=null?Parent.BackColor:UI.Bg);BackColor=Fill;
   var r=new Rectangle(0,0,Width-1,Height-1);using(var path=UI.Round(r,UI.P(10)))using(var b=new SolidBrush(Fill))using(var pen=new Pen(Stroke)){g.FillPath(b,path);g.DrawPath(pen,path);
    if(Bar!=Color.Empty){var old=g.Clip;g.SetClip(path);using(var bb=new SolidBrush(Bar))g.FillRectangle(bb,0,0,UI.P(5),Height);g.Clip=old;}}
  }
 }
 public sealed class StatusIcon:Control {
  IconKind kind=IconKind.Neutral;public Color Back=UI.Surface;
  public IconKind Kind{get{return kind;}set{if(kind!=value){kind=value;Invalidate();}}}
  public StatusIcon(){DoubleBuffered=true;SetStyle(ControlStyles.Selectable,false);}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Back);int s=Math.Min(Width,Height)-2;var rc=new Rectangle((Width-s)/2,(Height-s)/2,s,s);float cx=rc.X+s/2f,cy=rc.Y+s/2f;
   switch(kind){
    case IconKind.Success:using(var b=new SolidBrush(UI.Success))g.FillEllipse(b,rc);UI.Check(g,UI.OnAccent,cx,cy,s);break;
    case IconKind.Info:using(var b=new SolidBrush(UI.Info))g.FillEllipse(b,rc);using(var b2=new SolidBrush(UI.Bg)){g.FillEllipse(b2,cx-s*0.06f,cy-s*0.27f,s*0.12f,s*0.12f);g.FillRectangle(b2,cx-s*0.055f,cy-s*0.08f,s*0.11f,s*0.34f);}break;
    case IconKind.Danger:using(var b=new SolidBrush(UI.Danger))g.FillEllipse(b,rc);using(var pen=new Pen(UI.Bg,Math.Max(2f,s*0.12f)){StartCap=LineCap.Round,EndCap=LineCap.Round}){g.DrawLine(pen,cx-s*0.2f,cy-s*0.2f,cx+s*0.2f,cy+s*0.2f);g.DrawLine(pen,cx+s*0.2f,cy-s*0.2f,cx-s*0.2f,cy+s*0.2f);}break;
    case IconKind.Warning:using(var b=new SolidBrush(UI.Caution))g.FillPolygon(b,new[]{new PointF(cx,rc.Y),new PointF(rc.Right,rc.Bottom-s*0.06f),new PointF(rc.X,rc.Bottom-s*0.06f)});using(var b2=new SolidBrush(UI.OnCaution)){g.FillRectangle(b2,cx-s*0.05f,cy-s*0.12f,s*0.10f,s*0.30f);g.FillEllipse(b2,cx-s*0.065f,cy+s*0.23f,s*0.13f,s*0.13f);}break;
    default:using(var pen=new Pen(UI.Dim,Math.Max(2f,s*0.09f)))g.DrawEllipse(pen,rc.X+1,rc.Y+1,rc.Width-2,rc.Height-2);using(var b=new SolidBrush(UI.Dim))g.FillEllipse(b,cx-s*0.08f,cy-s*0.08f,s*0.16f,s*0.16f);break;
   }
  }
 }
 public sealed class StepBadge:Control {
  public StepState State=StepState.Active;public int Number=1;public Color Back=UI.Surface;
  public StepBadge(){DoubleBuffered=true;SetStyle(ControlStyles.Selectable,false);}
  public void Set(StepState s){if(State!=s){State=s;Invalidate();}}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Back);int s=Math.Min(Width,Height)-2;var rc=new Rectangle((Width-s)/2,(Height-s)/2,s,s);
   if(State==StepState.Done){using(var b=new SolidBrush(UI.Success))g.FillEllipse(b,rc);UI.Check(g,UI.OnAccent,rc.X+s/2f,rc.Y+s/2f,s);return;}
   Color fill=State==StepState.Active?UI.Accent:Color.Empty;Color fore=State==StepState.Active?UI.OnAccent:UI.Dim;
   if(fill!=Color.Empty)using(var b=new SolidBrush(fill))g.FillEllipse(b,rc);else using(var pen=new Pen(UI.Border,2f))g.DrawEllipse(pen,rc.X+1,rc.Y+1,rc.Width-2,rc.Height-2);
   TextRenderer.DrawText(g,Number.ToString(),UI.Semi,rc,fore,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
  }
 }
 public sealed class ThinProgress:Control {
  int v;public int Minimum=0,Maximum=100;public Color Back=UI.Surface;
  public int Value{get{return v;}set{int n=Math.Max(Minimum,Math.Min(Maximum,value));if(n!=v){v=n;Invalidate();}}}
  public ThinProgress(){DoubleBuffered=true;SetStyle(ControlStyles.Selectable,false);}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Back);var tr=new Rectangle(0,0,Width-1,Height-1);
   using(var path=UI.Round(tr,Height/2))using(var b=new SolidBrush(UI.SurfaceHi))g.FillPath(b,path);
   int w=Maximum>Minimum?(int)((long)(Width-1)*(v-Minimum)/(Maximum-Minimum)):0;if(w>=Height/2){using(var path=UI.Round(new Rectangle(0,0,w,Height-1),Height/2))using(var b=new SolidBrush(UI.Accent))g.FillPath(b,path);}
  }
 }
 public sealed class ReceiverArtwork:Control {
  public Color Glow=UI.Accent;
  public ReceiverArtwork(){DoubleBuffered=true;BackColor=UI.Surface;SetStyle(ControlStyles.Selectable,false);}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.Clear(BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;float s=Math.Min(Width/62f,Height/132f);g.TranslateTransform((Width-60*s)/2f,(Height-130*s)/2f);g.ScaleTransform(s,s);
   using(var metal=new SolidBrush(Color.FromArgb(120,142,165)))using(var body=new SolidBrush(Color.FromArgb(34,50,76)))using(var accent=new SolidBrush(Glow))using(var stroke=new Pen(Color.FromArgb(78,102,134),1.5f)){
    g.FillRectangle(metal,10,0,40,37);g.FillRectangle(body,0,29,60,100);g.DrawRectangle(stroke,0,29,60,100);g.FillRectangle(accent,13,48,34,4);g.FillRectangle(accent,26,103,8,8);g.DrawLine(stroke,23,8,23,22);g.DrawLine(stroke,37,8,37,22);
   }
  }
 }
 public sealed class ContentPanel:Panel { public ContentPanel(){DoubleBuffered=true;} }

 public sealed class RecoveryForm:Form {
  readonly string manifestDir,cache;bool turkish,busy,nativeRunning,scanning,preview,techOpen,inLayout,recovered;CardPanel c4;Label t4,d4;RescueCheck k1,k2,k3;RescueButton copyReport,recheck;Label auto4;System.Windows.Forms.Timer elapsedTimer=new System.Windows.Forms.Timer();Stopwatch runWatch=new Stopwatch();string runText="",lastCode="";readonly List<string> history=new List<string>();AudioStatus audioStatus=AudioStatus.Unknown;string lastIdentity="";PreparedFiles prepared;ReceiverState state=new ReceiverState();
  Panel scroller;ContentPanel content;CardPanel statusCard,c1,c2,c3,warnCard,techCard;StatusIcon statusIcon,warnIcon;ReceiverArtwork art;StepBadge b1,b2,b3;
  Label title,subtitle,status,detail,step,hashText,footer,t1,d1,t2,d2,t3,d3,hint2,hint3,warnText;ThinProgress progress;TextBox log;RescueButton prepare,choose,probe,recover,language,techToggle;RescueCheck model,probePassed;
  System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
  string T(string tr,string en){return turkish?tr:en;}
  string ErrorText(Exception e){string message=e is InvalidDataException?T("Dosya doğrulanamadı. Orijinal Logitech 112.2.212 dosyasını ve programın manifests klasörünü kontrol edin.","File verification failed. Check the original Logitech 112.2.212 file and the program's manifests folder."):e is HttpRequestException||e is TaskCanceledException?T("İndirme tamamlanamadı. İnternet bağlantınızı kontrol edin veya mevcut orijinal dosyayı seçin.","Download could not complete. Check your connection or select an existing original file."):e is UnauthorizedAccessException?T("Dosyaya erişim reddedildi. Seçilen dosyanın ve uygulama klasörünün izinlerini kontrol edin.","File access was denied. Check permissions for the selected file and application folder."):T("İşlem tamamlanamadı. Aşağıdaki teknik ayrıntıya bakın.","The operation could not complete. Check the technical detail below.");return message+Environment.NewLine+Environment.NewLine+T("Teknik ayrıntı: ","Technical detail: ")+e.Message;}
  int P(int v){return UI.P(v);}
  public RecoveryForm(string manifests,bool previewMode):this(manifests,previewMode,System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName=="tr"){}
  public RecoveryForm(string manifests,bool previewMode,bool useTurkish){
   manifestDir=manifests;preview=previewMode;turkish=useTurkish;cache=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ReceiverRescue","112.2.212");if(!previewMode)VerifiedPatcher.CleanupRuns(Path.Combine(cache,"runs"));
   using(var g=CreateGraphics())UI.K=g.DpiX/96f;
   Text="Receiver Rescue 0.4 · PRO X Wireless";AutoScaleMode=AutoScaleMode.None;BackColor=UI.Bg;ForeColor=UI.Text;Font=UI.Body;StartPosition=FormStartPosition.CenterScreen;
   var wa=Screen.PrimaryScreen.WorkingArea;Size=new Size(Math.Min(P(980),wa.Width-P(40)),Math.Min(P(800),wa.Height-P(40)));MinimumSize=new Size(Math.Min(P(760),wa.Width),Math.Min(P(520),wa.Height));
   Build();
   timer.Interval=2500;timer.Tick+=async(s,e)=>await Scan();elapsedTimer.Interval=1000;elapsedTimer.Tick+=(s,e)=>{if(nativeRunning){step.Text=runText+"  ·  "+T("Geçen süre: ","Elapsed: ")+Support.FormatElapsed(runWatch.Elapsed);DoLayout();}};
   Shown+=async(s,e)=>{DoLayout();if(preview){recovered=true;audioStatus=AudioStatus.Found;ApplyAudioText();state=new ReceiverState{BootloaderDevices=1,BootloaderInterfaces=1};step.Text=T("ÖRNEK ÖNİZLEME · CİHAZ İŞLEMİ YAPILMAZ","DEMO PREVIEW · NO DEVICE OPERATION");UpdateState();return;}timer.Start();await Scan();};
   FormClosed+=(s,e)=>{timer.Stop();elapsedTimer.Stop();};
   FormClosing+=(s,e)=>{if(nativeRunning){e.Cancel=true;MessageBox.Show(this,T("Önce açılan Logitech aracındaki işlemin bitmesini bekleyin ve o pencereyi kapatın. Bu başlatıcı, firmware yazımını kesintiye uğratmaz.","Wait for the Logitech tool to finish, then close its window. This launcher does not interrupt firmware transfer."),"Receiver Rescue");}};
  }
  Label L(string text,Font f,Color fore,Color back){return new Label{Text=text,Font=f,ForeColor=fore,BackColor=back,AutoSize=false,UseMnemonic=false,TextAlign=ContentAlignment.TopLeft};}
  RescueButton B(string text,BtnKind k){return new RescueButton{Text=text,Style=k};}
  RescueCheck C(string text,Color back){return new RescueCheck{Text=text,BackColor=back};}
  static int Fit(Control c,int x,int y,int w,int minH){int tw=Math.Max(20,w-(c is CheckBox?UI.P(30):0));var sz=TextRenderer.MeasureText(c.Text,c.Font,new Size(tw,0),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix|TextFormatFlags.NoPadding);int h=Math.Max(minH,sz.Height+UI.P(6));c.SetBounds(x,y,w,h);return h;}
  void Build(){
   scroller=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=UI.Bg};content=new ContentPanel{BackColor=UI.Bg,Location=Point.Empty};scroller.Controls.Add(content);Controls.Add(scroller);scroller.Resize+=(s,e)=>DoLayout();
   language=B(turkish?"English":"Türkçe",BtnKind.Ghost);language.AccessibleName=T("Dili değiştir","Change language");content.Controls.Add(language);
   language.Click+=(s,e)=>{if(busy||nativeRunning)return;bool m=model.Checked,p=probePassed.Checked,q1=k1.Checked,q2=k2.Checked,q3=k3.Checked;string oldLog=log.Text;turkish=!turkish;var old=Controls[0];Controls.Remove(old);old.Dispose();Build();model.Checked=m;probePassed.Checked=p;k1.Checked=q1;k2.Checked=q2;k3.Checked=q3;log.Text=oldLog;if(prepared!=null){progress.Value=100;step.Text=T("Dosyalar doğrulandı ve hazır.","Files verified and ready.");}UpdateState();DoLayout();};
   title=L("Receiver Rescue",UI.Title,UI.Text,UI.Bg);subtitle=L(T("İlk nesil PRO X Wireless · Standart / Shroud alıcı kurtarma","First-generation PRO X Wireless · Standard / Shroud receiver recovery"),UI.Body,UI.Muted,UI.Bg);content.Controls.Add(title);content.Controls.Add(subtitle);
   // status
   statusCard=new CardPanel();content.Controls.Add(statusCard);art=new ReceiverArtwork();statusIcon=new StatusIcon();status=L("—",UI.Section,UI.Muted,UI.Surface);detail=L("",UI.Body,UI.Muted,UI.Surface);
   statusCard.Controls.Add(art);statusCard.Controls.Add(statusIcon);statusCard.Controls.Add(status);statusCard.Controls.Add(detail);
   // step 1
   c1=new CardPanel();content.Controls.Add(c1);b1=new StepBadge{Number=1};t1=L(T("Kaynak dosyayı doğrulayın","Verify the source file"),UI.Section,UI.Text,UI.Surface);d1=L(T("Logitech 112.2.212 · Sabit SHA-256 · Yerel yama. Bu adımda hiçbir cihaza veri yazılmaz.","Logitech 112.2.212 · Pinned SHA-256 · Local patching. Nothing is written to any device in this step."),UI.Body,UI.Muted,UI.Surface);
   prepare=B(T("Resmî dosyayı indir ve hazırla","Download and prepare the official file"),BtnKind.Primary);prepare.Click+=async(s,e)=>await PrepareFrom(null);
   choose=B(T("Mevcut orijinal EXE dosyasını seç","Select an existing original EXE"),BtnKind.Secondary);choose.Click+=async(s,e)=>{using(var d=new OpenFileDialog{Title=T("Orijinal Logitech güncelleyicisini seçin","Select the original Logitech updater"),Filter=T("Logitech güncelleyicisi (*.exe)|*.exe","Logitech updater (*.exe)|*.exe")})if(d.ShowDialog(this)==DialogResult.OK)await PrepareFrom(d.FileName);};
   step=L(T("Dosyalar henüz hazırlanmadı. Yazma işlemi başlatılmadı.","Files are not prepared yet. No firmware operation has started."),UI.Small,UI.Muted,UI.Surface);progress=new ThinProgress{Minimum=0,Maximum=100};
   foreach(Control c in new Control[]{b1,t1,d1,prepare,choose,step,progress})c1.Controls.Add(c);
   // step 2
   c2=new CardPanel();content.Controls.Add(c2);b2=new StepBadge{Number=2};t2=L(T("Alıcı algılamasını kontrol edin","Check receiver detection"),UI.Section,UI.Text,UI.Surface);d2=L(T("Cihaza veri yazılmaz; yalnızca alıcının güncelleme aracı tarafından doğru algılanıp algılanmadığı kontrol edilir.","No firmware is written; this step only verifies that the receiver is properly recognized by the Logitech updater."),UI.Body,UI.Muted,UI.Surface);hint2=L("",UI.Small,UI.Caution,UI.Surface);
   model=C(T("Alıcımın ilk nesil PRO X Wireless (Standart / Shroud) olduğunu onaylıyorum.","I confirm that my receiver is a first-generation PRO X Wireless (Standard / Shroud)."),UI.Surface);model.CheckedChanged+=(s,e)=>UpdateButtons();
   probe=B(T("Algılama testini başlat (veri yazılmaz)","Run detection test (writes no firmware)"),BtnKind.Secondary);probe.Click+=async(s,e)=>await Launch(false);
   probePassed=C(T("Test aracı alıcıyı algıladı ve bilgisayara yalnızca arızalı alıcı bağlı.","The test detected the receiver, and only the failed receiver is connected."),UI.Surface);probePassed.CheckedChanged+=(s,e)=>UpdateButtons();
   foreach(Control c in new Control[]{b2,t2,d2,hint2,model,probe,probePassed})c2.Controls.Add(c);
   // step 3
   c3=new CardPanel();content.Controls.Add(c3);b3=new StepBadge{Number=3};t3=L(T("Alıcıyı kurtarın","Recover the receiver"),UI.Section,UI.Text,UI.Surface);
   d3=L(T("Firmware verileri değiştirilmez. Kulaklık / MCU yazımı atlanır. Yeni eşleştirme kimliği (pairing ID) yazılmaz. Yamalanmış EXE dosyası dijital imzalı değildir. Kurtarma aracı alıcıya her zaman 2.12 sürümünü yükler; kulaklığınız farklı bir sürümdeyse işlem sonrası eşleştirmeyi ve ses aktarımını ayrıca kontrol edin.","Firmware payloads remain unchanged. Headset / MCU updates are skipped. No new pairing ID is written. The patched EXE is not digitally signed. Recovery always flashes receiver firmware 2.12; if your headset runs a different version, test pairing and audio separately afterward."),UI.Body,UI.Muted,UI.Surface);
   warnCard=new CardPanel{Fill=UI.CautionTint,Stroke=Color.FromArgb(92,74,28),Bar=UI.Caution};warnIcon=new StatusIcon{Kind=IconKind.Warning,Back=UI.CautionTint};
   warnText=L(T("DENEYSEL · Bu adım alıcıya doğrudan firmware yazar. Kurtarma işlemi yalnızca tek bir alıcıda doğrulanmıştır; diğer cihazlarda kesin başarı garanti edilmez. İlerleme durumu, açılan Logitech penceresinde gösterilir. Aktarım sırasında alıcıyı USB portundan çıkarmayın.","EXPERIMENTAL · This step writes actual firmware to the receiver. Recovery has been verified on a single receiver only and is not guaranteed on other devices. Transfer progress is shown in the Logitech updater window. Do not unplug the receiver during the transfer."),UI.Body,UI.Text,UI.CautionTint);
   warnCard.Controls.Add(warnIcon);warnCard.Controls.Add(warnText);
   recover=B(T("Firmware kurtarma aracını başlat","Launch firmware recovery tool"),BtnKind.Caution);recover.Click+=async(s,e)=>await Launch(true);hint3=L("",UI.Small,UI.Caution,UI.Surface);
   foreach(Control c in new Control[]{b3,t3,d3,warnCard,recover,hint3})c3.Controls.Add(c);
   // step 4: after-recovery check (shown only once a normal receiver is seen after recovery)
   c4=new CardPanel();content.Controls.Add(c4);t4=L(T("Kurtarma sonrası kontrol","After-recovery check"),UI.Section,UI.Text,UI.Surface);d4=L(T("Alıcı normal USB modunda görüldü. Kulaklığı eşleştirin ve her maddeyi ayrı ayrı deneyin. Logitech aracının çıkış kodu, başarı kanıtı sayılmaz.","The receiver is back in normal USB mode. Pair the headset and verify each item. The Logitech tool's exit code is not proof of success."),UI.Body,UI.Muted,UI.Surface);
   k1=C(T("Kulaklık alıcıyla eşleşti","Headset paired with the receiver"),UI.Surface);k2=C(T("Ses çalışıyor","Audio works"),UI.Surface);k3=C(T("Mikrofon çalışıyor","Microphone works"),UI.Surface);
   auto4=L("",UI.Body,UI.Text,UI.Surface);recheck=B(T("Yeniden kontrol et","Check again"),BtnKind.Ghost);recheck.AccessibleName=T("Ses aygıtını yeniden kontrol et","Check the audio device again");recheck.Click+=async(s,e)=>await RunAudioCheck(false);ApplyAudioText();foreach(Control c in new Control[]{t4,d4,auto4,recheck,k1,k2,k3})c4.Controls.Add(c);
   // technical
   techToggle=B("",BtnKind.Ghost);techToggle.Click+=(s,e)=>SetTech(!techOpen);content.Controls.Add(techToggle);
   copyReport=B(T("Raporu kopyala","Copy report"),BtnKind.Ghost);copyReport.AccessibleName=T("Raporu panoya kopyala","Copy report to clipboard");content.Controls.Add(copyReport);
   copyReport.Click+=async(s,e)=>{try{Clipboard.SetText(Report());copyReport.Text=T("Kopyalandı","Copied");}catch(Exception ex){copyReport.Text=T("Kopyalanamadı","Copy failed");Note(ex.Message);}await Task.Delay(2000);if(!copyReport.IsDisposed)copyReport.Text=T("Raporu kopyala","Copy report");};
   techCard=new CardPanel();hashText=L("SOURCE   e0e0a4e3…d36d\nPROBE    f9d38820…e7dd\nRECOVER  7522c65e…7e85",UI.Mono,UI.Text,UI.Surface);
   log=new TextBox{ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,BackColor=UI.Bg,ForeColor=UI.Muted,BorderStyle=BorderStyle.None,Font=UI.Mono};techCard.Controls.Add(hashText);techCard.Controls.Add(log);content.Controls.Add(techCard);
   footer=L(T("Bağımsız topluluk projesidir · Logitech ile bağlantılı değildir · Firmware yedeği almaz","Independent community project · Not affiliated with Logitech · Does not back up firmware"),UI.Small,UI.Muted,UI.Bg);content.Controls.Add(footer);
   foreach(Control c in new Control[]{art,statusIcon})c.BackColor=UI.Surface;
   UpdateTechText();UpdateState();
  }
  string Report(){return Support.BuildReport(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3),turkish,state.Code,history,prepared!=null,recovered,audioStatus,k1.Checked,k2.Checked,k3.Checked,log.Text);}
  void UpdateTechText(){techToggle.Text=(techOpen?"▾  ":"▸  ")+T("Teknik kayıt ve doğrulama zinciri","Technical log and verification chain");techToggle.AccessibleName=techToggle.Text;}
  void SetTech(bool open){techOpen=open;UpdateTechText();DoLayout();}
  void DoLayout(){
   if(content==null||scroller==null||inLayout||IsDisposed)return;inLayout=true;
   try{for(int pass=0;pass<3;pass++){
    int W=Math.Max(P(560),scroller.ClientSize.Width);int m=P(24),pad=P(18),gap=P(14);int cw=Math.Min(W-2*m,P(880));int x0=Math.Max(m,(W-cw)/2);int y=P(20);
    language.SetBounds(x0+cw-P(110),y+P(2),P(110),P(40));title.SetBounds(x0,y,Math.Max(P(100),cw-P(124)),P(34));subtitle.SetBounds(x0,y+P(36),Math.Max(P(100),cw-P(124)),P(24));y+=P(74);
    // status card
    int ax=pad,aw=P(46),ix=ax+aw+P(16),tx=ix+P(34)+P(12),tw=Math.Max(P(80),cw-tx-pad);status.SetBounds(tx,pad,tw,P(28));int dh=Fit(detail,tx,pad+P(32),tw,P(20));int sh=Math.Max(P(124),pad+P(32)+dh+pad);
    statusCard.SetBounds(x0,y,cw,sh);art.SetBounds(ax,P(10),aw,sh-P(20));statusIcon.SetBounds(ix,pad,P(34),P(34));y+=sh+gap;
    // step cards share geometry
    int cx=pad+P(34)+P(14),cwi=Math.Max(P(120),cw-cx-pad);int yy;
    b1.SetBounds(pad,pad,P(34),P(34));t1.SetBounds(cx,pad+P(2),cwi,P(28));yy=pad+P(32);yy+=Fit(d1,cx,yy,cwi,P(20))+P(12);
    bool side=cwi>=P(600);int bw=side?(cwi-P(10))/2:cwi;prepare.SetBounds(cx,yy,bw,P(44));if(side){choose.SetBounds(cx+bw+P(10),yy,bw,P(44));yy+=P(44);}else{choose.SetBounds(cx,yy+P(52),bw,P(44));yy+=P(96);}
    yy+=P(14);yy+=Fit(step,cx,yy,cwi,P(20))+P(4);progress.SetBounds(cx,yy,cwi,P(8));yy+=P(8);c1.SetBounds(x0,y,cw,yy+pad);y+=yy+pad+gap;
    b2.SetBounds(pad,pad,P(34),P(34));t2.SetBounds(cx,pad+P(2),cwi,P(28));yy=pad+P(32);yy+=Fit(d2,cx,yy,cwi,P(20))+P(6);
    hint2.Visible=hint2.Text.Length>0;if(hint2.Visible)yy+=Fit(hint2,cx,yy,cwi,P(20))+P(6);
    yy+=P(6);yy+=Fit(model,cx,yy,cwi,P(30))+P(10);probe.SetBounds(cx,yy,Math.Min(cwi,P(440)),P(44));yy+=P(44)+P(10);yy+=Fit(probePassed,cx,yy,cwi,P(30));c2.SetBounds(x0,y,cw,yy+pad);y+=yy+pad+gap;
    b3.SetBounds(pad,pad,P(34),P(34));t3.SetBounds(cx,pad+P(2),cwi,P(28));yy=pad+P(32);yy+=Fit(d3,cx,yy,cwi,P(20))+P(12);
    int wx=P(12)+P(26)+P(12);int th=Fit(warnText,wx,P(12),Math.Max(P(60),cwi-wx-P(12)),P(24));warnIcon.SetBounds(P(16),P(12),P(26),P(26));int wh=th+P(24);warnCard.SetBounds(cx,yy,cwi,wh);yy+=wh+P(14);
    recover.SetBounds(cx,yy,Math.Min(cwi,P(440)),P(48));yy+=P(48);hint3.Visible=hint3.Text.Length>0;if(hint3.Visible)yy+=P(8)+Fit(hint3,cx,yy+P(8),cwi,P(20));c3.SetBounds(x0,y,cw,yy+pad);y+=yy+pad+gap;
    c4.Visible=recovered;
    if(recovered){int iw=cw-2*pad;yy=pad;t4.SetBounds(pad,yy,iw,P(28));yy+=P(32);yy+=Fit(d4,pad,yy,iw,P(20))+P(8);yy+=Fit(auto4,pad,yy,iw,P(20))+P(6);recheck.SetBounds(pad,yy,Math.Min(iw,P(240)),P(40));yy+=P(40)+P(10);yy+=Fit(k1,pad,yy,iw,P(30))+P(4);yy+=Fit(k2,pad,yy,iw,P(30))+P(4);yy+=Fit(k3,pad,yy,iw,P(30));c4.SetBounds(x0,y,cw,yy+pad);y+=yy+pad+gap;}
    // technical + footer
    techToggle.SetBounds(x0,y,Math.Min(cw-P(180),P(380)),P(40));copyReport.SetBounds(x0+cw-P(170),y,P(170),P(40));y+=P(40)+P(8);techCard.Visible=techOpen;
    if(techOpen){int th2=P(230);techCard.SetBounds(x0,y,cw,th2);hashText.SetBounds(pad,pad,cw-2*pad,P(60));log.SetBounds(pad,pad+P(66),cw-2*pad,th2-pad*2-P(66));y+=th2+gap;}
    y+=Fit(footer,x0,y,cw,P(20))+P(22);content.Size=new Size(W,y);
    if(W==Math.Max(P(560),scroller.ClientSize.Width))break;
   }}finally{inLayout=false;}
  }
  void ApplyAudioText(){
   string t=T("Otomatik denetim · Windows ses aygıtı: ","Automatic check · Windows audio device: ");Color c=UI.Muted;
   if(audioStatus==AudioStatus.Found){t+=T("bulundu ve çalışıyor.","found and working.");c=UI.Success;}
   else if(audioStatus==AudioStatus.NotFound){t+=T("bulunamadı veya çalışmıyor. Sürücü yükleniyor olabilir; biraz bekleyip yeniden kontrol edin.","not found or not working. The driver may still be loading; wait a moment and check again.");c=UI.Caution;}
   else t+=T("henüz kontrol edilmedi veya kontrol edilemedi.","not checked yet or could not be checked.");
   auto4.Text=t;auto4.ForeColor=c;
  }
  async Task RunAudioCheck(bool poll){
   if(preview||IsDisposed)return;recheck.Enabled=false;auto4.Text=T("Otomatik denetim · Windows ses aygıtı kontrol ediliyor…","Automatic check · checking the Windows audio device…");auto4.ForeColor=UI.Muted;DoLayout();
   AudioStatus st=AudioStatus.Unknown;int tries=poll?6:1;
   for(int i=0;i<tries&&!IsDisposed;i++){st=await Task.Run(()=>Support.CheckAudioDevice());if(st==AudioStatus.Found)break;if(i<tries-1)await Task.Delay(2000);}
   if(IsDisposed)return;audioStatus=st;ApplyAudioText();recheck.Enabled=true;Note("Windows audio device (automatic): "+st);DoLayout();
  }
  // Layout self-check used by --ui-test: walks synthetic device states and reports clipped text, overflow and overlapping cards.
  public List<string> CheckLayout(){
   var problems=new List<string>();string lang=turkish?"TR":"EN";
   var cases=new List<ReceiverState>{new ReceiverState{BootloaderDevices=1,BootloaderInterfaces=1},new ReceiverState{NormalDevices=1},new ReceiverState{HeadsetDevices=1},new ReceiverState{BootloaderDevices=2,BootloaderInterfaces=2},new ReceiverState{BootloaderDevices=1},new ReceiverState{BootloaderDevices=1,DescriptorUnavailable=true},new ReceiverState{UnsupportedDevices=1},new ReceiverState()};
   foreach(bool tech in new[]{false,true})foreach(AudioStatus a in new[]{AudioStatus.Found,AudioStatus.NotFound,AudioStatus.Unknown}){
    techOpen=tech;UpdateTechText();audioStatus=a;ApplyAudioText();
    foreach(var c in cases){state=c;UpdateState();Application.DoEvents();CheckAll(problems,lang+" w="+ClientSize.Width+" "+state.Code+" tech="+tech+" audio="+a);}
   }
   return problems.Distinct().ToList();
  }
  void CheckAll(List<string> problems,string ctx){
   if(content.Width>scroller.ClientSize.Width+1)problems.Add(ctx+": horizontal overflow");
   Control prev=null;
   foreach(Control card in new Control[]{statusCard,c1,c2,c3,c4,techCard}){if(!card.Visible)continue;if(prev!=null&&card.Top<prev.Bottom)problems.Add(ctx+": cards overlap ("+card.Name+card.GetType().Name+")");prev=card;}
   CheckChildren(content,problems,ctx);
  }
  void CheckChildren(Control parent,List<string> problems,string ctx){
   foreach(Control c in parent.Controls){
    if(!c.Visible)continue;
    if(c.Left<0||c.Top<0||c.Right>parent.ClientSize.Width||c.Bottom>parent.ClientSize.Height+(parent==content?int.MaxValue/2:0))problems.Add(ctx+": out of bounds: "+c.GetType().Name+" '"+Brief(c)+"'");
    var label=c as Label;var button=c as RescueButton;var check=c as RescueCheck;
    if(label!=null&&label.Text.Length>0){int need=TextRenderer.MeasureText(label.Text,label.Font,new Size(label.Width,0),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix|TextFormatFlags.NoPadding).Height;if(need>label.Height)problems.Add(ctx+": text clipped in label '"+Brief(c)+"'");}
    if(check!=null){int need=TextRenderer.MeasureText(check.Text,check.Font,new Size(Math.Max(20,check.Width-UI.P(30)),0),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix|TextFormatFlags.NoPadding).Height;if(need>check.Height)problems.Add(ctx+": text clipped in checkbox '"+Brief(c)+"'");}
    if(button!=null){int w=TextRenderer.MeasureText(button.Text,button.Font,new Size(int.MaxValue/2,0),TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding).Width;if(w>button.Width-UI.P(20))problems.Add(ctx+": text clipped in button '"+Brief(c)+"'");}
    if(c is CardPanel)CheckChildren(c,problems,ctx);
   }
  }
  static string Brief(Control c){string t=c.Text??"";return t.Length>40?t.Substring(0,40)+"…":t;}
  void Note(string message){log.AppendText(DateTime.Now.ToString("HH:mm:ss")+"  "+message+Environment.NewLine);}
  void UpdateButtons(){
   if(prepare==null)return;prepare.Enabled=choose.Enabled=language.Enabled=!busy&&!nativeRunning;probe.Enabled=!preview&&!busy&&!nativeRunning&&prepared!=null&&state.CanLaunch&&model.Checked;recover.Enabled=probe.Enabled&&probePassed.Checked;
   bool ready2=prepared!=null&&state.CanLaunch&&model.Checked;
   b1.Set(prepared!=null?StepState.Done:StepState.Active);b2.Set(prepared==null?StepState.Locked:(probePassed.Checked?StepState.Done:StepState.Active));b3.Set(ready2&&probePassed.Checked?StepState.Active:StepState.Locked);
   string h2="";if(prepared==null)h2=T("Önce 1. adımı tamamlayın.","Complete step 1 first.");else if(!state.CanLaunch)h2=T("Kurtarma modunda bir alıcı bekleniyor. Ayrıntı için durum kartına bakın.","Waiting for a receiver in recovery mode. See the status card.");else if(!model.Checked)h2=T("Devam etmek için modeli onaylayın.","Confirm the model to continue.");
   string h3="";if(!recover.Enabled&&!busy&&!nativeRunning){if(!ready2)h3=T("Önce 1. ve 2. adımı tamamlayın.","Complete steps 1 and 2 first.");else if(!probePassed.Checked)h3=T("Algılama testini çalıştırın, ardından yukarıdaki kutuyu işaretleyin.","Run the detection test, then select the checkbox above.");}
   if(hint2.Text!=h2||hint3.Text!=h3){hint2.Text=h2;hint3.Text=h3;DoLayout();}
  }
  void UpdateState(){if(state.Code!=lastCode){lastCode=state.Code;history.Add(DateTime.Now.ToString("HH:mm:ss")+"  "+state.Code);if(history.Count>40)history.RemoveAt(0);}
   if(lastIdentity!=state.Identity){lastIdentity=state.Identity;if(model!=null)model.Checked=false;if(probePassed!=null)probePassed.Checked=false;}
   IconKind kind=IconKind.Neutral;
   switch(state.Code){
    case "ready":kind=IconKind.Success;status.Text=T("Kurtarma modunda alıcı bulundu","Receiver found in recovery mode");detail.Text=T("Alıcınız güncelleme (kurtarma) modunda; bu, onarılabileceği anlamına gelir.\nBu mod Standart ve Shroud için aynı göründüğünden modeli siz onaylayın.\nTeknik kimlik: 170D:0100 · FF00/0001","Your receiver is in update (recovery) mode, so it can be repaired.\nThis mode looks the same on Standard and Shroud, so you confirm the model.\nTechnical ID: 170D:0100 · FF00/0001");break;
    case "normal-receiver":kind=IconKind.Info;status.Text=T("İlk nesil alıcı bulundu","First-gen receiver detected");detail.Text=T("046D:0ABA · Standart / Shroud ailesi\nNormal USB modu; kurtarma engellendi.\nSes ve eşleştirmeyi ayrıca kontrol edin.","046D:0ABA · Standard / Shroud family\nNormal USB mode; recovery is blocked.\nCheck audio and pairing separately.");break;
    case "disconnect-headset":kind=IconKind.Warning;status.Text=T("Kulaklığın USB kablosunu çıkarın","Unplug the headset's USB cable");detail.Text=T("Kulaklık USB veya güncelleme modunda algılandı.\nBilgisayara yalnızca arızalı alıcı bağlı olmalıdır.\nKulaklığa firmware yazılmaz.","Headset detected in USB or update mode.\nKeep only the failed receiver connected.\nNo firmware will be written to the headset.");break;
    case "multiple-receivers":kind=IconKind.Warning;status.Text=T("Birden fazla alıcı algılandı","Multiple receivers detected");detail.Text=T("Kurtarma modunda birden fazla alıcı bulundu.\nYalnızca bir arızalı alıcıyı bağlı bırakın.\nKurtarma engellendi.","More than one receiver in recovery mode found.\nLeave one failed receiver connected.\nRecovery is blocked.");break;
    case "check-interface":kind=IconKind.Warning;status.Text=T("USB aygıtı var, HID arayüzü hazır değil","USB device found; HID interface not ready");detail.Text=state.DescriptorUnavailable?T("170D:0100 bulundu; HID bilgisi okunamadı.\nDiğer güncelleyicileri kapatın ve alıcıyı yeniden takın.\nDoğrulanana kadar kurtarma engellendi.","170D:0100 found; HID details unavailable.\nClose other updaters and reconnect the receiver.\nRecovery is blocked until verified."):T("170D:0100 USB aygıtı bulundu.\nGerekli FF00/0001 HID arayüzü bulunamadı.\nKurtarma engellendi; alıcıyı yeniden bağlayın.","170D:0100 USB device found.\nThe required FF00/0001 HID interface is missing.\nRecovery is blocked; reconnect the receiver.");break;
    case "unsupported":kind=IconKind.Danger;status.Text=T("Desteklenmeyen Avnera aygıtı","Unsupported Avnera device found");detail.Text=T("Desteklenmeyen bir USB kimliği algılandı.\nCihaz adı tek başına yeterli değil.\nBu aygıta firmware yazılmaz.","An unsupported USB identity is present.\nThe device name alone is insufficient.\nFirmware launch is blocked.");break;
    default:status.Text=T("İlk nesil alıcı bekleniyor","Waiting for first-gen receiver");detail.Text=T("Normal veya kurtarma modunda alıcı bulunamadı.\nAlgılama otomatik yenilenir.\nUSB'de görünmeyen cihaz kurtarılamaz.","No receiver found in normal or recovery mode.\nDetection refreshes automatically.\nUSB-invisible devices cannot be recovered here.");break;
   }
   ApplyKind(kind);UpdateButtons();DoLayout();
  }
  void ApplyKind(IconKind kind){statusIcon.Kind=kind;Color c=kind==IconKind.Success?UI.Success:kind==IconKind.Warning?UI.Caution:kind==IconKind.Danger?UI.Danger:kind==IconKind.Info?UI.Info:UI.Muted;status.ForeColor=c;art.Glow=kind==IconKind.Neutral?UI.Dim:c;art.Invalidate();statusIcon.AccessibleName=status.Text;}
  async Task Scan(){if(scanning||preview||IsDisposed)return;scanning=true;try{state=await Task.Run(()=>DeviceDiscovery.Scan());if(!IsDisposed)UpdateState();}catch(Exception e){if(!IsDisposed){state=new ReceiverState();UpdateState();status.Text=T("Aygıt listesi okunamadı","Device list unavailable");detail.Text=T("Windows aygıt bilgisi alınamadı.\nYeniden tarama otomatik yapılır.\n","Windows device details unavailable.\nA new scan will run automatically.\n")+e.Message;ApplyKind(IconKind.Danger);UpdateButtons();DoLayout();}}finally{scanning=false;}}
  async Task PrepareFrom(string local){
   if(busy||nativeRunning||preview)return;busy=true;UpdateButtons();progress.Value=0;
   try{Directory.CreateDirectory(cache);string original=local;
    if(original==null){original=Path.Combine(cache,"Logitech-112.2.212-original.exe");if(!VerifiedPatcher.MatchesFile(original,VerifiedPatcher.SourceSha)){
     Note(T("Logitech'in resmî HTTPS adresinden indiriliyor.","Downloading from Logitech's official HTTPS URL."));step.Text=T("İndiriliyor… Bu işlem cihaza firmware yazmaz.","Downloading… This does not write device firmware.");string tmp=Path.Combine(cache,"download-"+Guid.NewGuid().ToString("N")+".tmp");
     ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
     // Buffer a bounded response so HttpClient.Timeout covers the complete body as well as the headers.
     try{using(var client=new HttpClient{Timeout=TimeSpan.FromSeconds(60),MaxResponseContentBufferSize=12*1024*1024})using(var response=await client.GetAsync(VerifiedPatcher.VendorUrl,HttpCompletionOption.ResponseContentRead)){
      response.EnsureSuccessStatusCode();long length=response.Content.Headers.ContentLength??0;if(length>12*1024*1024)throw new InvalidDataException("Unexpected download size");
      using(var input=await response.Content.ReadAsStreamAsync())using(var output=new FileStream(tmp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){byte[] buffer=new byte[65536];long total=0;int n;while((n=await input.ReadAsync(buffer,0,buffer.Length))>0){total+=n;if(total>12*1024*1024)throw new InvalidDataException("Download exceeded size limit");await output.WriteAsync(buffer,0,n);if(length>0)progress.Value=(int)Math.Min(85,total*85/length);}}
     }if(!VerifiedPatcher.MatchesFile(tmp,VerifiedPatcher.SourceSha))throw new InvalidDataException(T("Dosyanın SHA-256 değeri beklenenle eşleşmiyor. İşlem durduruldu.","Updater SHA-256 does not match. Stopped."));File.Copy(tmp,original,true);}finally{if(File.Exists(tmp))File.Delete(tmp);}
    }}
    step.Text=T("Dosya ve yama doğrulanıyor…","Verifying source and patch…");progress.Value=90;
    string source=original;prepared=await Task.Run(()=>VerifiedPatcher.Prepare(source,Path.Combine(cache,"prepared"),manifestDir));progress.Value=100;step.Text=T("Doğrulandı. Algılama testi ve kurtarma dosyaları hazır; cihaza henüz yazım yapılmadı.","Verified. Detection test and recovery files are ready; no device has been written.");Note("SOURCE SHA256: "+VerifiedPatcher.SourceSha);Note("RECOVERY SHA256: "+VerifiedPatcher.RecoverySha);Note(T("Gömülü kaynaklar değişmemiştir; çıktı, test edilen yamayla aynıdır.","Embedded resources are unchanged; output matches the tested patch."));
   }catch(Exception e){prepared=null;step.Text=T("Hazırlama durduruldu. Hiçbir firmware aracı açılmadı.","Preparation stopped. No firmware tool was launched.");Note(ErrorText(e));SetTech(true);MessageBox.Show(this,ErrorText(e),"Receiver Rescue",MessageBoxButtons.OK,MessageBoxIcon.Warning);}finally{busy=false;UpdateButtons();DoLayout();}
  }
  async Task Launch(bool recovery){
   if(preview||prepared==null||busy||nativeRunning)return;busy=true;UpdateButtons();ReceiverState target;try{target=await Task.Run(()=>DeviceDiscovery.Scan());}catch(Exception e){busy=false;state=new ReceiverState();UpdateState();MessageBox.Show(this,ErrorText(e),"Receiver Rescue");return;}busy=false;state=target;UpdateState();if(!target.CanLaunch||!model.Checked||(recovery&&!probePassed.Checked)){UpdateButtons();return;}
   string file=recovery?prepared.Recovery:prepared.Probe,expected=recovery?VerifiedPatcher.RecoverySha:VerifiedPatcher.ProbeSha;
   if(!VerifiedPatcher.MatchesFile(file,expected)){MessageBox.Show(this,T("Hazırlanan dosya değiştirilmiş. Lütfen yeniden hazırlayın.","Prepared file changed. Prepare again."));prepared=null;UpdateButtons();return;}
   if(recovery&&MessageBox.Show(this,T("Bu adım alıcıya gerçek firmware yazar. İşlem sırasında USB bağlantısını kesmeyin ve bilgisayarı kapatmayın. Devam etmek istiyor musunuz?","This step writes real firmware to the receiver. Do not unplug USB or shut down the computer during transfer. Continue?"),"Receiver Rescue",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes){UpdateButtons();return;}
   nativeRunning=true;UpdateButtons();runText=recovery?T("Logitech kurtarma penceresi açılıyor. Gerçek firmware ilerlemesini oradan izleyin.","Opening Logitech recovery. Watch actual firmware progress in its window."):T("Algılama testi başlatılıyor. Bu sürüm firmware göndermez.","Launching the detection test. This build cannot send firmware packets.");step.Text=runText;runWatch.Restart();elapsedTimer.Start();DoLayout();bool started=false;
   try{var child=ProtectedLauncher.Request(file,recovery?"recovery":"probe");
    if(child==null)throw new IOException("Protected helper did not start.");started=true;
    int result;using(child){await Task.Run(()=>child.WaitForExit());result=child.ExitCode;}
    if(result!=0)throw new InvalidDataException(T("Korumalı başlatma başarısız oldu. Eski başlatma yöntemi kullanılmadı.","Protected launch failed. The old launch method was not used."));
    runWatch.Stop();Note(T("Logitech aracı kapandı (süre ","Logitech tool closed (duration ")+Support.FormatElapsed(runWatch.Elapsed)+T("). Çıkış kodu, başarı kanıtı sayılmaz.","). Its exit code is not treated as proof of success."));await Scan();step.Text=state.NormalReceiver?T("Normal USB alıcı görüldü. Son olarak eşleştirme, ses ve mikrofonu deneyin.","Normal USB receiver detected. Verify pairing, audio and microphone next."):T("Araç kapandı. USB durumunu ve yerel raporu kontrol edin.","Tool closed. Check USB state and the local report.");if(recovery&&state.NormalReceiver){recovered=true;DoLayout();scroller.ScrollControlIntoView(c4);await RunAudioCheck(true);}}
   catch(Exception e){var w=e as System.ComponentModel.Win32Exception;string msg=w!=null&&w.NativeErrorCode==1223?T("Yönetici izni verilmedi; hiçbir işlem başlatılmadı.","Administrator permission was not granted; nothing was started."):ErrorText(e);if(e is InvalidDataException)prepared=null;if(!started)step.Text=T("Araç başlatılmadı. Cihaza hiçbir veri yazılmadı.","The tool was not started. Nothing was written to the device.");Note(msg);SetTech(true);MessageBox.Show(this,msg,"Receiver Rescue",MessageBoxButtons.OK,MessageBoxIcon.Warning);}finally{elapsedTimer.Stop();runWatch.Stop();nativeRunning=false;UpdateButtons();DoLayout();}
  }
 }
}
