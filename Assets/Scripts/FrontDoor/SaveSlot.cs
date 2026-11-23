using System;
using System.IO;
using UnityEngine;

namespace Caravans.FrontDoor
{
    // One autosave beside the player's persistent data. The title can see the slot; Play owns the contents.
    public static class SaveSlot
    {
        public const string FileName = "caravans_save.json";

        public static string DirectoryOverride;

        public static Func<string, bool> Validator;

        public static string Directory
        {
            get
            {
                if (!string.IsNullOrEmpty(DirectoryOverride))
                    return DirectoryOverride;
                return Application.persistentDataPath;
            }
        }

        public static string MainPath
        {
            get { return Path.Combine(Directory, FileName); }
        }

        public static string BackupPath
        {
            get { return MainPath + ".bak"; }
        }

        public static string TempPath
        {
            get { return MainPath + ".tmp"; }
        }

        public static bool TryWrite(string json)
        {
            string tmp = null;
            try
            {
                string folder = Directory;
                if (string.IsNullOrEmpty(folder))
                    return false;
                System.IO.Directory.CreateDirectory(folder);
                tmp = TempPath;
                WriteFlushed(tmp, json ?? "");
                string main = MainPath;
                if (!File.Exists(main))
                {
                    File.Move(tmp, main);
                    return true;
                }

                try
                {
                    File.Replace(tmp, main, BackupPath);
                }
                catch (Exception)
                {
                    File.Copy(main, BackupPath, true);
                    File.Delete(main);
                    File.Move(tmp, main);
                }

                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Caravans save was not written. " + error.Message);
                return false;
            }
        }

        public static bool TryReadValid(out string json)
        {
            if (TryCandidate(MainPath, out json))
                return true;
            if (TryCandidate(BackupPath, out json))
                return true;
            json = null;
            return false;
        }

        public static bool HasValidSave()
        {
            string json;
            return TryReadValid(out json);
        }

        public static void Clear()
        {
            TryDelete(MainPath);
            TryDelete(BackupPath);
            TryDelete(TempPath);
        }

        static bool TryCandidate(string path, out string json)
        {
            json = null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;
            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Caravans save skipped (" + Path.GetFileName(path) + "). " + error.Message);
                return false;
            }

            if (Accepts(text))
            {
                json = text;
                return true;
            }

            Debug.LogWarning("Caravans save skipped (" + Path.GetFileName(path) + ").");
            return false;
        }

        static bool Accepts(string text)
        {
            if (Validator != null)
                return Validator(text);
            return FallbackAccepts(text);
        }

        static bool FallbackAccepts(string text)
        {
            if (!JsonObject(text))
                return false;
            try
            {
                SaveHeader header = JsonUtility.FromJson<SaveHeader>(text);
                return header != null && header.saveVersion >= 1 && header.saveVersion <= 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static void WriteFlushed(string path, string json)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(true);
            }
        }

        static void TryDelete(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Caravans save was not cleared. " + error.Message);
            }
        }

        internal static bool JsonObject(string json)
        {
            if (string.IsNullOrEmpty(json))
                return false;
            int start = 0;
            int end = json.Length - 1;
            while (start <= end && char.IsWhiteSpace(json[start]))
                start++;
            while (end >= start && char.IsWhiteSpace(json[end]))
                end--;
            if (start > end || json[start] != '{' || json[end] != '}')
                return false;
            int depth = 0;
            bool inString = false;
            bool escape = false;
            for (int i = start; i <= end; i++)
            {
                char ch = json[i];
                if (inString)
                {
                    if (escape)
                        escape = false;
                    else if (ch == '\\')
                        escape = true;
                    else if (ch == '"')
                        inString = false;
                    continue;
                }

                if (ch == '"')
                    inString = true;
                else if (ch == '{')
                    depth++;
                else if (ch == '}')
                    depth--;
                if (depth < 0)
                    return false;
            }

            return depth == 0 && !inString;
        }

        [Serializable]
        class SaveHeader
        {
            public int saveVersion;
        }
    }
}
