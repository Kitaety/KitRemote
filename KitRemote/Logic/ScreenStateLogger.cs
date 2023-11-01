﻿using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX;
using System.Diagnostics;
using System.Drawing.Imaging;
using Resource = SharpDX.Direct3D11.Resource;

public class ScreenStateLogger
{
    private byte[] _previousScreen;
    private int _fps;
    private DateTime _startRecordingTime;

    private bool _run, _init;
    public int Size { get; private set; }


    public ScreenStateLogger()
    {
    }

    public void Start()
    {
        _run = true;
        _startRecordingTime = DateTime.Now;
        _fps = 0;

        var factory = new Factory1();
        //Get first adapter
        var adapter = factory.GetAdapter1(0);
        //Get device from adapter
        var device = new SharpDX.Direct3D11.Device(adapter);
        //Get front buffer of the adapter
        var output = adapter.GetOutput(0);
        var output1 = output.QueryInterface<Output1>();
        // Width/Height of desktop to capture
        int width = output.Description.DesktopBounds.Right;
        int height = output.Description.DesktopBounds.Bottom;
        // Create Staging texture CPU-accessible
        var textureDesc = new Texture2DDescription
        {
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = width,
            Height = height,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = { Count = 1, Quality = 0 },
            Usage = ResourceUsage.Staging
        };
        var screenTexture = new Texture2D(device, textureDesc);
        Task.Factory.StartNew(() =>
        {
            // Duplicate the output
            using var duplicatedOutput = output1.DuplicateOutput(device);

            while (_run)
            {
                try
                {
                    SharpDX.DXGI.Resource screenResource;
                    OutputDuplicateFrameInformation duplicateFrameInformation;
                    // Try to get duplicated frame within given time is ms
                    var result = duplicatedOutput.TryAcquireNextFrame(5, out duplicateFrameInformation, out screenResource);

                    if (result.Failure)
                    {
                        continue;
                    }
                    // copy resource into memory that can be accessed by the CPU
                    using (var screenTexture2D = screenResource.QueryInterface<Resource>())
                        device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);
                    // Get the desktop capture texture
                    var mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                    // Create Drawing.Bitmap
                    using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                    {
                        var boundsRect = new Rectangle(0, 0, width, height);
                        // Copy pixels from screen capture Texture to GDI bitmap
                        var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                        var sourcePtr = mapSource.DataPointer;
                        var destPtr = mapDest.Scan0;
                        for (int y = 0; y < height; y++)
                        {
                            // Copy a single line 
                            Utilities.CopyMemory(destPtr, sourcePtr, width * 4);
                            // Advance pointers
                            sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                            destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                        }
                        // Release source and dest locks
                        bitmap.UnlockBits(mapDest);
                        device.ImmediateContext.UnmapSubresource(screenTexture, 0);
                        using (var ms = new MemoryStream())
                        {
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
        });
        while (!_init) ;
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
}