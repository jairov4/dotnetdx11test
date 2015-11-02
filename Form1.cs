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
		private const int vertexSize = sizeof (float)*2;
		private Texture2D backBuffer;
		private Device device11;
		private Effect effect;
		private InputLayout layout;
		private RenderTargetView renderTargetView;
		private SwapChain swapChain;
		private Buffer indicesb2;
		private Buffer indicesb1;
		private const int featureCount = 150000;

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
				indicesb1.Dispose();
				indicesb2.Dispose();
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

		protected override void OnPaintBackground(PaintEventArgs e)
		{
		}

		internal void Render()
		{
			if (device11 == null)
			{
				return;
			}

			//AffectConstants();

			// Render
			device11.ImmediateContext.ClearRenderTargetView(renderTargetView, new Color4(1.0f, 0, 0, 1.0f));

			effect.GetVariableByName("color").AsVector().Set(new Color4(1.0f, 1.0f, 1.0f));
			effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(device11.ImmediateContext);

			device11.ImmediateContext.InputAssembler.SetIndexBuffer(indicesb1, Format.R32_UInt, 0);
			device11.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
			device11.ImmediateContext.DrawIndexed(featureCount*6, 0, 0);

			effect.GetVariableByName("color").AsVector().Set(new Color4(0.0f, 0.0f, 0.0f));
			effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(device11.ImmediateContext);

			device11.ImmediateContext.InputAssembler.SetIndexBuffer(indicesb2, Format.R32_UInt, 0);
			device11.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
			device11.ImmediateContext.DrawIndexed(featureCount*8, 0, 0);

			swapChain.Present(0, PresentFlags.None);

			Invalidate();
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
			// Matrices
			//var worldMatrix = Matrix.RotationY(0.5f);
			//var viewMatrix = Matrix.Translation(0, 0, 5.0f);
			//const float fov = 0.8f;
			//var projectionMatrix = Matrix.PerspectiveFovLH(fov, ClientSize.Width/(float) ClientSize.Height, 0.1f, 1000.0f);
			//effect.GetVariableByName("finalMatrix").AsMatrix().SetMatrix(worldMatrix * viewMatrix * projectionMatrix);
			
			// Matrix.Translation(50.0f * 2.0f / (float)ClientSize.Width, 0, 0);
			var projectionMatrix = Matrix.Scaling(2.0f/250000.0f, 2.0f/(2*(float) Math.PI), 1.0f)*
									Matrix.Translation(-1f, -1f, 0.0f);
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
				new InputElement("POSITION", 0, Format.R32G32_Float, 0, 0)
			});

			// Texture
			var rasterizerStateDescription = new RasterizerStateDescription { CullMode = CullMode.None, FillMode = FillMode.Solid };

			var rstate = RasterizerState.FromDescription(device11, rasterizerStateDescription);
			device11.ImmediateContext.Rasterizer.State = rstate;
			rstate.Dispose();
		}

		private void CreateGeometry()
		{
			var rnd = new Random();

			var vertices2 = new float[featureCount*4*2];
			for (var i = 0; i < vertices2.Length;)
			{
				var x = (float)rnd.NextDouble() * 250000;
				var y = (float)rnd.NextDouble() * (float)Math.PI * 2;
				var w = (float)rnd.NextDouble() * 300;
				var h = (float)rnd.NextDouble() * (float)Math.PI * 2 / 20.0f;
				
				vertices2[i++] = x;
				vertices2[i++] = y;

				vertices2[i++] = x + w;
				vertices2[i++] = y;

				vertices2[i++] = x;
				vertices2[i++] = y + h;

				vertices2[i++] = x + w;
				vertices2[i++] = y + h;
			}
			
			// Creating vertex buffer
			var stream = new DataStream(featureCount * 4 * vertexSize, true, true);
			stream.WriteRange(vertices2);
			stream.Position = 0;

			var vertexBuffer = new Buffer(device11, stream, new BufferDescription
			{
				BindFlags = BindFlags.VertexBuffer,
				CpuAccessFlags = CpuAccessFlags.None,
				OptionFlags = ResourceOptionFlags.None,
				SizeInBytes = (int)stream.Length,
				Usage = ResourceUsage.Default
			});
			stream.Dispose();

			// for quads, 6 indices per feature

			var indices1 = new int[featureCount * 6];
			for (int j = 0; j < featureCount; j++)
			{
				indices1[j * 6 + 0] = j * 4 + 0;
				indices1[j * 6 + 1] = j * 4 + 1;
				indices1[j * 6 + 2] = j * 4 + 2;

				indices1[j * 6 + 3] = j * 4 + 1;
				indices1[j * 6 + 4] = j * 4 + 2;
				indices1[j * 6 + 5] = j * 4 + 3;
			}
			
			// Index buffer
			stream = new DataStream(featureCount*6*sizeof (int), true, true);
			stream.WriteRange(indices1);
			stream.Position = 0;

			indicesb1 = new Buffer(device11, stream, new BufferDescription
			{
				BindFlags = BindFlags.IndexBuffer,
				CpuAccessFlags = CpuAccessFlags.None,
				OptionFlags = ResourceOptionFlags.None,
				SizeInBytes = (int) stream.Length,
				Usage = ResourceUsage.Default
			});
			stream.Dispose();

			// for lines, 8 indices per feature

			var indices2 = new int[featureCount * 8];
			for (int j = 0; j < featureCount; j++)
			{
				indices2[j * 8 + 0] = j * 4 + 0;
				indices2[j * 8 + 1] = j * 4 + 1;

				indices2[j * 8 + 2] = j * 4 + 1;
				indices2[j * 8 + 3] = j * 4 + 3;

				indices2[j * 8 + 4] = j * 4 + 3;
				indices2[j * 8 + 5] = j * 4 + 2;

				indices2[j * 8 + 6] = j * 4 + 2;
				indices2[j * 8 + 7] = j * 4 + 0;
			}

			// Index buffer
			stream = new DataStream(featureCount * 8 * sizeof(int), true, true);
			stream.WriteRange(indices2);
			stream.Position = 0;

			indicesb2 = new Buffer(device11, stream, new BufferDescription
			{
				BindFlags = BindFlags.IndexBuffer,
				CpuAccessFlags = CpuAccessFlags.None,
				OptionFlags = ResourceOptionFlags.None,
				SizeInBytes = (int)stream.Length,
				Usage = ResourceUsage.Default
			});
			stream.Dispose();

			// Uploading to the device
			device11.ImmediateContext.InputAssembler.InputLayout = layout;

			device11.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, vertexSize, 0));
			vertexBuffer.Dispose();
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			layout.Dispose();
			effect.Dispose();
			renderTargetView.Dispose();
			backBuffer.Dispose();
			indicesb1.Dispose();
			indicesb2.Dispose();
			swapChain.Dispose();
			device11.Dispose();
			device11 = null;
		}
	}
}