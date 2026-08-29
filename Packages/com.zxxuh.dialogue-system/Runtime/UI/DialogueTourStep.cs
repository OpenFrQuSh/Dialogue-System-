using System;
using DialogueSystem.Data;
using UnityEngine;

namespace DialogueSystem.UI
{
    [Serializable]
    public sealed class DialogueTourStep
    {
        [SerializeField] private DialogueAsset dialogue;
        [SerializeField] private int pathPointIndex;
        [SerializeField, Min(0f)] private float moveDuration;
        [SerializeField, Min(0f)] private float arrivalDelay;

        public DialogueTourStep(
            DialogueAsset dialogueAsset,
            int targetPathPointIndex,
            float cameraMoveDuration,
            float delayAfterArrival)
        {
            dialogue = dialogueAsset;
            pathPointIndex = targetPathPointIndex;
            moveDuration = Mathf.Max(0f, cameraMoveDuration);
            arrivalDelay = Mathf.Max(0f, delayAfterArrival);
        }

        public DialogueAsset Dialogue => dialogue;

        public int PathPointIndex => pathPointIndex;

        public float MoveDuration => moveDuration;

        public float ArrivalDelay => arrivalDelay;
    }
}
