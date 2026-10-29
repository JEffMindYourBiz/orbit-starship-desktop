using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

partial class Starship {
    readonly List<BitmapSource> blastFrames=new List<BitmapSource>();
    void LoadBlastFrames(){string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"effects","explosion");if(!Directory.Exists(folder))return;var files=Directory.GetFiles(folder,"*.png");Array.Sort(files,StringComparer.Ordinal);foreach(var file in files){var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.UriSource=new Uri(file);image.EndInit();image.Freeze();blastFrames.Add(image);}}
    void BlenderBlast(DrawingContext d,Point origin,double age){
        if(blastFrames.Count==0||age>3.8)return;double sample=Math.Min(blastFrames.Count-1,age*8),fraction=sample-Math.Floor(sample);int index=(int)sample;double opacity=age>2.8?Math.Max(0,3.8-age):1;var rectangle=new Rect(origin.X-210,origin.Y-230,420,420);
        d.PushOpacity(opacity*(1-fraction));d.DrawImage(blastFrames[index],rectangle);d.Pop();if(index+1<blastFrames.Count){d.PushOpacity(opacity*fraction);d.DrawImage(blastFrames[index+1],rectangle);d.Pop();}
    }
}
