using DialogueSystem.Data;
using UnityEditor;
using UnityEngine;

namespace DialogueSystem.Editor
{
    [CustomEditor(typeof(DialogueAsset))]
    public sealed class DialogueAssetInspector : UnityEditor.Editor
    {
        // 自定义 Inspector 将作者数据与校验结果放在同一处，降低配置遗漏导致的运行时错误。
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dialogue Validation", EditorStyles.boldLabel);

            if (GUILayout.Button("Validate Dialogue"))
            {
                ValidateAndDisplay();
            }
        }

        private void ValidateAndDisplay()
        {
            var issues = DialogueAssetValidator.Validate((DialogueAsset)target);
            if (issues.Count == 0)
            {
                Debug.Log("[DialogueSystem] 对话验证通过。", target);
                return;
            }

            foreach (var issue in issues)
            {
                var message = "[" + issue.Code + "] " + issue.Message;
                if (issue.Severity == DialogueValidationSeverity.Error)
                {
                    Debug.LogError("[DialogueSystem] " + message, target);
                }
                else
                {
                    Debug.LogWarning("[DialogueSystem] " + message, target);
                }
            }
        }
    }
}
