using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX;
using System.Diagnostics;
using System.Drawing.Imaging;
using Resource = SharpDX.Direct3D11.Resource;

public class ScreenStateLogger: IDisposable
{
    private byte[] _previousScreen;
    private int _fps;
    private DateTime _startRecordingTime;

    private bool _run, _init;
    private int _adapterIndex = 0, _outputIndex = 0;

    private Factory1 _factory;
    private Adapter1? _adapter;
    private SharpDX.Direct3D11.Device? _device;
    private Output? _output;
    private Output1? _output1;
    private Texture2DDescription? _textureDesc;
    private Texture2D? _screenTexture;

    private int _width, _height;

    public ScreenStateLogger(int adapter = 0, int output = 0)
    {
        _adapterIndex = adapter;
        _outputIndex = output;

        Init();
    }

	public void Init()
    {
	    _factory = new Factory1();
		
	    _adapter = _factory.GetAdapter1(_adapterIndex);
		_device = new SharpDX.Direct3D11.Device(_adapter);
	    _output = _adapter.GetOutput(_outputIndex);
	    _output1 = _output.QueryInterface<Output1>();
	    
	    _width = _output.Description.DesktopBounds.Right - _output.Description.DesktopBounds.Left;
	    _height = _output.Description.DesktopBounds.Bottom - _output.Description.DesktopBounds.Top;
	    
	    _textureDesc = new Texture2DDescription
	    {
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = _width,
            Height = _height,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = { Count = 1, Quality = 0 },
            Usage = ResourceUsage.Staging
        };
	    _screenTexture = new Texture2D(_device, _textureDesc.Value);
	}

	public void SetAdapter(int index)
	{
        _adapterIndex = index;
        _outputIndex = 0;

        _adapter = _factory.GetAdapter1(_adapterIndex);
	}

	public void SetOutput(int index)
	{
		_outputIndex = index;
	}

	public List<string> GetAdapters()
	{
        return _factory.Adapters.Select(adapter => adapter.Description.Description).ToList();
	}

	public List<string> GetOutputs()
	{
		return _adapter is null ? new List<string>() : _adapter.Outputs.Select(output => output.Description.DeviceName).ToList();
	}

	public void Start()
    {
	    Init();
		_run = true;
        _startRecordingTime = DateTime.Now;
        _fps = 0;
        var thread = new Thread(DuplicateScreen);
		thread.Start();
    }

	private void DuplicateScreen()
	{
		// Duplicate the output
		using var duplicatedOutput = _output1.DuplicateOutput(_device);

		while (_run)
		{
			try
			{
				// Try to get duplicated frame within given time is ms
				var result = duplicatedOutput.TryAcquireNextFrame(5, out _, out var screenResource);

				if (result.Failure)
				{
					continue;
				}
				// copy resource into memory that can be accessed by the CPU
				using (var screenTexture2D = screenResource.QueryInterface<Resource>())
					_device.ImmediateContext.CopyResource(screenTexture2D, _screenTexture);
				// Get the desktop capture texture
				var mapSource = _device.ImmediateContext.MapSubresource(_screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
				// Create Drawing.Bitmap
				using (var bitmap = new Bitmap(_width, _height, PixelFormat.Format32bppArgb))
				{
					//var bounds = _output.Description.DesktopBounds;
					//using var graphics = Graphics.FromImage(bitmap);
					//graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, new Size(_width,_height));
					//DrawCursor(graphics);

					//using var ms = new MemoryStream();
					//bitmap.Save(ms, ImageFormat.Bmp);
					//ScreenRefreshed?.Invoke(this, ms.ToArray());
					//UpdateFps();
					//_init = true;


					var boundsRect = new Rectangle(0, 0, _width, _height);
					// Copy pixels from screen capture Texture to GDI bitmap
					var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
					var sourcePtr = mapSource.DataPointer;
					var destPtr = mapDest.Scan0;
					for (int y = 0; y < _height; y++)
					{
						// Copy a single line 
						Utilities.CopyMemory(destPtr, sourcePtr, _width * 4);
						// Advance pointers
						sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
						destPtr = IntPtr.Add(destPtr, mapDest.Stride);
					}
					// Release source and dest locks
					bitmap.UnlockBits(mapDest);
					_device.ImmediateContext.UnmapSubresource(_screenTexture, 0);
					using (var ms = new MemoryStream())
					{
						//using var graphics = Graphics.FromImage(bitmap);
						//DrawCursor(graphics);

						bitmap.Save(ms, ImageFormat.Bmp);
						ScreenRefreshed?.Invoke(this, ms.ToArray());
						UpdateFps();
						_init = true;
					}
				}
				screenResource.Dispose();
				duplicatedOutput.ReleaseFrame();
			}
			catch (SharpDXException e)
			{
				if (e.ResultCode.Code != SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
				{
					Trace.TraceError(e.Message);
					Trace.TraceError(e.StackTrace);
				}
			}
		}

		Dispose();
	}

	private void DrawCursor(Graphics graphics)
	{
		if (_output is null)
		{
			return;
		}
		var cursorPosition = new Point(Cursor.Position.X - _output.Description.DesktopBounds.Left, Cursor.Position.Y - _output.Description.DesktopBounds.Top);
		Cursors.Default.Draw(graphics, new Rectangle(cursorPosition, Cursors.Default.Size));
	}

    public void Stop()
    {
        _run = false;
	}

	private void UpdateFps()
    {
        _fps++;

        if (!((DateTime.Now - _startRecordingTime).TotalSeconds > 1))
        {
            return;
        }

        FpsChange?.Invoke(_fps);
        _fps = 0;
        _startRecordingTime = DateTime.Now;
    }

    public delegate void ScreenRefreshedHandler(object sender, byte[] data);
    public ScreenRefreshedHandler ScreenRefreshed;

    public delegate void FpsHandler(int fps);
    public event FpsHandler FpsChange;

    public void Dispose()
    {
	    _device?.ImmediateContext.UnmapSubresource(_screenTexture, 0);

		_factory.Dispose();
	    _adapter?.Dispose();
	    _device?.Dispose();
	    _output?.Dispose();
	    _output1?.Dispose();
	    _screenTexture?.Dispose();
    }
}