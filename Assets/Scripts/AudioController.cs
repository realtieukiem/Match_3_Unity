using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AudioEntry
{
    public string key;        // Tên nhạc / hiệu ứng (string bạn sẽ gọi)
    public AudioClip clip;    // File âm thanh
    public float volume = 1f; // Volume riêng cho clip này (optional)
}

public class AudioController : MonoBehaviour
{
    public static AudioController Instance { get; private set; }

    [Header("Nguồn phát")]
    [SerializeField] private AudioSource bgmSource; // Nhạc nền
    [SerializeField] private AudioSource sfxSource; // Hiệu ứng

    [Header("Danh sách nhạc / sound theo key")]
    [SerializeField] private List<AudioEntry> audioEntries = new List<AudioEntry>();

    private Dictionary<string, AudioEntry> audioDict;

    private void Awake()
    {
        // Singleton đơn giản
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Nếu muốn sống qua scene

        BuildDictionary();
    }

    private void BuildDictionary()
    {
        audioDict = new Dictionary<string, AudioEntry>();

        foreach (var entry in audioEntries)
        {
            if (entry == null || entry.clip == null || string.IsNullOrEmpty(entry.key))
                continue;

            if (!audioDict.ContainsKey(entry.key))
            {
                audioDict.Add(entry.key, entry);
            }
            else
            {
                Debug.LogWarning($"Audio key bị trùng: {entry.key}");
            }
        }
    }

    private bool TryGetEntry(string key, out AudioEntry entry)
    {
        if (audioDict == null)
        {
            BuildDictionary();
        }

        if (!audioDict.TryGetValue(key, out entry))
        {
            Debug.LogWarning($"Không tìm thấy audio với key: {key}");
            return false;
        }

        return true;
    }

    // ================= BGM (nhạc nền) =================

    /// <summary>
    /// Play nhạc nền theo key
    /// </summary>
    public void PlayBgm(string key, bool loop = true)
    {
        if (!TryGetEntry(key, out var entry)) return;
        if (bgmSource == null)
        {
            Debug.LogError("Chưa gán BgmSource trong AudioController!");
            return;
        }

        bgmSource.clip = entry.clip;
        bgmSource.volume = entry.volume;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    // ================= SFX (hiệu ứng) =================

    /// <summary>
    /// Play hiệu ứng âm thanh theo key (one shot)
    /// </summary>
    public void PlaySfx(string key)
    {
        if (!TryGetEntry(key, out var entry)) return;
        if (sfxSource == null)
        {
            Debug.LogError("Chưa gán SfxSource trong AudioController!");
            return;
        }

        sfxSource.PlayOneShot(entry.clip, entry.volume);
    }

    /// <summary>
    /// Stop tất cả SFX đang phát
    /// </summary>
    public void StopSfx()
    {
        if (sfxSource != null)
            sfxSource.Stop();
    }
}
