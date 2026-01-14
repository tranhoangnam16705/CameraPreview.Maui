namespace BarcodeScanner.Maui.Models
{
    /// <summary>
    /// Represents a decoded barcode result
    /// </summary>
    public class BarcodeResult
    {
        public BarcodeResult(string text, byte[]? rawBytes, Point[]? resultPoints, BarcodeFormat barcodeFormat)
        {
            Text = text;
            RawBytes = rawBytes;
            ResultPoints = resultPoints;
            BarcodeFormat = barcodeFormat;
        }

        /// <summary>
        /// Raw text encoded by the barcode
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Raw bytes encoded by the barcode, if applicable
        /// </summary>
        public byte[]? RawBytes { get; }

        /// <summary>
        /// Points related to the barcode in the image. These are typically points identifying
        /// finder patterns or the corners of the barcode.
        /// </summary>
        public Point[]? ResultPoints { get; }

        /// <summary>
        /// Format of the barcode that was decoded
        /// </summary>
        public BarcodeFormat BarcodeFormat { get; }
    }
}
