#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using NAudio.Wave; 
using NAudio.Lame; 

namespace Bonnate
{
    public class MP3EasyVolumeEditor : EditorWindow
    {
        private AudioClip mAudioClip; // Selected audioclip
        private float mTargetDb = -3.0f; // Target decibel value
        private float mMaxDb; // audioclip's max db

        private Texture2D mListenBtnTexture;

        private string mScriptFolderPath;
        private string mBackupFolderPath;

        [MenuItem("Tools/Bonnate/MP3 Easy Volume Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<MP3EasyVolumeEditor>("MP3 Volume Editor");
            window.minSize = new Vector2(300, 210);
            window.maxSize = new Vector2(300, 210);
        }

        private void OnEnable()
        {
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            mScriptFolderPath = Path.GetDirectoryName(scriptPath);

            string imagePath = $"{mScriptFolderPath}/Images/ListenButtonImage.png";
            mListenBtnTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);

            // Create the backup folder path
            mBackupFolderPath = Path.Combine(mScriptFolderPath, "_Backups");
        }

        private void OnGUI()
        {
            // Create the backup folder if it doesn't exist
            if (!Directory.Exists(mBackupFolderPath))
            {
                Directory.CreateDirectory(mBackupFolderPath);
                AssetDatabase.Refresh();
                Log($"Thư mục Backup được tạo tại: {mBackupFolderPath}");
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("MP3 File");

            // Find an AudioClip among the selected objects in the Project window
            AudioClip selectedAudioClip = null;
            System.Object[] selectedObjects = Selection.objects;
            if (selectedObjects != null && selectedObjects.Length > 0)
            {
                foreach (var selectedObject in selectedObjects)
                {
                    if (selectedObject is AudioClip)
                    {
                        selectedAudioClip = selectedObject as AudioClip;
                        break; // Use the first found AudioClip
                    }
                }
            }

            if (selectedAudioClip != null)
            {
                // Set to null if the extension is not "mp3"
                string assetPath = AssetDatabase.GetAssetPath(selectedAudioClip);
                if (!string.IsNullOrEmpty(assetPath) && !assetPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                {
                    selectedAudioClip = null;
                }
            }

            // Disable direct object selection in the Project window
            EditorGUI.BeginDisabledGroup(true);
            selectedAudioClip = EditorGUILayout.ObjectField("", selectedAudioClip, typeof(AudioClip), false, GUILayout.Width(180)) as AudioClip;
            EditorGUI.EndDisabledGroup();

            // Update the audioClip
            mAudioClip = selectedAudioClip;

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Target Decibel ");
            GUILayout.FlexibleSpace();
            GUILayout.Label("-");
            mTargetDb = EditorGUILayout.FloatField("", mTargetDb, GUILayout.Width(126));
            mTargetDb = Mathf.Clamp(mTargetDb, 0f, 80f);
            GUILayout.Label("(dB)");

            if (GUILayout.Button(mListenBtnTexture, GUILayout.Width(20), GUILayout.Height(20)))
            {
                ListenToAudio();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Process and Replace"))
            {
                if (mAudioClip == null)
                {
                    Log("Vui lòng chọn file MP3.");
                }
                else
                {
                    (float[] samples, int channels, int sampleRate) = ProcessAndExport();

                    string audioClipPath = AssetDatabase.GetAssetPath(mAudioClip);

                    // Create backup
                    if (File.Exists(audioClipPath))
                    {
                        string backupPath = Path.Combine(mScriptFolderPath, "_Backups", Path.GetFileName(audioClipPath));
                        File.Copy(audioClipPath, $"{backupPath.Replace(".mp3", "")}_bak{System.DateTime.Now.ToString("_yyMMdd_HHMMss")}.mp3", true);
                    }

                    // Save as MP3
                    SaveMp3(audioClipPath, samples, channels, sampleRate);

                    AssetDatabase.Refresh();
                    Log("File MP3 đã được chỉnh sửa.");
                }
            }
            if (GUILayout.Button("Process and Export"))
            {
                if (mAudioClip == null)
                {
                    Log("Vui lòng chọn file MP3.");
                }
                else
                {
                    (float[] samples, int channels, int sampleRate) = ProcessAndExport();

                    string outputPath = EditorUtility.SaveFilePanel("Lưu file MP3 đã xử lý", "", "processed.mp3", "mp3");
                    if (!string.IsNullOrEmpty(outputPath))
                    {
                        SaveMp3(outputPath, samples, channels, sampleRate);
                        Log($"File MP3 đã được lưu tại {outputPath}.");
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Add Detail Info section
            GUILayout.Label("[Thông tin chi tiết Audio Clip]");
            if (mAudioClip == null)
            {
                GUILayout.Label($"Hãy chọn một file MP3 từ Project!");
            }
            else
            {
                GUILayout.Label($"Max DB: {GetMaxDB()}");
                GUILayout.Label($"Sample Rate: {mAudioClip.frequency} Hz");
                GUILayout.Label($"Channels: {mAudioClip.channels}");
                GUILayout.Label($"Length: {mAudioClip.length:F2} seconds");
            }

            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Powered by: Bonnate");
            if (GUILayout.Button("Github", GetHyperlinkLabelStyle()))
            {
                OpenURL("https://github.com/bonnate");
            }
            if (GUILayout.Button("Blog", GetHyperlinkLabelStyle()))
            {
                OpenURL("https://bonnate.tistory.com/");
            }
            GUILayout.EndHorizontal();
        }

        private float GetMaxDB()
        {
            if (mAudioClip == null)
            {
                return 0f;
            }

            float[] samples = new float[mAudioClip.samples * mAudioClip.channels];
            mAudioClip.GetData(samples, 0);

            mMaxDb = -Mathf.Infinity;
            for (int i = 0; i < samples.Length; i++)
            {
                float sample = Mathf.Abs(samples[i]);
                if (sample > 0) // Avoid log10(0)
                {
                    float db = 20.0f * Mathf.Log10(sample);
                    if (db > mMaxDb)
                    {
                        mMaxDb = db;
                    }
                }
            }

            return mMaxDb;
        }

        private (float[], int, int) ProcessAndExport()
        {
            int sampleRate = mAudioClip.frequency;
            int channels = mAudioClip.channels;

            float[] samples = new float[mAudioClip.samples * channels];
            mAudioClip.GetData(samples, 0);

            float dbDifference = (-mTargetDb) - mMaxDb;
            float multiplier = Mathf.Pow(10.0f, dbDifference / 20.0f);

            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= multiplier;
            }

            return (samples, channels, sampleRate);
        }

        private void SaveMp3(string path, float[] samples, int channels, int sampleRate)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new LameMP3FileWriter(path, new WaveFormat(sampleRate, 16, channels), LameMP3FileWriter.LAMEPreset.STANDARD))
            {
                byte[] pcmData = new byte[samples.Length * 2];
                for (int i = 0; i < samples.Length; i++)
                {
                    short pcmSample = (short)(samples[i] * 32767.0f);
                    pcmData[i * 2] = (byte)(pcmSample & 0xff);
                    pcmData[i * 2 + 1] = (byte)((pcmSample >> 8) & 0xff);
                }

                memoryStream.Write(pcmData, 0, pcmData.Length);
                memoryStream.Position = 0;
                memoryStream.CopyTo(writer);
            }
        }

        private void ListenToAudio()
        {
            if (mAudioClip == null)
            {
                Log("Vui lòng chọn file MP3.");
                return;
            }

            AudioSource audioSource = EditorUtility.CreateGameObjectWithHideFlags("AudioSource", HideFlags.HideAndDontSave, typeof(AudioSource)).GetComponent<AudioSource>();
            audioSource.clip = mAudioClip;

            if (-mTargetDb < mMaxDb)
            {
                float maxVolume = Mathf.Pow(10.0f, (-mTargetDb) / 20.0f);
                audioSource.volume = maxVolume;
                audioSource.Play();
            }
            else
            {
                (float[] samples, int channels, int sampleRate) = ProcessAndExport();
                AudioClip tempAudioClip = AudioClip.Create("ProcessedAudioClip", samples.Length / channels, channels, sampleRate, false);
                tempAudioClip.SetData(samples, 0);
                audioSource.clip = tempAudioClip;
                audioSource.Play();
            }
        }

        #region _HYPERLINK
        private GUIStyle GetHyperlinkLabelStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = new Color(0f, 0.5f, 1f);
            style.stretchWidth = false;
            style.wordWrap = false;
            return style;
        }

        private void OpenURL(string url)
        {
            EditorUtility.OpenWithDefaultApp(url);
        }
        #endregion

        private void Log(string content)
        {
            Debug.Log($"<color=cyan>[MP3 Easy Volume Editor]</color> {content}");
        }
    }
}
#endif