using System;
using System.Windows;
using System.Windows.Media;

partial class Starship {
    sealed class CrewOverlay:FrameworkElement {readonly Starship owner;public CrewOverlay(Starship owner){this.owner=owner;IsHitTestVisible=false;}protected override void OnRender(DrawingContext d){owner.DrawLift(d);}}
    CrewOverlay crewOverlay;
    void DrawLift(DrawingContext d){
        double x=Double.IsNaN(padX)?ActualWidth*.62:padX,bottom=Double.IsNaN(padBottom)?ActualHeight-115:padBottom;
        double floor=bottom-12,door=bottom-FlightProfile.HomeOffset-120+42,liftX=x-72,time=18-boarding;
        double liftY=boarding>0?floor+(door-floor)*Ease((time-5)/7):elapsed>=0&&elapsed<6?door+(floor-door)*Ease(elapsed/6):floor;
        Line(d,"#617A83",.65,liftX-8,door-26,liftX-8,floor+2);Line(d,"#617A83",.65,liftX+8,door-26,liftX+8,floor+2);
        d.DrawRoundedRectangle(B("#D82A3A43"),new Pen(B("#A8B7B8"),.8),new Rect(liftX-9,liftY-24,18,25),2,2);d.DrawRectangle(B("#773E6475"),null,new Rect(liftX-7,liftY-21,14,19));Line(d,"#CFDAD7",1,liftX-9,liftY+1,liftX+9,liftY+1);d.DrawEllipse(B("#7CF0BC"),null,new Point(liftX+6,liftY-22),1,1);
        double bridge=boarding>0?1:elapsed>=0?1-Ease(elapsed/3):0;
        if(bridge>0){Line(d,"#AAB7B8",2,liftX+8,door+1,liftX+8+(64-8)*bridge,door+1);Line(d,"#7C929B",.6,liftX+8,door-10,liftX+8+(64-8)*bridge,door-10);for(int n=0;n<7;n++)Line(d,"#899FA5",.6,liftX+8+n*8*bridge,door-10,liftX+8+n*8*bridge,door+1);}
        if(boarding<=0)return;
        for(int i=0;i<3;i++){
            double xx,yy,walk=0;
            if(time<5){double q=Ease((time-i*.45)/(5-i*.45));xx=x+95+i*8+(liftX+i*3-x-95-i*8)*q;yy=floor;walk=1;}
            else if(time<12){xx=liftX-4+i*4;yy=liftY;}
            else{double q=Ease((time-12-i*.55)/3.2);if(q>=1)continue;xx=liftX-4+i*4+(x-7-liftX+4-i*4)*q;yy=door;walk=1;}
            double step=Math.Sin(time*12-i*2)*1.6*walk;
            d.DrawRoundedRectangle(B("#ECEFEA"),new Pen(B("#9FAFB7"),.4),new Rect(xx-2.5,yy-11,5,7),1.4,1.4);d.DrawEllipse(B("#F7F5F0"),new Pen(B("#9DADB4"),.4),new Point(xx,yy-14),3.3,3.3);d.DrawRoundedRectangle(B("#26343E"),null,new Rect(xx-2.8,yy-15.5,5.6,2.6),1,1);Line(d,"#E8EEEE",1.5,xx-1,yy-4,xx-2-step,yy);Line(d,"#E8EEEE",1.5,xx+1,yy-4,xx+2+step,yy);Line(d,"#DDE6E8",1.1,xx-2,yy-10,xx-4+step,yy-6);Line(d,"#DDE6E8",1.1,xx+2,yy-10,xx+4-step,yy-6);
        }
    }
}
