using System;
using System.Windows.Media;
using System.Windows.Threading;

partial class Starship {
    bool frameHooked;TimeSpan lastRenderStamp=TimeSpan.MinValue;
    bool FlightActive {get{return !paused&&window!=null&&window.IsVisible&&(elapsed>=0||boarding>0);}}
    void StartDriver(){
        timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(250)};
        timer.Tick+=(a,b)=>{ReconcileFrames();if(!frameHooked)PulseScene();};timer.Start();clock.Restart();ReconcileFrames();
    }
    void ReconcileFrames(){
        bool active=FlightActive;if(active==frameHooked)return;
        frameHooked=active;clock.Restart();lastRenderStamp=TimeSpan.MinValue;
        if(active)CompositionTarget.Rendering+=RenderFrame;else CompositionTarget.Rendering-=RenderFrame;
    }
    void RenderFrame(object sender,EventArgs e){
        if(!FlightActive){ReconcileFrames();return;}
        var stamp=((RenderingEventArgs)e).RenderingTime;if(stamp==lastRenderStamp)return;lastRenderStamp=stamp;PulseScene();
    }
    void PulseScene(){
        double dt=Math.Min(clock.Elapsed.TotalSeconds,.5);clock.Restart();
        if(window.IsVisible&&!moving)Advance(dt);
        information.Text=paused?"PAUSED":(elapsed>=0||boarding>0)?phase:randomLaunches?"NEXT LAUNCH  "+TimeSpan.FromSeconds(Math.Max(0,nextLaunch)).ToString(@"m\:ss"):"READY · MANUAL MODE";
        ReconcileFrames();
    }
    void StopDriver(){if(timer!=null)timer.Stop();if(frameHooked)CompositionTarget.Rendering-=RenderFrame;frameHooked=false;}
}
