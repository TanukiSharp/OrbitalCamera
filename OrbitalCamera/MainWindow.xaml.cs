using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace OrbitalCamera
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly OrbitalCameraController cameraController;

        public MainWindow()
        {
            InitializeComponent();

            SetWindowTitle();

            cameraController = new OrbitalCameraController(new Vector3D(5.0, 3.0, 5.0));

            mainViewport.Camera = cameraController.Camera;

            InitializeCubeAndLights();
        }

        private void SetWindowTitle()
        {
            int up = (RenderCapability.Tier >> 16);
            int dn = (RenderCapability.Tier & 0xFFFF);

            Title = string.Format("Orbital Camera - Tier: {0}-{1}", up, dn);
        }

        private static readonly Vector3D light1Direction = new Vector3D(-2.0, -3.0, -1.0);
        private static readonly Vector3D light2Direction = new Vector3D(4.0, 3.0, 4.0);

        private void OnCreateTriangleButtonClick(object sender, RoutedEventArgs e)
        {
            InitializePyramidAndLights();
        }

        private void OnCreateCubeButtonClick(object sender, RoutedEventArgs e)
        {
            InitializeCubeAndLights();
        }

        private void InitializePyramidAndLights()
        {
            mainViewport.Children.Clear();
            mainViewport.Children.Add(CreateLight(light1Direction));
            mainViewport.Children.Add(CreateLight(light2Direction));
            mainViewport.Children.Add(CreatePyramid());
            mainViewport.Children.Add(CreateTiledGround());
        }

        private void InitializeCubeAndLights()
        {
            mainViewport.Children.Clear();
            mainViewport.Children.Add(CreateLight(light1Direction));
            mainViewport.Children.Add(CreateLight(light2Direction));
            mainViewport.Children.Add(CreateCube());
            mainViewport.Children.Add(CreateTiledGround());
        }

        private static Visual3D CreateLight(Vector3D direction)
        {
            return new ModelVisual3D
            {
                Content = new DirectionalLight(Colors.White, direction)
            };
        }

        private static GeometryModel3D CreateTriangle(Point3D p0, Point3D p1, Point3D p2, Material material)
        {
            var mesh = new MeshGeometry3D();

            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);

            Vector3D normal = ComputeNormal(p0, p1, p2);

            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);

            return new GeometryModel3D(mesh, material)
            {
                BackMaterial = material
            };
        }

        private static Visual3D CreatePyramid()
        {
            var pyramid = new Model3DGroup();

            Point3D p0 = new Point3D(2.0, -1.0, 0.0);
            Point3D p1 = new Point3D(-1.0, -1.0, -1.732);
            Point3D p2 = new Point3D(-1.0, -1.0, 1.732);
            Point3D p3 = new Point3D(0.0, 1.732, 0.0);

            Material material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 0, 119, 170)));

            // Base.
            pyramid.Children.Add(CreateTriangle(p0, p1, p2, material));
            // Face 1.
            pyramid.Children.Add(CreateTriangle(p0, p1, p3, material));
            // Face 2.
            pyramid.Children.Add(CreateTriangle(p0, p2, p3, material));
            // Face 3.
            pyramid.Children.Add(CreateTriangle(p1, p2, p3, material));

            return new ModelVisual3D
            {
                Content = pyramid,
            };
        }

        private static Visual3D CreateCube()
        {
            var cube = new Model3DGroup();

            Point3D p0 = new Point3D(-1.0, -1.0, -1.0);
            Point3D p1 = new Point3D(1.0, -1.0, -1.0);
            Point3D p2 = new Point3D(1.0, -1.0, 1.0);
            Point3D p3 = new Point3D(-1.0, -1.0, 1.0);
            Point3D p4 = new Point3D(-1.0, 1.0, -1.0);
            Point3D p5 = new Point3D(1.0, 1.0, -1.0);
            Point3D p6 = new Point3D(1.0, 1.0, 1.0);
            Point3D p7 = new Point3D(-1.0, 1.0, 1.0);

            Material material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 0, 119, 170)));

            // Front side triangles.
            cube.Children.Add(CreateTriangle(p3, p2, p6, material));
            cube.Children.Add(CreateTriangle(p3, p6, p7, material));

            // Right side triangles.
            cube.Children.Add(CreateTriangle(p2, p1, p5, material));
            cube.Children.Add(CreateTriangle(p2, p5, p6, material));

            // Back side triangles.
            cube.Children.Add(CreateTriangle(p1, p0, p4, material));
            cube.Children.Add(CreateTriangle(p1, p4, p5, material));

            // Left side triangles.
            cube.Children.Add(CreateTriangle(p0, p3, p7, material));
            cube.Children.Add(CreateTriangle(p0, p7, p4, material));

            // Top side triangles.
            cube.Children.Add(CreateTriangle(p7, p6, p5, material));
            cube.Children.Add(CreateTriangle(p7, p5, p4, material));

            // Bottom side triangles.
            cube.Children.Add(CreateTriangle(p2, p3, p0, material));
            cube.Children.Add(CreateTriangle(p2, p0, p1, material));

            return new ModelVisual3D
            {
                Content = cube,
            };
        }

        private static Visual3D CreateTiledGround()
        {
            var blackBrush = new DrawingBrush
            {
                Stretch = Stretch.None,
                TileMode = TileMode.Tile,
                Viewport = new Rect(0.0, 0.0, 0.05, 0.05),
                ViewportUnits = BrushMappingMode.Absolute,
            };

            var drawing = new DrawingGroup();

            var whiteTileGeometry = new RectangleGeometry(new Rect(0.0, 0.0, 2.0, 2.0));
            var blackTileGeometry = new GeometryGroup();
            blackTileGeometry.Children.Add(new RectangleGeometry(new Rect(0.0, 0.0, 1.0, 1.0)));
            blackTileGeometry.Children.Add(new RectangleGeometry(new Rect(1.0, 1.0, 1.0, 1.0)));

            drawing.Children.Add(new GeometryDrawing
            {
                Brush = Brushes.LightGray,
                Geometry = whiteTileGeometry
            });

            drawing.Children.Add(new GeometryDrawing
            {
                Brush = Brushes.DarkGray,
                Geometry = blackTileGeometry
            });

            if (drawing.CanFreeze)
                drawing.Freeze();

            blackBrush.Drawing = drawing;

            if (blackBrush.CanFreeze)
                blackBrush.Freeze();

            var p0 = new Point3D(-100.0, -1.1, -100.0);
            var p1 = new Point3D(+100.0, -1.1, -100.0);
            var p2 = new Point3D(+100.0, -1.1, +100.0);
            var p3 = new Point3D(-100.0, -1.1, +100.0);

            GeometryModel3D triangle1 = CreateTriangle(p0, p1, p2, new DiffuseMaterial(blackBrush));
            GeometryModel3D triangle2 = CreateTriangle(p0, p2, p3, new DiffuseMaterial(blackBrush));

            var pointsCollection = (triangle1.Geometry as MeshGeometry3D).TextureCoordinates;
            pointsCollection.Add(new Point(0.0, 0.0));
            pointsCollection.Add(new Point(1.0, 0.0));
            pointsCollection.Add(new Point(1.0, 1.0));

            pointsCollection = (triangle2.Geometry as MeshGeometry3D).TextureCoordinates;
            pointsCollection.Add(new Point(0.0, 0.0));
            pointsCollection.Add(new Point(1.0, 1.0));
            pointsCollection.Add(new Point(0.0, 1.0));

            var square = new Model3DGroup();

            square.Children.Add(triangle1);
            square.Children.Add(triangle2);

            return new ModelVisual3D
            {
                Content = square,
            };
        }

        private static Vector3D ComputeNormal(Point3D p0, Point3D p1, Point3D p2)
        {
            Vector3D v0 = new Vector3D(p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
            Vector3D v1 = new Vector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);

            return Vector3D.CrossProduct(v0, v1);
        }

        private void OnViewportMouseUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(e.MouseDevice.Target, CaptureMode.None);
        }

        private void OnViewportMouseDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(e.MouseDevice.Target, CaptureMode.Element);
        }

        private void OnViewportMouseMove(object sender, MouseEventArgs e)
        {
            cameraController.Update(e);
        }
    }
}
