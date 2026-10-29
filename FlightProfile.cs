using System;

// Compressed, continuous desktop choreography. Units are WPF device-independent pixels.
static class FlightProfile {
    public const double Duration=118;public const double HomeOffset=224+10*224/72.0;
    public struct Pose {public double X,Y,Angle,Flame,Heat,Fin,Scale;public bool Visible;}
    public sealed class Frame {public Pose Booster,Ship;public double ArmY,ArmOpen,HotStage;public bool Attached;public string Stage;}
    static double C(double q){return Math.Max(0,Math.Min(1,q));}
    static double E(double q){q=C(q);return q*q*q*(10+q*(-15+6*q));}
    static double L(double a,double b,double q){return a+(b-a)*E(q);}
    static Pose P(double x,double y,double angle=0,double flame=0){return new Pose{X=x,Y=y,Angle=angle,Flame=flame,Scale=1,Visible=true};}
    static void Attach(Frame f){double a=f.Booster.Angle*Math.PI/180;f.Ship=P(f.Booster.X+120*Math.Sin(a),f.Booster.Y-120*Math.Cos(a),f.Booster.Angle);f.Attached=true;}
    public static Frame At(double t,double width,double height,double padX,double bottom){var frame=Raw(t,width,height,padX,bottom);ContinuousFlight.Apply(frame,t,width,height,padX,bottom);return frame;}
    static Frame Raw(double t,double width,double height,double padX,double bottom){
        double home=bottom-HomeOffset,shipHome=home-120,shipCatch=shipHome-50;
        double dir=padX>width*.5?-1:1,travel=Math.Min(150,width*.22);
        double sx=padX+dir*travel,sy=Math.Max(175,home-Math.Min(height*.42,home-175));
        var f=new Frame{Booster=P(padX,home),Ship=P(padX,shipHome),ArmY=home+16,Stage="READY · FULL STACK",Attached=true};
        if(t<0)return f;
        if(t<7){f.Stage=t<4?"VENTING · COUNTDOWN":"RAPTOR IGNITION";f.Booster.Flame=t<4?0:L(0,105,(t-4)/3);f.ArmOpen=E((t-2)/4);return f;}
        if(t<18){double q=(t-7)/11,climb=E(q);f.Booster=P(padX+dir*travel*climb,home-(home-sy)*climb,dir*13*E(q),115);Attach(f);f.ArmOpen=1;f.Stage="ASCENT · PITCH TOWARD ORBIT";f.HotStage=E((t-17)/1);f.Ship.Flame=f.HotStage*32;return f;}
        f.Attached=false;f.ArmOpen=1;
        double angle=dir*13,rad=angle*Math.PI/180,shipStartX=sx+120*Math.Sin(rad),shipStartY=sy-120*Math.Cos(rad);
        if(t<24){double q=(t-18)/6;f.Booster=P(L(sx,sx+dir*85,q),L(sy,sy-80,q),L(angle,-dir*145,q),L(18,0,q));f.HotStage=1-E((t-18)/2);f.Stage="HOT STAGING · BOOSTER FLIP";}
        else if(t<30){double q=(t-24)/6;f.Booster=P(L(sx+dir*85,sx-dir*20,q),L(sy-80,sy-115,q),L(-dir*145,-dir*160,q),65*Math.Sin(Math.PI*q));f.Stage="BOOSTBACK BURN";}
        else if(t<36){double q=(t-30)/6;f.Booster=P(L(sx-dir*20,padX+dir*70,q),L(sy-115,sy+45,q),L(-dir*160,dir*7,q));f.Stage="BOOSTER COAST · GRID FIN GUIDANCE";}
        else if(t<49){double q=(t-36)/13;f.Booster=P(L(padX+dir*70,padX+dir*8,q)+Math.Sin(q*Math.PI*3)*8*Math.Sin(q*Math.PI),L(sy+45,home-90,q),L(dir*7,-dir*2,q)+Math.Sin(q*Math.PI*4)*3*Math.Sin(q*Math.PI));f.Booster.Fin=Math.Sin(q*Math.PI*5)*20;f.Stage="FIN-GUIDED RETURN";}
        else if(t<56){double q=(t-49)/7;f.Booster=P(L(padX+dir*8,padX,q),L(home-90,home,q),L(-dir*2,0,q),65*Math.Sin(Math.PI*q));f.Booster.Fin=Math.Sin(q*Math.PI*3)*10*(1-q);f.ArmOpen=1-E((t-53)/3);f.Stage="BOOSTER LANDING BURN · CATCH";}
        else{f.Booster=P(padX,home);f.ArmOpen=0;f.Stage="BOOSTER SECURED · SHIP COAST";}
        if(t<27){double q=(t-18)/9;f.Ship=P(shipStartX+dir*(width*.48)*(q*q),shipStartY-(height+450)*(q*q),L(angle,dir*28,q),85);f.Ship.Scale=1-.2*E(q);}
        else {f.Ship.Visible=false;}
        if(t>=57&&t<64){f.ArmY=L(home+16,shipCatch+12,(t-57)/7);f.ArmOpen=E((t-57)/7);}
        else if(t>=64){f.ArmY=shipCatch+12;f.ArmOpen=1;}
        if(t>=61&&t<77){double q=(t-61)/16;f.Ship=P(L(width*.7,padX+40,q),L(-180,shipHome-180,q),L(78,88,q));f.Ship.Heat=Math.Sin(Math.PI*C(q*.85))*.95;f.Ship.Fin=Math.Sin(q*8)*12;f.Stage="SHIP REENTRY · HEAT SHIELD / PLASMA";}
        else if(t>=77&&t<84){double q=(t-77)/7;f.Ship=P(L(padX+40,padX+10,q),L(shipHome-180,shipHome-120,q),L(88,80,q));f.Ship.Heat=L(Math.Sin(Math.PI*.85)*.95,.03,q);f.Ship.Fin=Math.Sin(q*9)*18;f.Stage="BELLY-FIRST DESCENT · FLAP CONTROL";}
        else if(t>=84&&t<90){double q=(t-84)/6;f.Ship=P(L(padX+10,padX,q),L(shipHome-120,shipHome-65,q),L(80,0,q),L(0,60,q));f.Ship.Heat=.03*(1-E(q));f.Stage="SHIP FLIP · RAPTOR RELIGHT";}
        else if(t>=90&&t<96){double q=(t-90)/6;f.Ship=P(padX,L(shipHome-65,shipCatch,q),0,60*(1-E(q)));f.ArmOpen=1-E((t-93)/3);f.Stage="SHIP LANDING BURN · TOWER CATCH";}
        else if(t>=96&&t<101){f.Ship=P(padX,shipCatch);f.ArmOpen=0;f.Stage="SHIP SECURED · BOTH VEHICLES HOME";}
        else if(t>=101){double q=(t-101)/7;f.Ship=P(padX,L(shipCatch,shipHome,q));f.ArmY=L(shipCatch+12,home+16,q);f.ArmOpen=0;f.Attached=t>=108;f.Stage=t<108?"LOWERING SHIP · RESTACK":"FULL STACK · READY FOR NEXT FLIGHT";}
        return f;
    }
}
