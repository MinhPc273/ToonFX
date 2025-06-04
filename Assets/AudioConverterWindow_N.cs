#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

public class AudioVolumeConverter_N : EditorWindow
{
    AudioClip audioClip;
    float targetDb = -3.0f;
    string ffmpegPath = "FFmpeg/ffmpeg.exe"; // Relative to StreamingAssets

    [MenuItem("Tools/Audio Volume Converter")]
    static void Init()
    {
        GetWindow<AudioVolumeConverter_N>("Volume Converter");
    }

    void OnGUI()
    {
        GUILayout.Label("🔊 Audio Volume Converter (to OGG)", EditorStyles.boldLabel);

        audioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", audioClip, typeof(AudioClip), false);
        targetDb = EditorGUILayout.FloatField("Target dB", targetDb);

        if (GUILayout.Button("🚀 Convert & Adjust Volume"))
        {
            if (!audioClip)
            {
                UnityEngine.Debug.LogError("❌ Hãy chọn một AudioClip trong Project.");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(audioClip);
            string absoluteInputPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);

            if (!File.Exists(absoluteInputPath))
            {
                UnityEngine.Debug.LogError("❌ Không tìm thấy file gốc.");
                return;
            }

            if (!CheckFFmpegExists(out string ffmpegFullPath))
                return;

            ConvertAndAdjust(absoluteInputPath, ffmpegFullPath);
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

    void ConvertAndAdjust(string inputFile, string ffmpeg)
    {
        string fileNameNoExt = Path.GetFileNameWithoutExtension(inputFile);
        string folder = Path.GetDirectoryName(inputFile);

        string tempWav = Path.Combine(Path.GetTempPath(), fileNameNoExt + "_temp.wav");
        string finalOgg = Path.Combine(folder, fileNameNoExt + "_adjusted.ogg");

        // Bước 1: Convert sang WAV
        RunFFmpeg(ffmpeg, $"-y -i \"{inputFile}\" \"{tempWav}\"");

        if (!File.Exists(tempWav))
        {
            UnityEngine.Debug.LogError("❌ Không tạo được WAV tạm.");
            return;
        }

        // Bước 2: Chỉnh âm lượng
        if (!AdjustVolume(tempWav, targetDb))
        {
            UnityEngine.Debug.LogError("❌ Không chỉnh được âm lượng.");
            return;
        }

        // Bước 3: Convert về OGG
        RunFFmpeg(ffmpeg, $"-y -i \"{tempWav}\" -c:a libvorbis -qscale:a 5 \"{finalOgg}\"");

        if (File.Exists(finalOgg))
        {
            UnityEngine.Debug.Log($"✅ Đã xuất file: {finalOgg}");
            AssetDatabase.Refresh();
        }
        else
        {
            UnityEngine.Debug.LogError("❌ Không tạo được file OGG.");
        }

        File.Delete(tempWav);
    }

    void RunFFmpeg(string exePath, string arguments)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
        };

        using (Process p = Process.Start(psi))
        {
            p.WaitForExit();
            string err = p.StandardError.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(err))
                UnityEngine.Debug.LogWarning(err);
        }
    }

    bool AdjustVolume(string wavPath, float targetDb)
    {
        if (!LoadWav(wavPath, out int channels, out int sampleRate, out float[] samples))
            return false;

        float maxDb = -80f;
        foreach (var s in samples)
        {
            float abs = Mathf.Abs(s);
            if (abs > 0)
            {
                float db = 20 * Mathf.Log10(abs);
                if (db > maxDb)
                    maxDb = db;
            }
        }

        float diff = targetDb - maxDb;
        float gain = Mathf.Pow(10f, diff / 20f);

        for (int i = 0; i < samples.Length; i++)
            samples[i] *= gain;

        SaveWav(wavPath, samples, channels, sampleRate);
        return true;
    }

    // ======= Đọc WAV đơn giản =======
    bool LoadWav(string path, out int channels, out int sampleRate, out float[] samples)
    {
        channels = 0;
        sampleRate = 0;
        samples = null;

        try
        {
            using var reader = new BinaryReader(File.OpenRead(path));

            reader.BaseStream.Seek(22, SeekOrigin.Begin);
            channels = reader.ReadInt16();
            sampleRate = reader.ReadInt32();
            reader.BaseStream.Seek(40, SeekOrigin.Begin);
            int dataSize = reader.ReadInt32();

            int sampleCount = dataSize / 2;
            samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
                samples[i] = reader.ReadInt16() / 32768f;

            return true;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Lỗi đọc WAV: " + e.Message);
            return false;
        }
    }

    void SaveWav(string path, float[] samples, int channels, int sampleRate)
    {
        using var writer = new BinaryWriter(File.Create(path));

        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + samples.Length * 2);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16);
        writer.Write((ushort)1); // PCM
        writer.Write((ushort)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * 2);
        writer.Write((ushort)(channels * 2));
        writer.Write((ushort)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(samples.Length * 2);

        foreach (var s in samples)
            writer.Write((short)(Mathf.Clamp(s, -1f, 1f) * 32767));
    }
}
#endif
