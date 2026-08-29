using System;
using UnityEditor;

namespace DialogueSystem.Editor
{
    public static class DialoguePackagePaths
    {
        public const string PackageId = "com.zxxuh.dialogue-system";
        public const string PackageRoot = "Packages/" + PackageId;
        public const string BundledFontSourcePath = PackageRoot + "/Fonts/NotoSansSC-Variable.ttf";
        public const string BundledFontAssetPath = PackageRoot + "/Fonts/NotoSansSC-Dynamic.asset";
        public const string GeneratedRoot = "Assets/DialogueSystemGenerated";
        public const string GeneratedSamplesRoot = GeneratedRoot + "/Samples";

        public static bool IsGeneratedAssetPath(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath)
                   && (string.Equals(assetPath, GeneratedRoot, StringComparison.Ordinal)
                       || assetPath.StartsWith(GeneratedRoot + "/", StringComparison.Ordinal));
        }

        public static void EnsureGeneratedFolder(string folderPath)
        {
            RequireGeneratedPath(folderPath);
            var parts = folderPath.Split('/');
            var parent = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var current = parent + "/" + parts[index];
                // AssetDatabase 只能逐层创建目录；逐段验证也能保证生成器不会越出自有根目录。
                if (!AssetDatabase.IsValidFolder(current))
                {
                    AssetDatabase.CreateFolder(parent, parts[index]);
                }

                parent = current;
            }
        }

        public static void DeleteGeneratedAsset(string assetPath)
        {
            RequireGeneratedPath(assetPath);
            // 仅删除生成器拥有的固定资源，避免菜单操作误伤主人自己的 Assets 内容。
            AssetDatabase.DeleteAsset(assetPath);
        }

        private static void RequireGeneratedPath(string assetPath)
        {
            // 所有编辑器写入入口都先校验边界，使未来新增生成器也不能越权修改用户资源。
            if (!IsGeneratedAssetPath(assetPath))
            {
                throw new InvalidOperationException(
                    $"[{PackageId}] 拒绝写入或删除非生成目录资源：{assetPath}。请使用 {GeneratedRoot}。 ");
            }
        }
    }
}
