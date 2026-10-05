using System.IO;
using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.App
{
    public sealed class JsonSaveSerializer : ISaveSerializer
    {
        public string ToJson(SaveData d) => JsonUtility.ToJson(d, true);
        public SaveData FromJson(string json) => JsonUtility.FromJson<SaveData>(json);
    }

    public sealed class FileSaveStore : ISaveStore
    {
        public static string OverridePath;

        public string Path => string.IsNullOrEmpty(OverridePath)
            ? System.IO.Path.Combine(Application.persistentDataPath, "pokiwar_save.json")
            : OverridePath;

        public string Read() => File.Exists(Path) ? File.ReadAllText(Path) : null;

        public void Write(string json)
        {
            var tmp = Path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(Path)) File.Delete(Path);
            File.Move(tmp, Path);
        }

        public void Delete()
        {
            if (File.Exists(Path)) File.Delete(Path);
        }
    }
}
