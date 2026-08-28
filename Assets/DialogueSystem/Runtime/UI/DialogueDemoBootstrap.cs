using DialogueSystem.Data;
using DialogueSystem.Execution;
using UnityEngine;

namespace DialogueSystem.UI
{
    public sealed class DialogueDemoBootstrap : MonoBehaviour
    {
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private DialogueView dialogueView;
        [SerializeField] private DialogueAsset dialogueAsset;

        private void Start()
        {
            // 示例场景在运行时完成绑定，避免 Unity 的组件 Start 顺序影响事件订阅。
            dialogueView.Bind(dialogueRunner);
            dialogueRunner.StartDialogue(dialogueAsset);
        }
    }
}
