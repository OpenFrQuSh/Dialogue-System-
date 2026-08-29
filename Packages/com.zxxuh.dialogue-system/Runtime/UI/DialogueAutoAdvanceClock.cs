using System;

namespace DialogueSystem.UI
{
    public sealed class DialogueAutoAdvanceClock
    {
        private const float BaseDelaySeconds = 0.65f;
        private const float DelayPerCharacterSeconds = 0.025f;
        private float targetSeconds;
        private float elapsedSeconds;
        private bool isPaused;

        public bool IsReady => elapsedSeconds >= targetSeconds;

        public void Begin(int visibleCharacterCount, float speedMultiplier)
        {
            if (visibleCharacterCount < 0 || speedMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException("Auto advance requires a non-negative character count and positive speed.");
            }

            targetSeconds = (BaseDelaySeconds + visibleCharacterCount * DelayPerCharacterSeconds) / speedMultiplier;
            elapsedSeconds = 0f;
            isPaused = false;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (!isPaused && !IsReady)
            {
                elapsedSeconds += deltaTime;
            }
        }

        public void Pause()
        {
            isPaused = true;
        }

        public void Resume()
        {
            isPaused = false;
        }
    }
}
