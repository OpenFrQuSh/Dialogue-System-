using System;

namespace DialogueSystem.UI
{
    public sealed class DialogueTextAnimator
    {
        private readonly float charactersPerSecond;
        private float revealedCharacters;
        private int totalCharacters;

        public DialogueTextAnimator(float charactersPerSecond)
        {
            if (charactersPerSecond <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(charactersPerSecond));
            }

            this.charactersPerSecond = charactersPerSecond;
        }

        public int VisibleCharacterCount { get; private set; }

        public bool IsComplete => VisibleCharacterCount >= totalCharacters;

        public void Begin(int visibleCharacterCount)
        {
            if (visibleCharacterCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(visibleCharacterCount));
            }

            totalCharacters = visibleCharacterCount;
            revealedCharacters = 0f;
            VisibleCharacterCount = 0;
        }

        public void Tick(float deltaTime, float speedMultiplier)
        {
            if (IsComplete)
            {
                return;
            }

            if (deltaTime < 0f || speedMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException("Timing values must be non-negative and speed must be positive.");
            }

            revealedCharacters += deltaTime * charactersPerSecond * speedMultiplier;
            VisibleCharacterCount = Math.Min(totalCharacters, (int)Math.Floor(revealedCharacters));
        }

        public void Complete()
        {
            revealedCharacters = totalCharacters;
            VisibleCharacterCount = totalCharacters;
        }
    }
}
