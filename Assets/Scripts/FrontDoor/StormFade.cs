using UnityEngine;

namespace Caravans.FrontDoor
{
    // Smoothstep fade of the title storm. A second Begin does not restart the
    // clock: leaving house select early has to let the fade already running finish.
    public sealed class StormFade
    {
        public bool Active { get; private set; }
        public bool Finished { get; private set; }
        public float Visual { get; private set; } = 1f;
        public float AudioAmplitude { get; private set; } = 1f;

        float audioFrom = 1f;
        float elapsed;
        float duration = 2.2f;

        public void Begin(float currentAudioAmplitude, float durationSeconds)
        {
            if (Active || Finished)
                return;
            Active = true;
            audioFrom = Mathf.Clamp01(currentAudioAmplitude);
            elapsed = 0f;
            duration = Mathf.Max(0.05f, durationSeconds);
            Visual = 1f;
            AudioAmplitude = audioFrom;
        }

        public void Tick(float delta)
        {
            if (!Active)
                return;
            elapsed += Mathf.Max(0f, delta);
            float u = Mathf.Clamp01(elapsed / duration);
            float shaped = u * u * (3f - 2f * u);
            Visual = 1f - shaped;
            AudioAmplitude = Mathf.Lerp(audioFrom, 0f, shaped);
            if (u < 1f)
                return;
            Active = false;
            Finished = true;
            Visual = 0f;
            AudioAmplitude = 0f;
        }

        public void Resume()
        {
            Active = false;
            Finished = false;
            Visual = 1f;
            AudioAmplitude = 1f;
            audioFrom = 1f;
            elapsed = 0f;
        }
    }
}
