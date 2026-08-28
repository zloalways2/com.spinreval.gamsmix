using UnityEngine;

namespace Utils.Formatter
{
    public class DurationFormatter
    {
        public string FormatDuraion(float durationInSeconds)
        {
            int totalSeconds = Mathf.RoundToInt(durationInSeconds);
            int seconds = totalSeconds % 60;
            int minutes = totalSeconds / 60;

            if (seconds == 0)
            {
                return $"{minutes} мин";
            }
            else if (minutes == 0)
            {
                return $"{seconds} сек";
            }
            else
            {
                return $"{minutes} мин {seconds} сек";
            }
        }
    }
}