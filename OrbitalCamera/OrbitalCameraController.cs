using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace OrbitalCamera
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct OrbitalCameraControllerOptions
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public double MinimumDirectionLength;
        public double MoveRatio;
        public double RotationRatio;
        public double RotationEpsilon;
        public double ZoomRatio;
        public double MinimumZoom;
        public double MaximumZoom;

        public static readonly OrbitalCameraControllerOptions Default = new OrbitalCameraControllerOptions()
        {
            MinimumDirectionLength = 0.001,
            MoveRatio = 0.0025,
            RotationRatio = 0.01,
            ZoomRatio = 0.005,
            MinimumZoom = 0.9,
            MaximumZoom = 1.2,
            RotationEpsilon = 0.001,
        };
    }

    public class OrbitalCameraController
    {
        private readonly OrbitalCameraControllerOptions options;
        private readonly PerspectiveCamera camera = new PerspectiveCamera();

        private Vector3D position;
        private Vector3D up;
        private Vector3D target;

        private double viewDirectionLength;
        private Vector3D viewVector;
        private Vector3D rightVector;
        private Vector3D upVector;

        private Point mousePreviousPosition;
        private readonly bool[] isMouseDown = new bool[3];

        /// <summary>
        /// Creates a default orbital camera controller, setting the camera on the position (0;0;10), targeting (0;0;0).
        /// </summary>
        public OrbitalCameraController()
            : this(OrbitalCameraControllerOptions.Default)
        {
        }

        /// <summary>
        /// Creates an orbital camera controller, targeting (0;0;0).
        /// </summary>
        /// <param name="position">Start position of the camera.</param>
        public OrbitalCameraController(Vector3D position)
            : this(position, new Vector3D())
        {
        }

        /// <summary>
        /// Creates an orbital camera controller, setting the camera on the position (0;0;10), targeting (0;0;0).
        /// </summary>
        /// <param name="options">Options to control camera initial state and updates.</param>
        public OrbitalCameraController(OrbitalCameraControllerOptions options)
            : this(new Vector3D(0.0, 0.0, 10.0), options)
        {
        }

        /// <summary>
        /// Creates an orbital camera controller, targeting (0;0;0).
        /// </summary>
        /// <param name="position">Start position of the camera.</param>
        /// <param name="options">Options to control camera initial state and updates.</param>
        public OrbitalCameraController(Vector3D position, OrbitalCameraControllerOptions options)
            : this(position, new Vector3D(), options)
        {
        }

        /// <summary>
        /// Creates an orbital camera controller.
        /// </summary>
        /// <param name="position">Position of the camera.</param>
        /// <param name="target">Location the camera targets.</param>
        public OrbitalCameraController(Vector3D position, Vector3D target)
            : this(position, target, OrbitalCameraControllerOptions.Default)
        {
        }

        /// <summary>
        /// Creates an orbital camera controller.
        /// </summary>
        /// <param name="position">Position of the camera.</param>
        /// <param name="target">Location the camera targets.</param>
        /// <param name="options">Options to control camera initial state and updates.</param>
        public OrbitalCameraController(Vector3D position, Vector3D target, OrbitalCameraControllerOptions options)
        {
            this.position = position;
            this.target = target;
            this.options = options;

            up = new Vector3D(0.0, 1.0, 0.0);

            UpdateParameters();
            UpdateInnerCamera();
        }

        /// <summary>
        /// Gets the subjacent Camera instance.
        /// </summary>
        public Camera Camera
        {
            get
            {
                return camera;
            }
        }

        /// <summary>
        /// Gets or sets the position of the camera.
        /// </summary>
        public Vector3D Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;

                UpdateParameters();
                UpdateInnerCamera();
            }
        }

        /// <summary>
        /// Gets or sets the location the camera targets.
        /// </summary>
        public Vector3D Target
        {
            get
            {
                return target;
            }
            set
            {
                target = value;

                UpdateParameters();
                UpdateInnerCamera();
            }
        }

        /// <summary>
        /// Gets or sets the up base vector of the camera.
        /// </summary>
        public Vector3D Up
        {
            get
            {
                return up;
            }
            set
            {
                up = value;

                UpdateParameters();
                UpdateInnerCamera();
            }
        }

        /// <summary>
        /// Gets the view vector (normalized) of the camera.
        /// </summary>
        public Vector3D ViewVector
        {
            get
            {
                return viewVector;
            }
        }

        /// <summary>
        /// Gets the up resulting vector (normalized) of the camera.
        /// </summary>
        public Vector3D UpVector
        {
            get
            {
                return upVector;
            }
        }

        /// <summary>
        /// Gets the right direction vector (normalized) of the camera.
        /// </summary>
        public Vector3D RightVector
        {
            get
            {
                return rightVector;
            }
        }

        /// <summary>
        /// Moves the camera in the given direction.
        /// </summary>
        /// <param name="direction">Direction vector to move the camera. Intensity of the vector is taken into account.</param>
        public void Move(Vector3D direction)
        {
            position += direction;
            target += direction;

            UpdateParameters();
            UpdateInnerCamera();
        }

        /// <summary>
        /// Rotates the camera around the target point.
        /// </summary>
        /// <param name="ry">Rotation angle around the Y axis, in radians.</param>
        /// <param name="rx">Rotation angle around the X axis, in radians.</param>
        public void Rotate(double ry, double rx)
        {
            double f = Math.Acos(Vector3D.DotProduct(viewVector, up));

            if (rx > 0.0)
                f = Math.Min(rx, f - options.RotationEpsilon);
            else
                f = Math.Max(rx, f - (float)Math.PI + options.RotationEpsilon);

            Transform3DGroup transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(up, ry * 180.0 / Math.PI)));
            transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(rightVector, f * 180.0 / Math.PI)));

            Matrix3D combinedTransformes = transformGroup.Value;

            viewVector *= combinedTransformes;
            position = target - viewVector * viewDirectionLength;

            UpdateParameters();
            UpdateInnerCamera();
        }

        /// <summary>
        /// Zooms in or out.
        /// </summary>
        /// <remarks>This moves the camera forward/backward along the view vector, so camera position is modified when performing a zoom.</remarks>
        /// <param name="zoom">Moving coeficient relative to the distance between camera position and target (unitless).</param>
        public void Zoom(double zoom)
        {
            double len = viewDirectionLength * zoom;
            len = Math.Max(options.MinimumDirectionLength, len);
            viewVector.Normalize();
            position = target - viewVector * len;

            UpdateParameters();
            UpdateInnerCamera();
        }

        /// <summary>
        /// Recomputes the <see cref="ViewVector"/>, <see cref="RightVector"/> and <see cref="UpVector"/> properties.
        /// </summary>
        private void UpdateParameters()
        {
            viewVector = target - position;
            viewDirectionLength = viewVector.Length;

            rightVector = Vector3D.CrossProduct(viewVector, up);
            rightVector.Normalize();

            upVector = Vector3D.CrossProduct(rightVector, viewVector);
            upVector.Normalize();

            viewVector.Normalize();
        }

        private void UpdateInnerCamera()
        {
            camera.Position = new Point3D(position.X, position.Y, position.Z);
            camera.LookDirection = viewVector;
            camera.UpDirection = up;
        }

        /// <summary>
        /// Updates the camera parameters.
        /// </summary>
        /// <param name="e">The mouse event to update the camera.</param>
        public void Update(MouseEventArgs e)
        {
            if (e == null)
                return;

            Point pos = MouseCheck(e);

            // Compute mouse movement delta.
            double mousedx = pos.X - mousePreviousPosition.X;
            double mousedy = pos.Y - mousePreviousPosition.Y;

            // Rotate camera.
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                double dx = options.RotationRatio * mousedx;
                double dy = options.RotationRatio * mousedy;
                Rotate(-dx, -dy);
            }

            // Translate camera.
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                Vector3D vec = rightVector * -mousedx + upVector * mousedy;
                Move(vec * viewDirectionLength * options.MoveRatio);
            }

            // Zoom.
            if (e.RightButton == MouseButtonState.Pressed)
            {
                double z = 1.0 + mousedy * options.ZoomRatio;
                z = Math.Max(options.MinimumZoom, Math.Min(z, options.MaximumZoom));
                Zoom(z);
            }

            // Saves the current position to use it again on next frame.
            mousePreviousPosition = pos;
        }

        /// <summary>
        /// Checks mouse status to avoid camera misplacement when mouse moves while a model window disappears.
        /// </summary>
        /// <param name="e">Mouse event information.</param>
        /// <returns>Returns the current mouse pointer location.</returns>
        private Point MouseCheck(MouseEventArgs e)
        {
            MouseCheckElement(e, e.LeftButton, 0);
            MouseCheckElement(e, e.MiddleButton, 1);
            MouseCheckElement(e, e.RightButton, 2);

            return e.GetPosition(e.MouseDevice.Target);
        }

        /// <summary>
        /// Checks a specific mouse button and stores the proper button state.
        /// </summary>
        /// <param name="e">Mouse event information.</param>
        /// <param name="button">Specific button to check.</param>
        /// <param name="index">Index of the button (for internal use).</param>
        private void MouseCheckElement(MouseEventArgs e, MouseButtonState button, int index)
        {
            if (button == MouseButtonState.Pressed)
            {
                if (isMouseDown[index] == false)
                {
                    mousePreviousPosition = e.GetPosition(e.MouseDevice.Target);
                    isMouseDown[index] = true;
                }
            }
            else
            {
                isMouseDown[index] = false;
            }
        }
    }
}
