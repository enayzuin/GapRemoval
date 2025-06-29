using System;

namespace GapRemovalApp.Utils
{
    public class VideoTimeCoordinator
    {
        public TimeSpan CurrentTime { get; private set; }
        public TimeSpan Duration { get; private set; }

        public event Action<TimeSpan>? TimeChanged;
        public event Action<TimeSpan>? DurationChanged;

        public void SetTime(TimeSpan time)
        {
            if (time != CurrentTime)
            {
                CurrentTime = time;
                TimeChanged?.Invoke(CurrentTime);
            }
        }

        public void SetDuration(TimeSpan duration)
        {
            if (duration != Duration)
            {
                Duration = duration;
                DurationChanged?.Invoke(Duration);
            }
        }
    }
} 