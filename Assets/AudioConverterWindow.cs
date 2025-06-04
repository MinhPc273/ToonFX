#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.IO;

public class AudioConverterWindow : EditorWindow
{
    string inputPath = "";
    string outputPath = "";
    string ffmpegPath = "FFmpeg/ffmpeg.exe"; // Relative to StreamingAssets

    enum ConvertMode
    {
        WAVtoMP3,
        MP3toWAV,
        ToOGG
    }

    ConvertMode convertMode = ConvertMode.WAVtoMP3;

    [MenuItem("Tools/Audio Converter")]
    public static void ShowWindow()
    {
        GetWindow<AudioConverterWindow>("Audio Converter");
    }

    void OnGUI()
    {
        GUILayout.Label("🎵 Audio Converter (FFmpeg)", EditorStyles.boldLabel);

        convertMode = (ConvertMode)EditorGUILayout.EnumPopup("Chế độ chuyển đổi", convertMode);

        if (GUILayout.Button("Chọn file..."))
        {
            string ext = convertMode switch
            {
                ConvertMode.WAVtoMP3 => "wav",
                ConvertMode.MP3toWAV => "mp3",
                ConvertMode.ToOGG => "mp3;wav",
                _ => "*"
            };

            inputPath = EditorUtility.OpenFilePanel("Chọn file âm thanh", "", ext);
            if (!string.IsNullOrEmpty(inputPath))
            {
                string fileName = Path.GetFileNameWithoutExtension(inputPath);
                string folder = Path.GetDirectoryName(inputPath);
                string newExt = convertMode switch
                {
                    ConvertMode.WAVtoMP3 => ".mp3",
                    ConvertMode.MP3toWAV => ".wav",
                    ConvertMode.ToOGG => ".ogg",
                    _ => ".converted"
                };
                outputPath = Path.Combine(folder, fileName + newExt);
            }
        }

        EditorGUILayout.TextField("Input File", inputPath);
        EditorGUILayout.TextField("Output File", outputPath);

        if (GUILayout.Button("🚀 Bắt đầu chuyển đổi"))
        {
            if (!File.Exists(inputPath))
            {
                UnityEngine.Debug.LogError("❌ Không tìm thấy file nguồn!");
                return;
            }

            if (!CheckFFmpegExists(out string ffmpegFullPath))
                return;

            RunFFmpegConversion(ffmpegFullPath);
        }
    }

    bool CheckFFmpegExists(out string fullPath)
    {
        fullPath = Path.Combine(Application.streamingAssetsPath, ffmpegPath);
        if (!File.Exists(fullPath))
        {
            UnityEngine.Debug.LogError("❌ Không tìm thấy ffmpeg.exe tại: " + fullPath);
            return false;
        }
        return true;
    }

    void RunFFmpegConversion(string ffmpegFullPath)
    {
        string arguments = "";

        switch (convertMode)
        {
            case ConvertMode.WAVtoMP3:
                arguments = $"-i \"{inputPath}\" -codec:a libmp3lame -qscale:a 2 \"{outputPath}\"";
                break;

            case ConvertMode.MP3toWAV:
                arguments = $"-i \"{inputPath}\" \"{outputPath}\"";
                break;

            case ConvertMode.ToOGG:
                arguments = $"-i \"{inputPath}\" -c:a libvorbis -qscale:a 5 \"{outputPath}\"";
                break;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = ffmpegFullPath,
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using (Process process = Process.Start(startInfo))
        {
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (File.Exists(outputPath))
                UnityEngine.Debug.Log($"✅ Đã chuyển đổi thành công: {Path.GetFileName(outputPath)}");
            else
                UnityEngine.Debug.LogError($"❌ Lỗi khi chuyển đổi: {error}");
        }
    }
}
#endif
