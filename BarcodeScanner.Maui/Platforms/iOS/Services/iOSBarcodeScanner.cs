using BarcodeScanner.Maui.Models;
using BarcodeScanner.Maui.Services;
using CoreImage;
using Foundation;
using Vision;

namespace BarcodeScanner.Maui.Platforms.iOS.Services
{
    public class iOSBarcodeScanner : IBarcodeScanner
    {
        public Task<BarcodeResult[]> DecodeAsync(byte[] imageData)
        {
            return DecodeAsync(imageData, BarcodeFormat.QR_CODE);
        }

        public Task<BarcodeResult[]> DecodeAsync(byte[] imageData, BarcodeFormat formats)
        {
            var tcs = new TaskCompletionSource<BarcodeResult[]>();

            try
            {
                using var data = NSData.FromArray(imageData);
                using var ciImage = new CIImage(data);

                if (ciImage == null)
                {
                    tcs.SetResult(Array.Empty<BarcodeResult>());
                    return tcs.Task;
                }

                var handler = new VNImageRequestHandler(ciImage, new NSDictionary());
                var request = new VNDetectBarcodesRequest((req, error) =>
                {
                    if (error != null)
                    {
                        tcs.TrySetResult(Array.Empty<BarcodeResult>());
                        return;
                    }

                    var observations = req.GetResults<VNBarcodeObservation>();
                    if (observations == null || observations.Length == 0)
                    {
                        tcs.TrySetResult(Array.Empty<BarcodeResult>());
                        return;
                    }

                    var barcodes = new List<BarcodeResult>();
                    foreach (var obs in observations)
                    {
                        if (string.IsNullOrEmpty(obs.PayloadStringValue))
                            continue;

                        var format = ConvertFromVisionSymbology(obs.Symbology);

                        // Filter by requested formats
                        if (!formats.HasFlag(format))
                            continue;

                        var cornerPoints = new Point[]
                        {
                            new(obs.TopLeft.X, obs.TopLeft.Y),
                            new(obs.TopRight.X, obs.TopRight.Y),
                            new(obs.BottomRight.X, obs.BottomRight.Y),
                            new(obs.BottomLeft.X, obs.BottomLeft.Y)
                        };

                        barcodes.Add(new BarcodeResult(
                            obs.PayloadStringValue,
                            null,
                            cornerPoints,
                            format));
                    }

                    tcs.TrySetResult(barcodes.ToArray());
                });

                // Set symbologies filter
                request.Symbologies = ConvertToVisionSymbologies(formats);

                handler.Perform(new VNRequest[] { request }, out _);
            }
            catch
            {
                tcs.TrySetResult(Array.Empty<BarcodeResult>());
            }

            return tcs.Task;
        }

        private static VNBarcodeSymbology[] ConvertToVisionSymbologies(BarcodeFormat format)
        {
            var symbologies = new List<VNBarcodeSymbology>();

            if (format.HasFlag(BarcodeFormat.QR_CODE)) symbologies.Add(VNBarcodeSymbology.QR);
            if (format.HasFlag(BarcodeFormat.AZTEC)) symbologies.Add(VNBarcodeSymbology.Aztec);
            if (format.HasFlag(BarcodeFormat.CODE_39)) symbologies.Add(VNBarcodeSymbology.Code39);
            if (format.HasFlag(BarcodeFormat.CODE_93)) symbologies.Add(VNBarcodeSymbology.Code93);
            if (format.HasFlag(BarcodeFormat.CODE_128)) symbologies.Add(VNBarcodeSymbology.Code128);
            if (format.HasFlag(BarcodeFormat.DATA_MATRIX)) symbologies.Add(VNBarcodeSymbology.DataMatrix);
            if (format.HasFlag(BarcodeFormat.EAN_8)) symbologies.Add(VNBarcodeSymbology.Ean8);
            if (format.HasFlag(BarcodeFormat.EAN_13)) symbologies.Add(VNBarcodeSymbology.Ean13);
            if (format.HasFlag(BarcodeFormat.ITF)) symbologies.Add(VNBarcodeSymbology.Itf14);
            if (format.HasFlag(BarcodeFormat.PDF_417)) symbologies.Add(VNBarcodeSymbology.Pdf417);
            if (format.HasFlag(BarcodeFormat.UPC_E)) symbologies.Add(VNBarcodeSymbology.Upce);
            if (format.HasFlag(BarcodeFormat.CODABAR)) symbologies.Add(VNBarcodeSymbology.Codabar);

            return symbologies.Count == 0
                ? new[] { VNBarcodeSymbology.QR }
                : symbologies.ToArray();
        }

        private static BarcodeFormat ConvertFromVisionSymbology(VNBarcodeSymbology symbology)
        {
            if (symbology == VNBarcodeSymbology.QR) return BarcodeFormat.QR_CODE;
            if (symbology == VNBarcodeSymbology.Aztec) return BarcodeFormat.AZTEC;
            if (symbology == VNBarcodeSymbology.Code39) return BarcodeFormat.CODE_39;
            if (symbology == VNBarcodeSymbology.Code93) return BarcodeFormat.CODE_93;
            if (symbology == VNBarcodeSymbology.Code128) return BarcodeFormat.CODE_128;
            if (symbology == VNBarcodeSymbology.DataMatrix) return BarcodeFormat.DATA_MATRIX;
            if (symbology == VNBarcodeSymbology.Ean8) return BarcodeFormat.EAN_8;
            if (symbology == VNBarcodeSymbology.Ean13) return BarcodeFormat.EAN_13;
            if (symbology == VNBarcodeSymbology.Itf14) return BarcodeFormat.ITF;
            if (symbology == VNBarcodeSymbology.Pdf417) return BarcodeFormat.PDF_417;
            if (symbology == VNBarcodeSymbology.Upce) return BarcodeFormat.UPC_E;
            if (symbology == VNBarcodeSymbology.Codabar) return BarcodeFormat.CODABAR;

            return BarcodeFormat.QR_CODE;
        }
    }
}
