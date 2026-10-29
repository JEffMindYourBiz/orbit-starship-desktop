using System;
using System.Windows;
using System.Windows.Media;

partial class Starship {
    readonly System.Diagnostics.Stopwatch frameMeter=System.Diagnostics.Stopwatch.StartNew();int observedFrames;double sampleStart,observedFps;bool sampleSaved;
    static Brush Haze(string color){string key="haze:"+color;Brush cached;if(brushes.TryGetValue(key,out cached))return cached;var c=(Color)ColorConverter.ConvertFromString(color);var g=new RadialGradientBrush(c,Color.FromArgb(0,c.R,c.G,c.B));g.GradientOrigin=new Point(.42,.4);g.Freeze();brushes[key]=g;return g;}
    void Vehicle(DrawingContext d,FlightProfile.Pose p,bool ship,double t){
        if(!p.Visible)return;
        if(modelView!=null){d.PushTransform(new TranslateTransform(p.X,p.Y));d.PushTransform(new RotateTransform(p.Angle));d.PushTransform(new ScaleTransform(p.Scale,p.Scale));if(ship&&p.Heat>0)Plasma(d,p.Heat,t);if(p.Flame>0)Exhaust(d,ship?124:230,ship?p.Flame:p.Flame*1.6,t,ship);d.Pop();d.Pop();d.Pop();return;}
        DrawingGroup art=ship?shipModel:boosterModel;
        if(art==null){art=new DrawingGroup();using(var dc=art.Open())DrawVehicleBody(dc,0,0,ship,0,0);art.Freeze();if(ship)shipModel=art;else boosterModel=art;}
        d.PushTransform(new TranslateTransform(p.X,p.Y));d.PushTransform(new RotateTransform(p.Angle));d.PushTransform(new ScaleTransform(p.Scale,p.Scale));
        if(ship&&p.Heat>0)Plasma(d,p.Heat,t);
        d.DrawDrawing(art);
        if(!ship){for(int side=-1;side<=1;side+=2){double x=side<0?-33:14;d.PushTransform(new RotateTransform(p.Fin*side,x+9,22));d.DrawRectangle(B("#2D3A3D"),new Pen(B("#A4B2B7"),.8),new Rect(x,18,19,9));for(int n=1;n<5;n++)Line(d,"#829497",.6,x+n*3.5,18,x+n*3.5,27);for(int n=1;n<3;n++)Line(d,"#829497",.6,x,18+n*3,x+19,18+n*3);d.Pop();}}
        else if(Math.Abs(p.Fin)>0.1){d.PushOpacity(.7);Line(d,"#7C9199",2,-14,95,-28,115+p.Fin*.15);Line(d,"#3E525D",2,14,95,27,115-p.Fin*.15);d.Pop();}
        if(p.Flame>0)Exhaust(d,ship?124:230,ship?p.Flame:p.Flame*1.6,t,ship);
        d.Pop();d.Pop();d.Pop();
    }
    void Exhaust(DrawingContext d,double y,double length,double t,bool ship){
        double flicker=1+.045*Math.Sin(t*49)+.025*Math.Sin(t*83),len=length*flicker;
        var aura=new RadialGradientBrush(Color.FromArgb(65,255,126,173),Color.FromArgb(0,255,126,173));d.DrawEllipse(aura,null,new Point(0,y+len*.45),27,len*.85);
        for(int layer=0;layer<4;layer++){
            double width=13-layer*2.5,extent=len*(1-layer*.13);var g=new StreamGeometry();using(var q=g.Open()){q.BeginFigure(new Point(-width,y),true,true);q.BezierTo(new Point(-width-5,y+extent*.3),new Point(-width*.65+Math.Sin(t*40)*2,y+extent*.8),new Point(Math.Sin(t*31)*2,y+extent),true,false);q.BezierTo(new Point(width*.65,y+extent*.75),new Point(width+5,y+extent*.3),new Point(width,y),true,false);}
            var brush=new LinearGradientBrush();brush.StartPoint=new Point(.5,0);brush.EndPoint=new Point(.5,1);brush.GradientStops.Add(new GradientStop(layer<2?Color.FromArgb(185,255,115,185):Color.FromArgb(245,255,245,248),0));brush.GradientStops.Add(new GradientStop(layer<2?Color.FromArgb(130,184,107,255):Color.FromArgb(200,178,207,255),.55));brush.GradientStops.Add(new GradientStop(Color.FromArgb(0,184,107,255),1));d.DrawGeometry(brush,null,g);
        }
        for(int i=0;i<6;i++){double yy=y+8+i*len*.13;double w=(7-i*.6)*(1+.08*Math.Sin(t*33+i));d.PushOpacity(.28-i*.035);var diamond=new StreamGeometry();using(var q=diamond.Open()){q.BeginFigure(new Point(0,yy-4),true,true);q.PolyLineTo(new[]{new Point(w,yy+2),new Point(0,yy+9),new Point(-w,yy+2)},true,false);}d.DrawGeometry(B("#FFF9F1"),null,diamond);d.Pop();}
        for(int i=0;i<18;i++){double q=(t*3+i*.173)%1;d.PushOpacity((1-q)*.5);d.DrawEllipse(B("#FFC3DA"),null,new Point(Math.Sin(i*7.1)*q*14,y+q*len),.6+q,.9+q*2);d.Pop();}
    }
    void Plasma(DrawingContext d,double heat,double t){
        d.PushOpacity(heat);
        var glow=new RadialGradientBrush(Color.FromArgb(140,255,88,18),Color.FromArgb(0,255,88,18));d.DrawEllipse(glow,null,new Point(21,43),32,89);
        var bow=Geometry.Parse("M 3,-41 C 32,-32 40,17 34,64 C 32,96 22,119 10,126");
        for(int layer=0;layer<4;layer++){d.PushOpacity(.3+layer*.15);d.DrawGeometry(null,new Pen(B(layer<2?"#FF5B16":"#FFD384"),12-layer*2.7),bow);d.Pop();}
        for(int i=0;i<23;i++){double yy=-32+i*7,phase=t*10+i*.7;double reach=(16+18*(.5+.5*Math.Sin(phase)))*heat;var flow=new StreamGeometry();using(var q=flow.Open()){q.BeginFigure(new Point(21+9*Math.Sin((yy+30)/160*Math.PI),yy),false,false);q.BezierTo(new Point(15,yy+10),new Point(-reach*.5,yy+17+Math.Sin(phase)*8),new Point(-reach,yy+9),true,false);}d.PushOpacity(.25+.25*Math.Sin(phase)*Math.Sin(phase));d.DrawGeometry(null,new Pen(B(i%3==0?"#FFF1B3":"#FF8E36"),1.3),flow);d.Pop();}
        d.Pop();
    }
    void Tower(DrawingContext d,double x,double bottom,FlightProfile.Frame f){
        double top=bottom-417;
        var steel=new LinearGradientBrush(Color.FromRgb(139,147,145),Color.FromRgb(43,54,58),0);
        d.DrawRectangle(B("#1D2B32"),null,new Rect(x-91,top,35,417));
        for(int i=0;i<16;i++){double y=top+6+i*25;Line(d,"#B5B9AE",1.1,x-90,y,x-57,y+24);Line(d,"#677A7F",1,x-57,y,x-90,y+24);Line(d,"#919C9B",1.4,x-93,y,x-54,y);}
        Line(d,"#C1C7BC",3,x-92,top,x-92,bottom);Line(d,"#85918C",3,x-54,top,x-54,bottom);Line(d,"#3E515B",4,x-71,top,x-71,bottom);
        d.DrawRectangle(steel,new Pen(B("#A7ADA5"),1),new Rect(x-95,top-17,47,19));for(int i=0;i<9;i++)Line(d,"#5C6E70",.6,x-93+i*5,top-16,x-93+i*5,top);
        // Travelling carriage, two deep triangular truss arms, capture pads.
        double yArm=f.ArmY,open=f.ArmOpen,tipLeft=x-17-open*27,tipRight=x+17+open*27;
        d.DrawRectangle(steel,new Pen(B("#A0ADA9"),1),new Rect(x-95,yArm-26,47,55));
        for(int side=0;side<2;side++){double tip=side==0?tipLeft:tipRight,yy=yArm+side*7;Line(d,side==0?"#B5BDAF":"#607780",4,x-65,yy-9,tip,yy-9-open*7);Line(d,"#7F918E",3,x-65,yy+17,tip,yy-6-open*7);Line(d,"#9CA79D",2,x-65,yy-9,x-65,yy+17);for(int i=1;i<5;i++){double q=i/5.0,xx=x-65+(tip-x+65)*q;Line(d,"#7B8D8C",1,xx,yy-9-open*7*q,xx-8,yy+17-(23+open*7)*q);}d.DrawRectangle(B("#333F43"),null,new Rect(tip-3,yy-12-open*7,7,10));}
        d.DrawRoundedRectangle(steel,new Pen(B("#526671"),1),new Rect(x-112,bottom-7,216,9),2,2);d.DrawRectangle(B("#384B53"),null,new Rect(x-23,bottom-16,46,12));
        for(int i=0;i<4;i++)Line(d,"#74858A",3,x-20+i*13,bottom-3,x-24+i*16,bottom+9);
        // Crew access bridge at the ship's door; stows before ignition.
        double door=bottom-FlightProfile.HomeOffset-120+42,extension=boarding>0?1:elapsed>=0?1-Ease(elapsed/3):0;
        if(extension>0){Line(d,"#A5B5B9",3,x-55,door+10,x-55+49*extension,door+10);Line(d,"#667F89",1,x-55,door-1,x-55+49*extension,door-1);for(int i=0;i<6;i++)Line(d,"#A2B1B5",.7,x-55+i*9*extension,door-1,x-55+i*9*extension,door+10);}
    }
    void Astronauts(DrawingContext d,double x,double bottom){
        if(boarding<=0)return;
        double time=18-boarding,door=bottom-FlightProfile.HomeOffset-120+42;
        for(int i=0;i<3;i++){double progress=(time-i*1.8)/13;if(progress<0||progress>=1)continue;double xx,yy;
            if(progress<.35){double q=progress/.35;xx=x+105-(175*q);yy=bottom-12;}
            else if(progress<.78){xx=x-70;yy=bottom-12-(bottom-12-door)*Ease((progress-.35)/.43);}
            else{xx=x-70+66*Ease((progress-.78)/.22);yy=door;}
            double step=Math.Sin(time*11-i*2)*2;
            d.DrawRoundedRectangle(B("#F1EEE8"),new Pen(B("#9DADB5"),.5),new Rect(xx-3,yy-12,6,8),2,2);d.DrawEllipse(B("#F2F2F0"),new Pen(B("#9DACB1"),.5),new Point(xx,yy-15),4,4);d.DrawRoundedRectangle(B("#27353E"),null,new Rect(xx-3,yy-17,6,3),1,1);Line(d,"#E6ECEE",1.8,xx-1,yy-5,xx-2-step,yy);Line(d,"#E6ECEE",1.8,xx+1,yy-5,xx+2+step,yy);Line(d,"#DDE6E8",1.4,xx-3,yy-11,xx-5+step,yy-7);Line(d,"#DDE6E8",1.4,xx+3,yy-11,xx+5-step,yy-7);
        }
    }
    Point ImpactPoint(double time,double width,double height,double x,double bottom){var f=FlightProfile.At(time,width,height,x,bottom);var p=scenario==MissionScenario.ShipBreakup||scenario==MissionScenario.ShipHardLanding?f.Ship:f.Booster;return new Point(scenario==MissionScenario.BoosterOffPad?Math.Min(width-65,x+180):p.X,scenario==MissionScenario.PadExplosion?bottom-80:scenario==MissionScenario.BoosterHardLanding||scenario==MissionScenario.BoosterOffPad?bottom-55:scenario==MissionScenario.ShipHardLanding?bottom-280:p.Y+60);}
    void Explosion(DrawingContext d,Point origin,double age){
        double floor=ActualHeight-8;if(blastFrames.Count>0)BlenderBlast(d,origin,age);
        if(blastFrames.Count==0&&age<4){double q=age/4;var fire=new RadialGradientBrush();fire.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(240*(1-q)),255,249,220),0));fire.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(190*(1-q)),255,126,26),.4));fire.GradientStops.Add(new GradientStop(Color.FromArgb(0,184,107,255),1));d.DrawEllipse(fire,null,origin,35+age*95,25+age*70);if(age<1.2)d.DrawEllipse(null,new Pen(B("#A9FFBB65"),(1-age/1.2)*4),origin,age*160,age*120);}
        if(modelView==null)for(int i=0;i<75;i++){var p=MissionEffects.Debris(origin,i,age,floor,ActualWidth);d.PushTransform(new RotateTransform((i*77+age*(i%2==0?170:-140)),p.X,p.Y));string color=i%4==0?"#28333B":i%3==0?"#A8B2AF":"#667A83";double w=i%17==0?23:i%7==0?14:3+i%5,h=i%17==0?40:i%9==0?22:3+i%4;var edge=new Pen(B("#B3C2C3"),.65);if(i%11==0)d.DrawEllipse(null,new Pen(B("#A8B8BC"),1.5),p,w*.65,h*.5);else if(i%7==0){var shard=new StreamGeometry();using(var q=shard.Open()){q.BeginFigure(new Point(p.X-w/2,p.Y-h/2),true,true);q.PolyLineTo(new[]{new Point(p.X+w/2,p.Y+h*.2),new Point(p.X-w*.3,p.Y+h/2)},true,false);}d.DrawGeometry(B(color),edge,shard);}else{d.DrawRectangle(B(color),edge,new Rect(p.X-w/2,p.Y-h/2,w,h));if(i%5==0){Line(d,"#B3C2C3",.5,p.X-w/2,p.Y,p.X+w/2,p.Y);Line(d,"#B3C2C3",.5,p.X,p.Y-h/2,p.X,p.Y+h/2);}}if(age<2.6){d.PushOpacity((1-age/2.6)*.8);d.DrawEllipse(B("#FFC565"),null,p,w*.6,h*.5);d.Pop();}d.Pop();}
        if(blastFrames.Count==0&&age<3.5){for(int i=0;i<15;i++){d.PushOpacity((1-age/3.5)*.65);d.DrawEllipse(Haze(i%3==0?"#FFFFE2A3":"#F8FF741D"),null,new Point(origin.X+Math.Sin(i*13)*age*35,origin.Y+Math.Cos(i*7)*age*28),18+age*24,14+age*22);d.Pop();}}
        if(age<10){for(int i=0;i<16;i++){d.PushOpacity(Math.Max(0,(1-age/10)*.45));d.DrawEllipse(Haze(i%2==0?"#DD5E5350":"#DD3C4146"),null,new Point(origin.X+Math.Sin(i*4)*age*22,origin.Y-age*22-i*3),12+age*10,10+age*8);d.Pop();}}
    }
    protected override void OnRender(DrawingContext d){
        if(window!=null&&(elapsed>=0||boarding>0)){observedFrames++;double now=frameMeter.Elapsed.TotalSeconds;if(now-sampleStart>=2){observedFps=observedFrames/(now-sampleStart);observedFrames=0;sampleStart=now;if(!sampleSaved&&elapsed>8){try{System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"live-frame-check.txt"),"Observed window redraws/second: "+observedFps.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+" over a two-second live sample. Target 60 Hz; this counts WPF redraws, not compositor presentations.\r\n");sampleSaved=true;}catch{}}}}
        double bottom=Double.IsNaN(padBottom)?ActualHeight-115:padBottom,x=Double.IsNaN(padX)?ActualWidth*.62:padX,t=elapsed;
        var f=FlightProfile.At(t,ActualWidth,ActualHeight,x,bottom);phase=boarding>0?"CREW BOARDING · ACCESS ARM":f.Stage;
        double impact=MissionEffects.IncidentTime(scenario);bool failed=t>=impact;
        if(!failed)ApplyFailureApproach(f,t,bottom);
        if(failed){phase="MISSION FAILURE · "+ScenarioName(scenario).ToUpperInvariant();if(scenario==MissionScenario.PadExplosion||scenario==MissionScenario.HotStageFailure){f.Ship.Visible=false;f.Booster.Visible=false;}else if(scenario==MissionScenario.ShipBreakup||scenario==MissionScenario.ShipHardLanding)f.Ship.Visible=false;else f.Booster.Visible=false;}
        if(modelView==null)Tower(d,x,bottom,f);else UpdateModels(f,x,bottom);if(modelView==null)Astronauts(d,x,bottom);
        Vehicle(d,f.Ship,true,t);Vehicle(d,f.Booster,false,t);
        if(f.HotStage>0){d.PushOpacity(f.HotStage*.6);d.DrawEllipse(B("#FFF1DA"),null,new Point(f.Booster.X,f.Booster.Y),24,14);d.Pop();}
        if(t>=0&&t<14){for(int i=0;i<35;i++){double age=(t*.8+i*.137)%2.4,spread=t<6?18:70;d.PushOpacity((1-age/2.4)*(t<6?.35:.65));d.DrawEllipse(Haze(i%3==0?"#E4E4DCD0":"#E4A4B6BF"),null,new Point(x+Math.Sin(i*9.7)*spread*age,t<4?bottom-200-age*12:bottom-age*9),8+age*10,5+age*5);d.Pop();}}
        if(failed){d.PushOpacity(t-impact>14?Math.Max(0,(18-t+impact)/4):1);Explosion(d,ImpactPoint(impact,ActualWidth,ActualHeight,x,bottom),t-impact);d.Pop();}
        Label(d,"STARSHIP / SUPER HEAVY",x-108,bottom+4,"#A0BAC5",8);
    }
}
