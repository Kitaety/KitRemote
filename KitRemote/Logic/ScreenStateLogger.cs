using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX;
using System.Diagnostics;
using System.Drawing.Imaging;
using KitRemote.Logic;
using Resource = SharpDX.Direct3D11.Resource;
using SharpDX.Mathematics.Interop;

public class ScreenStateLogger: IDisposable
{
    //private byte[] _previousScreen;
    private int _fps;
    private DateTime _startRecordingTime;

    private bool _run;
    private int _adapterIndex, _outputIndex;

    private Factory1 _factory;
    private Adapter1? _adapter;
    private SharpDX.Direct3D11.Device? _device;
    private Output? _output;
    private Output1? _output1;
    private OutputDuplication? _duplicatedOutput;
    private Texture2DDescription? _textureDesc;
    private Texture2D? _screenTexture;

    private int _width, _height;

    private SharpDX.DXGI.Resource? _screenResource = null;
    
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

    public DisplayInfo[] GetDisplays()
    {
        var displays = new List<DisplayInfo>();

        for (var adapterIndex = 0; adapterIndex < _factory.Adapters.Length; adapterIndex++)
        {
            var adapter = _factory.Adapters[adapterIndex];

            for (var outputIndex = 0; outputIndex < adapter.Outputs.Length; outputIndex++)
            {
                var adapterOutput = adapter.Outputs[outputIndex];
                var displayInfo = new DisplayInfo()
                {
                    AdapterIndex = adapterIndex,
                    OutputIndex = outputIndex,
                    DisplayName = adapterOutput.Description.DeviceName
                };
                displays.Add(displayInfo);
            }
        }

        return displays.ToArray();
    }

    public void SetDisplay(DisplayInfo displayInfo)
    {
        _adapterIndex = displayInfo.AdapterIndex;
        _outputIndex = displayInfo.OutputIndex;
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

    private bool CopyResourceIntoMemory()
    {
        var result = _duplicatedOutput?.TryAcquireNextFrame(0, out _, out _screenResource);

        if (result is null || result.Value.Failure)
        {
            return false;
        }

        using var screenTexture2D = _screenResource!.QueryInterface<Resource>();
        _device!.ImmediateContext.CopyResource(screenTexture2D, _screenTexture);

        return true;
    }

	private void DuplicateScreen()
	{
        _duplicatedOutput = _output1?.DuplicateOutput(_device);

        if (_duplicatedOutput == null)
        {
            throw new Exception("output not found");
        }

        while (_run)
		{
			try
            {
                // copy resource into memory that can be accessed by the CPU
                if (!CopyResourceIntoMemory())
                {
                    continue;
                }

                var data = ScreenCapture();
                ScreenRefreshed?.Invoke(this, data);
                UpdateFps();
                _screenResource?.Dispose();
                _duplicatedOutput.ReleaseFrame();
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

    private byte[] ScreenCapture()
    {
        if (_device is null)
        {
            throw new Exception("Device not found");
        }

        // Get the desktop capture texture
        var mapSource = _device.ImmediateContext.MapSubresource(_screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
        
        // Create Drawing.Bitmap
        using var bitmap = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
        var boundsRect = new Rectangle(0, 0, _width, _height);

        // Copy pixels from screen capture Texture to GDI bitmap
        var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
        var sourcePtr = mapSource.DataPointer;
        var destPtr = mapDest.Scan0;
        for (var y = 0; y < _height; y++)
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

        using var ms = new MemoryStream();
        using var graphics = Graphics.FromImage(bitmap);
        DrawCursor(graphics);

        bitmap.Save(ms, ImageFormat.Bmp);

        return ms.ToArray();
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
    
    public void Dispose()
    {
        _duplicatedOutput?.Dispose();
	    _device?.ImmediateContext.UnmapSubresource(_screenTexture, 0);

		_factory.Dispose();
	    _adapter?.Dispose();
	    _device?.Dispose();
	    _output?.Dispose();
	    _output1?.Dispose();
	    _screenTexture?.Dispose();
    }

    public delegate void ScreenRefreshedHandler(object sender, byte[] data);
    public ScreenRefreshedHandler ScreenRefreshed;

    public delegate void FpsHandler(int fps);
    public event FpsHandler FpsChange;


    public static int[] CaptureFromScreen()
    {
        // Create device and factory
        var factory = new Factory1();
        var adapter = factory.Adapters.FirstOrDefault();
        var device = new SharpDX.Direct3D11.Device(adapter);

        // Create output duplication
        var output = adapter.Outputs.FirstOrDefault();
        var outputDuplication = output.QueryInterface<OutputDuplication>();

        // Get screen bounds
        var bounds = output.Description.DesktopBounds;

        // Create staging resource for screen capture
        var textureDescription = new Texture2DDescription()
        {
            CpuAccessFlags = CpuAccessFlags.Read,
            Format = Format.B8G8R8A8_UNorm,
            Width = bounds.Right - bounds.Left,
            Height = bounds.Bottom - bounds.Top,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = { Count = 1, Quality = 0 },
            Usage = ResourceUsage.Staging,
            BindFlags = BindFlags.None
        };
        var texture = new Texture2D(device, textureDescription);

        var result = outputDuplication.TryAcquireNextFrame(0, out _, out var screenResource);

        if (result.Failure)
        {
            return new int[]{};
        }

        using var screenTexture = screenResource.QueryInterface<Resource>();

        device.ImmediateContext.CopyResource(screenResource.QueryInterface<Texture2D>(), texture);

        // Read pixels from staging resource
        var screenData = device.ImmediateContext.MapSubresource(
            texture,
            0,
            MapMode.Read,
            SharpDX.Direct3D11.MapFlags.None,
            out var dataStream);

        var rect = new RawRectangle(0, 0, textureDescription.Width, textureDescription.Height);
        var rawData = new int[textureDescription.Width * textureDescription.Height * 4];
        Utilities.Read<Int32>(dataStream.DataPointer, rawData, 0, rawData.Length);

        device.ImmediateContext.UnmapSubresource(texture, 0);

        // Cleanup resources
        screenResource.Dispose();
        screenTexture.Dispose();
        texture.Dispose();
        outputDuplication.Dispose();
        output.Dispose();
        device.Dispose();
        adapter.Dispose();
        factory.Dispose();

        return rawData;
    }
}