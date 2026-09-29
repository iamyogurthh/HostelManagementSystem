namespace HostelManagementSystem.Models
{
    public class NrcTownship
    {
        public int RegionCode { get; set; }           // e.g., 12
        public string StateOrRegion { get; set; } = string.Empty; // e.g., "Yangon"
        public string TownshipMm { get; set; } = string.Empty;    // e.g., "ကမာရွတ်မြို့နယ်"
        public string TownshipEng { get; set; } = string.Empty;   // e.g., "Kamayut Township"
        public string CodeMm { get; set; } = string.Empty;        // e.g., "ကမရ"
        public string CodeEng { get; set; } = string.Empty;       // e.g., "KaMaYa"

        // Formatted Full NRC Prefix in English (e.g., "12/KaMaYa")
        public string FullNrcPrefixEng => $"{RegionCode}/{CodeEng}";

        // Formatted Full NRC Prefix in Myanmar (e.g., "၁၂/ကမရ")
        public string FullNrcPrefixMm => $"{ToMyanmarDigits(RegionCode)}/{CodeMm}";

        private static string ToMyanmarDigits(int number)
        {
            string str = number.ToString();
            return str.Replace('0', '၀')
                      .Replace('1', '၁')
                      .Replace('2', '၂')
                      .Replace('3', '၃')
                      .Replace('4', '၄')
                      .Replace('5', '၅')
                      .Replace('6', '၆')
                      .Replace('7', '၇')
                      .Replace('8', '၈')
                      .Replace('9', '၉');
        }
    }
}