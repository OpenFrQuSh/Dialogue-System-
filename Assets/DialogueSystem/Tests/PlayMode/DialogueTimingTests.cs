using DialogueSystem.UI;
using NUnit.Framework;

namespace DialogueSystem.Tests
{
    public sealed class DialogueTimingTests
    {
        [Test]
        public void Tick_RevealsCharactersAtConfiguredRate()
        {
            var animator = new DialogueTextAnimator(20f);
            animator.Begin(6);

            animator.Tick(0.1f, 1f);

            Assert.That(animator.VisibleCharacterCount, Is.EqualTo(2));
            Assert.That(animator.IsComplete, Is.False);
        }

        [Test]
        public void Complete_RevealsEntireLineImmediately()
        {
            var animator = new DialogueTextAnimator(20f);
            animator.Begin(4);

            animator.Complete();

            Assert.That(animator.VisibleCharacterCount, Is.EqualTo(4));
            Assert.That(animator.IsComplete, Is.True);
        }

        [Test]
        public void AutoClock_PausePreventsElapsedTimeAndSpeedScalesDelay()
        {
            var clock = new DialogueAutoAdvanceClock();
            clock.Begin(10, 2f);
            clock.Tick(0.3f);
            clock.Pause();
            clock.Tick(1f);
            clock.Resume();
            clock.Tick(0.275f);

            Assert.That(clock.IsReady, Is.True);
        }
    }
}
