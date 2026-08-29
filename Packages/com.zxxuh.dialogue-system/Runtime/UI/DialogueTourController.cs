using System;
using System.Collections;
using System.Collections.Generic;
using DialogueSystem.Execution;
using UnityEngine;

namespace DialogueSystem.UI
{
    public enum DialogueTourState
    {
        Idle,
        Presenting,
        Transitioning,
        Completed,
        Failed
    }

    public sealed class DialogueTourController : MonoBehaviour
    {
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private DialogueView dialogueView;
        [SerializeField] private DialogueCameraSpline cameraSpline;
        [SerializeField] private DialogueCanvasFader canvasFader;
        [SerializeField] private List<DialogueTourStep> steps = new List<DialogueTourStep>();
        [SerializeField] private bool playOnStart;

        private Coroutine transitionRoutine;
        private bool isSubscribed;

        public int CurrentStepIndex { get; private set; } = -1;

        public DialogueTourState State { get; private set; } = DialogueTourState.Idle;

        public void Configure(
            DialogueRunner runner,
            DialogueCameraSpline spline,
            DialogueCanvasFader fader,
            IReadOnlyList<DialogueTourStep> tourSteps,
            bool startAutomatically = false)
        {
            Configure(runner, null, spline, fader, tourSteps, startAutomatically);
        }

        public void Configure(
            DialogueRunner runner,
            DialogueView view,
            DialogueCameraSpline spline,
            DialogueCanvasFader fader,
            IReadOnlyList<DialogueTourStep> tourSteps,
            bool startAutomatically = false)
        {
            Unsubscribe();
            dialogueRunner = runner;
            dialogueView = view;
            cameraSpline = spline;
            canvasFader = fader;
            steps = tourSteps == null ? new List<DialogueTourStep>() : new List<DialogueTourStep>(tourSteps);
            playOnStart = startAutomatically;
            Subscribe();
        }

        private void Start()
        {
            if (playOnStart)
            {
                BeginTour();
            }
        }

        public void BeginTour()
        {
            if (State == DialogueTourState.Presenting || State == DialogueTourState.Transitioning)
            {
                return;
            }

            Subscribe();
            if (!ValidateConfiguration(out var error))
            {
                FailTour(error);
                return;
            }

            StopTransition();
            CurrentStepIndex = 0;
            var firstStep = steps[0];

            try
            {
                cameraSpline.SnapToControlPoint(firstStep.PathPointIndex);
                canvasFader.ShowImmediate();
                // View 必须先订阅 Runner，首段同步发布的 Presented 事件才不会丢失。
                dialogueView?.Bind(dialogueRunner);
                State = DialogueTourState.Presenting;
                dialogueRunner.StartDialogue(firstStep.Dialogue);
            }
            catch (Exception exception)
            {
                FailTour(exception.Message);
            }
        }

        private void OnDialogueEnded(string endingId)
        {
            // 只有稳定展示状态可以消费结束事件，防止切换期间的重复通知连续跳步。
            if (State != DialogueTourState.Presenting)
            {
                return;
            }

            State = DialogueTourState.Transitioning;
            transitionRoutine = StartCoroutine(
                CurrentStepIndex >= steps.Count - 1 ? CompleteTour() : AdvanceStep());
        }

        private IEnumerator AdvanceStep()
        {
            yield return canvasFader.FadeOut(false);

            var nextIndex = CurrentStepIndex + 1;
            var nextStep = steps[nextIndex];
            yield return cameraSpline.MoveToControlPoint(nextStep.PathPointIndex, nextStep.MoveDuration);

            if (nextStep.ArrivalDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(nextStep.ArrivalDelay);
            }

            CurrentStepIndex = nextIndex;
            // 下一段在透明状态下先写入文本，淡入时不会短暂显示上一段内容。
            dialogueRunner.StartDialogue(nextStep.Dialogue);
            if (State == DialogueTourState.Failed)
            {
                yield break;
            }

            yield return canvasFader.FadeIn();
            transitionRoutine = null;
            State = DialogueTourState.Presenting;
        }

        private IEnumerator CompleteTour()
        {
            yield return canvasFader.FadeOut(true);
            transitionRoutine = null;
            State = DialogueTourState.Completed;
        }

        private void OnDialogueFailed(string message)
        {
            if (State == DialogueTourState.Completed || State == DialogueTourState.Failed)
            {
                return;
            }

            FailTour($"步骤 {CurrentStepIndex + 1} 播放失败：{message}");
        }

        private bool ValidateConfiguration(out string error)
        {
            if (dialogueRunner == null)
            {
                error = "未绑定 DialogueRunner。";
                return false;
            }

            if (cameraSpline == null)
            {
                error = "未绑定 DialogueCameraSpline。";
                return false;
            }

            if (canvasFader == null)
            {
                error = "未绑定 DialogueCanvasFader。";
                return false;
            }

            if (steps == null || steps.Count == 0)
            {
                error = "没有可播放的步骤。";
                return false;
            }

            for (var index = 0; index < steps.Count; index++)
            {
                var step = steps[index];
                if (step == null || step.Dialogue == null)
                {
                    error = $"步骤 {index + 1} 未绑定 DialogueAsset。";
                    return false;
                }

                if (step.PathPointIndex < 0 || step.PathPointIndex >= cameraSpline.ControlPointCount)
                {
                    error = $"步骤 {index + 1} 的路径点索引 {step.PathPointIndex} 无效。";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private void FailTour(string message)
        {
            StopTransition();
            State = DialogueTourState.Failed;

            // 失败时保留透明 Canvas 对象供检查，但关闭交互以免继续改变 Runner 状态。
            if (canvasFader != null)
            {
                canvasFader.HideImmediate(false);
            }

            Debug.LogError($"[DialogueSystem Tour] {name}：{message}", this);
        }

        private void Subscribe()
        {
            if (isSubscribed || dialogueRunner == null)
            {
                return;
            }

            dialogueRunner.Ended += OnDialogueEnded;
            dialogueRunner.Failed += OnDialogueFailed;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || dialogueRunner == null)
            {
                isSubscribed = false;
                return;
            }

            dialogueRunner.Ended -= OnDialogueEnded;
            dialogueRunner.Failed -= OnDialogueFailed;
            isSubscribed = false;
        }

        private void StopTransition()
        {
            if (transitionRoutine == null)
            {
                return;
            }

            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        private void OnDisable()
        {
            StopTransition();
            Unsubscribe();
        }

        private void OnDestroy()
        {
            StopTransition();
            Unsubscribe();
        }
    }
}
