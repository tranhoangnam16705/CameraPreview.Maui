---

```md
# 📸 CameraPreview.Maui

**CameraPreview.Maui** — A custom **Camera Preview Control** for .NET MAUI apps.

This library provides a reusable `CameraView` control that supports:

- Live camera preview
- Front / back camera switching
- Snapshot and high-quality photo capture
- Access to raw camera frames for machine learning or custom processing
- Full support on Android (CameraX) and iOS (AVFoundation)

---

## 📦 Project Structure

```

CameraPreview.Maui/
├── CameraPreview.Maui/                # MAUI camera control source
│   ├── Controls/                      # Public UI control definition
│   ├── Handlers/                      # Platform handlers (Android + iOS)
│   │   ├── Android/                   # Android CameraX implementation
│   │   └── iOS/                       # iOS AVFoundation implementation
│   ├── Models/                        # Events, requests, enums (ImageFormat, CameraInfo, etc.)
│   └── Platforms/                     # Platform-specific utilities
│
├── CameraPreview.Sample/              # Example MAUI app consuming the control
│   ├── MainPage.xaml
│   └── MainPage.xaml.cs
├── CameraPreview.Maui.slnx
└── README.md

````

---

## 🧠 Features

- ✅ Live camera preview
- ✅ Viewer control for camera in MAUI
- ✅ Switch between front / back cameras
- ✅ Take high quality photos (TakePhotoAsync)
- ✅ Snapshot from preview (GetSnapShotAsync)
- ✅ Access raw camera buffer for ML
- ✅ Handles rotation + mirror correctly
- ✅ Events & commands to integrate into MVVM

---

## 🚀 Quick Start

### 1. Add to Solution

Clone the repo and add `CameraPreview.Maui` project to your MAUI solution.

```bash
git clone https://github.com/tranhoangnam16705/CameraPreview.Maui.git
````

Then reference it from your MAUI application project.

---

## 🔐 Permissions

### Android

In **Platforms/Android/AndroidManifest.xml**:

```xml
<uses-permission android:name="android.permission.CAMERA" />
```

Request at runtime:

```csharp
await Permissions.RequestAsync<Permissions.Camera>();
```

---

### iOS

In **Platforms/iOS/Info.plist**:

```xml
<key>NSCameraUsageDescription</key>
<string>Camera access is required to preview and take photos</string>
```

---

## 📘 Usage Example

### XAML

```xml
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:camera="clr-namespace:CameraPreview.Maui;assembly=CameraPreview.Maui"
    x:Class="CameraPreview.Sample.MainPage">

    <camera:CameraView
        x:Name="cameraPreview"
        AutoStart="True"
        HorizontalOptions="Fill"
        VerticalOptions="Fill" />
</ContentPage>
```

---

### C# Code-Behind

Start / Stop camera:

```csharp
await cameraPreview.StartAsync();
cameraPreview.Stop();
```

Switch Camera:

```csharp
await cameraPreview.SwitchCameraAsync();
```

---

## 📸 Capture APIs

### Snapshot from preview

Snapshot uses the current preview frame:

```csharp
var stream = await cameraPreview.GetSnapShotAsync(ImageFormat.Png);
var img = ImageSource.FromStream(() => stream);
```

---

### Take high quality photo

This uses platform native capture (higher resolution):

```csharp
var stream = await cameraPreview.TakePhotoAsync(ImageFormat.Jpeg);
var photo = ImageSource.FromStream(() => stream);
```

---

## 🧠 Frame Events

Receive raw camera frames for analysis:

```csharp
cameraPreview.FrameReady += (sender, args) =>
{
    // args.ImageData is byte[] from camera buffer
    // args.Width, args.Height
    // Use for ML / face detection / custom render
};
```

---

## 📊 API Reference

### CameraView Properties

| Property     | Description                              |
| ------------ | ---------------------------------------- |
| `AutoStart`  | Automatically start preview on view load |
| `Camera`     | Gets / sets selected camera (front/back) |
| `Cameras`    | List of available camera devices         |
| `IsRunning`  | Indicates preview status                 |
| `FrameReady` | Event fired on each analyzed frame       |

---

### CameraView Methods

| Method                          | Description                |
| ------------------------------- | -------------------------- |
| `StartAsync()`                  | Start camera preview       |
| `Stop()`                        | Stop camera preview        |
| `SwitchCameraAsync()`           | Switch front / back        |
| `GetSnapShotAsync(ImageFormat)` | Return preview snapshot    |
| `TakePhotoAsync(ImageFormat)`   | Capture high quality photo |

---

## 🛠 Platform Implementation Details

### Android (CameraX)

* Uses `CameraX` API
* Preview + ImageAnalysis + ImageCapture
* Handles rotation with `ImageProxy.ImageInfo.RotationDegrees`
* Produces correct orientation + mirror front camera
* No storage permission required for in-memory photo

### iOS (AVFoundation)

* Uses `AVCaptureSession`
* Video data output + photo output
* High quality photo via `AVCapturePhotoOutput`
* Handles orientation / mirror
* Compatible with iOS 17+ using modern APIs

---

## 🧪 Sample App

The included `CameraPreview.Sample` project shows:

✔ Embedding the view
✔ UI interaction
✔ Taking snapshot / photo
✔ Handling frames for ML / MediaPipe integration

Examples in sample:

```csharp
cameraPreview.FrameReady += OnFrameReady;
await cameraPreview.StartAsync();
var snapshot = await cameraPreview.GetSnapShotAsync(ImageFormat.Png);
```

---

## ❗ Notes

✔ Snapshot ≠ High quality photo
✔ Do not block UI when processing frames
✔ Designed for easy MVVM integration
✔ Supports extension for face detection / ML

You can integrate with libraries like MediaPipe or custom face processing.

---

## 📜 License

This project is open source under the **MIT License**.

---

## 🤝 Contributing

Contributions and discussions are welcome!
Feel free to open an issue, PR, or suggest new features.

---

## 🧑‍💻 Author

**Tran Hoang Nam**
[https://github.com/tranhoangnam16705](https://github.com/tranhoangnam16705)

```

---

## 📌 Notes

* This README assumes the structure and APIs based on your repo layout and typical MAUI camera control design.
* If you want, I can tailor the API reference section to match *exact method signatures and event args types* from your code.
* You can also include screenshots & usage diagrams.

---

[1]: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/maui/views/camera-view?utm_source=chatgpt.com "CameraView - .NET MAUI Community Toolkit - Community Toolkits for .NET | Microsoft Learn"
