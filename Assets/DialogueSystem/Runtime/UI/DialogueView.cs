using DialogueSystem.Data;
using DialogueSystem.Execution;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DialogueSystem.UI
{
    public sealed class DialogueView : MonoBehaviour
    {
        // 速度档位覆盖逐字与自动等待，符合对话界面的统一倍速预期。
        private static readonly float[] PlaybackSpeeds = { 1f, 2f, 4f };

        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text speedLabel;
        [SerializeField] private TMP_Text autoLabel;
        private readonly DialogueTextAnimator animator = new DialogueTextAnimator(30f);
        private readonly DialogueAutoAdvanceClock autoAdvanceClock = new DialogueAutoAdvanceClock();
        private DialogueRunner runner;
        // 生成器会在编辑器阶段绑定这些面板，必须序列化才能在场景保存、关闭并重新加载后继续收到事件。
        [SerializeField] private DialogueHistoryPanel historyPanel;
        [SerializeField] private DialogueChoiceListPanel choicePanel;
        private int playbackSpeedIndex;
        private bool autoAdvanceEnabled;
        private bool autoClockStarted;

        public float PlaybackSpeed => PlaybackSpeeds[playbackSpeedIndex];

        public bool IsAutoAdvanceEnabled => autoAdvanceEnabled;

        public void Bind(DialogueRunner dialogueRunner)
        {
            if (runner != null)
            {
                runner.Presented -= OnPresented;
                runner.HistoryChanged -= OnHistoryChanged;
            }

            runner = dialogueRunner;
            if (runner != null)
            {
                runner.Presented += OnPresented;
                runner.HistoryChanged += OnHistoryChanged;
            }
        }

        public void BindHistoryPanel(DialogueHistoryPanel panel)
        {
            historyPanel = panel;
        }

        public void BindChoicePanel(DialogueChoiceListPanel panel)
        {
            choicePanel = panel;
        }

        public void ConfigureControlLabels(TMP_Text speedText, TMP_Text autoText)
        {
            speedLabel = speedText;
            autoLabel = autoText;
            RefreshControlLabels();
        }

        public void HandleAdvanceClick()
        {
            if (runner == null || runner.Current == null) return;
            if (runner.Current.Kind == DialogueNodeKind.Line && !animator.IsComplete)
            {
                animator.Complete();
                ApplyVisibleCharacters();
                return;
            }
            if (runner.Current.Kind == DialogueNodeKind.Line) runner.Advance();
        }

        public void HandleSpeedClick()
        {
            playbackSpeedIndex = (playbackSpeedIndex + 1) % PlaybackSpeeds.Length;
            RefreshControlLabels();
        }

        public void HandleAutoClick()
        {
            // 自动模式只改变推进策略，不影响玩家随时手动点击推进。
            autoAdvanceEnabled = !autoAdvanceEnabled;
            RefreshControlLabels();
        }

        public void HandleSkipClick()
        {
            // 跳过委托给 Runner，确保历史记录与普通推进路径完全一致。
            runner?.Skip();
        }

        // 将帧驱动暴露为可控入口，既供 Unity Update 调用，也让自动推进可被确定性测试。
        public void Tick(float unscaledDeltaTime)
        {
            if (runner == null || runner.Current == null || runner.Current.Kind != DialogueNodeKind.Line)
            {
                return;
            }

            if (!animator.IsComplete)
            {
                animator.Tick(unscaledDeltaTime, PlaybackSpeed);
                ApplyVisibleCharacters();
                return;
            }

            if (!autoAdvanceEnabled)
            {
                return;
            }

            if (!autoClockStarted)
            {
                autoAdvanceClock.Begin(animator.VisibleCharacterCount, PlaybackSpeed);
                autoClockStarted = true;
            }

            autoAdvanceClock.Tick(unscaledDeltaTime);
            if (autoAdvanceClock.IsReady)
            {
                autoClockStarted = false;
                runner.Advance();
            }
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        private void OnPresented(DialoguePresentation presentation)
        {
            choicePanel?.ClearChoices();
            if (presentation.Kind == DialogueNodeKind.Choice)
            {
                // Presentation 的 Choices 已完成条件过滤，索引可原样传回 Runner。
                choicePanel?.ShowChoices(presentation.Choices, index => runner?.SelectChoice(index));
            }

            if (speakerText != null) speakerText.text = presentation.Speaker ?? string.Empty;
            if (bodyText == null) return;
            bodyText.text = presentation.Text ?? string.Empty;
            bodyText.ForceMeshUpdate();
            animator.Begin(bodyText.textInfo == null ? 0 : bodyText.textInfo.characterCount);
            autoClockStarted = false;
            ApplyVisibleCharacters();
        }

        private void ApplyVisibleCharacters()
        {
            if (bodyText != null) bodyText.maxVisibleCharacters = animator.VisibleCharacterCount;
        }

        private void RefreshControlLabels()
        {
            if (speedLabel != null)
            {
                speedLabel.text = PlaybackSpeed.ToString("0") + "X";
            }

            if (autoLabel != null)
            {
                autoLabel.text = autoAdvanceEnabled ? "AUTO ON" : "AUTO";
            }
        }

        private void OnHistoryChanged(IReadOnlyList<DialogueHistoryEntry> history)
        {
            historyPanel?.SetHistory(history);
        }

        private void OnDestroy()
        {
            if (runner != null)
            {
                runner.Presented -= OnPresented;
                runner.HistoryChanged -= OnHistoryChanged;
            }
        }
    }
}
