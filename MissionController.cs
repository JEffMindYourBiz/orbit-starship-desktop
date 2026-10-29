using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Forms=System.Windows.Forms;

partial class Starship {
    MissionScenario scenario=MissionScenario.Nominal;
    MissionStats stats;string statsPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"mission-stats.txt");
    bool incidentSent,takeoffCounted,crew;double boarding;
    TextBlock statsLabel;Point incidentOrigin;Window statsWindow;
    void SaveStats(){try{stats.Save(statsPath);}catch{}}
    void BeginMission(MissionScenario requested,bool test){
        if(!test&&(elapsed>=0||boarding>0))return;
        if(stats==null)stats=MissionStats.Load(statsPath);
        scenario=requested==MissionScenario.Automatic?MissionEffects.Choose(random):requested;
        crew=random.Next(5)==0;boarding=crew?18:0;elapsed=boarding>0?-1:0;
        incidentSent=false;takeoffCounted=false;paused=false;stats.Attempts++;if(test)stats.Tests++;if(crew)stats.CrewFlights++;SaveStats();
        frameMeter.Restart();observedFrames=0;sampleStart=0;sampleSaved=false;
        if(window!=null)window.Show();if(controls!=null)controls.Show();InvalidateVisual();
    }
    void StepMission(double dt){
        if(paused)return;
        if(boarding>0){boarding=Math.Max(0,boarding-dt);phase="CREW BOARDING · ACCESS ARM";if(boarding==0){stats.CrewBoarded+=3;SaveStats();elapsed=0;}InvalidateVisual();return;}
        if(elapsed<0){if(randomLaunches){nextLaunch-=dt;if(nextLaunch<=0)BeginMission(MissionScenario.Automatic,false);}return;}
        elapsed+=dt;
        double impact=MissionEffects.IncidentTime(scenario);
        if(!takeoffCounted&&elapsed>=7&&impact>=7){takeoffCounted=true;stats.Takeoffs++;SaveStats();}
        if(!incidentSent&&elapsed>=impact){
            incidentSent=true;var frame=FlightProfile.At(impact,ActualWidth,ActualHeight,padX,padBottom);ApplyFailureApproach(frame,impact,padBottom);
            var p=scenario==MissionScenario.ShipBreakup||scenario==MissionScenario.ShipHardLanding?frame.Ship:frame.Booster;
            incidentOrigin=new Point(p.X,p.Y+(scenario==MissionScenario.PadExplosion?160:70));
            if(scenario==MissionScenario.BoosterOffPad)incidentOrigin.X=Math.Max(60,Math.Min(ActualWidth-60,padX+180));
            SpawnWreckage(frame);stats.Failures++;int count;stats.Types.TryGetValue(scenario,out count);stats.Types[scenario]=count+1;SaveStats();AlertCompanion();
        }
        bool ended=incidentSent?elapsed>=impact+18:elapsed>=FlightProfile.Duration;
        if(ended){if(!incidentSent){stats.Successes++;SaveStats();}elapsed=-1;Schedule();incidentSent=false;scenario=MissionScenario.Nominal;}
        if(statsLabel!=null)statsLabel.Text=stats.Summary()+"\nLive redraw rate: "+observedFps.ToString("F0")+" fps";InvalidateVisual();
    }
    static void AlertCompanion(){try{using(var signal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\OrbitStarshipEmergency"))signal.Set();}catch{}}
    static void TestCompanionSignal(){using(var ack=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\OrbitCompanionEmergencyAck")){while(ack.WaitOne(0)){}AlertCompanion();bool ok=ack.WaitOne(3000);File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"companion-integration-test.txt"),ok?"PASS: running Companion received the Starship emergency signal and entered run-to-bed state.\r\n":"FAIL: running Companion did not acknowledge the emergency signal.\r\n");}}
    void AddMissionMenus(Forms.ContextMenuStrip menu){
        stats=MissionStats.Load(statsPath);
        menu.Items.Add("Clear wreckage",null,(a,b)=>ClearWreckage());menu.Items.Add("Mission statistics",null,(a,b)=>ShowStats());
        var tests=new Forms.ToolStripMenuItem("Test scenario");
        foreach(MissionScenario option in Enum.GetValues(typeof(MissionScenario))){if(option==MissionScenario.Automatic)continue;var chosen=option;tests.DropDownItems.Add(ScenarioName(option),null,(a,b)=>BeginMission(chosen,true));}
        tests.DropDownItems.Add(new Forms.ToolStripSeparator());tests.DropDownItems.Add("Crew boarding + normal flight",null,(a,b)=>CrewDemo());
        tests.DropDownItems.Add("Companion · run to bed",null,(a,b)=>AlertCompanion());menu.Items.Add(tests);
    }
    void CrewDemo(){BeginMission(MissionScenario.Nominal,true);if(!crew){crew=true;boarding=18;elapsed=-1;stats.CrewFlights++;SaveStats();}}
    static string ScenarioName(MissionScenario s){switch(s){case MissionScenario.Nominal:return "Successful full mission";case MissionScenario.PadExplosion:return "Pad explosion";case MissionScenario.HotStageFailure:return "Hot-stage breakup";case MissionScenario.BoosterHardLanding:return "Booster hard landing";case MissionScenario.BoosterOffPad:return "Booster misses the pad";case MissionScenario.ShipBreakup:return "Ship reentry breakup";case MissionScenario.ShipHardLanding:return "Ship hard landing";default:return s.ToString();}}
    void ShowStats(){if(statsWindow!=null){statsWindow.Activate();return;}statsLabel=new TextBlock{Text=stats.Summary(),Foreground=B("#D9E8EF"),FontSize=14,Margin=new Thickness(22),LineHeight=25};statsWindow=new Window{Title="Starship · mission statistics",Width=380,Height=480,Content=statsLabel,Background=B("#0D1C29"),Topmost=true,ResizeMode=ResizeMode.CanResize,WindowStartupLocation=WindowStartupLocation.CenterScreen};statsWindow.Closed+=(a,b)=>{statsWindow=null;statsLabel=null;};statsWindow.Show();if(area.Width>0){statsWindow.Left=area.Left+Math.Max(0,(area.Width-380)/2);statsWindow.Top=area.Top+Math.Max(0,(area.Height-480)/2);}}
}
