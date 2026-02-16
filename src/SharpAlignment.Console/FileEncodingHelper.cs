using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SharpAlignment;

/// <summary>
/// Helper class for detecting and preserving file encodings.
/// </summary>
public static class FileEncodingHelper
{
    /// <summary>
    /// Reads a file and returns its content along with the detected encoding.
    /// </summary>
    public static async Task<(string Content, Encoding Encoding)> ReadAllTextWithEncodingAsync(string path)
    {
        // Read the file as bytes to detect the encoding
        var bytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);

        // Detect encoding based on BOM or default to UTF-8 without BOM
        var encoding = DetectEncoding(bytes);

        // Skip the preamble (BOM) when decoding
        var preamble = encoding.GetPreamble();
        var startIndex = 0;
        if (preamble.Length > 0 && bytes.Length >= preamble.Length)
        {
            var hasPreamble = true;
            for (int i = 0; i < preamble.Length; i++)
            {
                if (bytes[i] != preamble[i])
                {
                    hasPreamble = false;
                    break;
                }
            }

            if (hasPreamble)
            {
                startIndex = preamble.Length;
            }
        }

        // Decode the content using the detected encoding, skipping the BOM
        var content = encoding.GetString(bytes, startIndex, bytes.Length - startIndex);

        return (content, encoding);
    }

    /// <summary>
    /// Writes text to a file using the specified encoding.
    /// </summary>
    public static async Task WriteAllTextWithEncodingAsync(string path, string content, Encoding encoding)
    {
        var preamble = encoding.GetPreamble();
        var contentBytes = encoding.GetBytes(content);
        var bytes = new byte[preamble.Length + contentBytes.Length];
        preamble.CopyTo(bytes, 0);
        contentBytes.CopyTo(bytes, preamble.Length);
        await File.WriteAllBytesAsync(path, bytes).ConfigureAwait(false);
    }

    /// <summary>
    /// Detects the encoding of a byte array based on BOM (Byte Order Mark).
    /// Returns UTF-8 without BOM as the default if no BOM is detected.
    /// </summary>
    private static Encoding DetectEncoding(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            // UTF-8 with BOM
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        }
        else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            if (bytes.Length >= 4 && bytes[2] == 0x00 && bytes[3] == 0x00)
            {
                // UTF-32 LE
                return new UTF32Encoding(bigEndian: false, byteOrderMark: true);
            }

            // UTF-16 LE
            return new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
        }
        else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            // UTF-16 BE
            return new UnicodeEncoding(bigEndian: true, byteOrderMark: true);
        }
        else if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
        {
            // UTF-32 BE
            return new UTF32Encoding(bigEndian: true, byteOrderMark: true);
        }

        // Default to UTF-8 without BOM
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }
}
