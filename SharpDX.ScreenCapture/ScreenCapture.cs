namespace SharpDX.ScreenCapture
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using SharpDX.Direct3D11;
    using SharpDX.DXGI;

    public class ScreenCapture : IDisposable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object lockObj = new object();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ManualResetEventSlim _resetEvent = new ManualResetEventSlim(true);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _disposed;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _loop;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Thread _captureThread;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _adapterIndex;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _outputIndex;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<ScreenCapturedEventArgs> OnScreenCaptured;

        /// <summary>
        /// Gets or sets the timeout to aquire the frame.
        /// </summary>
        public int AcquireNextFrameTimeout { get; set; } = 500;

        /// <summary>
        /// Create a new <see cref="ScreenCapture"/> instance.
        /// </summary>
        public ScreenCapture(int adapterIndex = 0, int outputIndex = 0)
        {
            _adapterIndex = adapterIndex;
            _outputIndex = outputIndex;
        }

        /// <summary>
        /// Start the screen capture.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Start()
        {
            ThrowIfDisposed();

            lock (lockObj)
            {
                // reset the loop status
                _loop = false;
                _resetEvent.Wait();
                _loop = true;

                // create the thread
                _captureThread = new Thread(MainLoop);
                _captureThread.IsBackground = true;

#if DEBUG
                // set the thread name for debugging purposes
                _captureThread.Name = "ScreenCaptureThread";
#endif

                // start the thread
                _captureThread.Start();
            }
        }

        /// <summary>
        /// Stop the screen capture.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Stop()
        {
            ThrowIfDisposed();

            lock (lockObj)
            {
                _loop = false;
                _resetEvent.Wait();

                _captureThread = null;
            }
        }

        private void MainLoop()
        {
            // do not inline this property
            var timeoutInMilliseconds = AcquireNextFrameTimeout;

            // create the DXGI Factory1 
            // get the adapter
            // get the device from adapter
            // get the front buffer of the adapter
            // create the staging CPU-accessible texture
            // duplicate the output
            using (var factory = new Factory1())
            using (var adapter = factory.GetAdapter1(_adapterIndex))
            using (var device = new Direct3D11.Device(adapter))
            using (var output = adapter.GetOutput(_outputIndex))
            using (var output1 = output.QueryInterface<Output1>())
            using (var screenTexture = new Texture2D(device, new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = output.Description.DesktopBounds.Right,
                Height = output.Description.DesktopBounds.Bottom,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            }))
            using (var duplicatedOutput = output1.DuplicateOutput(device))
            {
                DXGI.Resource screenResource;
                OutputDuplicateFrameInformation duplicateFrameInformation;

                try
                {
                    _resetEvent.Reset();

                    // main loop
                    while (_loop)
                    {
                        // try to get duplicated frame within given time
                        if (duplicatedOutput.TryAcquireNextFrame(timeoutInMilliseconds, out duplicateFrameInformation, out screenResource).Code < 0)
                            continue;

                        // copy resource into memory that can be accessed by the CPU
                        using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                            device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);

                        // get the captured texture
                        var mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, Direct3D11.MapFlags.None);

                        try
                        {
                            OnScreenCaptured?.Invoke(this, new ScreenCapturedEventArgs(mapSource));
                        }
                        finally
                        {
                            // release the texture
                            device.ImmediateContext.UnmapSubresource(screenTexture, 0);

                            // dispose the resource
                            screenResource.Dispose();

                            // release the acquire frame
                            duplicatedOutput.ReleaseFrame();
                        }
                    }
                }
                finally
                {
                    _resetEvent.Set();
                }
            }
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _captureThread = null;
                _resetEvent.Dispose();

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ScreenCapture));
        }
    }
}
