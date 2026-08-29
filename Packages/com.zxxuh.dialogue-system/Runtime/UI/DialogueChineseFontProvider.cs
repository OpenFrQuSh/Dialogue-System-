using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DialogueSystem.UI
{
    public sealed class DialogueChineseFontProvider : MonoBehaviour
    {
        private static readonly string[] PreferredFontNames =
        {
            "Microsoft YaHei",
            "Microsoft YaHei UI",
            "微软雅黑",
            "SimHei",
            "黑体",
            "SimSun",
            "宋体",
            "Noto Sans CJK SC",
            "Noto Sans SC",
            "Arial Unicode MS"
        };

        [SerializeField] private Transform textRoot;
        [SerializeField] private TMP_FontAsset bundledFontAsset;

        private bool warningPublished;

        public string SelectedFontName { get; private set; }

        public void Configure(Transform root, TMP_FontAsset fontAsset = null)
        {
            textRoot = root;
            bundledFontAsset = fontAsset;
        }

        private void Awake()
        {
            ApplyFont();
        }

        public bool ApplyFont()
        {
            var root = textRoot == null ? transform : textRoot;
            if (bundledFontAsset == null)
            {
                PublishMissingFontWarning();
                return false;
            }

            SelectedFontName = bundledFontAsset.name;
            // 包含 inactive 子对象，确保历史面板和选项模板第一次打开时已经支持中文。
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = bundledFontAsset;
            }

            return true;
        }

        public static string SelectInstalledFont(IReadOnlyCollection<string> installedFontNames)
        {
            if (installedFontNames == null || installedFontNames.Count == 0)
            {
                return null;
            }

            // 先遍历固定候选，再返回系统提供的真实名称，兼顾稳定优先级与大小写差异。
            foreach (var preferredName in PreferredFontNames)
            {
                foreach (var installedName in installedFontNames)
                {
                    if (string.Equals(preferredName, installedName, StringComparison.OrdinalIgnoreCase))
                    {
                        return installedName;
                    }
                }
            }

            return null;
        }

        private void PublishMissingFontWarning()
        {
            if (warningPublished)
            {
                return;
            }

            warningPublished = true;
            Debug.LogWarning(
                "[DialogueSystem] 未绑定随包发布的中文 TMP 字体，示例将回退到默认字体。" +
                "请重新执行 Tools/Dialogue System/Create Guided Tour Samples。",
                this);
        }
    }
}
