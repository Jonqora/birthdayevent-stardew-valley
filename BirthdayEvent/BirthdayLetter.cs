using System;
using StardewModdingAPI;

namespace OrbitalEventCreator
{
    public class BirthdayLetter : IAssetEditor
    {
        public BirthdayLetter()
        {
        }

        public bool CanEdit<T>(IAssetInfo asset)
        {
            return asset.AssetNameEquals("Data\\mail");
        }

        public void Edit<T>(IAssetData asset)
        {
            var data = asset.AsDictionary<string, string>().Data;
            data["WizardBirthdayMail"] = "= Esteemed @... = ^ ^The elementals have informed me that today marks the completion of your 26th revolution around the sun. Congratulations! ^ ^You have accomplished much in your twenty-six years, and I am sure you will accomplish much more.  ^ ^To celebrate the occasion, I have arranged an activity for you later today. Please meet me in the town square at 9:00 AM this morning to begin. ^ ^⁠— M. Rasmodius ^— (With some help from the \"ellen\"-mentals. <)";
        }
    }
}

