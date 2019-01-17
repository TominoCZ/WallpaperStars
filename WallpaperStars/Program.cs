using System;
using System.Collections.Generic;
using System.Design;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using WindowUtils;

namespace WallpaperStars
{
	class Window : GameWindow
	{
		private Matrix4 _proj = Matrix4.Identity;

		private readonly List<Particle> _particles = new List<Particle>();

		private readonly Random _rand = new Random();

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			using (var w = new Window())
			{
				w.Run();
			}
		}

		public Window() : base(640, 480, new GraphicsMode(32, 8, 0, 4), "")
		{
			TargetUpdateFrequency = 60;
			TargetRenderFrequency = 60;

			VSync = VSyncMode.On;

			var thisScreen = Screen.FromPoint(Location);

			WindowUtil.SetAsWallpaper(WindowInfo.Handle);

			var screens = Screen.AllScreens;

			var minX = int.MaxValue;
			var minY = int.MaxValue;

			foreach (var screen in screens)
			{
				minX = Math.Min(thisScreen.Bounds.Location.X + screen.Bounds.Location.X, minX);
				minY = Math.Min(thisScreen.Bounds.Location.Y + screen.Bounds.Location.Y, minY);
			}

			Location = new Point(-minX, -minY);

			WindowBorder = WindowBorder.Hidden;
			Size = Screen.PrimaryScreen.Bounds.Size;
		}

		protected override void OnLoad(EventArgs e)
		{
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

			GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
			GL.Enable(EnableCap.Multisample);
		}

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			foreach (var particle in _particles)
			{
				particle.Update(e.Time);
			}

			SpawnParticle();
			SpawnParticle();
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			GL.MatrixMode(MatrixMode.Projection);
			var m = _proj;
			GL.LoadMatrix(ref m);

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			for (var index = _particles.Count - 1; index >= 0; index--)
			{
				var particle = _particles[index];

				particle.Render();

				if (particle.IsOffScreen)
					_particles.Remove(particle);
			}

			SwapBuffers();
		}

		protected override void OnResize(EventArgs e)
		{
			GL.Viewport(ClientRectangle);
			_proj = Matrix4.CreateOrthographicOffCenter(0, Width, Height, 0, 0, 1);
		}

		private void SpawnParticle()
		{
			float x = (float)_rand.NextDouble() * ClientSize.Width - ClientSize.Width / 2f;
			float y = (float)_rand.NextDouble() * ClientSize.Height - ClientSize.Height / 2f;
			float z = (float)_rand.NextDouble() * 50 + 100;

			var p = new Particle(this, x, y, z);

			_particles.Add(p);
		}
	}

	class Particle
	{
		private readonly Window _w;

		public bool IsOffScreen { get; private set; }

		public float X;
		public float Y;
		public float Z;

		private readonly float _startZ;
		private float _lastZ;

		public Particle(Window w, float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;

			_w = w;

			_startZ = z;
			_lastZ = z;
		}

		public void Update(double delta)
		{
			_lastZ = Z;
			Z = Math.Max(0, Z - (float)delta * 75);
		}

		public void Render()
		{
			var trailZ = Z - (Z - _lastZ) * 3;

			if (Z < 1 || trailZ == 0)
				return;

			var lastPx = _w.ClientSize.Width / 2f + (X / trailZ) * 60;
			var lastPy = _w.ClientSize.Height / 2f + (Y / trailZ) * 60;

			if (!_w.ClientRectangle.Contains((int)lastPx, (int)lastPy))
			{
				IsOffScreen = true;
				return;
			}

			var pX = _w.ClientSize.Width / 2f + (X / Z) * 60;
			var pY = _w.ClientSize.Height / 2f + (Y / Z) * 60;

			var a = -(_startZ + Z) / 150 * 360 * 1.75f;

			var c1 = HUE(a);
			var c2 = HUE(a + 30);

			c1.A = MathHelper.Clamp((_startZ - trailZ) / 50, 0, 1);
			c2.A = MathHelper.Clamp((_startZ - Z) / 50, 0, 1);

			GL.Begin(PrimitiveType.Lines);
			GL.Color4(c1);
			GL.Vertex2(lastPx, lastPy);
			GL.Color4(c2);
			GL.Vertex2(pX, pY);
			GL.End();

			//var radius = 50 / Z;
			//GL.Color3(1f, 0, 0);
			//GL.Begin(PrimitiveType.Polygon);
			//for (int i = 0; i < 32; i++)
			//{
			//	float angle = i / 32f * MathHelper.TwoPi;

			//	float x = (float)Math.Cos(angle) * radius;
			//	float y = (float)-Math.Sin(angle) * radius;

			//	GL.Vertex2(pX + x, pY + y);
			//}
			//GL.End();
		}

		private Color4 HUE(float angle)
		{
			var rad = MathHelper.DegreesToRadians(angle);

			var r = Math.Sin(rad) * 0.5 + 0.5;
			var g = Math.Sin(rad + MathHelper.ThreePiOver2) * 0.5 + 0.5;
			var b = Math.Sin(rad + MathHelper.ThreePiOver2 / 2) * 0.5 + 0.5;

			return new Color4((float)r, (float)g, (float)b, 1);
		}
	}

	static class GLU
	{
		public static void RenderQuad(float x, float y, float w, float h)
		{
			GL.Begin(PrimitiveType.Quads);
			GL.Vertex2(x, y);
			GL.Vertex2(x, y + h);
			GL.Vertex2(x + w, y + h);
			GL.Vertex2(x + w, y);
			GL.End();
		}
	}
}
