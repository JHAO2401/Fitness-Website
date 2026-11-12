namespace GYM.Helpers
{
    public static class RankHelper
    {
        // Maps a Lbs value to a rank name (Bronze → God)
        public static string GetRank(int lbs)
        {
            if (lbs >= 1000) return "God";
            if (lbs >= 700) return "Titan";
            if (lbs >= 500) return "Champion";
            if (lbs >= 400) return "Diamond";
            if (lbs >= 300) return "Platinum";
            if (lbs >= 200) return "Gold";
            if (lbs >= 100) return "Silver";
            return "Bronze";
        }

        // Returns the image URL for a Lbs-based rank
        public static string GetRankImageUrl(int lbs)
        {
            return $"/image/RankUP/{GetRank(lbs)}.png";
        }

        // Maps a Level value to a rank name (Bronze → God)
        public static string GetRankByLevel(int level)
        {
            if (level >= 40) return "God";
            if (level >= 30) return "Titan";
            if (level >= 25) return "Champion";
            if (level >= 20) return "Diamond";
            if (level >= 15) return "Platinum";
            if (level >= 10) return "Gold";
            if (level >= 5) return "Silver";
            return "Bronze";
        }

        // Returns the image URL for a Level-based rank
        public static string GetRankImageUrlByLevel(int level)
        {
            return $"/image/RankUP/{GetRankByLevel(level)}.png";
        }
    }
}