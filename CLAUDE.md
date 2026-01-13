# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**CameraPreview.Maui** is a .NET MAUI library providing camera preview and capture functionality with MediaPipe integration for real-time computer vision analysis (face detection, hand tracking, drowsiness detection, smoking detection).

The solution consists of three projects:
- **CameraPreview.Maui**: Core camera control library with platform-specific handlers
- **Mediapipe.Maui**: MediaPipe wrapper for ML-based visual analysis
- **CameraPreview.Sample**: Sample MAUI application demonstrating usage

## Build Commands

### Build the entire solution
```bash
dotnet build CameraPreview.Maui.slnx
```

### Build specific projects
```bash
# Camera library
dotnet build CameraPreview.Maui/CameraPreview.Maui.csproj

# MediaPipe library
dotnet build Mediapipe.Maui/Mediapipe.Maui.csproj

# Sample app
dotnet build CameraPreview.Sample/CameraPreview.Sample.csproj
```

### Build for specific platforms
```bash
# Android
dotnet build -f net10.0-android

# iOS
dotnet build -f net10.0-ios
```

### Clean build artifacts
```bash
dotnet clean
```

## Platform Requirements

- **Target Frameworks**: net10.0-android, net10.0-ios
- **Android**: Minimum SDK 28 (Android 9.0)
- **iOS**: Minimum 15.0

## Architecture Overview

### MAUI Handler Pattern

The library follows MAUI's handler architecture to bridge cross-platform controls with native implementations:

1. **CameraView** (`Controls/CameraView.cs`): Cross-platform MAUI View with bindable properties and events
2. **ICameraView** (`Controls/ICameraView.cs`): Interface defining camera operations
3. **CameraViewHandler**: Platform-specific handlers that connect CameraView to native implementations
   - Android: `Platforms/Android/Handler/CameraViewHandler.cs` → `AndroidCameraView`
   - iOS: `Platforms/iOS/Handler/CameraViewHandler.cs` → `iOSCameraView`

### Registration Pattern

Both libraries use extension methods for service registration:

```csharp
// In MauiProgram.cs
builder.UseCameraPreView()  // Registers camera handlers
       .UseMediaPipe();      // Registers MediaPipe detectors
```

**CameraPreview.Maui** (`Extensions.cs`):
- Registers platform-specific handlers using conditional compilation (`#if ANDROID` / `#elif IOS`)
- Maps `CameraView` control to native handler implementations

**Mediapipe.Maui** (`Extensions.cs`):
- Registers `IMediaPipeDetectorFactory` (platform-specific)
- Registers `IMediaPipeDetector<FaceLandmarksResult>` and `IMediaPipeDetector<HandLandmarksResult>` as singletons
- Analyzers (DrowsinessAnalyzer, SmokingAnalyzer, etc.) are registered as transient services in the app

### Platform-Specific Implementations

**Android**:
- Uses **CameraX** API (Xamarin.AndroidX.Camera.* packages)
- Native implementation in `AndroidCameraView.cs`
- Handles ImageAnalysis, ImageCapture, and Preview use cases
- Frame rotation handled via `ImageProxy.ImageInfo.RotationDegrees`

**iOS**:
- Uses **AVFoundation** (AVCaptureSession)
- Native implementation in `iOSCameraView.cs`
- Supports video data output and photo output
- Handles orientation and mirroring for front camera

### MediaPipe Integration

MediaPipe detectors follow an abstract factory pattern:

1. **IMediaPipeDetectorFactory**: Platform-specific factory creates detectors
   - Android: `AndroidMediaPipeDetectorFactory`
   - iOS: `iOSMediaPipeDetectorFactory`

2. **MediaPipeDetectorBase**: Abstract base class with common detection logic
   - Timing/performance tracking
   - Initialization lifecycle
   - Generic detection pipeline

3. **Platform Implementations**:
   - `Platforms/Android/Services/Face/AndroidFaceLandmarker.cs`
   - `Platforms/Android/Services/Hand/AndroidHandLandmarker.cs`
   - `Platforms/iOS/Services/Face/iOSFaceLandmarker.cs`
   - `Platforms/iOS/Services/Hand/iOSHandLandmarker.cs`

4. **Analyzers** (`Services/`): Higher-level business logic
   - `DrowsinessAnalyzer`: Calculates Eye Aspect Ratio (EAR) and tracks drowsiness over consecutive frames
   - `SmokingAnalyzer`: Detects hand-to-face gestures indicating smoking
   - `FaceMeshAnalyzer`: Face mesh detection and rendering
   - `HandTrackingAnalyzer`: Hand landmark detection

### Event Flow

```
Camera Frame → Handler → VirtualView.RaiseFrameReady() → CameraView.FrameReady event
                                                            ↓
                                                       User subscribes
                                                            ↓
                                                    MediaPipe Analyzer
```

The sample app demonstrates this in `CameraPage.xaml.cs`:
1. Subscribe to `FrameReady` event
2. Pass frame data to analyzer
3. Update UI with results on MainThread

## Key APIs

### CameraView Control

```csharp
// Start/Stop
await cameraView.StartAsync();
cameraView.Stop();

// Switching cameras
await cameraView.SwitchCameraAsync();
cameraView.Camera = frontCamera;  // Bindable property

// Capture
var snapshot = await cameraView.GetSnapShotAsync(ImageFormat.Png);
var photo = await cameraView.TakePhotoAsync(ImageFormat.Jpeg);
await cameraView.SaveSnapShotAsync(ImageFormat.Jpeg, filePath);

// Permission helper
await CameraView.RequestPermissions(withMic: false, withStorageWrite: false);
```

### MediaPipe Detection

```csharp
// Initialize detector (after camera starts)
var options = new MediaPipeOptions { MaxNumResults = 3 };
await analyzer.InitializeAsync(options);

// Analyze frame
var result = await analyzer.AnalyzeAsync(imageData);
```

## Important Implementation Details

### Snapshot vs Photo
- **Snapshot** (`GetSnapShotAsync`): Captures from current preview frame (lower quality, instant)
- **Photo** (`TakePhotoAsync`): Uses native camera capture API (higher quality, slight delay)

### Handler Commands
Operations like `StartAsync`, `Stop`, `GetSnapShotAsync`, etc. use MAUI's command mapper pattern:
- Called via `Handler?.Invoke(nameof(MethodName), args)`
- Args can include `TaskCompletionSource` for async results

### Frame Processing
- Android provides `ImageProxy` with rotation metadata
- iOS provides `CMSampleBuffer`
- Both are converted to `byte[]` for cross-platform MediaPipe processing
- Frame rotation/mirroring handled at platform level

### MediaPipe Models
Models are embedded resources in `Mediapipe.Maui/LandmarkModels/`:
- `face_landmarker.task`
- `hand_landmarker.task`
- `face_detection_short_range.tflite`

Android models are also marked as `AndroidAsset` for native access.

## Common Development Workflows

### Adding a New Analyzer
1. Create analyzer class in `Mediapipe.Maui/Services/`
2. Inherit from or use existing `IMediaPipeDetector<TResult>`
3. Define result model in `Models/`
4. Register in `MauiProgram.cs` as transient/singleton
5. Inject into page/view model

### Modifying Camera Behavior
1. Update `CameraView.cs` for cross-platform API
2. Modify platform handlers to implement new functionality
3. Use conditional compilation for platform-specific code
4. Test on both Android and iOS

### Working with MediaPipe
- Platform-specific detectors are in `Platforms/{Android|iOS}/Services/`
- Shared business logic goes in `Services/` analyzers
- Models managed by `LandmarkModelProvider.cs`
- Results implement `IDetectionResult` interface

## Known Patterns

### Conditional Compilation
The codebase uses `#if ANDROID` / `#elif IOS` extensively for platform-specific registration and implementation.

### Async Task Completion
Commands that need to return values use `TaskCompletionSource`:
```csharp
var tcs = new TaskCompletionSource<Stream>();
Handler?.Invoke(nameof(TakePhotoAsync), new TakePhotoRequest(imageFormat, tcs));
return tcs.Task;
```

### UI Thread Marshaling
MediaPipe results are processed off-thread but UI updates require MainThread:
```csharp
MainThread.BeginInvokeOnMainThread(() => {
    // Update UI
});
```

## Dependencies

### CameraPreview.Maui
- Microsoft.Maui.Controls 10.0.20
- Xamarin.AndroidX.Camera.* 1.4.2.3 (Android only)

### Mediapipe.Maui
- Microsoft.Maui.Controls 10.0.20
- MediaPipeTasksVision.Android 0.10.29 (Android)
- MediaPipeTasksVision.iOS 0.10.21 (iOS)

## Testing Notes

- No automated tests currently in the solution
- Manual testing via CameraPreview.Sample app
- Test both camera orientations (front/back)
- Test snapshot vs photo capture differences
- Verify MediaPipe detection performance on real devices
