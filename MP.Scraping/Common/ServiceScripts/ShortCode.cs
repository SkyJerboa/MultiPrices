using System;

namespace MP.Scraping.Common.ServiceScripts
{
    public class ShortCode : Attribute
    {
        public string ServiceCode;

        public ShortCode(string serviceCode)
        {
            ServiceCode = serviceCode;
        }
    }
}
