using Android.Gms.Tasks;
using Android.Graphics;
using Android.Runtime;
using BarcodeScanner.Maui.Models;
using Java.Util;
using IBarcodeScanner = BarcodeScanner.Maui.Services.IBarcodeScanner;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;
using Point = Microsoft.Maui.Graphics.Point;

namespace BarcodeScanner.Maui.Platforms.Android.Services
{
    public class AndroidBarcodeScanner : IBarcodeScanner
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
                using var bitmap = BitmapFactory.DecodeByteArray(imageData, 0, imageData.Length);
                if (bitmap == null)
                {
                    tcs.SetResult(Array.Empty<BarcodeResult>());
                    return tcs.Task;
                }

                var image = InputImage.FromBitmap(bitmap, 0);
                var options = new BarcodeScannerOptions.Builder()
                    .SetBarcodeFormats(ConvertToMLKitFormat(formats))
                    .Build();

                var scanner = BarcodeScanning.GetClient(options);
                var gmsTask = scanner.Process(image);

                gmsTask.AddOnSuccessListener(new OnSuccessListener(result =>
                {
                    try
                    {
                        var barcodes = ProcessResults(result);
                        tcs.TrySetResult(barcodes);
                    }
                    catch
                    {
                        tcs.TrySetResult(Array.Empty<BarcodeResult>());
                    }
                }));

                gmsTask.AddOnFailureListener(new OnFailureListener(_ =>
                {
                    tcs.TrySetResult(Array.Empty<BarcodeResult>());
                }));
            }
            catch
            {
                tcs.TrySetResult(Array.Empty<BarcodeResult>());
            }

            return tcs.Task;
        }

        private static BarcodeResult[] ProcessResults(Java.Lang.Object? results)
        {
            if (results == null)
                return Array.Empty<BarcodeResult>();

            using var list = results.JavaCast<ArrayList>();
            if (list?.IsEmpty ?? true)
                return Array.Empty<BarcodeResult>();

            var barcodes = new List<BarcodeResult>();

            foreach (var item in list.ToArray())
            {
                using var barcode = item.JavaCast<Barcode>();
                if (barcode == null) continue;

                var points = barcode.GetCornerPoints();
                Point[]? cornerPoints = points?.Select(p => new Point(p.X, p.Y)).ToArray();

                barcodes.Add(new BarcodeResult(
                    barcode.RawValue ?? string.Empty,
                    barcode.GetRawBytes(),
                    cornerPoints,
                    ConvertFromMLKitFormat(barcode.Format)));
            }

            return barcodes.ToArray();
        }

        private static int ConvertToMLKitFormat(BarcodeFormat format)
        {
            int result = 0;

            if (format.HasFlag(BarcodeFormat.QR_CODE)) result |= Barcode.FormatQrCode;
            if (format.HasFlag(BarcodeFormat.AZTEC)) result |= Barcode.FormatAztec;
            if (format.HasFlag(BarcodeFormat.CODABAR)) result |= Barcode.FormatCodabar;
            if (format.HasFlag(BarcodeFormat.CODE_39)) result |= Barcode.FormatCode39;
            if (format.HasFlag(BarcodeFormat.CODE_93)) result |= Barcode.FormatCode93;
            if (format.HasFlag(BarcodeFormat.CODE_128)) result |= Barcode.FormatCode128;
            if (format.HasFlag(BarcodeFormat.DATA_MATRIX)) result |= Barcode.FormatDataMatrix;
            if (format.HasFlag(BarcodeFormat.EAN_8)) result |= Barcode.FormatEan8;
            if (format.HasFlag(BarcodeFormat.EAN_13)) result |= Barcode.FormatEan13;
            if (format.HasFlag(BarcodeFormat.ITF)) result |= Barcode.FormatItf;
            if (format.HasFlag(BarcodeFormat.PDF_417)) result |= Barcode.FormatPdf417;
            if (format.HasFlag(BarcodeFormat.UPC_A)) result |= Barcode.FormatUpcA;
            if (format.HasFlag(BarcodeFormat.UPC_E)) result |= Barcode.FormatUpcE;

            return result == 0 ? Barcode.FormatQrCode : result;
        }

        private static BarcodeFormat ConvertFromMLKitFormat(int format)
        {
            return format switch
            {
                Barcode.FormatQrCode => BarcodeFormat.QR_CODE,
                Barcode.FormatAztec => BarcodeFormat.AZTEC,
                Barcode.FormatCodabar => BarcodeFormat.CODABAR,
                Barcode.FormatCode39 => BarcodeFormat.CODE_39,
                Barcode.FormatCode93 => BarcodeFormat.CODE_93,
                Barcode.FormatCode128 => BarcodeFormat.CODE_128,
                Barcode.FormatDataMatrix => BarcodeFormat.DATA_MATRIX,
                Barcode.FormatEan8 => BarcodeFormat.EAN_8,
                Barcode.FormatEan13 => BarcodeFormat.EAN_13,
                Barcode.FormatItf => BarcodeFormat.ITF,
                Barcode.FormatPdf417 => BarcodeFormat.PDF_417,
                Barcode.FormatUpcA => BarcodeFormat.UPC_A,
                Barcode.FormatUpcE => BarcodeFormat.UPC_E,
                _ => BarcodeFormat.QR_CODE
            };
        }

        private class OnSuccessListener : Java.Lang.Object, IOnSuccessListener
        {
            private readonly Action<Java.Lang.Object?> _callback;
            public OnSuccessListener(Action<Java.Lang.Object?> callback) => _callback = callback;
            public void OnSuccess(Java.Lang.Object? result) => _callback?.Invoke(result);
        }

        private class OnFailureListener : Java.Lang.Object, IOnFailureListener
        {
            private readonly Action<Java.Lang.Exception> _callback;
            public OnFailureListener(Action<Java.Lang.Exception> callback) => _callback = callback;
            public void OnFailure(Java.Lang.Exception e) => _callback?.Invoke(e);
        }
    }
}
