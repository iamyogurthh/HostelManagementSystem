using CommunityToolkit.Mvvm.ComponentModel;
using HostelManagementSystem.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace HostelManagementSystem.ViewModels.StudentViewModels
{
    public partial class NrcInputViewModel : ObservableObject
    {
        private readonly NrcManager _nrcManager;

        public ObservableCollection<string> RegionCodes { get; } = new();
        public ObservableCollection<string> CitizenTypes { get; } = new();
        public ObservableCollection<string> TownshipCodes { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FullNrc))]
        private string _selectedRegionCode = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FullNrc))]
        private string _selectedTownshipCode = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FullNrc))]
        private string _selectedCitizenType = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FullNrc))]
        private string _nrcNumber = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        private static readonly Regex NrcRegex = new Regex(
            @"^(?<region>\d{1,2}\/|\d{1,2})?\s*(?<township>[A-Za-z0-9]+)\s*[\(\s]*\(?(?<type>[^\)\d]+)\)?[\)\s]*(?<number>\d+)$",
            RegexOptions.Compiled);

        public NrcInputViewModel(NrcManager nrcManager)
        {
            _nrcManager = nrcManager;
            InitializeDataSources();
        }

        public NrcInputViewModel() : this(NrcManager.Instance) { }

        private void InitializeDataSources()
        {
            RegionCodes.Clear();
            foreach (var region in _nrcManager.GetRegionCodesFormatted())
            {
                RegionCodes.Add(region);
            }

            CitizenTypes.Clear();
            foreach (var type in _nrcManager.CitizenTypes)
            {
                CitizenTypes.Add(type);
            }

            SelectedRegionCode = RegionCodes.FirstOrDefault() ?? string.Empty;
            SelectedCitizenType = CitizenTypes.FirstOrDefault() ?? string.Empty;
        }

        partial void OnSelectedRegionCodeChanged(string value)
        {
            TownshipCodes.Clear();
            int regionNumber = ParseRegionNumber(value);

            if (regionNumber > 0)
            {
                foreach (var township in _nrcManager.GetTownshipCodesEngByRegion(regionNumber))
                {
                    TownshipCodes.Add(township);
                }
            }

            if (!TownshipCodes.Contains(SelectedTownshipCode))
            {
                SelectedTownshipCode = TownshipCodes.FirstOrDefault() ?? string.Empty;
            }
        }

        public bool Validate(string personLabel)
        {
            int regionNumber = ParseRegionNumber(SelectedRegionCode);

            string cleanCitizenType = SelectedCitizenType?.Trim(' ', '(', ')') ?? string.Empty;
            string rawCitizenType = SelectedCitizenType ?? string.Empty;
            string townshipCode = SelectedTownshipCode ?? string.Empty;
            string cleanNumber = NrcNumber?.Trim() ?? string.Empty;

            if (regionNumber <= 0 || string.IsNullOrWhiteSpace(townshipCode) || string.IsNullOrWhiteSpace(cleanNumber))
            {
                ErrorMessage = $"Invalid {personLabel} NRC details.";
                return false;
            }

            bool checkClean = _nrcManager.ValidateNrc(regionNumber, townshipCode, cleanCitizenType, cleanNumber);
            bool checkRaw = _nrcManager.ValidateNrc(regionNumber, townshipCode, rawCitizenType, cleanNumber);
            bool isValid = checkClean || checkRaw;

            ErrorMessage = isValid ? string.Empty : $"Invalid {personLabel} NRC details.";
            return isValid;
        }

        public void SetNrc(string? fullNrc)
        {
            if (string.IsNullOrWhiteSpace(fullNrc))
            {
                SelectedTownshipCode = string.Empty;
                NrcNumber = string.Empty;
                return;
            }

            var match = NrcRegex.Match(fullNrc.Trim());

            if (match.Success)
            {
                string rawRegion = match.Groups["region"].Value;
                string township = match.Groups["township"].Value;
                string rawType = match.Groups["type"].Value.Replace("(", "").Replace(")", "").Trim();
                string number = match.Groups["number"].Value;

                int regionNum = ParseRegionNumber(rawRegion);
                var matchedRegion = RegionCodes.FirstOrDefault(r => ParseRegionNumber(r) == regionNum);

                if (!string.IsNullOrEmpty(matchedRegion))
                {
                    SelectedRegionCode = matchedRegion;
                }

                var matchedTownship = TownshipCodes.FirstOrDefault(t => string.Equals(t.Trim(), township.Trim(), StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(matchedTownship))
                {
                    SelectedTownshipCode = matchedTownship;
                }
                else
                {
                    TownshipCodes.Add(township);
                    SelectedTownshipCode = township;
                }

                string cleanRawType = rawType.Trim(' ', '(', ')');

                var matchedCitizenType = CitizenTypes.FirstOrDefault(c =>
                {
                    string cleanC = c.Trim(' ', '(', ')');
                    return string.Equals(cleanC, cleanRawType, StringComparison.OrdinalIgnoreCase);
                });

                SelectedCitizenType = matchedCitizenType ?? cleanRawType;
                NrcNumber = number;
            }
            else
            {
                NrcNumber = fullNrc;
            }
        }

        public string FullNrc
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NrcNumber))
                    return string.Empty;

                string cleanCitizenType = SelectedCitizenType?.Trim(' ', '(', ')') ?? string.Empty;
                return $"{SelectedRegionCode}{SelectedTownshipCode}({cleanCitizenType}){NrcNumber.Trim()}";
            }
        }

        private static int ParseRegionNumber(string formattedRegion)
        {
            if (string.IsNullOrEmpty(formattedRegion)) return 0;
            string numericString = formattedRegion.Replace("/", "").Trim();
            return int.TryParse(numericString, out int regionCode) ? regionCode : 0;
        }
    }
}