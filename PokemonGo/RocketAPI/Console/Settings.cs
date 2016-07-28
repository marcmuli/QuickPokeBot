#region

using System.Configuration;
using System.Globalization;
using System.Runtime.CompilerServices;
using PokemonGo.RocketAPI.Enums;
using System.Collections.Generic;
using AllEnum;
using System;
using System.Text.RegularExpressions;

#endregion

namespace PokemonGo.RocketAPI.Console
{
    public class Settings : ISettings
    {
        /// <summary>
        ///     Don't touch. User settings are in Console/App.config
        /// </summary>
        public string TransferType => GetSetting() != string.Empty ? GetSetting() : "none";
        public int TransferCPThreshold => GetSetting() != string.Empty ? int.Parse(GetSetting(), CultureInfo.InvariantCulture) : 0;
        public bool EvolveAllGivenPokemons => GetSetting() != string.Empty ? System.Convert.ToBoolean(GetSetting(), CultureInfo.InvariantCulture) : false;


        public AuthType AuthType => (GetSetting() != string.Empty ? GetSetting() : "Ptc") == "Ptc" ? AuthType.Ptc : AuthType.Google;
        public string PtcUsername => GetSetting() != string.Empty ? GetSetting() : "username";
        public string PtcPassword => GetSetting() != string.Empty ? GetSetting() : "password";

        public string GoogleEmail => GetSetting() != string.Empty ? GetSetting() : "username";
        public string GooglePassword => GetSetting() != string.Empty ? GetSetting() : "password";

        public double DefaultLatitude => GetSetting() != string.Empty ? double.Parse(GetSetting(), CultureInfo.InvariantCulture) : 51.22640;

        //Default Amsterdam Central Station if another location is not specified
        public double DefaultLongitude => GetSetting() != string.Empty ? double.Parse(GetSetting(), CultureInfo.InvariantCulture) : 6.77874;

        //Default Amsterdam Central Station if another location is not specified

        // LEAVE EVERYTHING ALONE

        public string LevelOutput => GetSetting() != string.Empty ? GetSetting() : "time";

        public int LevelTimeInterval => GetSetting() != string.Empty ? System.Convert.ToInt16(GetSetting()) : 600;

        public int ItemRecyclingCount => GetSetting() != string.Empty ? System.Convert.ToInt16(GetSetting()) : 0;

        ICollection<KeyValuePair<ItemId, int>> ISettings.ItemRecycleFilter
        {
            get
            {
                ICollection<KeyValuePair<ItemId, int>> pairs = new Dictionary<ItemId, int>();
                string[] filterStrings = GetSetting().Replace(" ", "").Split(',');
                Regex regex = new Regex(@"[A-Za-z]:[0-9]");
                foreach (var filterString in filterStrings)
                {
                    if (!regex.Match(filterString).Success)
                        continue;
                    ItemId item;
                    int count;
                    string[] pairString = filterString.Split(':');
                    if (!Enum.TryParse<ItemId>("Item" + pairString[0], out item) ||
                        !int.TryParse(pairString[1], out count))
                        continue;
                    pairs.Add(new KeyValuePair<ItemId, int>(item, count));
                }
                return pairs;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int RecycleItemsInterval => GetSetting() != string.Empty ? Convert.ToInt16(GetSetting()) : 60;

        public string Language => GetSetting() != string.Empty ? GetSetting() : "english";

        public string GoogleRefreshToken
        {
            get { return GetSetting() != string.Empty ? GetSetting() : string.Empty; }
            set { SetSetting(value); }
        }

        private string GetSetting([CallerMemberName] string key = null)
        {
            return ConfigurationManager.AppSettings[key];
        }

        private void SetSetting(string value, [CallerMemberName] string key = null)
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (key != null) configFile.AppSettings.Settings[key].Value = value;
            configFile.Save();
        }
    }
}
