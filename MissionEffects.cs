using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;

enum MissionScenario {Automatic,Nominal,PadExplosion,HotStageFailure,BoosterHardLanding,BoosterOffPad,ShipBreakup,ShipHardLanding}
sealed class MissionStats {
    public int Attempts,Takeoffs,Successes,Failures,CrewFlights,CrewBoarded,Tests;
    public readonly Dictionary<MissionScenario,int> Types=new Dictionary<MissionScenario,int>();
    public static MissionStats Load(string path){var s=new MissionStats();try{foreach(var line in File.ReadAllLines(path)){var p=line.Split('=');int n;if(p.Length!=2||!int.TryParse(p[1],out n)||n<0)continue;switch(p[0]){case "attempts":s.Attempts=n;break;case "takeoffs":s.Takeoffs=n;break;case "successes":s.Successes=n;break;case "failures":s.Failures=n;break;case "crewFlights":s.CrewFlights=n;break;case "crewBoarded":s.CrewBoarded=n;break;case "tests":s.Tests=n;break;default:MissionScenario t;if(Enum.TryParse(p[0],out t))s.Types[t]=n;break;}}}catch{}return s;}
    public void Save(string path){var lines=new List<string>{"attempts="+Attempts,"takeoffs="+Takeoffs,"successes="+Successes,"failures="+Failures,"crewFlights="+CrewFlights,"crewBoarded="+CrewBoarded,"tests="+Tests};foreach(var t in Types)lines.Add(t.Key+"="+t.Value);string temp=path+".tmp";File.WriteAllLines(temp,lines);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
    public string Summary(){string text="Mission attempts: "+Attempts+"\nActual takeoffs: "+Takeoffs+"\nSuccessful full returns: "+Successes+"\nFailed missions: "+Failures+"\nCrewed missions: "+CrewFlights+"\nAstronauts boarded: "+CrewBoarded+"\nManual scenario tests (included above): "+Tests;foreach(var t in Types)text+="\n"+t.Key+": "+t.Value;return text;}
}
static class MissionEffects {
    public static MissionScenario Choose(Random random){return random.Next(10)==0?(MissionScenario)(2+random.Next(6)):MissionScenario.Nominal;}
    public static double IncidentTime(MissionScenario s){switch(s){case MissionScenario.PadExplosion:return 5.5;case MissionScenario.HotStageFailure:return 20;case MissionScenario.BoosterHardLanding:return 53;case MissionScenario.BoosterOffPad:return 52;case MissionScenario.ShipBreakup:return 73;case MissionScenario.ShipHardLanding:return 92;default:return double.PositiveInfinity;}}
    public static Point Debris(Point origin,int i,double age,double floor,double width){
        double vx=Math.Sin(i*19.37)*Math.Max(190,width*.45),vy=-65-Math.Abs(Math.Cos(i*7.13))*230;
        double x=origin.X+vx*age,y=origin.Y+vy*age+110*age*age;
        if(y>floor){double impact=(-vy+Math.Sqrt(vy*vy+440*Math.Max(0,floor-origin.Y)))/220;double after=Math.Max(0,age-impact);y=floor-Math.Abs(Math.Sin(after*4.1+i*.2))*Math.Max(0,28-after*12);x=origin.X+vx*impact+vx*.17*(1-Math.Exp(-after*1.5));}
        x=Math.Abs(x)%(Math.Max(1,width)*2);if(x>width)x=width*2-x;
        return new Point(Math.Max(2,Math.Min(width-2,x)),Math.Min(floor,y));
    }
}
