namespace MP.Scraping.Common.Helpers
{
    public static class LanguageHelper
    {
        public static string GetLCIDString(string lang)
        {
            if (lang == null)
                return null;

            switch(lang.ToLower())
            {
                case "french":
                case "fr": return "fr_FR"; //Franch //Français
                case "russian":
                case "ru": return "ru_RU"; //Russian //Русский
                case "english":
                case "en": return "en_US"; //English //English (United States)
                case "polish":
                case "pl": return "pl_PL"; //Polish //Polski
                case "german":
                case "de": return "de_DE"; //German //Deutsch
                case "spanish - spain":
                case "es": return "es_ES"; //Spanish //Español
                case "spanish - latin america": return "es_LA"; //Spanish Latin America es-419 //Español (América latina)
                case "es_mx": return "es_MX"; //Spanish (Mexico) //Español (México)
                case "italian":
                case "it": return "it_IT"; //Italian //Italiano
                case "portuguese - brazil":
                case "br": return "pt_BR"; //Portuguese - Brazil //Português(Brasil)
                case "portuguese":
                case "pt": return "pt_PT"; //Portuguese (Portugal) //Português (Portugal)
                case "zh": case "cn":
                case "simplified chinese": return "zh_CN"; //Simplified Chinese //中文 (简体)
                case "traditional chinese": return "zh_TW"; //Traditional Chinese //中文 (簡體)
                case "ja":
                case "japanese":
                case "jp": return "ja_JP"; //Japanese //日本語
                case "ko":
                case "korean":
                case "kr": return "ko_KR"; //Korean //한국어
                case "da": return "da_DK"; //Danish //dansk
                case "no": return "no_NO"; //Norwegian //bokmål
                case "swedish":
                case "sv": return "sv_SE"; //Swedish (Sweden) //svenska
                case "turkish":
                case "tr": return "tr_TR"; //Turkish (Turkey) //Türkçe
                case "hungarian":
                case "hu": return "hu_HU"; //Hungarian (Hungary) //magyar
                case "arabic":
                case "ar": return "ar_SA"; //Arabic (Saudi Arabia) //ﺔﻴﺐﺮﻌﻠﺍ
                case "czech":
                case "cz": return "cs_CZ"; //Czech //čeština
                case "fi": return "fi_FI"; //Finnish //suomi
                case "dutch":
                case "nl": return "nl_NL"; //Dutch //Nederlands
                case "gk": case "el":
                case "gr": return "el_GR"; //Greek //ελληνικά
                case "uk": return "uk_UA"; //Ukrainian //україньска
                case "ca": return "ca_ES"; //Catalan //català
                case "romanian":
                case "ro": return "ro_RO"; //Romanian //română
                case "thai":
                case "th": return "th_TH"; //Thai (Thailand) //ไทย
                case "bl":
                case "bg": return "bg_BG"; //Bulgarian //български
                case "sk": return "sk_SK"; //Slovak //slovenčina
                case "sb":
                case "sr": return "sr_SP"; //Serbian (Cyrillic) //српски
                case "be": return "be_BY"; //Belarusian //беларуски
                case "he": return "he_IL"; //Hebrew //תירבע
                case "is": return "is_IS"; //Icelandic //íslenska
                case "fa": return "fa_IR"; //Farsi - Iran //ﻰﺴﺮﺎﻓ
                case "vietnamese": return "vi_VN";//Vietnamese (Viet Nam) //Tiểng Việt
                default:
                    Serilog.Log.Warning($"Unknown language '{lang}'");
                    return lang;
            }
        }
    }
}
