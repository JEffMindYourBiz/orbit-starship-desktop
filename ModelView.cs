using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;

partial class Starship {
    Grid sceneHost;Viewport3D modelView;OrthographicCamera modelCamera;
    Model3DGroup shipMesh,boosterMesh,towerMesh,armMesh;
    readonly List<Tuple<GeometryModel3D,Vector3D,Point3D>> movingParts=new List<Tuple<GeometryModel3D,Vector3D,Point3D>>();
    readonly List<Tuple<GeometryModel3D,int>> armParts=new List<Tuple<GeometryModel3D,int>>();
    Grid PrepareModels(){
        sceneHost=new Grid();sceneHost.Children.Add(this);
        string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"models");
        shipMesh=ReadModel(Path.Combine(folder,"ship.orbmesh"));boosterMesh=ReadModel(Path.Combine(folder,"booster.orbmesh"));towerMesh=ReadModel(Path.Combine(folder,"tower.orbmesh"));armMesh=ReadModel(Path.Combine(folder,"arms.orbmesh"));
        SplitArms();
        modelView=new Viewport3D{IsHitTestVisible=false,ClipToBounds=false};modelCamera=new OrthographicCamera(new Point3D(0,0,500),new Vector3D(0,0,-1),new Vector3D(0,1,0),700){NearPlaneDistance=350,FarPlaneDistance=650};modelView.Camera=modelCamera;
        var scene=new Model3DGroup();scene.Children.Add(new AmbientLight(Color.FromRgb(96,106,124)));scene.Children.Add(new DirectionalLight(Color.FromRgb(255,245,225),new Vector3D(-.65,-.25,-1)));scene.Children.Add(new DirectionalLight(Color.FromRgb(138,177,215),new Vector3D(.8,.2,-.5)));
        wreckageMesh=new Model3DGroup();scene.Children.Add(wreckageMesh);scene.Children.Add(towerMesh);scene.Children.Add(armMesh);scene.Children.Add(shipMesh);scene.Children.Add(boosterMesh);modelView.Children.Add(new ModelVisual3D{Content=scene});sceneHost.Children.Add(modelView);crewOverlay=new CrewOverlay(this);sceneHost.Children.Add(crewOverlay);LoadBlastFrames();return sceneHost;
    }
    void SplitArms(){var children=new List<GeometryModel3D>();foreach(GeometryModel3D original in armMesh.Children){var mesh=(MeshGeometry3D)original.Geometry;for(int side=-1;side<=1;side+=2){var indices=new Int32Collection();for(int i=0;i<mesh.TriangleIndices.Count;i+=3){double z=mesh.Positions[mesh.TriangleIndices[i]].Z+mesh.Positions[mesh.TriangleIndices[i+1]].Z+mesh.Positions[mesh.TriangleIndices[i+2]].Z;if((z>=0?1:-1)!=side)continue;indices.Add(mesh.TriangleIndices[i]);indices.Add(mesh.TriangleIndices[i+1]);indices.Add(mesh.TriangleIndices[i+2]);}if(indices.Count==0)continue;var geometry=new MeshGeometry3D{Positions=mesh.Positions,Normals=mesh.Normals,TextureCoordinates=mesh.TextureCoordinates,TriangleIndices=indices};geometry.Freeze();var model=new GeometryModel3D(geometry,original.Material){BackMaterial=original.BackMaterial};children.Add(model);armParts.Add(Tuple.Create(model,side));}}armMesh.Children.Clear();foreach(var model in children)armMesh.Children.Add(model);}
    Model3DGroup ReadModel(string file){
        var group=new Model3DGroup();using(var reader=new BinaryReader(File.OpenRead(file))){int magic=reader.ReadInt32();if(magic!=0x4F524234)throw new InvalidDataException("Unsupported 3D model format");int count=reader.ReadInt32();if(count<1||count>1000)throw new InvalidDataException("Invalid mesh count");for(int m=0;m<count;m++){
            string name=reader.ReadString(),texture=reader.ReadString();float r=reader.ReadSingle(),g=reader.ReadSingle(),b=reader.ReadSingle(),a=reader.ReadSingle(),metal=reader.ReadSingle(),rough=reader.ReadSingle();
            int vertices=reader.ReadInt32(),indices=reader.ReadInt32();if(vertices<0||vertices>3000000||indices<0||indices>9000000)throw new InvalidDataException("Invalid mesh size");
            var mesh=new MeshGeometry3D();var positions=new Point3DCollection(vertices);var normals=new Vector3DCollection(vertices);var uv=new PointCollection(vertices);for(int i=0;i<vertices;i++){positions.Add(new Point3D(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()));normals.Add(new Vector3D(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()));uv.Add(new Point(reader.ReadSingle(),reader.ReadSingle()));}var triangles=new Int32Collection(indices);for(int i=0;i<indices;i++){int index=reader.ReadInt32();if(index<0||index>=vertices)throw new InvalidDataException("Mesh index out of range");triangles.Add(index);}for(int v=0;v<positions.Count;v++)positions[v]=positions[v]+normals[v]*(String.IsNullOrEmpty(texture)?-.015-m*.0001:.035+m*.0002);mesh.Positions=positions;mesh.Normals=normals;mesh.TextureCoordinates=uv;mesh.TriangleIndices=triangles;mesh.Freeze();
            Brush diffuse=new SolidColorBrush(Color.FromScRgb(1,Math.Max(.02f,r),Math.Max(.02f,g),Math.Max(.02f,b)));

            if(name.ToLowerInvariant().Contains("logo")||name.ToLowerInvariant().Contains("serial"))continue;if(name.ToLowerInvariant().Contains("heatshield")||name.ToLowerInvariant().Contains("tiles"))diffuse=new SolidColorBrush(Color.FromRgb(27,31,35));else if(metal>.3&&(r+g+b)/3>.08)diffuse=new SolidColorBrush(Color.FromRgb(158,169,176));diffuse.Freeze();var material=new MaterialGroup();material.Children.Add(new DiffuseMaterial(diffuse));if(metal>.2){var shine=new SolidColorBrush(Color.FromArgb((byte)(100+metal*100),224,235,242));shine.Freeze();material.Children.Add(new SpecularMaterial(shine,Math.Max(12,(1-rough)*110)));}material.Freeze();var model=new GeometryModel3D(mesh,material){BackMaterial=material};group.Children.Add(model);
            if(name.StartsWith("FIN_")||name.StartsWith("FLAP_")){var box=mesh.Bounds;movingParts.Add(Tuple.Create(model,name.StartsWith("FIN_")?new Vector3D(1,0,0):new Vector3D(0,1,0),new Point3D(box.X+box.SizeX/2,box.Y+box.SizeY/2,box.Z+box.SizeZ/2)));}
        }}return group;
    }
    static Transform3D Placement(double x,double y,double angle,double scale,double yaw=-30){var group=new Transform3DGroup();group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0,1,0),yaw)));group.Children.Add(new ScaleTransform3D(scale,scale,scale));group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0,0,1),-angle)));group.Children.Add(new TranslateTransform3D(x,y,0));return group;}
    void UpdateModels(FlightProfile.Frame frame,double x,double bottom){
        if(modelView==null)return;modelCamera.Width=ActualWidth;
        shipMesh.Transform=Placement(frame.Ship.X-ActualWidth/2,ActualHeight/2-frame.Ship.Y,frame.Ship.Angle,frame.Ship.Visible?frame.Ship.Scale:0);
        boosterMesh.Transform=Placement(frame.Booster.X-ActualWidth/2,ActualHeight/2-frame.Booster.Y,frame.Booster.Angle,frame.Booster.Visible?frame.Booster.Scale:0);
        towerMesh.Transform=Placement(x-ActualWidth/2,ActualHeight/2-bottom,0,1);armMesh.Transform=Placement(x-ActualWidth/2,ActualHeight/2-frame.ArmY,0,1);
        foreach(var arm in armParts)arm.Item1.Transform=new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0,1,0),-arm.Item2*frame.ArmOpen*23),new Point3D(-24*224/72.0,0,arm.Item2*4.2*224/72));
        UpdateWreckage();if(crewOverlay!=null)crewOverlay.InvalidateVisual();foreach(var part in movingParts){double angle=part.Item2.X==1?frame.Booster.Fin:frame.Ship.Fin;part.Item1.Transform=new RotateTransform3D(new AxisAngleRotation3D(part.Item2,angle),part.Item3);}
    }
}
