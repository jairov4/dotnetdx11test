using System;
using System.Drawing;
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

		// DX Resources
		private Device device11;
		private Effect effect;
		private InputLayout layout;
		private SwapChain swapChain;
		private Buffer indicesb2;
		private Buffer indicesb1;

		private const int featureCount = 5000000;
		private float visibleRangeX1;
		private float visibleRangeX2;
		private float visibleRangeY1;
		private float visibleRangeY2;

		public MainForm()
		{
			visibleRangeX1 = 0;
			visibleRangeX2 = 250000f;
			visibleRangeY1 = 0;
			visibleRangeY2 = 2 * (float)Math.PI;

			InitializeComponent();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			ResizeRenderTarget();
			Invalidate();
		}

		protected override void OnDoubleClick(EventArgs e)
		{
			visibleRangeX1 = 0;
			visibleRangeX2 = 250000f;
			visibleRangeY1 = 0;
			visibleRangeY2 = 2 * (float)Math.PI;

			AffectConstants();
			Invalidate();
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			var zoomFactor = 1.0f - 0.1f*e.Delta/120.0f;
			var px = (visibleRangeX2 - visibleRangeX1) * ((float)e.Location.X / ClientSize.Width) + visibleRangeX1;
			var xd1 = px - (px - visibleRangeX1)*zoomFactor;
			var xd2 = px + (visibleRangeX2 - px)*zoomFactor;
			visibleRangeX1 = xd1;
			visibleRangeX2 = xd2;

			AffectConstants();
			Invalidate();
		}

		private void ResizeRenderTarget()
		{
			if (device11 == null)
			{
				CreateDeviceAndSwapChain();
			}
			else
			{
				ReleaseResources();
				swapChain.ResizeBuffers(0, 0, 0, Format.Unknown, SwapChainFlags.None);
			}

			InitializeResources();
			AffectConstants();
		}

		private void ReleaseResources()
		{
			layout.Dispose();
			effect.Dispose();
			indicesb1.Dispose();
			indicesb2.Dispose();
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
			// Render
			var rt = device11.ImmediateContext.OutputMerger.GetRenderTargets(1);
			device11.ImmediateContext.ClearRenderTargetView(rt[0], new Color4(1.0f, 1.0f, 1.0f, 1.0f));
			rt[0].Dispose();
			
			effect.GetVariableByName("color").AsVector().Set(new Color4(138f/255f, 43f/255f, 226f/255f));
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
		}
		
		private void CreateDeviceAndSwapChain()
		{
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
		}

		private void AffectConstants()
		{
			// Matrices
			var visibleRangeLengthX = visibleRangeX2 - visibleRangeX1;
			var visibleRangeLengthY = visibleRangeY2 - visibleRangeY1;

			var projectionMatrix = Matrix.Translation(-visibleRangeX1, -visibleRangeY1, 0)*
									Matrix.Scaling(2.0f/visibleRangeLengthX, 2.0f/visibleRangeLengthY, 1.0f)*
									Matrix.Translation(-1f, -1f, 0.0f);

			effect.GetVariableByName("finalMatrix").AsMatrix().SetMatrix(projectionMatrix);
		}

		private void InitializeResources()
		{
			var backBuffer = Resource.FromSwapChain<Texture2D>(swapChain, 0);

			// Defining render view
			var renderTargetView = new RenderTargetView(device11, backBuffer);
			backBuffer.Dispose();

			device11.ImmediateContext.OutputMerger.SetTargets(renderTargetView);
			renderTargetView.Dispose();

			device11.ImmediateContext.Rasterizer.SetViewports(new Viewport(0, 0, ClientSize.Width, ClientSize.Height, 0.0f, 1.0f));

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

			device11.ImmediateContext.InputAssembler.InputLayout = layout;

			// Texture
			var rasterizerStateDescription = new RasterizerStateDescription { CullMode = CullMode.None, FillMode = FillMode.Solid };

			var rstate = RasterizerState.FromDescription(device11, rasterizerStateDescription);
			device11.ImmediateContext.Rasterizer.State = rstate;
			rstate.Dispose();
		
			var rnd = new Random();

			// Creating vertex buffer
			var vertices2 = new float[featureCount*4*2];
			for (var i = 0; i < vertices2.Length;)
			{
				var x = (float)rnd.NextDouble() * 250000;
				var y = (float)rnd.NextDouble() * (float)Math.PI * 2;
				var w = (float)rnd.NextDouble() * 3;
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
			
			device11.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, vertexSize, 0));
			vertexBuffer.Dispose();

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
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			ReleaseResources();
			swapChain.Dispose();
			device11.Dispose();
			device11 = null;
		}
	}
}