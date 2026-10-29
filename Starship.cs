using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Forms=System.Windows.Forms;

partial class Starship : FrameworkElement {
    Window window,controls; Forms.NotifyIcon tray; DispatcherTimer timer; Stopwatch clock=new Stopwatch();
    readonly Random random=new Random();
    double elapsed=-1,padX=Double.NaN,padBottom=Double.NaN,nextLaunch;
    DrawingGroup shipModel,boosterModel; static readonly Dictionary<string,Brush> brushes=new Dictionary<string,Brush>();
    bool paused,randomLaunches=true; Rect area; string phase="READY ON THE PAD";
    TextBlock information;CheckBox randomBox;Forms.ToolStripMenuItem randomMenu;bool moving;
    string settings=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launchpad.txt");
    [DllImport("user32.dll")]static extern int GetWindowLong(IntPtr h,int n);
    [DllImport("user32.dll")]static extern int SetWindowLong(IntPtr h,int n,int v);
    [DllImport("user32.dll")]static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindow(string c,string t);
    [STAThread]static void Main(string[] args){
        try {if(args.Contains("--test-companion")){TestCompanionSignal();return;}if(args.Contains("--self-test")){SelfTest();return;}if(args.Contains("--model-preview")){RenderModelPreview();return;}if(args.Contains("--render")){RenderPreviews();return;}bool made;using(var mutex=new Mutex(true,"Local\\OrbitStarshipDesktop",out made)){
            if(!made){PostMessage(FindWindow(null,"Orbit Starship"),0x8001,IntPtr.Zero,IntPtr.Zero);return;}
            var app=new Application();var scene=new Starship();scene.Open();if(args.Contains("--demo"))scene.CrewDemo();app.Run();
        }}catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"error.txt"),ex.ToString());}
    }
    void Open(){
        var s=Forms.Screen.AllScreens.FirstOrDefault(x=>x.DeviceName.EndsWith("DISPLAY2"))??Forms.Screen.PrimaryScreen;
        double scale=Forms.Screen.PrimaryScreen.Bounds.Width/SystemParameters.PrimaryScreenWidth;
        area=new Rect(s.WorkingArea.Left/scale,s.WorkingArea.Top/scale,s.WorkingArea.Width/scale,s.WorkingArea.Height/scale);
        padX=area.Width*.68;padBottom=area.Height-110;
        try{var saved=File.ReadAllLines(settings);double px,py;if(saved.Length>=2&&Double.TryParse(saved[0],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out px)&&Double.TryParse(saved[1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out py)&&!Double.IsNaN(px)&&!Double.IsInfinity(px)&&!Double.IsNaN(py)&&!Double.IsInfinity(py)){padX=px*area.Width;padBottom=py*area.Height;}if(saved.Length>2)randomLaunches=saved[2]!="off";}catch{}
        ClampPad();Schedule();
        window=new Window{Title="Orbit Starship",Left=area.Left,Top=area.Top,Width=area.Width,Height=area.Height,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,Topmost=true,ShowInTaskbar=false,ShowActivated=false,ResizeMode=ResizeMode.NoResize,IsHitTestVisible=false,Content=PrepareModels()};
        window.SourceInitialized+=delegate {var h=new System.Windows.Interop.WindowInteropHelper(window).Handle;SetWindowLong(h,-20,GetWindowLong(h,-20)|0x20|0x80|0x08000000);System.Windows.Interop.HwndSource.FromHwnd(h).AddHook((IntPtr a,int m,IntPtr w,IntPtr l,ref bool handled)=>{if(m==0x8001){Launch();handled=true;}return IntPtr.Zero;});};
        tray=new Forms.NotifyIcon{Text="Orbit Starship · launch control",Icon=System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location),Visible=true};
        var menu=new Forms.ContextMenuStrip();menu.Items.Add("Manual takeoff",null,(a,b)=>Launch());
        AddMissionMenus(menu);menu.Items.Add("Pause / resume",null,(a,b)=>{paused=!paused;});
        randomMenu=new Forms.ToolStripMenuItem("Random takeoffs · every 2–6 minutes"){CheckOnClick=true,Checked=randomLaunches};randomMenu.CheckedChanged+=(a,b)=>{randomLaunches=randomMenu.Checked;randomBox.IsChecked=randomLaunches;Save();Schedule();};menu.Items.Add(randomMenu);
        menu.Items.Add("Reset position · screen two",null,(a,b)=>{padX=area.Width*.68;padBottom=area.Height-110;ClampPad();PositionControls();Save();InvalidateVisual();});
        menu.Items.Add("Show / hide launchpad",null,(a,b)=>{if(window.IsVisible){window.Hide();controls.Hide();}else{window.Show();controls.Show();}});
        menu.Items.Add("Exit Starship",null,(a,b)=>Application.Current.Shutdown());tray.ContextMenuStrip=menu;tray.DoubleClick+=(a,b)=>Launch();
        CreateControls();Application.Current.Exit+=(a,b)=>{Save();StopDriver();tray.Dispose();};
        window.Show();controls.Show();StartDriver();
    }
    void Advance(double dt){StepMission(dt);}
    void Schedule(){nextLaunch=120+random.NextDouble()*240;}
    void Launch(){BeginMission(MissionScenario.Automatic,false);}
    void ClampPad(){padX=Math.Max(125,Math.Min(area.Width-105,padX));padBottom=Math.Max(470,Math.Min(area.Height-110,padBottom));}
    void Save(){try{File.WriteAllLines(settings,new[]{(padX/area.Width).ToString(System.Globalization.CultureInfo.InvariantCulture),(padBottom/area.Height).ToString(System.Globalization.CultureInfo.InvariantCulture),randomLaunches?"on":"off"});}catch{}}
    void PositionControls(){if(controls==null)return;controls.Left=area.Left+padX-117;controls.Top=area.Top+padBottom+18;}
    void CreateControls(){
        var panel=new StackPanel();var grip=new TextBlock{Text="⠿  STARBASE  /  DRAG TO MOVE",FontSize=10,Foreground=B("#8FCBD8"),Padding=new Thickness(8,6,8,5),Cursor=Cursors.SizeAll};panel.Children.Add(grip);
        var row=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(7,0,7,4)};var launch=new Button{Content="🚀  TAKE OFF",FontSize=11,FontWeight=FontWeights.SemiBold,Padding=new Thickness(9,4,9,4),Background=B("#185C70"),Foreground=Brushes.White,BorderBrush=B("#458A9A"),Cursor=Cursors.Hand};launch.Click+=(a,b)=>Launch();row.Children.Add(launch);
        randomBox=new CheckBox{Content="Random",IsChecked=randomLaunches,Foreground=B("#C3D9E4"),FontSize=11,Margin=new Thickness(10,6,0,0),ToolTip="Take off at random intervals, 2–6 minutes after restacking"};randomBox.Checked+=(a,b)=>{randomLaunches=true;randomMenu.Checked=true;Schedule();Save();};randomBox.Unchecked+=(a,b)=>{randomLaunches=false;randomMenu.Checked=false;Save();};row.Children.Add(randomBox);var statsButton=new Button{Content="Stats",FontSize=10,Padding=new Thickness(5,3,5,3),Margin=new Thickness(8,0,0,0),Background=B("#1D3445"),Foreground=Brushes.White,BorderThickness=new Thickness(0)};statsButton.Click+=(a,b)=>ShowStats();row.Children.Add(statsButton);panel.Children.Add(row);
        information=new TextBlock{Text="READY ON THE PAD",Foreground=B("#779BAA"),FontSize=9,TextTrimming=TextTrimming.CharacterEllipsis,Margin=new Thickness(8,0,4,5)};panel.Children.Add(information);
        controls=new Window{Title="Starship launch controls",Width=234,Height=88,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,Topmost=true,ShowInTaskbar=false,ShowActivated=false,ResizeMode=ResizeMode.NoResize,Content=new Border{Background=B("#EC0B1722"),BorderBrush=B("#365E70"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(9),Child=panel}};
        controls.SourceInitialized+=delegate{var h=new System.Windows.Interop.WindowInteropHelper(controls).Handle;SetWindowLong(h,-20,GetWindowLong(h,-20)|0x80|0x08000000);};
        var context=new ContextMenu();var showStats=new MenuItem{Header="Mission statistics"};showStats.Click+=(a,b)=>ShowStats();context.Items.Add(showStats);var clear=new MenuItem{Header="Clear wreckage"};clear.Click+=(a,b)=>ClearWreckage();context.Items.Add(clear);var testMenu=new MenuItem{Header="Test scenario"};foreach(MissionScenario option in Enum.GetValues(typeof(MissionScenario))){if(option==MissionScenario.Automatic)continue;var chosen=option;var item=new MenuItem{Header=ScenarioName(chosen)};item.Click+=(a,b)=>BeginMission(chosen,true);testMenu.Items.Add(item);}var crewTest=new MenuItem{Header="Crew boarding + normal flight"};crewTest.Click+=(a,b)=>CrewDemo();testMenu.Items.Add(crewTest);var companionTest=new MenuItem{Header="Companion · run to bed"};companionTest.Click+=(a,b)=>AlertCompanion();testMenu.Items.Add(companionTest);context.Items.Add(testMenu);controls.ContextMenu=context;
        grip.MouseLeftButtonDown+=(a,b)=>{moving=true;try{controls.DragMove();padX=controls.Left-area.Left+117;padBottom=controls.Top-area.Top-18;ClampPad();PositionControls();Save();InvalidateVisual();}finally{moving=false;}};
        controls.LocationChanged+=(a,b)=>{if(moving){padX=controls.Left-area.Left+117;padBottom=controls.Top-area.Top-18;ClampPad();InvalidateVisual();}};PositionControls();
    }
    static double Clamp(double n){return Math.Max(0,Math.Min(1,n));}
    static double Ease(double n){n=Clamp(n);return n*n*(3-2*n);}
    static Brush B(string c){Brush b;if(brushes.TryGetValue(c,out b))return b;var solid=new SolidColorBrush((Color)ColorConverter.ConvertFromString(c));solid.Freeze();brushes[c]=solid;return solid;}
    void Line(DrawingContext d,string c,double width,double x,double y,double xx,double yy){d.DrawLine(new Pen(B(c),width),new Point(x,y),new Point(xx,yy));}
    void Label(DrawingContext d,string text,double x,double y,string c,double size){d.DrawText(new FormattedText(text,System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),size,B(c)),new Point(x,y));}
    void DrawVehicleBody(DrawingContext d,double x,double y,bool ship,double flame,double t){
        d.PushTransform(new TranslateTransform(x,y));
        double h=ship?120:224,w=28;
        var metal=new LinearGradientBrush();metal.StartPoint=new Point(0,0);metal.EndPoint=new Point(1,0);metal.GradientStops.Add(new GradientStop(Color.FromRgb(62,75,87),0));metal.GradientStops.Add(new GradientStop(Color.FromRgb(224,237,240),.32));metal.GradientStops.Add(new GradientStop(Color.FromRgb(137,157,169),.64));metal.GradientStops.Add(new GradientStop(Color.FromRgb(47,61,76),1));
        d.DrawRoundedRectangle(metal,new Pen(B("#8196A4"),.7),new Rect(-w/2,0,w,h),1,1);
        if(ship){
            // Ogive nose, dark windward heat shield, paired forward and aft flaps.
            var g=Geometry.Parse("M -14,3 C -14,-12 -8,-31 0,-40 C 8,-31 14,-12 14,3 Z");d.DrawGeometry(metal,new Pen(B("#A8B9C4"),.6),g);
            d.DrawGeometry(B("#18232B"),null,Geometry.Parse("M -5,-36 C 7,-27 14,-7 14,3 L 14,119 L -7,119 L -7,-20 Z"));
            d.PushClip(new RectangleGeometry(new Rect(-7,0,21,119)));for(int j=0;j<34;j++)for(int i=0;i<6;i++){double xx=-6+i*4+(j%2)*2,yy=j*3.5;var tile=new StreamGeometry();using(var q=tile.Open()){q.BeginFigure(new Point(xx-2,yy),false,true);q.PolyLineTo(new[]{new Point(xx-1,yy-1.7),new Point(xx+1,yy-1.7),new Point(xx+2,yy),new Point(xx+1,yy+1.7),new Point(xx-1,yy+1.7)},true,false);}d.DrawGeometry(null,new Pen(B("#35444B"),.35),tile);}d.Pop();
            d.DrawGeometry(metal,new Pen(B("#8096A1"),.5),Geometry.Parse("M -13,-7 L -25,11 L -24,29 L -14,19 Z M 13,-7 L 23,10 L 22,28 L 14,19 Z M -13,86 L -31,113 L -29,125 L -14,115 Z M 13,86 L 29,111 L 28,123 L 14,115 Z"));
            Line(d,"#162630",1,-14,18,-23,27);Line(d,"#162630",1,-14,113,-29,123);
        }
        // Fine stainless-steel weld rings rather than wide cartoon bands.
        for(double yy=4;yy<h-4;yy+=5.5){Line(d,"#61737C",.45,-13,yy,ship?-8:13,yy);Line(d,"#B2C1C4",.25,-11,yy+1,ship?-8:10,yy+1);}
        Line(d,"#E4EDEF",.75,-8,2,-8,h-5);Line(d,"#455862",.7,-13,2,-13,h-2);
        if(!ship){
            // Vented hot-stage crown and visible mesh grid fins.
            d.DrawRectangle(B("#586970"),null,new Rect(-14,0,28,11));for(int i=0;i<9;i++)d.DrawRectangle(B("#101D25"),null,new Rect(-12+i*3,2,1.4,7));
            for(int side=-1;side<=1;side+=2){double xx=side<0?-33:14;d.DrawRectangle(B("#263940"),new Pen(B("#93A4A8"),.8),new Rect(xx,18,19,9));for(int k=1;k<5;k++)Line(d,"#73858A",.55,xx+k*3.5,18,xx+k*3.5,27);for(int k=1;k<3;k++)Line(d,"#73858A",.55,xx,18+k*3,xx+19,18+k*3);}
            Line(d,"#C5D0D1",1.5,-16,11,-16,31);Line(d,"#C5D0D1",1.5,16,11,16,31);
            d.DrawRectangle(B("#35464C"),null,new Rect(-14,h-16,28,15));for(int i=0;i<7;i++){double xx=-12+i*4;d.DrawGeometry(B("#142027"),new Pen(B("#798B91"),.45),Geometry.Parse("M "+xx+","+(h-4)+" l -1,7 l 3,0 l -1,-7 Z"));}
        }
        if(ship){for(int i=0;i<3;i++)d.DrawEllipse(B("#1D2C34"),new Pen(B("#879BA4"),.5),new Point(-8+i*8,h),3,4);}
        if(flame>0){double flicker=.87+.13*Math.Sin(t*63);var plume=new StreamGeometry();using(var g=plume.Open()){g.BeginFigure(new Point(-9,h),true,true);g.LineTo(new Point(0,h+flame*flicker),true,false);g.LineTo(new Point(9,h),true,false);}d.DrawGeometry(B("#A74A7CFF"),null,plume);d.DrawEllipse(B("#75A6EFFF"),null,new Point(0,h+8),13,16);d.DrawGeometry(B("#FFF3DE"),null,Geometry.Parse("M -5,"+h+" L 0,"+(h+flame*.62).ToString(System.Globalization.CultureInfo.InvariantCulture)+" L 5,"+h+" Z"));}
        d.Pop();
    }
}
