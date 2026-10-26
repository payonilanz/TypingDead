using System;
using System.Drawing;

namespace TypingDead.Effects
{
    /// <summary>
    /// Abstract base for short-lived, self-contained effects. The manager can
    /// update and draw every effect without knowing its concrete type.
    /// </summary>
    public abstract class VisualEffect
    {
        public float Age { get; protected set; }
        public float Duration { get; protected set; }

        public bool IsFinished
        {
            get { return Age >= Duration; }
        }

        protected VisualEffect(float duration)
        {
            Duration = Math.Max(0.01f, duration);
            Age = 0f;
        }

        public virtual void Update(float deltaTime)
        {
            Age += deltaTime;
        }

        public abstract void Draw(Graphics g);
    }
}
