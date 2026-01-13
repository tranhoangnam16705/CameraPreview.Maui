using System.Collections.ObjectModel;

namespace CameraPreview.Maui.Controls
{
    public class CameraView : View, ICameraView
    {
        #region Bindable Properties

        public static readonly BindableProperty IsRunningProperty =
            BindableProperty.Create(
                nameof(IsRunning),
                typeof(bool),
                typeof(CameraView),
                false,
                BindingMode.TwoWay);

        public bool IsRunning
        {
            get => (bool)GetValue(IsRunningProperty);
            private set => SetValue(IsRunningProperty, value);
        }

        public static readonly BindableProperty AutoStartProperty =
            BindableProperty.Create(
                nameof(AutoStart),
                typeof(bool),
                typeof(CameraView),
                false);

        public bool AutoStart
        {
            get => (bool)GetValue(AutoStartProperty);
            set => SetValue(AutoStartProperty, value);
        }

        public static readonly BindableProperty CamerasProperty =
            BindableProperty.Create(
                nameof(Cameras),
                typeof(ObservableCollection<CameraPreviewInfo>),
                typeof(CameraView),
                new ObservableCollection<CameraPreviewInfo>());

        /// <summary>
        /// List of available cameras in the device. This is a bindable property.
        /// </summary>
        public ObservableCollection<CameraPreviewInfo> Cameras
        {
            get { return (ObservableCollection<CameraPreviewInfo>)GetValue(CamerasProperty); }
            set { SetValue(CamerasProperty, value); }
        }

        public static readonly BindableProperty CameraProperty =
            BindableProperty.Create(
                nameof(Camera),
                typeof(CameraPreviewInfo),
                typeof(CameraView),
                null, propertyChanged: CameraChanged);

        /// <summary>
        /// Set the camera to use by the controler. This is a bindable property.
        /// </summary>
        public CameraPreviewInfo Camera
        {
            get { return (CameraPreviewInfo)GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }

        public static readonly BindableProperty SnapShotProperty = BindableProperty.Create(
            nameof(SnapShot),
            typeof(ImageSource),
            typeof(CameraView),
            null,
            BindingMode.OneWayToSource);

        /// <summary>
        /// Refreshes according to the frequency set in the AutoSnapShotSeconds property (if AutoSnapShotAsImageSource is set to true)
        /// or when GetSnapShot is called or TakeAutoSnapShot is set to true
        /// </summary>
        public ImageSource SnapShot
        {
            get { return (ImageSource)GetValue(SnapShotProperty); }
            private set { SetValue(SnapShotProperty, value); }
        }

        public static readonly BindableProperty SnapShotStreamProperty = BindableProperty.Create(
            nameof(SnapShotStream),
            typeof(Stream),
            typeof(CameraView),
            null,
            BindingMode.OneWayToSource);

        /// <summary>
        /// Refreshes according to the frequency set in the AutoSnapShotSeconds property or when GetSnapShot is called.
        /// WARNING. Each time a snapshot is made, the previous stream is disposed.
        /// </summary>
        public Stream SnapShotStream
        {
            get { return (Stream)GetValue(SnapShotStreamProperty); }
            internal set { SetValue(SnapShotStreamProperty, value); }
        }

        #endregion Bindable Properties

        #region Events

        /// <summary>
        /// Raised when a new camera frame is available
        /// </summary>
        public event EventHandler<CameraFrameEventArgs> FrameReady;

        /// <summary>
        /// Raised when camera starts successfully
        /// </summary>
        public event EventHandler CameraStarted;

        /// <summary>
        /// Raised when camera stops
        /// </summary>
        public event EventHandler CameraStopped;

        /// <summary>
        /// Raised when an error occurs
        /// </summary>
        public event EventHandler<string> CameraError;

        public event EventHandler<string> TakePhotoSaved;

        #endregion Events

        #region Internal Event Raisers (for Handler to call)

        internal void RefreshDevices()
        {
            Task.Run(() =>
            {
                OnPropertyChanged(nameof(Cameras));
            });
        }

        internal void RaiseFrameReady(CameraFrameEventArgs args)
        {
            FrameReady?.Invoke(this, args);
        }

        internal void RaiseCameraStarted()
        {
            IsRunning = true;
            CameraStarted?.Invoke(this, EventArgs.Empty);
        }

        internal void RaiseCameraStopped()
        {
            IsRunning = false;
            CameraStopped?.Invoke(this, EventArgs.Empty);
        }

        internal void RaiseCameraError(string error)
        {
            CameraError?.Invoke(this, error);
        }

        internal void RaisePhotoSaved(string error)
        {
            TakePhotoSaved?.Invoke(this, error);
        }

        #endregion Internal Event Raisers (for Handler to call)

        #region ICameraView Implementation

        public async Task StartAsync()
        {
            if (Handler != null)
            {
                Handler.Invoke(nameof(StartAsync));
                // Wait a bit for handler to process
                await Task.Delay(100);
            }
        }

        public void Stop()
        {
            Handler?.Invoke(nameof(Stop));
        }

        /// <summary>
        /// Takes a capture form the active camera playback.
        /// </summary>
        /// <param name="imageFormat">The capture image format</param>
        public Task<ImageSource> GetSnapShotAsync(ImageFormat imageFormat = ImageFormat.Png)
        {
            var tcs = new TaskCompletionSource<ImageSource>();

            Handler?.Invoke(
                nameof(GetSnapShotAsync),
                new SnapshotRequest(imageFormat, tcs)
            );

            return tcs.Task;
        }

        /// <summary>
        /// Saves a capture form the active camera playback in a file
        /// </summary>
        /// <param name="imageFormat">The capture image format</param>
        /// <param name="SnapFilePath">Full path for the file</param>
        public Task<bool> SaveSnapShotAsync(ImageFormat imageFormat, string SnapFilePath)
        {
            var tcs = new TaskCompletionSource<bool>();

            Handler?.Invoke(
                nameof(SaveSnapShotAsync),
                new SaveSnapshotRequest(imageFormat, SnapFilePath, tcs)
            );

            return tcs.Task;
        }

        /// <summary>
        /// Takes a photo from the camera selected.
        /// </summary>
        /// <param name="imageFormat">The capture image format</param>
        /// <returns>A stream with the photo info</returns>
        public Task<Stream> TakePhotoAsync(ImageFormat imageFormat = ImageFormat.Jpeg)
        {
            var tcs = new TaskCompletionSource<Stream>();

            Handler?.Invoke(
                nameof(TakePhotoAsync),
                new TakePhotoRequest(imageFormat, tcs)
            );

            return tcs.Task;
        }

        #endregion ICameraView Implementation

        #region Property Changed

        private static void CameraChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null && oldValue != newValue && bindable is CameraView cameraView && newValue is CameraPreviewInfo)
            {
            }
        }

        public static async Task<bool> RequestPermissions(bool withMic = false, bool withStorageWrite = false)
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted) return false;
            }
            if (withMic)
            {
                status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Microphone>();
                    if (status != PermissionStatus.Granted) return false;
                }
            }
            if (withStorageWrite)
            {
                status = await Permissions.CheckStatusAsync<Permissions.Media>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.CheckStatusAsync<Permissions.Media>();
                    if (status != PermissionStatus.Granted)
                    {
                        PermissionStatus status1 = await Permissions.RequestAsync<Permissions.Media>();
                        if (status1 != PermissionStatus.Granted) return false;
                    }
                }
            }
            return true;
        }

        #endregion Property Changed
    }

    public sealed record SnapshotRequest(
    ImageFormat Format,
    TaskCompletionSource<ImageSource> Completion);

    public sealed record SaveSnapshotRequest(
    ImageFormat Format, string SnapFilePath,
    TaskCompletionSource<bool> Completion);

    public sealed record TakePhotoRequest(
    ImageFormat Format,
    TaskCompletionSource<Stream> Completion);
}