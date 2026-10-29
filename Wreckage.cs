using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

partial class Starship {
    void ApplyFailureApproach(FlightProfile.Frame frame,double time,double bottom){double impact=MissionEffects.IncidentTime(scenario);if((scenario==MissionScenario.BoosterHardLanding||scenario==MissionScenario.BoosterOffPad)&&time>46){double q=Ease((time-46)/(impact-46));if(scenario==MissionScenario.BoosterOffPad)frame.Booster.X+=180*q;double target=scenario==MissionScenario.BoosterOffPad?bottom-224:bottom-FlightProfile.HomeOffset;frame.Booster.Y+=(target-frame.Booster.Y)*q;frame.Booster.Angle+=q*12;frame.Booster.Flame*=1-q;}if(scenario==MissionScenario.ShipHardLanding&&time>87){double q=Ease((time-87)/5);frame.Ship.Y+=(bottom-370-frame.Ship.Y)*q;frame.Ship.Angle+=q*18;frame.Ship.Flame*=1-q;}}
    sealed class FragmentTemplate {public GeometryModel3D Model;public Point3D Centre;public double Radius;}
    sealed class FlyingPart {public GeometryModel3D Model;public Point Origin;public double Started,Radius;public int Seed;public Vector3D Axis;public double Spin;public RotateTransform3D SpinTransform;public TranslateTransform3D Position;}
    readonly List<FlyingPart> wreckage=new List<FlyingPart>();readonly Dictionary<bool,List<FragmentTemplate>> fragments=new Dictionary<bool,List<FragmentTemplate>>();
    readonly Stopwatch worldClock=Stopwatch.StartNew();Model3DGroup wreckageMesh;
    List<FragmentTemplate> Fragments(bool ship){
        List<FragmentTemplate> ready;if(fragments.TryGetValue(ship,out ready))return ready;ready=new List<FragmentTemplate>();
        var model=ship?shipMesh:boosterMesh;
        foreach(GeometryModel3D item in model.Children){var mesh=(MeshGeometry3D)item.Geometry;var buckets=new Dictionary<int,Int32Collection>();
            for(int i=0;i<mesh.TriangleIndices.Count;i+=3){var a=mesh.Positions[mesh.TriangleIndices[i]];var b=mesh.Positions[mesh.TriangleIndices[i+1]];var c=mesh.Positions[mesh.TriangleIndices[i+2]];double x=(a.X+b.X+c.X)/3,y=(a.Y+b.Y+c.Y)/3,z=(a.Z+b.Z+c.Z)/3;int band=(int)Math.Floor((y+240)/18),sector=(int)Math.Floor((Math.Atan2(z,x)+Math.PI)/(Math.PI*2)*6);int key=band*8+sector;Int32Collection indices;if(!buckets.TryGetValue(key,out indices)){indices=new Int32Collection();buckets[key]=indices;}indices.Add(mesh.TriangleIndices[i]);indices.Add(mesh.TriangleIndices[i+1]);indices.Add(mesh.TriangleIndices[i+2]);}
            foreach(var bucket in buckets.Values){if(bucket.Count<3)continue;var positions=new Point3DCollection();var normals=new Vector3DCollection();var uv=new PointCollection();var indices=new Int32Collection();var remap=new Dictionary<int,int>();foreach(int index in bucket){int mapped;if(!remap.TryGetValue(index,out mapped)){mapped=positions.Count;remap[index]=mapped;positions.Add(mesh.Positions[index]);normals.Add(mesh.Normals[index]);uv.Add(mesh.TextureCoordinates[index]);}indices.Add(mapped);}var geometry=new MeshGeometry3D{Positions=positions,Normals=normals,TextureCoordinates=uv,TriangleIndices=indices};geometry.Freeze();var box=geometry.Bounds;var centre=new Point3D(box.X+box.SizeX/2,box.Y+box.SizeY/2,box.Z+box.SizeZ/2);double radius=Math.Max(2,Math.Min(20,Math.Max(box.SizeX,Math.Max(box.SizeY,box.SizeZ))*.45));ready.Add(new FragmentTemplate{Model=new GeometryModel3D(geometry,item.Material){BackMaterial=item.BackMaterial},Centre=centre,Radius=radius});}
        }
        fragments[ship]=ready;return ready;
    }
    void SpawnWreckage(FlightProfile.Frame frame){
        if(wreckageMesh==null)return;bool both=scenario==MissionScenario.PadExplosion||scenario==MissionScenario.HotStageFailure;
        if(both||scenario==MissionScenario.ShipBreakup||scenario==MissionScenario.ShipHardLanding)SpawnParts(true,frame.Ship);
        if(both||scenario==MissionScenario.BoosterHardLanding||scenario==MissionScenario.BoosterOffPad)SpawnParts(false,frame.Booster);
    }
    void SpawnParts(bool ship,FlightProfile.Pose pose){
        var templates=Fragments(ship);var original=Placement(pose.X-ActualWidth/2,ActualHeight/2-pose.Y,pose.Angle,pose.Scale);double now=worldClock.Elapsed.TotalSeconds;int seedOffset=wreckage.Count;
        foreach(var template in templates){var start=original.Transform(template.Centre);var model=new GeometryModel3D(template.Model.Geometry,template.Model.Material){BackMaterial=template.Model.BackMaterial};var group=new Transform3DGroup();group.Children.Add(new TranslateTransform3D(-template.Centre.X,-template.Centre.Y,-template.Centre.Z));group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0,1,0),-30)));group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0,0,1),-pose.Angle)));var axis=new Vector3D(Math.Sin(seedOffset*5.7)+.3,Math.Cos(seedOffset*3.1)+.2,.7);axis.Normalize();var spin=new RotateTransform3D(new AxisAngleRotation3D(axis,0));var position=new TranslateTransform3D(start.X,start.Y,0);group.Children.Add(spin);group.Children.Add(position);model.Transform=group;wreckageMesh.Children.Add(model);wreckage.Add(new FlyingPart{Model=model,Origin=new Point(start.X+ActualWidth/2,ActualHeight/2-start.Y),Started=now,Radius=template.Radius,Seed=seedOffset++,Axis=axis,Spin=120+(seedOffset%9)*24,SpinTransform=spin,Position=position});}
    }
    void UpdateWreckage(){
        double now=worldClock.Elapsed.TotalSeconds;foreach(var piece in wreckage){double age=now-piece.Started;if(age>19)continue;var p=MissionEffects.Debris(piece.Origin,piece.Seed,age,ActualHeight-piece.Radius-6,ActualWidth);piece.Position.OffsetX=p.X-ActualWidth/2;piece.Position.OffsetY=ActualHeight/2-p.Y;piece.SpinTransform.Rotation=new AxisAngleRotation3D(piece.Axis,Math.Min(age,8)*piece.Spin);}
    }
    void ClearWreckage(){wreckage.Clear();if(wreckageMesh!=null)wreckageMesh.Children.Clear();InvalidateVisual();}
}
