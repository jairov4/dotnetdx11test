using System;
using System.Windows.Forms;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using Buffer = SlimDX.Direct3D11.Buffer;
using Device = SlimDX.Direct3D11.Device;
using Resource = SlimDX.Direct3D11.Resource;

namespace DiscoveringDirect3D11
{
	public partial class MainForm : Form
	{
		private const int vertexSize = sizeof (float)*5;
		private Texture2D backBuffer;
		private Device device11;
		private Effect effect;
		private InputLayout layout;
		private RenderTargetView renderTargetView;
		private SwapChain swapChain;

		public MainForm()
		{
			InitializeComponent();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			ResizeRenderTarget();
		}

		private void ResizeRenderTarget()
		{
			if (swapChain == null)
			{
				CreateDeviceAndSwapChain();
			}
			else
			{
				layout.Dispose();
				effect.Dispose();
				renderTargetView.Dispose();
				backBuffer.Dispose();
				swapChain.Dispose();
				device11.Dispose();
				device11 = null;
				CreateDeviceAndSwapChain();
			}

			backBuffer = Resource.FromSwapChain<Texture2D>(swapChain, 0);

			// Defining render view
			renderTargetView = new RenderTargetView(device11, backBuffer);
			device11.ImmediateContext.OutputMerger.SetTargets(renderTargetView);
			device11.ImmediateContext.Rasterizer.SetViewports(new Viewport(0, 0, ClientSize.Width, ClientSize.Height, 0.0f, 1.0f));
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			Render();
		}

		internal void Render()
		{
			if (device11 == null)
			{
				return;
			}

			var projectionMatrix = Matrix.Translation(50.0f * 2.0f / (float)ClientSize.Width, 0, 0);
			effect.GetVariableByName("finalMatrix").AsMatrix().SetMatrix(projectionMatrix);

			// Render
			device11.ImmediateContext.ClearRenderTargetView(renderTargetView, new Color4(1.0f, 0, 0, 1.0f));
			
			effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(device11.ImmediateContext);
			device11.ImmediateContext.DrawIndexed(6, 0, 0);
			swapChain.Present(0, PresentFlags.None);
		}
		
		private void CreateDeviceAndSwapChain()
		{
			renderTargetView?.Dispose();

			// Creating device (we accept dx10 cards or greater)
			FeatureLevel[] levels =
			{
				FeatureLevel.Level_11_0,
				FeatureLevel.Level_10_1,
				FeatureLevel.Level_10_0
			};

			// Defining our swap chain
			var desc = new SwapChainDescription();
			desc.BufferCount = 1;
			desc.Usage = Usage.BackBuffer | Usage.RenderTargetOutput;
			desc.ModeDescription = new ModeDescription(0, 0, new Rational(0, 0), Format.R8G8B8A8_UNorm);
			desc.SampleDescription = new SampleDescription(1, 0);
			desc.OutputHandle = Handle;
			desc.IsWindowed = true;
			desc.SwapEffect = SwapEffect.Discard;

			Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, levels, desc, out device11, out swapChain);
			
			// Preparing shaders
			PrepareShaders();

			// Creating geometry
			CreateGeometry();

			// Setting constants
			AffectConstants();
		}

		private void AffectConstants()
		{
			// Texture
			var texture2D = Texture2D.FromFile(device11, "yoda.jpg");
			var view = new ShaderResourceView(device11, texture2D);

			effect.GetVariableByName("yodaTexture").AsResource().SetResource(view);

			var rasterizerStateDescription = new RasterizerStateDescription {CullMode = CullMode.None, FillMode = FillMode.Solid};

			device11.ImmediateContext.Rasterizer.State = RasterizerState.FromDescription(device11, rasterizerStateDescription);

			// Matrices
			//var worldMatrix = Matrix.RotationY(0.5f);
			//var viewMatrix = Matrix.Translation(0, 0, 5.0f);
			//const float fov = 0.8f;
			//var projectionMatrix = Matrix.PerspectiveFovLH(fov, ClientSize.Width/(float) ClientSize.Height, 0.1f, 1000.0f);
			//effect.GetVariableByName("finalMatrix").AsMatrix().SetMatrix(worldMatrix * viewMatrix * projectionMatrix);
			
			var projectionMatrix = Matrix.Translation(50.0f * 2.0f / (float)ClientSize.Width, 0, 0);
			effect.GetVariableByName("finalMatrix").AsMatrix().SetMatrix(projectionMatrix);
		}

		private void PrepareShaders()
		{
			using (var byteCode = ShaderBytecode.CompileFromFile("Effet.fx", "bidon", "fx_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None))
			{
				effect = new Effect(device11, byteCode);
			}

			var technique = effect.GetTechniqueByIndex(0);
			var pass = technique.GetPassByIndex(0);
			layout = new InputLayout(device11, pass.Description.Signature, new[]
			{
				new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
				new InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0)
			});
		}

		private void CreateGeometry()
		{
			float[] vertices =
			{
				-1.0f, -1.0f, 0f, 0f, 1.0f,
				1.0f, -1.0f, 0f, 1.0f, 1.0f,
				1.0f, 1.0f, 0f, 1.0f, 0.0f,
				-1.0f, 1.0f, 0f, 0.0f, 0.0f
			};

			short[] faces =
			{
				(short) 0, (short) 1, (short) 2,
				(short) 0, (short) 2, (short) 3
			};

			var featureCount = 50;
			var vertices2 = new float[featureCount*4];
			var indices2 = new float[featureCount*6];

			var rnd = new Random();
			for (var i = 0; i < 5000000;)
			{
				var x = (float)rnd.NextDouble() * 250000;
				var y = (float)rnd.NextDouble() * 250000;
				var w = (float)rnd.NextDouble() * 3;
				var h = (float)rnd.NextDouble() * (float)Math.PI * 2;

				vertices2[i++] = x;
				vertices2[i++] = y;
				vertices2[i++] = w;
				vertices2[i++] = h;
			}

			// Creating vertex buffer
			var stream = new DataStream(4*vertexSize, true, true);
			stream.WriteRange(vertices);
			stream.Position = 0;

			var vertexBuffer = new Buffer(device11, stream, new BufferDescription
			{
				BindFlags = BindFlags.VertexBuffer,
				CpuAccessFlags = CpuAccessFlags.None,
				OptionFlags = ResourceOptionFlags.None,
				SizeInBytes = (int) stream.Length,
				Usage = ResourceUsage.Default
			});
			stream.Dispose();

			// Index buffer
			stream = new DataStream(6*2, true, true);
			stream.WriteRange(faces);
			stream.Position = 0;

			var indices = new Buffer(device11, stream, new BufferDescription
			{
				BindFlags = BindFlags.IndexBuffer,
				CpuAccessFlags = CpuAccessFlags.None,
				OptionFlags = ResourceOptionFlags.None,
				SizeInBytes = (int) stream.Length,
				Usage = ResourceUsage.Default
			});
			stream.Dispose();

			// Uploading to the device
			device11.ImmediateContext.InputAssembler.InputLayout = layout;
			device11.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
			device11.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, vertexSize, 0));
			device11.ImmediateContext.InputAssembler.SetIndexBuffer(indices, Format.R16_UInt, 0);
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			layout.Dispose();
			effect.Dispose();
			renderTargetView.Dispose();
			backBuffer.Dispose();
			swapChain.Dispose();
			device11.Dispose();
			device11 = null;
		}
	}
}