namespace GYM.Helpers
{
    public static class LevelHelper
    {
        // Each level requires progressively more EXP
        // Converts total EXP into the user's current level (each level costs 50 more EXP than the last)
        public static int GetLevel(int exp)
        {
            int level = 1;
            int required = 100;
            int remaining = exp;

            while (remaining >= required)
            {
                remaining -= required;
                level++;
                required += 50; // each level needs 50 more EXP than the last
            }

            return level;
        }

        // Returns how much EXP the user has accumulated within their current level
        public static int GetExpIntoCurrentLevel(int exp)
        {
            int required = 100;
            int remaining = exp;

            while (remaining >= required)
            {
                remaining -= required;
                required += 50;
            }

            return remaining;
        }

        // Returns how much total EXP is needed to complete the current level
        public static int GetExpNeededForNextLevel(int exp)
        {
            int required = 100;
            int remaining = exp;

            while (remaining >= required)
            {
                remaining -= required;
                required += 50;
            }

            return required;
        }
    }
}