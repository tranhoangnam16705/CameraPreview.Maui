using BarcodeScanner.Maui.Models;

namespace BarcodeScanner.Maui.Services
{
    /// <summary>
    /// Interface for barcode scanning service
    /// </summary>
    public interface IBarcodeScanner
    {
        /// <summary>
        /// Decode barcodes from image data
        /// </summary>
        /// <param name="imageData">Image data as byte array (JPEG/PNG format)</param>
        /// <returns>Array of detected barcodes, empty if none found</returns>
        Task<BarcodeResult[]> DecodeAsync(byte[] imageData);

        /// <summary>
        /// Decode barcodes with specific format filter
        /// </summary>
        Task<BarcodeResult[]> DecodeAsync(byte[] imageData, BarcodeFormat formats);
    }
}
