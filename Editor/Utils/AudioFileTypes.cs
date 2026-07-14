using System.IO;
using System.Linq;

namespace SheetsLocalization.Editor.Utils
{
    /// <summary>
    /// Single source of truth for the audio file types the tool imports and syncs.
    /// </summary>
    public static class AudioFileTypes
    {
        public static readonly string[] Extensions = { ".mp3", ".wav", ".ogg", ".m4a", ".aac", ".flac" };

        public static readonly string[] SearchPatterns = { "*.mp3", "*.wav", "*.ogg", "*.m4a", "*.aac", "*.flac" };

        public static bool HasAudioExtension(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return Extensions.Contains(extension);
        }
    }
}
