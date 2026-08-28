namespace Core.WheelOfLuck
{
    public readonly struct ResultConfig
    {
        public readonly bool IsWin;
        public readonly int BaseXP;
        public readonly bool ReportAnalytics;

        public ResultConfig(bool isWin, int baseXP, bool reportAnalytics = true)
        {
            IsWin = isWin;
            BaseXP = baseXP;
            ReportAnalytics = reportAnalytics;
        }
    }
}