namespace Mediapipe.Maui.LandmarkModels
{
    public static class LandmarkModelProvider
    {
        private const string ResourceRoot = "Mediapipe.Maui.LandmarkModels";

        public static async Task<string> GetModelPathAsync(string modelFileName)
        {
            var localPath = Path.Combine(
                FileSystem.AppDataDirectory,
                modelFileName);

            if (File.Exists(localPath))
                return localPath;

            var resourceName = $"{ResourceRoot}.{modelFileName}";

            using var stream = GetResourceStream(resourceName)
                ?? throw new FileNotFoundException(resourceName);

            using var fileStream = File.Create(localPath);
            await stream.CopyToAsync(fileStream);

            return localPath;
        }

        private static Stream GetResourceStream(string resourceName)
        {
#if ANDROID || IOS
            return typeof(LandmarkModelProvider)
                .Assembly
                .GetManifestResourceStream(resourceName);
#else
        throw new PlatformNotSupportedException();
#endif
        }
    }
}