using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Sorajiro.Editor
{
    [Serializable] public sealed class GeneratedReceipt
    {
        public string recipe;
        public string unity;
        public string hdrp;
        public string state = "generated_placeholder";
        public bool renderVerified;
        public string[] files;
        public string[] hashes;
    }

    public static class GeneratedContentGuard
    {
        public const string Output = "Assets/Sorajiro/Generated/M1_v001";
        public const string ScenePath = Output + "/L_LookDev.unity";
        public const string Recipe = "sorajiro-m1-v001-meters-yup-zforward-no-fur-20261012";
        public const string ReceiptName = "generation-receipt.json";

        public static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }

        public static string[] Files(string folder)
        {
            if (!Directory.Exists(folder)) return Array.Empty<string>();
            RejectLinksRecursively(folder);
            return Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                .Where(p => Path.GetFileName(p) != ReceiptName && Path.GetFileName(p) != ReceiptName + ".meta")
                .OrderBy(p => p, StringComparer.Ordinal).ToArray()
                ;
        }

        private static void RejectLinksRecursively(string folder)
        {
            if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Symlink generated content refused.");
            foreach (string entry in Directory.GetFileSystemEntries(folder))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Symlink generated content refused.");
                if ((attributes & FileAttributes.Directory) != 0) RejectLinksRecursively(entry);
            }
        }

        public static bool CanReuse(string folder, string unity, string hdrp, string recipe = Recipe)
        {
            string receiptPath = Path.Combine(folder, ReceiptName);
            string[] files = Files(folder);
            if (!File.Exists(receiptPath))
            {
                if (files.Length != 0) throw new InvalidOperationException("Existing content has no receipt. Preserve it and use a new revision.");
                return false;
            }
            GeneratedReceipt receipt = JsonUtility.FromJson<GeneratedReceipt>(File.ReadAllText(receiptPath));
            string[] relative = files.Select(p => Path.GetRelativePath(folder, p).Replace('\\', '/')).ToArray();
            if (receipt == null || receipt.recipe != recipe || receipt.unity != unity || receipt.hdrp != hdrp ||
                receipt.files == null || receipt.hashes == null || receipt.files.Length == 0 ||
                !relative.SequenceEqual(receipt.files) || !files.Select(HashFile).SequenceEqual(receipt.hashes) ||
                !relative.Contains("L_LookDev.unity"))
                throw new InvalidOperationException("Generated content/recipe/engine differs. No overwrite allowed; use a new revision.");
            return true;
        }

        public static void WriteReceipt(string folder, string unity, string hdrp, string recipe = Recipe)
        {
            string path = Path.Combine(folder, ReceiptName);
            if (File.Exists(path)) throw new InvalidOperationException("Receipt already exists; no overwrite allowed.");
            string[] files = Files(folder);
            var receipt = new GeneratedReceipt {
                recipe = recipe, unity = unity, hdrp = hdrp,
                files = files.Select(p => Path.GetRelativePath(folder, p).Replace('\\', '/')).ToArray(),
                hashes = files.Select(HashFile).ToArray()
            };
            File.WriteAllText(path, JsonUtility.ToJson(receipt, true) + "\n", new UTF8Encoding(false));
        }
    }
}
