namespace BarcodeScanner.Maui.Models
{
    /// <summary>
    /// Event arguments for barcode detection events
    /// </summary>
    public record BarcodeEventArgs
    {
        /// <summary>
        /// The detected barcode results
        /// </summary>
        public required BarcodeResult[] Result { get; init; }
    }
}
