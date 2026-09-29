using HostelManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HostelManagementSystem.Utilities
{
    public class NrcManager
    {
        private static readonly Lazy<NrcManager> _instance = new(() => new NrcManager());
        public static NrcManager Instance => _instance.Value;

        public List<NrcTownship> AllTownships { get; private set; }

        public List<string> CitizenTypes { get; } = new()
        {
            "(နိုင်)", // National / Citizen
            "(ဧည့်)",  // Associate Citizen
            "(ပြု)",   // Naturalized Citizen
            "(သ)"    // Special / Other
        };

        public NrcManager()
        {
            AllTownships = LoadTownships();
        }

        #region Public Methods

        /// <summary>
        /// Gets all available Region/State numbers (1 to 14)
        /// </summary>
        public List<int> GetRegionNumbers()
        {
            return AllTownships.Select(t => t.RegionCode).Distinct().OrderBy(x => x).ToList();
        }

        /// <summary>
        /// Gets formatted region codes (e.g., "1/", "12/")
        /// </summary>
        public List<string> GetRegionCodesFormatted()
        {
            return GetRegionNumbers().Select(r => $"{r}/").ToList();
        }

        /// <summary>
        /// Filter township codes by Region Code (English code: KaMaYa)
        /// </summary>
        public List<string> GetTownshipCodesEngByRegion(int regionCode)
        {
            return AllTownships
                .Where(t => t.RegionCode == regionCode)
                .Select(t => t.CodeEng)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        /// <summary>
        /// Filter township codes by Region Code (Myanmar code: ကမရ)
        /// </summary>
        public List<string> GetTownshipCodesMmByRegion(int regionCode)
        {
            return AllTownships
                .Where(t => t.RegionCode == regionCode)
                .Select(t => t.CodeMm)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        /// <summary>
        /// Validate if an entire NRC string is formatted correctly.
        /// Example valid NRC: 12/KaMaYa(N)123456 or 12/ကမရ(နိုင်)123456
        /// </summary>
        public bool ValidateNrc(int regionCode, string townshipCode, string citizenType, string number)
        {
            if (regionCode < 1 || regionCode > 14) return false;
            if (string.IsNullOrWhiteSpace(townshipCode)) return false;
            if (string.IsNullOrWhiteSpace(number) || number.Length != 6 || !number.All(char.IsDigit)) return false;

            return AllTownships.Any(t => t.RegionCode == regionCode &&
                                        (t.CodeEng.Equals(townshipCode, StringComparison.OrdinalIgnoreCase) ||
                                         t.CodeMm.Equals(townshipCode, StringComparison.OrdinalIgnoreCase)));
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Gets distinct state/region names (e.g., "Kachin", "Kayah", "Yangon")
        /// </summary>
        public List<string> GetStateNames()
        {
            return AllTownships
                .Select(t => t.StateOrRegion)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Gets state/region mappings with their numeric code (e.g., "1 - Kachin", "12 - Yangon")
        /// </summary>
        public List<string> GetStateCodes()
        {
            return AllTownships
                .Select(t => $"{t.RegionCode} - {t.StateOrRegion}")
                .Distinct()
                .OrderBy(x => int.Parse(x.Split(" - ")[0]))
                .ToList();
        }

        /// <summary>
        /// Gets a dictionary mapping RegionCode to State/Region Name (e.g., Key: 1, Value: "Kachin")
        /// </summary>
        public Dictionary<int, string> GetStateCodeMap()
        {
            return AllTownships
                .GroupBy(t => t.RegionCode)
                .ToDictionary(g => g.Key, g => g.First().StateOrRegion);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets all citizen/national registration types e.g., "(နိုင်)", "(ဧည့်)", "(ပြု)", "(သ)"
        /// </summary>
        public List<string> GetNrcTypes()
        {
            return CitizenTypes;
        }

        /// <summary>
        /// Gets all townships for a given Region Code (1 to 14)
        /// </summary>
        public List<NrcTownship> GetTownshipsByState(int regionCode)
        {
            return AllTownships
                .Where(t => t.RegionCode == regionCode)
                .OrderBy(t => t.TownshipEng)
                .ToList();
        }

        /// <summary>
        /// Gets all townships by State/Region name (e.g., "Kachin", "Yangon")
        /// </summary>
        public List<NrcTownship> GetTownshipsByState(string stateOrRegion)
        {
            if (string.IsNullOrWhiteSpace(stateOrRegion))
                return new List<NrcTownship>();

            return AllTownships
                .Where(t => t.StateOrRegion.Equals(stateOrRegion, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.TownshipEng)
                .ToList();
        }

        #endregion
        #region Data Loader

        private List<NrcTownship> LoadTownships()
        {
            return new List<NrcTownship>
            {
                // ---------------- 1 / KACHIN ----------------
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ဗန်းမော်မြို့နယ်", TownshipEng = "Bhamo Township", CodeMm = "", CodeEng = "BaMaNa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ချီဖွေမြို့နယ်", TownshipEng = "Chipwi Township", CodeMm = "ခဖန", CodeEng = "KhaHpaNa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ဒေါဖုန်းယန်မြို့နယ်", TownshipEng = "Dawthponeyan Township", CodeMm = "ဒဖယ", CodeEng = "DaHpaYa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ဟိုပင်မြို့နယ်", TownshipEng = "Hopin Township", CodeMm = "ဟပန", CodeEng = "HaPaNa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ဖားကန့်မြို့နယ်", TownshipEng = "Hpakant Township", CodeMm = "ဖကန", CodeEng = "HpaKaNa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "အင်ဂျန်ယန်မြို့နယ်", TownshipEng = "Injangyang Township", CodeMm = "အကယ", CodeEng = "AhGaYa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ကာမိုင်မြို့နယ်", TownshipEng = "Kamaing Township", CodeMm = "ကမတ", CodeEng = "KaMaTa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ကန်ပိုက်တီမြို့နယ်", TownshipEng = "Kan Paik Ti Township", CodeMm = "", CodeEng = "KaPaTa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ခေါင်လန်ဖူးမြို့နယ်", TownshipEng = "Khaunglanhpu Township", CodeMm = "ခလဖ", CodeEng = "KhaLaHpa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "လွယ်ဂျယ်မြို့နယ်", TownshipEng = "Lwegel Township", CodeMm = "လဂန", CodeEng = "LaGaNa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "မချမ်းဘောမြို့နယ်", TownshipEng = "Machanbaw Township", CodeMm = "မခဘ", CodeEng = "MaKhaBa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "မန်စီမြို့နယ်", TownshipEng = "Mansi Township", CodeMm = "မစန", CodeEng = "MaSaNa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "မိုးကောင်းမြို့နယ်", TownshipEng = "Mogaung Township", CodeMm = "မကတ", CodeEng = "MaKaTa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "မိုးညှင်းမြို့နယ်", TownshipEng = "Mohnyin Township", CodeMm = "မညန", CodeEng = "MaNyaNa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "မိုးမောက်မြို့နယ်", TownshipEng = "Momauk Township", CodeMm = "မမန", CodeEng = "MaMaNa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "မြစ်ကြီးနားမြို့နယ်", TownshipEng = "Myitkyina Township", CodeMm = "မကန", CodeEng = "MaKaNa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "မြို့လှမြို့နယ်", TownshipEng = "Myo Hla Township", CodeMm = "မလန", CodeEng = "MaLaNa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "နောင်မွန်းမြို့နယ်", TownshipEng = "Nawngmun Township", CodeMm = "နမန", CodeEng = "NaMaNa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ပန်ဝါမြို့နယ်", TownshipEng = "Pang War Township", CodeMm = "ပဝန", CodeEng = "PaWaNa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ပန်နန်ဒင်မြို့နယ်", TownshipEng = "Pannandin Township", CodeMm = "ပနဒ", CodeEng = "PaNaDa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ပူတာအိုမြို့နယ်", TownshipEng = "Puta-O Township", CodeMm = "ပတအ", CodeEng = "PaTaAh" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ဆားတောင်မြို့နယ်", TownshipEng = "Sadung Township", CodeMm = "ဆဒန", CodeEng = "SaDaNa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ရှင်ဗွေယန်မြို့နယ်", TownshipEng = "Shin Bway Yang Township", CodeMm = "ရဗယ", CodeEng = "YaBaYa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ရွှေကူမြို့နယ်", TownshipEng = "Shwegu Township", CodeMm = "ရကန", CodeEng = "YaKaNa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ဆင်ဘိုမြို့နယ်", TownshipEng = "Sinbo Township", CodeMm = "ဆဘန", CodeEng = "SaBaNa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ဆွမ်ပရာဘွမ်မြို့နယ်", TownshipEng = "Sumprabum Township", CodeMm = "ဆပရ", CodeEng = "SaPaYa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "တနိုင်းမြို့နယ်", TownshipEng = "Tanai Township", CodeMm = "တနန", CodeEng = "TaNaNa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ဆော်လော်မြို့နယ်", TownshipEng = "Tsawlaw Township", CodeMm = "", CodeEng = "TaSaLa" },
                new NrcTownship { RegionCode = 1, StateOrRegion = "Kachin", TownshipMm = "ဝိုင်းမော်မြို့နယ်", TownshipEng = "Waingmaw Township", CodeMm = "ဝမန", CodeEng = "WaMaNa" },

                // ---------------- 2 / KAYAH ----------------
                new NrcTownship { RegionCode = 2, StateOrRegion = "Kayah", TownshipMm = "ဘောလခဲမြို့နယ်", TownshipEng = "Bawlakhe Township", CodeMm = "ဘလခ", CodeEng = "BaLaKha" },
                new NrcTownship { RegionCode = 2, StateOrRegion = "Kayah", TownshipMm = "ဒီမောဆိုးမြို့နယ်", TownshipEng = "Demoso Township", CodeMm = "", CodeEng = "DaMaSa" },
                new NrcTownship { RegionCode = 2, StateOrRegion = "Kayah", TownshipMm = "ဖားဆောင်းမြို့နယ်", TownshipEng = "Hpasawng Township", CodeMm = "ဖဆန", CodeEng = "HpaSaNa" },
                new NrcTownship { RegionCode = 2, StateOrRegion = "Kayah", TownshipMm = "ဖရူဆိုမြို့နယ်", TownshipEng = "Hpruso Township", CodeMm = "ဖရဆ", CodeEng = "HpaYaSa" },
                new NrcTownship { RegionCode = 2, StateOrRegion = "Kayah", TownshipMm = "လွိုင်ကော်မြို့နယ်", TownshipEng = "Loikaw Township", CodeMm = "လကန", CodeEng = "LaKaNa" },
                new NrcTownship { RegionCode = 2, StateOrRegion = "Kayah", TownshipMm = "မယ်စဲ့မြို့နယ်", TownshipEng = "Mese Township", CodeMm = "ပစန", CodeEng = "MaSaNa" },
                new NrcTownship { RegionCode = 2, StateOrRegion = "Kayah", TownshipMm = "ရှားတောမြို့နယ်", TownshipEng = "Shadaw Township", CodeMm = "ရတန", CodeEng = "YaTaNa" },
                new NrcTownship { RegionCode = 2, StateOrRegion = "Kayah", TownshipMm = "ရွာသစ်မြို့နယ်", TownshipEng = "Ywarthit Township", CodeMm = "ရသန", CodeEng = "YaThaNa" },

                // ---------------- 3 / KAYIN ----------------
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "ဘောကလိမြို့နယ်", TownshipEng = "Baw Ga Li Township", CodeMm = "", CodeEng = "BaGaLa" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "လှိုင်းဘွဲ့မြို့နယ်", TownshipEng = "Hlaingbwe Township", CodeMm = "လဘန", CodeEng = "LaBaNa" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "ဘားအံမြို့နယ်", TownshipEng = "Hpa-An Township", CodeMm = "ဘအန", CodeEng = "BaAhNa" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "ဖာပွန်မြို့နယ်", TownshipEng = "Hpapun Township", CodeMm = "ဖပန", CodeEng = "HpaPaNa" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "ဘုရားသုံးဆူမြို့နယ်", TownshipEng = "Hpayarthonesu Township", CodeMm = "ဘသဆ", CodeEng = "BaThaSa" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "ကမာမောင်းမြို့နယ်", TownshipEng = "Kamarmaung Township", CodeMm = "ကမမ", CodeEng = "KaMaMa" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "ကော့ကရိတ်မြို့နယ်", TownshipEng = "Kawkareik Township", CodeMm = "ကကရ", CodeEng = "KaKaYa" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "ကျိုက်ဒုံမြို့နယ်", TownshipEng = "Kyaikdon Township", CodeMm = "ကဒန", CodeEng = "KaDaNa" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "ကြာအင်းဆိပ်ကြီးမြို့နယ်", TownshipEng = "Kyainseikgyi Township", CodeMm = "", CodeEng = "KaSaKa" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "ကျုံဒိုးမြို့နယ်", TownshipEng = "Kyondoe Township", CodeMm = "ကဒတ", CodeEng = "KaDaTa" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "လိပ်သိုမြို့နယ်", TownshipEng = "Leik Tho Township", CodeMm = "လသန", CodeEng = "LaThaNa" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "မြဝတီမြို့နယ်", TownshipEng = "Myawaddy Township", CodeMm = "မဝတ", CodeEng = "MaWaTa" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "ပိုင်ကျုံမြို့နယ်", TownshipEng = "Paingkyon Township", CodeMm = "ပကန", CodeEng = "PaKaNa" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "ရှမ်းရွာသစ်မြို့နယ်", TownshipEng = "Shan Ywar Thit Township", CodeMm = "ရရသ", CodeEng = "YaYaTha" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "စုကလိမြို့နယ်", TownshipEng = "Su Ka Li Township", CodeMm = "စကလ", CodeEng = "SaKaLa" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "သံတောင်မြို့နယ်", TownshipEng = "Thandaung Township", CodeMm = "သတန", CodeEng = "ThaTaNa" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "သံတောင်ကြီးမြို့နယ်", TownshipEng = "Thandaunggyi Township", CodeMm = "သတက", CodeEng = "ThaTaKa" },
                new NrcTownship { RegionCode = 3, StateOrRegion = "Kayin", TownshipMm = "ဝေါလေမြိုင်မြို့နယ်", TownshipEng = "Lay Myaing(Waw Lay) Township", CodeMm = "ဝလမ", CodeEng = "WaLaMa" },

                // ---------------- 4 / CHIN ----------------
                new NrcTownship { RegionCode = 4, StateOrRegion = "Chin", TownshipMm = "ကျီခါးမြို့နယ်", TownshipEng = "Cikha Township", CodeMm = "ကခန", CodeEng = "KaKhaNa" },
                new NrcTownship { RegionCode = 4, StateOrRegion = "Chin", TownshipMm = "ဖလန်းမြို့နယ်", TownshipEng = "Falam Township", CodeMm = "ဖလန", CodeEng = "HpaLaNa" },
                new NrcTownship { RegionCode = 4, StateOrRegion = "Chin", TownshipMm = "ဟားခါးမြို့နယ်", TownshipEng = "Hakha Township", CodeMm = "ဟခန", CodeEng = "HaKhaNa" },
                new NrcTownship { RegionCode = 4, StateOrRegion = "Chin", TownshipMm = "ကန်ပက်လက်မြို့နယ်", TownshipEng = "Kanpetlet Township", CodeMm = "", CodeEng = "KaPaLa" },
                new NrcTownship { RegionCode = 4, StateOrRegion = "Chin", TownshipMm = "မတူပီမြို့နယ်", TownshipEng = "Matupi Township", CodeMm = "မတပ", CodeEng = "MaTaPa" },
                new NrcTownship { RegionCode = 4, StateOrRegion = "Chin", TownshipMm = "မင်းတပ်မြို့နယ်", TownshipEng = "Mindat Township", CodeMm = "မတန", CodeEng = "MaTaNa" },
                new NrcTownship { RegionCode = 4, StateOrRegion = "Chin", TownshipMm = "ပလက်ဝမြို့နယ်", TownshipEng = "Paletwa Township", CodeMm = "ပလဝ", CodeEng = "PaLaWa" },
                new NrcTownship { RegionCode = 4, StateOrRegion = "Chin", TownshipMm = "ရေဇဝါမြို့နယ်", TownshipEng = "Rezua Township", CodeMm = "ရဇန", CodeEng = "YaZaNa" },
                new NrcTownship { RegionCode = 4, StateOrRegion = "Chin", TownshipMm = "ရိဒ်ခေါဒါရ်မြို့နယ်", TownshipEng = "Rihkhawdar Township", CodeMm = "ရခဒ", CodeEng = "YaKhaDa" },
                new NrcTownship { RegionCode = 4, StateOrRegion = "Chin", TownshipMm = "စမီးမြို့နယ်", TownshipEng = "Samee Township", CodeMm = "ဆမန", CodeEng = "SaMaNa" },
                new NrcTownship { RegionCode = 4, StateOrRegion = "Chin", TownshipMm = "တီးတိန်မြို့နယ်", TownshipEng = "Tedim Township", CodeMm = "တတန", CodeEng = "TaTaNa" },
                new NrcTownship { RegionCode = 4, StateOrRegion = "Chin", TownshipMm = "ထန်တလန် မြို့နယ်", TownshipEng = "Thantlang Township", CodeMm = "", CodeEng = "HtaTaLa" },
                new NrcTownship { RegionCode = 4, StateOrRegion = "Chin", TownshipMm = "တွန်းဇံမြို့နယ်", TownshipEng = "Tonzang Township", CodeMm = "တဇန", CodeEng = "TaZaNa" },

                // ---------------- 5 / SAGAING ----------------
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "အရာတော်မြို့နယ်", TownshipEng = "Ayadaw Township", CodeMm = "အရတ", CodeEng = "AhYaTa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ဗန်းမောက်မြို့နယ်", TownshipEng = "Banmauk Township", CodeMm = "", CodeEng = "BaMaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ဘုတလင်မြို့နယ်", TownshipEng = "Budalin Township", CodeMm = "ဘတလ", CodeEng = "BaTaLa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ချောင်းဦးမြို့နယ်", TownshipEng = "Chaung-U Township", CodeMm = "ခဥတ", CodeEng = "KhaOuTa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ခန္တီးမြို့နယ်", TownshipEng = "Hkamti Township", CodeMm = "ခတန", CodeEng = "KhaTaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ဟုမ္မလင်းမြို့နယ်", TownshipEng = "Homalin Township", CodeMm = "", CodeEng = "HaMaLa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "အင်းတော်မြို့နယ်", TownshipEng = "Indaw Township", CodeMm = "အတန", CodeEng = "AhTaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ကလေးမြို့နယ်", TownshipEng = "Kale Township", CodeMm = "", CodeEng = "KaLaHta" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ကလေးဝမြို့နယ်", TownshipEng = "KaLewa Township", CodeMm = "", CodeEng = "KaLaWa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ကန့်ဘလူမြို့နယ်", TownshipEng = "Kanbalu Township", CodeMm = "ကဘလ", CodeEng = "KaBaLa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ကနီမြို့နယ်", TownshipEng = "Kani Township", CodeMm = "ကနန", CodeEng = "KaNaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ကသာမြို့နယ်", TownshipEng = "Katha Township", CodeMm = "ကသန", CodeEng = "KaThaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ကောလင်းမြို့နယ်", TownshipEng = "Kawlin Township", CodeMm = "", CodeEng = "KaLaTa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ခင်ဦးမြို့နယ်", TownshipEng = "Khin-U Township", CodeMm = "ခဥန", CodeEng = "KhaOuNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ကျွန်းလှမြို့နယ်", TownshipEng = "Kyunhla Township", CodeMm = "ကလန", CodeEng = "KaLaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "လဟယ်မြို့နယ်", TownshipEng = "Lahe Township", CodeMm = "လဟန", CodeEng = "LaHaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "လေရှီးမြို့နယ်", TownshipEng = "Layshi Township", CodeMm = "လရန", CodeEng = "LaYaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "မော်လိုက်မြို့နယ်", TownshipEng = "Mawlaik Township", CodeMm = "မလန", CodeEng = "MaLaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "မင်းကင်းမြို့နယ်", TownshipEng = "Mingin Township", CodeMm = "မကန", CodeEng = "MaKaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "မုံရွာမြို့နယ်", TownshipEng = "Monywa Township", CodeMm = "မရန", CodeEng = "MaYaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "မြောင်မြို့နယ်", TownshipEng = "Myaung Township", CodeMm = "မမန", CodeEng = "MaMaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "မြင်းမူမြို့နယ်", TownshipEng = "Myinmu Township", CodeMm = "မတ", CodeEng = "MaMaTa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "နန်းယွန်းမြို့နယ်", TownshipEng = "Nanyun Township", CodeMm = "နယန", CodeEng = "NaYaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ငါးဇွန်မြို့နယ်", TownshipEng = "Ngazun Township", CodeMm = "", CodeEng = "NgaZaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ပုလဲမြို့နယ်", TownshipEng = "Pale Township", CodeMm = "ပလန", CodeEng = "PaLaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ဖောင်းပြင်မြို့နယ်", TownshipEng = "Paungbyin Township", CodeMm = "ဖပန", CodeEng = "HpaPaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ပင်လယ်ဘူးမြို့နယ်", TownshipEng = "Pinlebu Township", CodeMm = "ပလဘ", CodeEng = "PaLaBa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "စစ်ကိုင်းမြို့နယ်", TownshipEng = "Sagaing Township", CodeMm = "စကန", CodeEng = "SaKaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ဆားလင်းကြီးမြို့နယ်", TownshipEng = "Salingyi Township", CodeMm = "", CodeEng = "SaLaKa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ရွှေဘိုမြို့နယ်", TownshipEng = "Shwebo Township", CodeMm = "ရဘန", CodeEng = "YaBaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ဒီပဲယင်းမြို့နယ်", TownshipEng = "Tabayin Township", CodeMm = "", CodeEng = "DaPaYa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "တမူးမြို့နယ်", TownshipEng = "Tamu Township", CodeMm = "", CodeEng = "TaMaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "တန့်ဆည်မြို့နယ်", TownshipEng = "Taze Township", CodeMm = "", CodeEng = "TaSaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ထီးချိုင်မြို့နယ်", TownshipEng = "Tigyaing Township", CodeMm = "ထခန", CodeEng = "HtaKhaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ဝက်လက်မြို့နယ်", TownshipEng = "Wetleet Township", CodeMm = "ဝလန", CodeEng = "WaLaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ဝန်သိုမြို့နယ်", TownshipEng = "Wuntho Township", CodeMm = "ဝသန", CodeEng = "WaThaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ရေဦးမြို့နယ်", TownshipEng = "Ye-U Township", CodeMm = "ရဥန", CodeEng = "YaOuNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ယင်းမာပင်မြို့နယ်", TownshipEng = "Yinmabin Township", CodeMm = "", CodeEng = "YaMaPa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ကျောက်မြောင်းမြို့နယ်", TownshipEng = "Kyaukmyaung Township", CodeMm = "ကမန", CodeEng = "KaMaNa" },
                new NrcTownship { RegionCode = 5, StateOrRegion = "Sagaing", TownshipMm = "ခမ်းပတ်မြို့နယ်", TownshipEng = "Khampat Township", CodeMm = "ခပန", CodeEng = "KhaPaNa" },

                // ---------------- 6 / TANINTHARYI ----------------
                new NrcTownship { RegionCode = 6, StateOrRegion = "Tanintharyi", TownshipMm = "ဘုတ်ပြင်းမြို့နယ်", TownshipEng = "Bokpyin Township", CodeMm = "ဘပန", CodeEng = "BaPaNa" },
                new NrcTownship { RegionCode = 6, StateOrRegion = "Tanintharyi", TownshipMm = "ထားဝယ်မြို့နယ်", TownshipEng = "Dawei Township", CodeMm = "ထဝန", CodeEng = "HtaWaNa" },
                new NrcTownship { RegionCode = 6, StateOrRegion = "Tanintharyi", TownshipMm = "ကလိန်အောင်မြို့နယ်", TownshipEng = "Kaleinaung Township", CodeMm = "ကလအ", CodeEng = "KaLaAh" },
                new NrcTownship { RegionCode = 6, StateOrRegion = "Tanintharyi", TownshipMm = "ကော့သောင်းမြို့နယ်", TownshipEng = "Kawthoung Township", CodeMm = "ကသန", CodeEng = "KaThaNa" },
                new NrcTownship { RegionCode = 6, StateOrRegion = "Tanintharyi", TownshipMm = "ကျွန်းစုမြို့နယ်", TownshipEng = "Kyunsu Township", CodeMm = "ကစန", CodeEng = "KaSaNa" },
                new NrcTownship { RegionCode = 6, StateOrRegion = "Tanintharyi", TownshipMm = "လောင်းလုံမြို့နယ်", TownshipEng = "Launglon Township", CodeMm = "လလန", CodeEng = "LaLaNa" },
                new NrcTownship { RegionCode = 6, StateOrRegion = "Tanintharyi", TownshipMm = "မြိတ်မြို့နယ်", TownshipEng = "Myeik Township", CodeMm = "မမန", CodeEng = "MaMaNa" },
                new NrcTownship { RegionCode = 6, StateOrRegion = "Tanintharyi", TownshipMm = "ပုလောမြို့နယ်", TownshipEng = "Palaw Township", CodeMm = "ပလန", CodeEng = "PaLaNa" },
                new NrcTownship { RegionCode = 6, StateOrRegion = "Tanintharyi", TownshipMm = "တနင်္သာရီတိုင်း", TownshipEng = "Tanintharyi Township", CodeMm = "တသရ", CodeEng = "TaThaYa" },
                new NrcTownship { RegionCode = 6, StateOrRegion = "Tanintharyi", TownshipMm = "သရက်ချောင်းမြို့နယ်", TownshipEng = "Thayetchaung Township", CodeMm = "သရခ", CodeEng = "ThaYaKha" },
                new NrcTownship { RegionCode = 6, StateOrRegion = "Tanintharyi", TownshipMm = "ရေဖြူမြို့နယ်", TownshipEng = "Yepyu Township", CodeMm = "ရဖန", CodeEng = "YaHpaNa" },
                new NrcTownship { RegionCode = 6, StateOrRegion = "Tanintharyi", TownshipMm = "ခမောက်ကြီးမြို့နယ်", TownshipEng = "Khamaukgyi Township", CodeMm = "ခမန", CodeEng = "KhaMaNa" },
                new NrcTownship { RegionCode = 6, StateOrRegion = "Tanintharyi", TownshipMm = "မေတ္တာမြို့နယ်", TownshipEng = "Myittar Township", CodeMm = "မတန", CodeEng = "MaTaNa" },
                new NrcTownship { RegionCode = 6, StateOrRegion = "Tanintharyi", TownshipMm = "ပလောက်မြို့နယ်", TownshipEng = "Palauk Myo Nal Khwal", CodeMm = "", CodeEng = "PaLaTa" },
                new NrcTownship { RegionCode = 6, StateOrRegion = "Tanintharyi", TownshipMm = "ကရသူရိမြို့နယ်", TownshipEng = "Karathuri Township", CodeMm = "ကရရ", CodeEng = "KaYaYa" },

                // ---------------- 7 / BAGO ----------------
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ဒိုက်ဦးမြို့နယ်", TownshipEng = "Daik-U Township", CodeMm = "ဒဥန", CodeEng = "DaOuNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ကြို့ပင်ကောက်မြို့နယ်", TownshipEng = "Gyobingauk Township", CodeMm = "ကပက", CodeEng = "KaPaKa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ကဝမြို့နယ်", TownshipEng = "Kawa Township", CodeMm = "ကဝန", CodeEng = "KaWaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ကျောက်ကြီးမြို့နယ်", TownshipEng = "Kyaukkyi Township", CodeMm = "ကကန", CodeEng = "KaKaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ကျောက်တံခါးမြို့နယ်", TownshipEng = "Kyauktaga Township", CodeMm = "ကတခ", CodeEng = "KaTaKha" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "လက်ပံတန်းမြို့နယ်", TownshipEng = "Letpadan Township", CodeMm = "", CodeEng = "LaPaTa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "မင်းလှမြို့နယ်", TownshipEng = "Minhla Township", CodeMm = "မလန", CodeEng = "MaLaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "မိုးညိုမြို့နယ်", TownshipEng = "Monyo Township", CodeMm = "မညန", CodeEng = "MaNyaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "နတ္တလင်းမြို့နယ်", TownshipEng = "Nattalin Township", CodeMm = "နတလ", CodeEng = "NaTaLa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ညောင်လေးပင်မြို့နယ်", TownshipEng = "Nyaunglebin Township", CodeMm = "ညလပ", CodeEng = "NyaLaPa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "အုတ်ဖိုမြို့နယ်", TownshipEng = "Okpho Township", CodeMm = "အဖန", CodeEng = "AhHpaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "အုတ်တွင်းမြို့နယ်", TownshipEng = "Oktwin Township", CodeMm = "အတန", CodeEng = "AhTaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ပန်းတောင်းမြို့နယ်", TownshipEng = "Padaung Township", CodeMm = "ပတန", CodeEng = "PaTaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ပေါက်ခေါင်းမြို့နယ်", TownshipEng = "Pauk Kaung Township", CodeMm = "ပခတ", CodeEng = "PaKhaTa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ပဲခူးမြို့နယ်", TownshipEng = "Bago Township", CodeMm = "ပခန", CodeEng = "PaKhaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ပေါင်းတည်မြို့နယ်", TownshipEng = "Paungde Township", CodeMm = "ပတတ", CodeEng = "PaTaTa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ပဲနွယ်ကုန်းမြို့နယ်", TownshipEng = "Pennwegone Township", CodeMm = "ပနက", CodeEng = "PaNaKa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ဖြူးမြို့နယ်", TownshipEng = "Phyu Township", CodeMm = "ဖမန", CodeEng = "HpaMaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ပြည်မြို့နယ်", TownshipEng = "Pyay Township", CodeMm = "ပမန", CodeEng = "PaMaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ရွှေတောင်မြို့နယ်", TownshipEng = "Shwedaung Township", CodeMm = "ရတန", CodeEng = "YaTaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ရွှေကျင်မြို့နယ်", TownshipEng = "Shwegyin Township", CodeMm = "ရကန", CodeEng = "YaKaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ထန်းတပင်မြို့နယ်", TownshipEng = "Tantabin Township", CodeMm = "ထတပ", CodeEng = "HtaTaPa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "တောင်ငူမြို့နယ်", TownshipEng = "Taungoo Township", CodeMm = "တငန", CodeEng = "TaNgaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "သနပ္ပင်မြို့နယ်", TownshipEng = "Thanatpin Township", CodeMm = "သနပ", CodeEng = "ThaNaPa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "သာယာဝတီမြို့နယ်", TownshipEng = "Thayarwady Township", CodeMm = "သဝတ", CodeEng = "ThaWaTa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "သဲကုန်းမြို့နယ်", TownshipEng = "Thegon Township", CodeMm = "သကန", CodeEng = "ThaKaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "သုံးဆယ်မြို့နယ်", TownshipEng = "Thonze Township", CodeMm = "သဆန", CodeEng = "ThaSaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ဝေါမြို့နယ်", TownshipEng = "Waw Township", CodeMm = "ဝမန", CodeEng = "WaMaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ရေတာရှည်မြို့နယ်", TownshipEng = "Yedashe Township", CodeMm = "ရတရ", CodeEng = "YaTaYa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ဇီးကုန်းမြို့နယ်", TownshipEng = "Zigon Township", CodeMm = "ဇကန", CodeEng = "ZaKaNa" },
                new NrcTownship { RegionCode = 7, StateOrRegion = "Bago", TownshipMm = "ပြွန်တန်ဆာမြို့နယ်", TownshipEng = "Pyontahsar Township", CodeMm = "ပတဆ", CodeEng = "PaTaSa" },

                // ---------------- 8 / MAGWAY ----------------
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "အောင်လံမြို့နယ်", TownshipEng = "Aunglan Township", CodeMm = "အလန", CodeEng = "AhLaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "ချောက်မြို့နယ်", TownshipEng = "Chauk Township", CodeMm = "ခမန", CodeEng = "KhaMaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "ဂန့်ဂေါမြို့နယ်", TownshipEng = "Gangaw Township", CodeMm = "ဂဂန", CodeEng = "GaGaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "ကံမမြို့နယ်", TownshipEng = "Kamma Township", CodeMm = "ကမန", CodeEng = "KaMaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "မကွေးမြို့နယ်", TownshipEng = "Magwe Township", CodeMm = "မကန", CodeEng = "MaKaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "မင်းဘူး(စကု)မြို့နယ်", TownshipEng = "Minbu(Sagu) Township", CodeMm = "မဘန", CodeEng = "MaBaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "မင်းတုန်းမြို့နယ်", TownshipEng = "Mindon Township", CodeMm = "မတန", CodeEng = "MaTaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "မင်းလှမြို့နယ်", TownshipEng = "Minhla Township", CodeMm = "မလန", CodeEng = "MaLaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "မြိုင်မြို့နယ်", TownshipEng = "Myaing Township", CodeMm = "မန", CodeEng = "MaMaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "မြေထဲမြို့နယ်", TownshipEng = "Myayhtae Township", CodeMm = "မထန", CodeEng = "MaHtaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "မြို့သစ်မြို့နယ်", TownshipEng = "Myothit Township", CodeMm = "မသန", CodeEng = "MaThaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "နတ်မောက်မြို့နယ်", TownshipEng = "Natmauk Township", CodeMm = "နမန", CodeEng = "NaMaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "ငဖဲမြို့နယ်", TownshipEng = "Ngape Township", CodeMm = "ငဖန", CodeEng = "NgaHpaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "ပခုက္ကူ မြို့နယ်", TownshipEng = "Pakokku Township", CodeMm = "ပခက", CodeEng = "PaKhaKa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "ပေါက်မြို့နယ်", TownshipEng = "Pauk Township", CodeMm = "ပမန", CodeEng = "PaMaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "ပွင့်ဖြူမြို့နယ်", TownshipEng = "Pwintbyu Township", CodeMm = "ပဖန", CodeEng = "PaHpaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "စလင်းမြို့နယ်", TownshipEng = "Salin Township", CodeMm = "စလန", CodeEng = "SaLaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "ဆောမြို့နယ်", TownshipEng = "Saw Township", CodeMm = "ဆမန", CodeEng = "SaMaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "ဆိတ်ဖြူမြို့နယ်", TownshipEng = "Seikphyu Township", CodeMm = "ဆဖန", CodeEng = "SaHpaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "စေတုတ္တရာမြို့နယ်", TownshipEng = "Sidoktaya Township", CodeMm = "စတရ", CodeEng = "SaTaYa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "ဆင်ပေါင်ဝဲမြို့နယ်", TownshipEng = "Sinbaungwe Township", CodeMm = "", CodeEng = "SaPaWa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "တောင်တွင်းကြီးမြို့နယ်", TownshipEng = "Taungdwingyi Township", CodeMm = "", CodeEng = "TaTaKa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "သရက်မြို့နယ်", TownshipEng = "Thayet Township", CodeMm = "သရန", CodeEng = "ThaYaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "ထီးလင်းမြို့နယ်", TownshipEng = "Tilin Township", CodeMm = "ထလန", CodeEng = "HtaLaNa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "ရေနံချောင်းမြို့နယ်", TownshipEng = "Yenangyaung Township", CodeMm = "ရနခ", CodeEng = "YaNaKha" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "ရေစကြိုမြို့နယ်", TownshipEng = "Yesagyo Township", CodeMm = "ရစက", CodeEng = "YaSaKa" },
                new NrcTownship { RegionCode = 8, StateOrRegion = "Magway", TownshipMm = "ကျောက်ထုမြို့နယ်", TownshipEng = "Kyaukhtu Myo Nal Khwal", CodeMm = "ကထန", CodeEng = "KaHtaNa" },

                // ---------------- 9 / MANDALAY ----------------
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "အမရပူရမြို့နယ်", TownshipEng = "Amarapura Township", CodeMm = "အမရ", CodeEng = "AhMaYa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "အောင်မြေသာဇံမြို့နယ်", TownshipEng = "Aungmyaythazan Township", CodeMm = "", CodeEng = "AhMaZa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ချမ်းအေးသာဇံမြို့နယ်", TownshipEng = "Chanayethazan Township", CodeMm = "", CodeEng = "KhaAhZa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ချမ်းမြေ့သာစည်မြို့နယ်", TownshipEng = "Chanmyathazi Township", CodeMm = "", CodeEng = "KhaMaSa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ကျောက်ပန်းတောင်းမြို့နယ်", TownshipEng = "Kyukpadaung Township", CodeMm = "ကပတ", CodeEng = "KaPaTa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ကျောက်ဆည်မြို့နယ်", TownshipEng = "Kyukse Township", CodeMm = "ကဆန", CodeEng = "KaSaNa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "မတ္တရာမြို့နယ်", TownshipEng = "Madaya Township", CodeMm = "မတရ", CodeEng = "MaTaYa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "မဟာအောင်မြေမြို့နယ်", TownshipEng = "Mahaaungmyay Township", CodeMm = "မလန", CodeEng = "MaHaMa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "မလှိုင်မြို့နယ်", TownshipEng = "Mahlaing Township", CodeMm = "မလ", CodeEng = "MaLaNa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "မိတ္ထီလာမြို့နယ်", TownshipEng = "Meiktila Township", CodeMm = "", CodeEng = "MaHtaLa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "မိုးကုတ်မြို့နယ်", TownshipEng = "Mogoke Township", CodeMm = "မကန", CodeEng = "MaKaNa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "မြင်းခြံမြို့နယ်", TownshipEng = "Myingyan Township", CodeMm = "မခန", CodeEng = "MaKhaNa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "မြစ်သားမြို့နယ်", TownshipEng = "Myittha Township", CodeMm = "မသန", CodeEng = "MaThaNa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "နွားထိုးကြီးမြို့နယ်", TownshipEng = "Natogyi Township", CodeMm = "နထက", CodeEng = "NaHtaKa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ငါ့သရောက်မြို့နယ်", TownshipEng = "Ngathayouk Township", CodeMm = "ငသရ", CodeEng = "Nga ThaYa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ငါးဇွန်မြို့နယ်", TownshipEng = "Ngazun Township", CodeMm = "", CodeEng = "NgaZaNa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ညောင်ဦးမြို့နယ်", TownshipEng = "Nyaung-U Township", CodeMm = "ညဥန", CodeEng = "NyaOuNa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ပုသိမ်ကြီးမြို့နယ်", TownshipEng = "Patheingyi Township", CodeMm = "ပသက", CodeEng = "PaThaKa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ပျော်ဘွယ်မြို့နယ်", TownshipEng = "Pyawbwe Township", CodeMm = "ပဘန", CodeEng = "PaBaNa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ပြည်ကြီးတံခွန်မြို့နယ်", TownshipEng = "Pyigyitagon Township", CodeMm = "ပကခ", CodeEng = "PaKaKha" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ပြင်ဦးလွင်မြို့နယ်", TownshipEng = "Pyinoolwin Township", CodeMm = "ပဉ္စလ", CodeEng = "PaOuLa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "စဉ့်ကူးမြို့နယ်", TownshipEng = "Singu Township", CodeMm = "စကန", CodeEng = "SaKaNa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "စဉ့်ကိုင်းမြို့နယ်", TownshipEng = "Sintgaing Township", CodeMm = "", CodeEng = "SaKaTa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "သပိတ်ကျင်းမြို့နယ်", TownshipEng = "Tabeikkyin Township", CodeMm = "သပက", CodeEng = "ThaPaKa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "တံတားဦးမြို့နယ်", TownshipEng = "Tada-U Township", CodeMm = "တတဥ", CodeEng = "TaTaOu" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "တောင်သာမြို့နယ်", TownshipEng = "Taungtha Township", CodeMm = "တသန", CodeEng = "TaThaNa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "သာစည်မြို့နယ်", TownshipEng = "Thazi Township", CodeMm = "သစန", CodeEng = "ThaSaNa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ဝမ်းတွင်းမြို့နယ်", TownshipEng = "Wundwin Township", CodeMm = "ဝတန", CodeEng = "WaTaNa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ရမည်းသင်းမြို့နယ်", TownshipEng = "Yemathin Township", CodeMm = "ရမသ", CodeEng = "YaMaTha" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "တကောင်းမြို့နယ်", TownshipEng = "Tagaung Township", CodeMm = "", CodeEng = "TaKaTa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ဒက္ခိဏသီရိမြို့နယ်", TownshipEng = "Dekhinathiri TownShip", CodeMm = "ဒခသ", CodeEng = "DaKhaTha" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "လယ်ဝေးမြို့နယ်", TownshipEng = "Lewe TownShip", CodeMm = "လဝန", CodeEng = "LaWaNa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ဥတ္တရသီရိမြို့နယ်", TownshipEng = "Ottarathiri TownShip", CodeMm = "ဥတသ", CodeEng = "OuTaTha" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ပုပ္ပသီရိမြို့နယ်", TownshipEng = "Popathiri TownShip", CodeMm = "", CodeEng = "PaBaTha" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ပျဉ်းမနားမြို့နယ်", TownshipEng = "Pyinmana TownShip", CodeMm = "ပမန", CodeEng = "PaMaNa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "တပ်ကုန်းမြို့နယ်", TownshipEng = "Tatkon TownShip", CodeMm = "တကန", CodeEng = "TaKaNa" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ဇမ္ဗူသီရိမြို့နယ်", TownshipEng = "Zabuthiri TownShip", CodeMm = "", CodeEng = "ZaBaTha" },
                new NrcTownship { RegionCode = 9, StateOrRegion = "Mandalay", TownshipMm = "ဇေယျာသီရိမြို့နယ်", TownshipEng = "Zayarthiri TownShip", CodeMm = "", CodeEng = "ZaYaTha" },

                // ---------------- 10 / MON ----------------
                new NrcTownship { RegionCode = 10, StateOrRegion = "Mon", TownshipMm = "ဘီးလင်းမြို့နယ်", TownshipEng = "Billin Township", CodeMm = "ဘလန", CodeEng = "BaLaNa" },
                new NrcTownship { RegionCode = 10, StateOrRegion = "Mon", TownshipMm = "ချောင်းဆုံမြို့နယ်", TownshipEng = "Chaungzon Township", CodeMm = "ခဆန", CodeEng = "KhaSaNa" },
                new NrcTownship { RegionCode = 10, StateOrRegion = "Mon", TownshipMm = "ခေါဇာမြို့နယ်", TownshipEng = "Khawzar Township", CodeMm = "ခဇန", CodeEng = "KhaZaNa" },
                new NrcTownship { RegionCode = 10, StateOrRegion = "Mon", TownshipMm = "ကျိုက်မရောမြို့နယ်", TownshipEng = "Kyaikmaraw Township", CodeMm = "ကမရ", CodeEng = "KaMaYa" },
                new NrcTownship { RegionCode = 10, StateOrRegion = "Mon", TownshipMm = "ကျိုက်ထိုမြို့နယ်", TownshipEng = "Kyaikto Township", CodeMm = "ကထန", CodeEng = "KaHtaNa" },
                new NrcTownship { RegionCode = 10, StateOrRegion = "Mon", TownshipMm = "လမိုင်းမြို့နယ်", TownshipEng = "Lamine Myo Nal Khwal", CodeMm = "လမန", CodeEng = "LaMaNa" },
                new NrcTownship { RegionCode = 10, StateOrRegion = "Mon", TownshipMm = "မော်လမြိုင်မြို့နယ်", TownshipEng = "Mawlamyine Township", CodeMm = "မလမ", CodeEng = "MaLaMa" },
                new NrcTownship { RegionCode = 10, StateOrRegion = "Mon", TownshipMm = "မုဒုံမြို့နယ်", TownshipEng = "Mudon Township", CodeMm = "မဒန", CodeEng = "MaDaNa" },
                new NrcTownship { RegionCode = 10, StateOrRegion = "Mon", TownshipMm = "ပေါင်မြို့နယ်", TownshipEng = "Paung Township", CodeMm = "ပမန", CodeEng = "PaMaNa" },
                new NrcTownship { RegionCode = 10, StateOrRegion = "Mon", TownshipMm = "သံဖြူဇရပ်မြို့နယ်", TownshipEng = "Thanbyuzayat Township", CodeMm = "သဖရ", CodeEng = "ThaHpaYa" },
                new NrcTownship { RegionCode = 10, StateOrRegion = "Mon", TownshipMm = "သထုံမြို့နယ်", TownshipEng = "Thaton Township", CodeMm = "သထန", CodeEng = "ThaHtaNa" },
                new NrcTownship { RegionCode = 10, StateOrRegion = "Mon", TownshipMm = "ရေးမြို့နယ်", TownshipEng = "Ye Township", CodeMm = "ရမန", CodeEng = "YaMaNa" },

                // ---------------- 11 / RAKHINE ----------------
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "အမ်းမြို့နယ်", TownshipEng = "Ann Township", CodeMm = "အမန", CodeEng = "AhMaNa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "ဘူးသီးတောင်မြို့နယ်", TownshipEng = "Buthidaung Township", CodeMm = "ဘသတ", CodeEng = "BaThaTa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "ဂွမြို့နယ်", TownshipEng = "Gwa Township", CodeMm = "ဂမန", CodeEng = "GaMaNa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "ကျောက်ဖြူမြို့နယ်", TownshipEng = "Kyaukpyu Township", CodeMm = "ကဖန", CodeEng = "KaHpaNa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "ကျောက်တော်မြို့နယ်", TownshipEng = "Kyauktaw Township", CodeMm = "ကတန", CodeEng = "KaTaNa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "မအီမြို့နယ်", TownshipEng = "Maei Myo Nal Khwal", CodeMm = "မအတ", CodeEng = "MaAhTa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "မောင်တောမြို့နယ်", TownshipEng = "Maungdaw Township", CodeMm = "မတန", CodeEng = "MaTaNa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "မင်းပြားမြို့နယ်", TownshipEng = "Minbya Township", CodeMm = "မပန", CodeEng = "MaPaNa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "မာန်အောင်မြို့နယ်", TownshipEng = "Munaung Township", CodeMm = "မအန", CodeEng = "MaAhNa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "မြောက်ဦးမြို့နယ်", TownshipEng = "Myauk-U Township", CodeMm = "မဥန", CodeEng = "MaOuNa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "မြေပုံမြို့နယ်", TownshipEng = "Myebon Township", CodeMm = "မပတ", CodeEng = "MaPaTa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "ပေါက်တောမြို့နယ်", TownshipEng = "Pauktaw Township", CodeMm = "ပတန", CodeEng = "PaTaNa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "ပုဏ္ဏားကျွန်းမြို့နယ်", TownshipEng = "Ponnagyun Township", CodeMm = "ပဏတ", CodeEng = "PaNaTa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "ရမ်းဗြဲမြို့နယ်", TownshipEng = "Ramree Township", CodeMm = "ရဗန", CodeEng = "YaBaNa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "ရသေ့တောင်မြို့နယ်", TownshipEng = "Rathedaung Township", CodeMm = "ရသတ", CodeEng = "YaThaTa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "စစ်တွေမြို့နယ်", TownshipEng = "Sittwe Township", CodeMm = "စတန", CodeEng = "SaTaNa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "သံတွဲမြို့နယ်", TownshipEng = "Thandwe Township", CodeMm = "သတန", CodeEng = "ThaTaNa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "တောင်ကုတ်မြို့နယ်", TownshipEng = "Toungup Township", CodeMm = "တကန", CodeEng = "TaKaNa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "ကျိန်တလီမြို့နယ်", TownshipEng = "Kyeintali Township", CodeMm = "", CodeEng = "KaTaLa" },
                new NrcTownship { RegionCode = 11, StateOrRegion = "Rakhine", TownshipMm = "တောင်ပိုလက်ဝဲမြို့နယ်", TownshipEng = "Taungpyolatwae Township", CodeMm = "တပ၀", CodeEng = "TaPaWa" },

                // ---------------- 12 / YANGON ----------------
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "အလုံမြို့နယ်", TownshipEng = "Ahlone Township", CodeMm = "အလန", CodeEng = "AhLaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ဗဟန်းမြို့နယ်", TownshipEng = "Bahan Township", CodeMm = "ဗဟန", CodeEng = "BaHaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ဗိုလ်တစ်ထောင်မြို့နယ်", TownshipEng = "Botahtaung Township", CodeMm = "", CodeEng = "BaTaHta" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ကိုကိုးကျွန်းမြို့နယ်", TownshipEng = "Cocokyun Township", CodeMm = "ကကက", CodeEng = "KaKaKa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ဒဂုံမြို့သစ် (အရှေ့ပိုင်း) မြို့နယ်", TownshipEng = "Dagon Myothit(East)", CodeMm = "ဒဂရ", CodeEng = "DaGaYa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ဒဂုံမြို့သစ် (မြောက်ပိုင်း) မြို့နယ်", TownshipEng = "Dagon Myothit(North)", CodeMm = "", CodeEng = "DaGaMa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ဒဂုံမြို့သစ်(ဆိပ်ကမ်း) မြို့နယ်", TownshipEng = "Dagon Myothit(Seikkan)", CodeMm = "", CodeEng = "DaGaSa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ဒဂုံမြို့သစ် (တောင်ပိုင်း) မြို့နယ်", TownshipEng = "Dagon Myothit(South)", CodeMm = "", CodeEng = "DaGaTa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ဒဂုံမြို့နယ်", TownshipEng = "Dagon Township", CodeMm = "ဒဂန", CodeEng = "DaGaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ဒလမြို့နယ်", TownshipEng = "Dala Township", CodeMm = "ဒလန", CodeEng = "DaLaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ဒေါပုံမြို့နယ်", TownshipEng = "Dawbon Township", CodeMm = "ဒပန", CodeEng = "DaPaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "လှိုင်သာယာမြို့နယ်", TownshipEng = "Hlaingtharya Township", CodeMm = "လသယ", CodeEng = "LaThaYa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "လှိုင်မြို့နယ်", TownshipEng = "Hlaing Township", CodeMm = "လမန", CodeEng = "LaMaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "လှည်းကူးမြို့နယ်", TownshipEng = "Hlegu Township", CodeMm = "လကန", CodeEng = "LaKaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "မှော်ဘီမြို့နယ်", TownshipEng = "Hmawbi Township", CodeMm = "မဘန", CodeEng = "MaBaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ထန်းတပင်မြို့နယ်", TownshipEng = "Htantabin Township", CodeMm = "", CodeEng = "HtaTaPa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "အင်းစိန်မြို့နယ်", TownshipEng = "Insein Township", CodeMm = "အစန", CodeEng = "AhSaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "သုံးခွမြို့နယ်", TownshipEng = "Thongwa Township", CodeMm = "သခန", CodeEng = "ThaKhaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ကမာရွတ်မြို့နယ်", TownshipEng = "Kamayut Township", CodeMm = "ကမရ", CodeEng = "KaMaYa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ကော့မှူးမြို့နယ်", TownshipEng = "Kawhmu Township", CodeMm = "ကမန", CodeEng = "KaMaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ခရမ်းမြို့နယ်", TownshipEng = "Kayan Township", CodeMm = "ခရန", CodeEng = "KhaYaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ကွမ်းခြံကုန်းမြို့နယ်", TownshipEng = "Kungyangon Township", CodeMm = "ကခက", CodeEng = "KaKhaKa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ကျောက်တံတားမြို့နယ်", TownshipEng = "Kyauktada Township", CodeMm = "ကတတ", CodeEng = "KaTaTa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ကျောက်တန်းမြို့နယ်", TownshipEng = "Kyauktan Township", CodeMm = "ကတန", CodeEng = "KaTaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ကြည့်မြင်တိုင်မြို့နယ်", TownshipEng = "Kyimyindaing Township", CodeMm = "ကမတ", CodeEng = "KaMaTa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "လမ်းမတော်မြို့နယ်", TownshipEng = "Lanmadaw Township", CodeMm = "လမတ", CodeEng = "LaMaTa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "လသာမြို့နယ်", TownshipEng = "Latha Township", CodeMm = "လသန", CodeEng = "LaThaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "မရမ်းကုန်းမြို့နယ်", TownshipEng = "Mayangone Township", CodeMm = "မရက", CodeEng = "MaYaKa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "မင်္ဂလာဒုံမြို့နယ်", TownshipEng = "Mingalardon Township", CodeMm = "မဂဒ", CodeEng = "MaGaDa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "မင်္ဂလာတောင်ညွန့်မြို့နယ်", TownshipEng = "Mingalartaungnyunt Township", CodeMm = "မဂတ", CodeEng = "MaGaTa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "မြောက်ဥက္ကလာပမြို့နယ်", TownshipEng = "North Oakkalapa Township", CodeMm = "ဥကမ", CodeEng = "OuKaMa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ပန်းဘဲတန်းမြို့နယ်", TownshipEng = "Pabedan Township", CodeMm = "ပဘတ", CodeEng = "PaBaTa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ပုဇွန်တောင်မြို့နယ်", TownshipEng = "Pazundaung Township", CodeMm = "ပဇတ", CodeEng = "PaZaTa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "စမ်းချောင်းမြို့နယ်", TownshipEng = "Sanchaung Township", CodeMm = "စခန", CodeEng = "SaKhaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ဆိပ်ကြီးခနောင်တိုမြို့နယ်", TownshipEng = "Seikgyikanaungto Township", CodeMm = "ဆကခ", CodeEng = "SaKaKha" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ဆိပ်ကမ်းမြို့နယ်", TownshipEng = "Seikkan Township", CodeMm = "ဆကန", CodeEng = "SaKaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ရွှေပြည်သာမြို့နယ်", TownshipEng = "Shwepyithar Township", CodeMm = "ရပသ", CodeEng = "YaPaTha" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "တောင်ဥက္ကလာပမြို့နယ်", TownshipEng = "South Oakkalapa Township", CodeMm = "ဥကတ", CodeEng = "OuKaTa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "တံတားမြို့နယ်", TownshipEng = "Tada Township", CodeMm = "", CodeEng = "TaTaHta" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "တိုက်ကြီးမြို့နယ်", TownshipEng = "Taikkyi Township", CodeMm = "တကန", CodeEng = "TaKaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "တာမွေမြို့နယ်", TownshipEng = "Tamwe Township", CodeMm = "တမန", CodeEng = "TaMaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "သာကေတမြို့နယ်", TownshipEng = "Thaketa Township", CodeMm = "သကတ", CodeEng = "ThaKaTa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "သန်လျင်မြို့နယ်", TownshipEng = "Thanlyin Township", CodeMm = "သလန", CodeEng = "ThaLaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "သင်္ဃန်းကျွန်းမြို့နယ်", TownshipEng = "Thingangkuun Township", CodeMm = "သဃက", CodeEng = "ThaGaKa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "တွံတေးမြို့နယ်", TownshipEng = "Twantay Township", CodeMm = "တတန", CodeEng = "TaTaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ရန်ကင်းမြို့နယ်", TownshipEng = "Yankin Township", CodeMm = "ရကန", CodeEng = "YaKaNa" },
                new NrcTownship { RegionCode = 12, StateOrRegion = "Yangon", TownshipMm = "ဥက္ကံမြို့နယ်", TownshipEng = "Oakkan Township", CodeMm = "ဥကန", CodeEng = "OuKaNa" },

                // ---------------- 13 / SHAN ----------------
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "အိုက်ချန်မြို့နယ်", TownshipEng = "Aik Chan Township", CodeMm = "အခန", CodeEng = "AhKhaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ချင်းရွှေဟော်မြို့နယ်", TownshipEng = "Chinshwehaw Sub-Township", CodeMm = "ခရဟ", CodeEng = "KhaYaHa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ခွန်းမားမြို့နယ်", TownshipEng = "Hkun Mar Township", CodeMm = "", CodeEng = "KhaMaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ဟိုတောင်မြိုနယ်", TownshipEng = "Ho Tawng Township", CodeMm = "ဟတန", CodeEng = "HaTaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ဟိုပန်မြို့နယ်", TownshipEng = "Hopang Township", CodeMm = "ဟပန", CodeEng = "HaPaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ဟိုပုံးမြို့နယ်", TownshipEng = "Hopong Township", CodeMm = "", CodeEng = "HaPaTa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ဆောင်းဖမြို့နယ်", TownshipEng = "Hsawng Hpa Township", CodeMm = "ဆဖန", CodeEng = "SaHpaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "သိန္နီမြို့နယ်", TownshipEng = "Hseni Township", CodeMm = "သနန", CodeEng = "ThaNaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ဆီဆိုင်မြို့နယ်", TownshipEng = "Hsihseng Township", CodeMm = "ဆဆန", CodeEng = "SaSaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "သီပေါမြို့နယ်", TownshipEng = "Hsipaw Township", CodeMm = "သပန", CodeEng = "ThaPaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ကလောင်ဖားမြို့နယ်", TownshipEng = "Ka Lawng Hpar Township", CodeMm = "", CodeEng = "KaLaHpa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ကလောမြို့နယ်", TownshipEng = "Kalaw Township", CodeMm = "ကလန", CodeEng = "KaLaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ကာလိမြို့နယ်", TownshipEng = "Kali Township", CodeMm = "ကလဒ", CodeEng = "KaLaDa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ကောင်မင်ဆန်းမြို့နယ်", TownshipEng = "Kawng Min Hsang Township", CodeMm = "ကမဆ", CodeEng = "KaMaSa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ကျိုင်းတုံမြို့နယ်", TownshipEng = "Kengtung Township", CodeMm = "ကတန", CodeEng = "KaTaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ကုန်းကြမ်းမြို့နယ်", TownshipEng = "Konkyan Township", CodeMm = "ကရန", CodeEng = "KaYaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ကျိုင်းတောင်မြို့နယ်", TownshipEng = "Kyaing Taung Township", CodeMm = "", CodeEng = "KaTaTa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ကွန်ဟိန်းမြို့နယ်", TownshipEng = "Kunhing Township", CodeMm = "ကဟန", CodeEng = "KaHaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ကွမ်းလုံမြို့နယ်", TownshipEng = "Kuniong Township", CodeMm = "", CodeEng = "KaLaTa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ကွတ်ခိုင်မြို့နယ်", TownshipEng = "Kutkai Township", CodeMm = "ကခန", CodeEng = "KaKhaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ကျောက်မဲမြို့နယ်", TownshipEng = "Kyaukme Township", CodeMm = "ကမန", CodeEng = "KaMaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ကျောက်တလုံးကြီးမြို့နယ်", TownshipEng = "Kyauktalonegyi Township", CodeMm = "", CodeEng = "KaTaLa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ကျေးသီးမြို့နယ်", TownshipEng = "Kyethi Township", CodeMm = "ကသန", CodeEng = "KaThaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "လဲချားမြို့နယ်", TownshipEng = "Laihka Township", CodeMm = "လခန", CodeEng = "LaKhaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "လင်းခေးမြို့နယ်", TownshipEng = "Langkho Township", CodeMm = "လခတ", CodeEng = "LaKhaTa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "လားရှိုးမြို့နယ်", TownshipEng = "Lashio Township", CodeMm = "လရန", CodeEng = "LaYaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "လောက်ကိုင်မြို့နယ်", TownshipEng = "Laukkaing Township", CodeMm = "", CodeEng = "LaKaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "လင်ဟော်မြို့နယ်", TownshipEng = "Lin Haw Township", CodeMm = "လဟန", CodeEng = "LaHaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "လွိုင်လင်မြို့နယ်", TownshipEng = "Loilen Township", CodeMm = "လလန", CodeEng = "LaLaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မဘိမ်းမြို့နယ်", TownshipEng = "Mabein Township", CodeMm = "မဘန", CodeEng = "MaBaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မန်မန်ဆိုင်မြို့နယ်", TownshipEng = "Man Man Hseng Township", CodeMm = "", CodeEng = "MaMaSa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မန်တွန်မြို့နယ်", TownshipEng = "Man Tun Township", CodeMm = "မတန", CodeEng = "MaTaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မန်တုန်မြို့နယ်", TownshipEng = "Manton Township", CodeMm = "မတတ", CodeEng = "MaTaTa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မက်မန်မြို့နယ်", TownshipEng = "Matman Township", CodeMm = "မမန", CodeEng = "MaMaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မောက်မယ်မြို့နယ်", TownshipEng = "Mawkmai Township", CodeMm = "မမန", CodeEng = "MaMaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုင်းဖျန်မြို့နယ်", TownshipEng = "Mong Hpen Township", CodeMm = "မဖန", CodeEng = "MaHpaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုင်းကားမြို့နယ်", TownshipEng = "Mong Kar Township", CodeMm = "မကန", CodeEng = "MaKaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုင်းပေါက်မြို့နယ်", TownshipEng = "Mong Pawk Township", CodeMm = "မပန", CodeEng = "MaPaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုင်းဖြတ်မြို့နယ်", TownshipEng = "Monghpyak Township", CodeMm = "မဖန", CodeEng = "MaHpaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုင်းဆက်မြို့နယ်", TownshipEng = "Mong Hsat Township", CodeMm = "မဆန", CodeEng = "MaSaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုင်းရှူးမြို့နယ်", TownshipEng = "Monghsu Township", CodeMm = "မရန", CodeEng = "MaYaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုင်းခတ်မြို့နယ်", TownshipEng = "Mongkhet Township", CodeMm = "မကန", CodeEng = "MaKhaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုင်းလားမြို့နယ်", TownshipEng = "Mongla Township", CodeMm = "မခန", CodeEng = "MaLaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုင်းမောမြို့နယ်", TownshipEng = "Mongmao Township", CodeMm = "မမတ", CodeEng = "MaMaTa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုးမိတ်မြို့နယ်", TownshipEng = "Mongmit Township", CodeMm = "မမတ", CodeEng = "MaMaTa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုးနဲမြို့နယ်", TownshipEng = "Mongnai Township", CodeMm = "မနန", CodeEng = "MaNaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုင်းပြင်းမြို့နယ်", TownshipEng = "Mongping Township", CodeMm = "မပန", CodeEng = "MaPaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုင်းတုံမြို့နယ်", TownshipEng = "Mongton Township", CodeMm = "မတန", CodeEng = "MaTaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုင်းရယ်မြို့နယ်", TownshipEng = "Mongyai Township", CodeMm = "မရတ", CodeEng = "MaYaTa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုင်းယန်းမြို့နယ်", TownshipEng = "Mongyang Township", CodeMm = "မယန", CodeEng = "MaYaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုင်းယောင်းမြို့နယ်", TownshipEng = "Mongyawng Township", CodeMm = "မယန", CodeEng = "MaYaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မူဆယ်မြို့နယ်", TownshipEng = "Muse Township", CodeMm = "မဆတ", CodeEng = "MaSaTa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "နမ်ခမ်း", TownshipEng = "Nam Hkam Wu Township", CodeMm = "နခဝ", CodeEng = "NaKhaWa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "နမ်တစ်မြို့နယ်", TownshipEng = "Nam Tit Township", CodeMm = "နတန", CodeEng = "NaTaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "နမ်းခမ်းမြို့နယ်", TownshipEng = "Namhkan Township", CodeMm = "နခန", CodeEng = "NaKhaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "နမ္မတူမြို့နယ်", TownshipEng = "Namtu Township", CodeMm = "နမတ", CodeEng = "NaMaTa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "နမ်ဖိုင်မြို့နယ်", TownshipEng = "Nan Hpai Township", CodeMm = "နဖန", CodeEng = "NaHpaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "နမ့်ဆန်မြို့နယ်", TownshipEng = "Nanhsan Township", CodeMm = "နဆန", CodeEng = "NaSaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "နာကောင်မြို့နယ်", TownshipEng = "Nar Kawng Township", CodeMm = "နကန", CodeEng = "NaKaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "နာဝီမြို့နယ်", TownshipEng = "Nar Wee Township", CodeMm = "န၀န", CodeEng = "NaWaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "နာဖန့်မြို့နယ်", TownshipEng = "Narphan Township", CodeMm = "နဖန", CodeEng = "NaPhaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "နောင်ခစ်မြို့နယ်", TownshipEng = "Nawng Hkit Township", CodeMm = "နခန", CodeEng = "NaKhaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "နောင်ချိုမြို့နယ်", TownshipEng = "Nawnghkio Township", CodeMm = "နခတ", CodeEng = "NaKhaTa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ညောင်ရွှေမြို့နယ်", TownshipEng = "Nyaungshwe Township", CodeMm = "ညရန", CodeEng = "NyaYaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ပန်သင်းမြို့နယ်", TownshipEng = "Pang Hkam Township", CodeMm = "ပခန", CodeEng = "PaKhaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ပန်ယန်မြို့နယ်", TownshipEng = "Pang Yang Township", CodeMm = "ပယန", CodeEng = "PaYaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ပန်ဆန်မြို့နယ်", TownshipEng = "Pangsang Township", CodeMm = "ပဆန", CodeEng = "PaSaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ပန်ဝါမြို့နယ်", TownshipEng = "Pangwaun Township", CodeMm = "ပဝန", CodeEng = "PaWaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ဖယ်ခုံမြို့နယ်", TownshipEng = "Pekon Township", CodeMm = "ဖခန", CodeEng = "HpaKhaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ပင်းတယမြို့နယ်", TownshipEng = "Pindaya Township", CodeMm = "ပတယ", CodeEng = "PaTaYa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ပင်လောင်းမြို့နယ်", TownshipEng = "Pinlaung Township", CodeMm = "ပလန", CodeEng = "PaLaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "တာချီလိတ်မြို့နယ်", TownshipEng = "Tachileik Township", CodeMm = "တခလ", CodeEng = "TaKhaLa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "တန့်ယန်းမြို့နယ်", TownshipEng = "Tangyan Township", CodeMm = "တယန", CodeEng = "TaYaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "တောင်ကြီးမြို့နယ်", TownshipEng = "Taunggyi Township", CodeMm = "တကန", CodeEng = "TaKaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ယောင်လင်းမြို့နယ်", TownshipEng = "Yawng Lin Township", CodeMm = "ယလန", CodeEng = "YaLaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ရပ်စောက်မြို့နယ်", TownshipEng = "Yetsawk Township", CodeMm = "ရစန", CodeEng = "YaSaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ရင်ဖန့် မမို့နယ်", TownshipEng = "Yin Pang Township", CodeMm = "ရဖန", CodeEng = "YaHpaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ရွာငံမြို့နယ်", TownshipEng = "Ywangan Township", CodeMm = "ရငန", CodeEng = "YaNgaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "နောင်တရာမြို့နယ်", TownshipEng = "Naungtayar Myo Nal Khwal", CodeMm = "နတယ", CodeEng = "NaTaYa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ပင်လုံမြို့နယ်", TownshipEng = "Pinlon Township", CodeMm = "", CodeEng = "PaLaTa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ခိုလမ်မြို့နယ်", TownshipEng = "Kholan", CodeMm = "ခလန", CodeEng = "KhaLaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မန့်ဟီးရိုးမြို့နယ်", TownshipEng = "Manhero", CodeMm = "မဟရ", CodeEng = "MaHaYa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ပုံပါကျင်မြို့နယ်", TownshipEng = "Ponparkyin", CodeMm = "ပပက", CodeEng = "PaPaKa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "တာမိုးညမြို့နယ်", TownshipEng = "Tarmoenyae", CodeMm = "တမည", CodeEng = "TaMaNya" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုးဗြဲမြို့နယ်", TownshipEng = "Moebyae", CodeMm = "", CodeEng = "MaBaTa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "မိုင်းငေါ့မြို့နယ်", TownshipEng = "Mongngawt Township", CodeMm = "မငန", CodeEng = "MaNgaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "အင်းတော်မြို့နယ်", TownshipEng = "Intaw Tonwship", CodeMm = "အတန", CodeEng = "AhTaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "တာလေမြို့နယ်", TownshipEng = "Tarlay Township", CodeMm = "တလန", CodeEng = "TaLaNa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "အာဂယမြို့နယ်", TownshipEng = "Ah Ga Ya Township", CodeMm = "အဂယ", CodeEng = "AhGaYa" },
                new NrcTownship { RegionCode = 13, StateOrRegion = "Shan", TownshipMm = "ပတနမြို့နယ်", TownshipEng = "Pa Ta Na Township", CodeMm = "ပတန", CodeEng = "PaTaNa" },

                // ---------------- 14 / AYEYARWADDY ----------------
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "အမရမြို့နယ်", TownshipEng = "Amar Township", CodeMm = "အမတ", CodeEng = "AhMaTa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ဘိုကလေးမြို့နယ်", TownshipEng = "Bogale Township", CodeMm = "ဘကလ", CodeEng = "BaKaLa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ဓနုဖြူမြို့နယ်", TownshipEng = "Danubyu Township", CodeMm = "ဓနဖ", CodeEng = "DaNaHpa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ဒေးဒရဲမြို့နယ်", TownshipEng = "Dedaye Township", CodeMm = "", CodeEng = "DaDaYa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "အိမ်မဲမြို့နယ်", TownshipEng = "Einme Township", CodeMm = "အမန", CodeEng = "AhMaNa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ဟိုင်းကြီးကျွန်းမြို့နယ်", TownshipEng = "Haigyi Island Township", CodeMm = "ဟကက", CodeEng = "HaKaKa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ဟင်္သာတမြို့နယ်", TownshipEng = "Hinthada Township", CodeMm = "ဟသတ", CodeEng = "HaThaTa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "အင်္ဂပူမြို့နယ်", TownshipEng = "Ingapu Township", CodeMm = "အဂပ", CodeEng = "AhGaPa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ကန်ကြီးထောင့်မြို့နယ်", TownshipEng = "Kangyidaunt Township", CodeMm = "", CodeEng = "KaKaHta" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ကျိုက်လတ်မြို့နယ်", TownshipEng = "Kyaiklat Township", CodeMm = "ကလန", CodeEng = "KaLaNa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ကြံခင်းမြို့နယ်", TownshipEng = "Kyangin Township", CodeMm = "ကခန", CodeEng = "KaKhaNa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ကျောင်းကုန်းမြို့နယ်", TownshipEng = "Kyaunggon Township", CodeMm = "ကကန", CodeEng = "KaKaNa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ကျုံပျော်မြို့နယ်", TownshipEng = "Kyonpyaw Township", CodeMm = "ကပန", CodeEng = "KaPaNa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "လပွတ္တာမြို့နယ်", TownshipEng = "Labutta Township", CodeMm = "", CodeEng = "LaPaTa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "လေးမျက်နှာမြို့နယ်", TownshipEng = "Lemyethana Township", CodeMm = "လမန", CodeEng = "LaMaNa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "မအူပင်မြို့နယ်", TownshipEng = "Maubin Township", CodeMm = "မအပ", CodeEng = "MaAhPa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "မော်လမြိုင်ကျွန်းမြို့နယ်", TownshipEng = "Mawlamyinegyun Township", CodeMm = "မမက", CodeEng = "MaMaKa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "မြန်အောင်မြို့နယ်", TownshipEng = "Myanaung Township", CodeMm = "မအန", CodeEng = "MaAhNa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "မြောင်းမြမြို့နယ်", TownshipEng = "Myaungmya Township", CodeMm = "မမန", CodeEng = "MaMaNa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ငပုတောမြို့နယ်", TownshipEng = "Ngapudaw Township", CodeMm = "ငပတ", CodeEng = "NgaPaTa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ငါးသိုင်းချောင်းမြို့နယ်", TownshipEng = "Ngathinechaung Township", CodeMm = "ငသခ", CodeEng = "Nga ThaKha" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ငရုတ်ကောင်းမြို့နယ်", TownshipEng = "Ngayotekaung Township", CodeMm = "ငရက", CodeEng = "NgaYaKa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ငွေဆောင်မြို့နယ်", TownshipEng = "Ngwesaung Township", CodeMm = "ငဆန", CodeEng = "NgaSaNa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ငွေသောင်ယံမြို့နယ်", TownshipEng = "Ngwethoungyan Township", CodeMm = "ငသယ", CodeEng = "Nga ThaYa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ညောင်တုန်းမြို့နယ်", TownshipEng = "Nyaungdon Township", CodeMm = "ညတန", CodeEng = "NyaTaNa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ပန်းတနော်မြို့နယ်", TownshipEng = "Pantanaw Township", CodeMm = "ပတန", CodeEng = "PaTaNa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ပုသိမ်မြို့နယ်", TownshipEng = "Pathein Township", CodeMm = "ပသန", CodeEng = "PaThaNa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ဖျာပုံမြို့နယ်", TownshipEng = "Pyapon Township", CodeMm = "ဖပန", CodeEng = "HpaPaNa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ပြင်စလူမြို့နယ်", TownshipEng = "Pyinsalu Township", CodeMm = "ပစလ", CodeEng = "PaSaLa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ရွှေသောင်ယံမြို့နယ်", TownshipEng = "Shwethoungyan Township", CodeMm = "ရသယ", CodeEng = "YaThaYa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "သာပေါင်းမြို့နယ်", TownshipEng = "Thabaung Township", CodeMm = "သပန", CodeEng = "ThaPaNa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ဝါးခယ်မမြို့နယ်", TownshipEng = "Wakema Township", CodeMm = "", CodeEng = "WaKhaMa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ရေကြည်မြို့နယ်", TownshipEng = "Yegyi Township", CodeMm = "ရကန", CodeEng = "YaKaNa" },
                new NrcTownship { RegionCode = 14, StateOrRegion = "Ayarwaddy", TownshipMm = "ဇလွန်မြို့နယ်", TownshipEng = "Zalun Township", CodeMm = "ဇလန", CodeEng = "ZaLaNa" }
            };
        }

        #endregion
    }
}