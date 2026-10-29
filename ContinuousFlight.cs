using System;

static class ContinuousFlight {
    static double Clamp(double q){return Math.Max(0,Math.Min(1,q));}
    static double Ease(double q){q=Clamp(q);return q*q*(3-2*q);}
    static double Curve(double t,double[] times,double[] values,bool flightAngle=false){
        if(t<=times[0])return values[0];if(t>=times[times.Length-1])return values[values.Length-1];int i=0;while(t>times[i+1])i++;
        double dt=times[i+1]-times[i],q=(t-times[i])/dt;
        double va=i==0?0:(values[i+1]-values[i-1])/(times[i+1]-times[i-1]);
        double vb=i+1==times.Length-1?0:(values[i+2]-values[i])/(times[i+2]-times[i]);
        if(flightAngle){if(i==2)va=(values[2]-values[1])/(times[2]-times[1]);if(i+1==2)vb=(values[2]-values[1])/(times[2]-times[1]);}
        return Hermite(values[i],values[i+1],va,vb,dt,q);
    }
    static double Hermite(double a,double b,double va,double vb,double dt,double q){return (2*q*q*q-3*q*q+1)*a+(q*q*q-2*q*q+q)*dt*va+(-2*q*q*q+3*q*q)*b+(q*q*q-q*q)*dt*vb;}
    static FlightProfile.Pose Booster(double t,double width,double height,double pad,double bottom){
        double home=bottom-FlightProfile.HomeOffset,dir=pad>width*.5?-1:1,travel=Math.Min(150,width*.22),sx=pad+dir*travel,sy=Math.Max(175,home-Math.Min(height*.42,home-175));
        double[] times={7,12,18,21,24,27,30,36,43,49,53,56};
        double[] x={pad,pad+dir*20,sx,sx+dir*40,sx+dir*85,sx+dir*40,sx-dir*20,pad+dir*70,pad+dir*35,pad+dir*8,pad+dir*2,pad};
        double[] y={home,home-70,sy,sy-50,sy-80,sy-115,sy-115,sy+45,home-180,home-90,home-25,home};
        double[] angles={0,dir*4,dir*13,-dir*65,-dir*145,-dir*165,-dir*160,dir*7,-dir*3,-dir*2,dir,0};
        // Interpolate the centre of mass, not a hinge at the top of the booster.
        for(int i=0;i<x.Length;i++){double a=angles[i]*Math.PI/180;x[i]-=112*Math.Sin(a);y[i]+=112*Math.Cos(a);}
        double angle=Curve(t,times,angles,true),rad=angle*Math.PI/180;
        return new FlightProfile.Pose{X=Curve(t,times,x)+112*Math.Sin(rad),Y=Curve(t,times,y)-112*Math.Cos(rad),Angle=angle,Scale=1,Visible=true};
    }
    static FlightProfile.Pose AttachedShip(FlightProfile.Pose booster){double a=booster.Angle*Math.PI/180;return new FlightProfile.Pose{X=booster.X+120*Math.Sin(a),Y=booster.Y-120*Math.Cos(a),Angle=booster.Angle,Scale=1,Visible=true};}
    public static void Apply(FlightProfile.Frame f,double t,double width,double height,double pad,double bottom){
        double home=bottom-FlightProfile.HomeOffset,shipHome=home-120,shipCatch=shipHome-50,dir=pad>width*.5?-1:1;
        if(t>=7&&t<56){f.Booster=Booster(t,width,height,pad,bottom);f.Booster.Fin=Math.Sin(t*2.1)*17*Ease((t-32)/4)*(1-Ease((t-52)/4));
            f.Booster.Flame=t<16?115:t<23?115*(1-Ease((t-16)/3)) + 18*(1-Ease((t-21)/2)):t<31?70*Ease((t-23)/1.5)*(1-Ease((t-28)/3)):72*Ease((t-47)/2)*(1-Ease((t-53)/3));
        }
        if(t>=7&&t<18){f.Ship=AttachedShip(f.Booster);f.Ship.Flame=85*Ease((t-17)/2);f.Attached=true;}
        else if(t>=18&&t<27){
            var start=AttachedShip(Booster(18,width,height,pad,bottom));var before=AttachedShip(Booster(17.999,width,height,pad,bottom));var after=AttachedShip(Booster(18.001,width,height,pad,bottom));double q=(t-18)/9;
            f.Ship=new FlightProfile.Pose{X=Hermite(start.X,start.X+dir*width*.5,(after.X-before.X)/.002,dir*55,9,q),Y=Hermite(start.Y,-500,(after.Y-before.Y)/.002,-180,9,q),Angle=Hermite(start.Angle,dir*28,(after.Angle-before.Angle)/.002,0,9,q),Scale=1-.2*Ease(q),Flame=85,Visible=true};
        }
        if(t>=61&&t<96){
            double[] times={61,67,72,77,82,84,87,90,93,96};
            double[] xs={width*.7,pad+100,pad+60,pad+40,pad+20,pad+10,pad+4,pad,pad,pad};
            double[] ys={-230,shipHome-420,shipHome-300,shipHome-180,shipHome-130,shipHome-120,shipHome-88,shipHome-65,shipHome-55,shipCatch};
            double[] angles={78,83,88,88,85,80,40,0,-1,0};
            f.Ship=new FlightProfile.Pose{X=Curve(t,times,xs),Y=Curve(t,times,ys),Angle=Curve(t,times,angles),Scale=1,Visible=true,Heat=.95*Ease((t-61)/6)*(1-Ease((t-75)/9)),Fin=Math.Sin(t*1.7)*13*Ease((t-61)/3)*(1-Ease((t-90)/6)),Flame=65*Ease((t-82)/4)*(1-Ease((t-92)/4))};
        }
        // Catch pins are separate from fins: booster pins at 68.5 m; ship pins at 40 m above its base.
        double boosterPin=home+(72-68.5)*224/72,shipPin=shipCatch-(40*224/72-120);
        if(t<57)f.ArmY=boosterPin;
        else if(t<64)f.ArmY=boosterPin+(shipPin-boosterPin)*Ease((t-57)/7);
        else if(t<101)f.ArmY=shipPin;
        else if(t<108)f.ArmY=f.Ship.Y-(40*224/72-120);
        else if(t<115)f.ArmY=(shipHome-(40*224/72-120))+(boosterPin-shipHome+(40*224/72-120))*Ease((t-108)/7);
        else f.ArmY=boosterPin;
        if(t>=108)f.ArmOpen=Ease((t-108)/1.5)*(1-Ease((t-115)/3));
    }
}
