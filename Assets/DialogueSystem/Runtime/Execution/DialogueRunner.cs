using System;
using System.Collections.Generic;
using DialogueSystem.Data;
using UnityEngine;

namespace DialogueSystem.Execution
{
    public sealed class DialogueRunner : MonoBehaviour
    {
        [SerializeField]
        private DialogueAsset startupDialogue;

        [SerializeField]
        private bool playOnStart;

        private readonly DialogueSession session = new DialogueSession();
        private DialogueAsset lastStartedAsset;
        private bool endedPublished;

        public event Action<DialoguePresentation> Presented;
        public event Action<IReadOnlyList<DialogueHistoryEntry>> HistoryChanged;
        public event Action<string> Ended;
        public event Action<string> Failed;

        public DialoguePresentation Current => session.Current;

        public bool IsRunning { get; private set; }

        private void Start()
        {
            // 空资产保护让 Prefab 可独立复用，而无需强制绑定示例对话资源。
            if (playOnStart && startupDialogue != null)
            {
                StartDialogue(startupDialogue);
            }
        }

        public void StartDialogue(DialogueAsset asset)
        {
            ExecuteSafely(() =>
            {
                session.Start(asset);
                lastStartedAsset = asset;
                IsRunning = true;
                endedPublished = false;
            });
        }

        public void Advance()
        {
            ExecuteSafely(session.Advance);
        }

        public void SelectChoice(int visibleChoiceIndex)
        {
            ExecuteSafely(() => session.SelectChoice(visibleChoiceIndex));
        }

        public void Skip()
        {
            ExecuteSafely(() => session.SkipToDecisionOrEnd());
        }

        public void Restart()
        {
            if (lastStartedAsset != null)
            {
                StartDialogue(lastStartedAsset);
            }
        }

        private void ExecuteSafely(Action operation)
        {
            try
            {
                operation();
                PublishState();
            }
            catch (Exception exception)
            {
                IsRunning = false;
                Failed?.Invoke(exception.Message);
                Debug.LogError("[DialogueSystem] " + exception, this);
            }
        }

        private void PublishState()
        {
            if (session.Current == null)
            {
                return;
            }

            Presented?.Invoke(session.Current);
            HistoryChanged?.Invoke(session.History);

            if (session.IsEnded)
            {
                IsRunning = false;
                if (!endedPublished)
                {
                    endedPublished = true;
                    Ended?.Invoke(session.EndingId);
                }
            }
        }
    }
}
