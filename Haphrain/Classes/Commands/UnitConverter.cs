using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haphrain.Classes.Commands
{
    public class UnitConverter : ModuleBase<SocketCommandContext>
    {
        private string[] SupportedUnitsDist = new string[] { "km", "m", "cm", "mm", "mi", "yd", "ft", "inch" };
        private string[] SupportedUnitsLiq = new string[] { "l", "dl", "cl", "ml", "gal","oz" };
        private string[] SupportedUnitsWgt = new string[] { "kg", "g", "dg", "cg", "mg", "st", "lbs", "oz"};

        [Command("convert dist"), Summary("Convert distance units"), Priority(2)]
        public async Task ConvertMetricImp(string convertAmt, string StartUnit, string EndUnit)
        {
            StartUnit = StartUnit.ToLower();
            EndUnit = EndUnit.ToLower();
            double amtToConvert = 0d;
            bool hasDot = false;

            #region Errorchecking
            if (!SupportedUnitsDist.Contains(StartUnit))
            {
                var msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention} -> Your start unit is incorrect, supported units: {string.Join(" - ", SupportedUnitsDist)}");
                GlobalVars.AddRandomTracker(msg);
                return;
            }
            if (!SupportedUnitsDist.Contains(EndUnit))
            {
                var msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention} -> Your end unit is incorrect, supported units: {string.Join(" - ", SupportedUnitsDist)}");
                GlobalVars.AddRandomTracker(msg);
                return;
            }

            try
            {
                if (convertAmt.Contains(','))
                {
                    amtToConvert = double.Parse(convertAmt.Replace(@",", @"."));
                }
                else
                {
                    amtToConvert = double.Parse(convertAmt);
                    hasDot = true;
                }
            }
            catch
            {
                var msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention} -> Something went wrong while reading your number, your entry: {convertAmt}");
                GlobalVars.AddRandomTracker(msg);
                return;
            }
            #endregion

            double valuesToSend = 0d;
            switch (EndUnit)
            {
                case "km":
                    valuesToSend = ConvertHelpers.ToKm(amtToConvert, StartUnit);
                    break;
                case "m":
                    valuesToSend = ConvertHelpers.ToMeters(amtToConvert, StartUnit);
                    break;
                case "cm":
                    valuesToSend = ConvertHelpers.ToCm(amtToConvert, StartUnit);
                    break;
                case "mm":
                    valuesToSend = ConvertHelpers.ToMm(amtToConvert, StartUnit);
                    break;
                case "mi":
                    valuesToSend = ConvertHelpers.ToMiles(amtToConvert, StartUnit);
                    break;
                case "ft":
                    valuesToSend = ConvertHelpers.ToFeet(amtToConvert, StartUnit);
                    break;
                case "inch":
                    valuesToSend = ConvertHelpers.ToInches(amtToConvert, StartUnit);
                    break;
                default:
                    break;
            }

            NumberFormatInfo ni = new CultureInfo("sv-SE", false).NumberFormat;
            string r = valuesToSend.ToString("N6", ni);

            if (r.Contains(','))
            {
                for (int i = r.Length - 1; i > 0; i--)
                {
                    if (!r.Contains(',')) break;
                    if (r[i] == '0' || r[i] == ',')
                    {
                        var rArray = r.ToList();
                        rArray.RemoveAt(i);
                        r = string.Join("", rArray);
                    }
                    else break;
                }
            }
            if (r.Contains(',') && hasDot)
                r = r.Replace(@",", @".");
            await Context.Channel.SendMessageAsync($"`{convertAmt} {StartUnit} ≈ {r} {EndUnit}`");
        }

        [Command("convert temp"), Summary("Convert temperature units"), Priority(2)]
        public async Task ConvertCF(string convertAmt, string StartUnit, string EndUnit)
        {
            StartUnit = StartUnit.ToUpper();
            EndUnit = EndUnit.ToUpper();
            double amtToConvert = 0d;
            bool hasDot = false;

            #region Errorchecking
            if (!("CFK").Contains(StartUnit))
            {
                var msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention} -> Your start unit is incorrect, please use *C*, *F* or *K*");
                GlobalVars.AddRandomTracker(msg);
                return;
            }
            if (!("CFK").Contains(EndUnit))
            {
                var msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention} -> Your end unit is incorrect, please use *C*, *F* or *K*");
                GlobalVars.AddRandomTracker(msg);
                return;
            }
            if (StartUnit == EndUnit)
            {
                var msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention} -> Why would you want to convert to the same unit measure?");
                GlobalVars.AddRandomTracker(msg);
                return;
            }

            try
            {
                if (convertAmt.Contains(','))
                {
                    amtToConvert = double.Parse(convertAmt.Replace(@",", @"."));
                }
                else
                {
                    amtToConvert = double.Parse(convertAmt);
                    hasDot = true;
                }
            }
            catch
            {
                var msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention} -> Something went wrong while reading your number, your entry: {convertAmt}");
                GlobalVars.AddRandomTracker(msg);
                return;
            }
            #endregion

            double resultValue = 0d;
            if (EndUnit == "F")
                resultValue = ConvertHelpers.ToFahrenheit(amtToConvert, StartUnit);
            else if (EndUnit == "C")
                resultValue = ConvertHelpers.ToCelcius(amtToConvert, StartUnit);
            else
                resultValue = ConvertHelpers.ToKelvin(amtToConvert, StartUnit);

            NumberFormatInfo ni = new CultureInfo("sv-SE", false).NumberFormat;
            string r = resultValue.ToString("N6", ni);
            if (r.Contains(','))
            {
                for (int i = r.Length-1; i > 0; i--)
                {
                    if (!r.Contains(',')) break;
                    if (r[i] == '0' || r[i] == ',')
                    {
                        var rArray = r.ToList();
                        rArray.RemoveAt(i);
                        r = string.Join("", rArray);
                    }
                    else break;
                }
            }
            if (r.Contains(',') && hasDot)
                r = r.Replace(@",", @".");
            await Context.Channel.SendMessageAsync($"`{convertAmt} °{StartUnit} ≈ {r} °{EndUnit}`");
        }

        [Command("convert liq"), Summary("Convert liquid volume units"), Priority(2)]
        public async Task ConvertLiq(string convertAmt, string StartUnit, string EndUnit)
        {
            StartUnit = StartUnit.ToLower();
            EndUnit = EndUnit.ToLower();
            double amtToConvert = 0d;
            bool hasDot = false;

            #region Errorchecking
            if (!SupportedUnitsLiq.Contains(StartUnit))
            {
                var msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention} -> Your start unit is incorrect, supported units: {string.Join(" - ", SupportedUnitsLiq)}");
                GlobalVars.AddRandomTracker(msg);
                return;
            }
            if (!SupportedUnitsLiq.Contains(EndUnit))
            {
                var msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention} -> Your end unit is incorrect, supported units: {string.Join(" - ", SupportedUnitsLiq)}");
                GlobalVars.AddRandomTracker(msg);
                return;
            }

            try
            {
                if (convertAmt.Contains(','))
                {
                    amtToConvert = double.Parse(convertAmt.Replace(@",", @"."));
                }
                else
                {
                    amtToConvert = double.Parse(convertAmt);
                    hasDot = true;
                }
            }
            catch
            {
                var msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention} -> Something went wrong while reading your number, your entry: {convertAmt}");
                GlobalVars.AddRandomTracker(msg);
                return;
            }
            #endregion

            double valuesToSend = 0d;
            switch (EndUnit)
            {
                case "l":
                    valuesToSend = ConvertHelpers.ToLiters(amtToConvert, StartUnit);
                    break;
                case "dl":
                    valuesToSend = ConvertHelpers.ToDl(amtToConvert, StartUnit);
                    break;
                case "cl":
                    valuesToSend = ConvertHelpers.ToCl(amtToConvert, StartUnit);
                    break;
                case "ml":
                    valuesToSend = ConvertHelpers.ToMl(amtToConvert, StartUnit);
                    break;
                case "gal":
                    valuesToSend = ConvertHelpers.ToGallons(amtToConvert, StartUnit);
                    break;
                case "oz":
                    valuesToSend = ConvertHelpers.ToFlOunces(amtToConvert, StartUnit);
                    break;
                default: break;
            }

            NumberFormatInfo ni = new CultureInfo("sv-SE", false).NumberFormat;
            string r = valuesToSend.ToString("N6", ni);
            if (r.Contains(','))
            {
                for (int i = r.Length - 1; i > 0; i--)
                {
                    if (!r.Contains(',')) break;
                    if (r[i] == '0' || r[i] == ',')
                    {
                        var rArray = r.ToList();
                        rArray.RemoveAt(i);
                        r = string.Join("", rArray);
                    }
                    else break;
                }
            }
            if (r.Contains(',') && hasDot)
                r = r.Replace(@",", @".");
            await Context.Channel.SendMessageAsync($"`{convertAmt} {StartUnit} ≈ {r} {EndUnit}`");
        }

        [Command("convert wgt"), Summary("Convert weight units"), Priority(2)]
        public async Task ConvertWgt(string convertAmt, string StartUnit, string EndUnit)
        {
            StartUnit = StartUnit.ToLower();
            if (StartUnit == "pound" || StartUnit == "pounds") StartUnit = "lbs";
            if (StartUnit == "ounce") StartUnit = "oz";
            if (StartUnit == "stone") StartUnit = "st";

            EndUnit = EndUnit.ToLower();
            if (EndUnit == "pound" || EndUnit == "pounds") EndUnit = "lbs";
            if (EndUnit == "ounce") EndUnit = "oz";
            if (EndUnit == "stone") EndUnit = "st";

            double amtToConvert = 0d;
            bool hasDot = false;

            #region Errorchecking
            if (!SupportedUnitsWgt.Contains(StartUnit))
            {
                var msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention} -> Your start unit is incorrect, supported units: {string.Join(" - ", SupportedUnitsWgt)}");
                GlobalVars.AddRandomTracker(msg);
                return;
            }
            if (!SupportedUnitsWgt.Contains(EndUnit))
            {
                var msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention} -> Your end unit is incorrect, supported units: {string.Join(" - ", SupportedUnitsWgt)}");
                GlobalVars.AddRandomTracker(msg);
                return;
            }

            try
            {
                if (convertAmt.Contains(','))
                {
                    amtToConvert = double.Parse(convertAmt.Replace(@",", @"."));
                }
                else
                {
                    amtToConvert = double.Parse(convertAmt);
                    hasDot = true;
                }
            }
            catch
            {
                var msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention} -> Something went wrong while reading your number, your entry: {convertAmt}");
                GlobalVars.AddRandomTracker(msg);
                return;
            }
            #endregion

            double valuesToSend = 0d;
            switch (EndUnit)
            {
                case "kg":
                    valuesToSend = ConvertHelpers.ToKilos(amtToConvert, StartUnit);
                    break;
                case "g":
                    valuesToSend = ConvertHelpers.ToG(amtToConvert, StartUnit);
                    break;
                case "dg":
                    valuesToSend = ConvertHelpers.ToDg(amtToConvert, StartUnit);
                    break;
                case "cg":
                    valuesToSend = ConvertHelpers.ToCg(amtToConvert, StartUnit);
                    break;
                case "mg":
                    valuesToSend = ConvertHelpers.ToMg(amtToConvert, StartUnit);
                    break;
                case "st":
                    valuesToSend = ConvertHelpers.ToStone(amtToConvert, StartUnit);
                    break;
                case "lbs":
                    valuesToSend = ConvertHelpers.ToLbs(amtToConvert, StartUnit);
                    break;
                case "oz":
                    valuesToSend = ConvertHelpers.ToOz(amtToConvert, StartUnit);
                    break;
                default: break;
            }

            NumberFormatInfo ni = new CultureInfo("sv-SE", false).NumberFormat;
            string r = valuesToSend.ToString("N6", ni);
            if (r.Contains(','))
            {
                for (int i = r.Length - 1; i > 0; i--)
                {
                    if (!r.Contains(',')) break;
                    if (r[i] == '0' || r[i] == ',')
                    {
                        var rArray = r.ToList();
                        rArray.RemoveAt(i);
                        r = string.Join("", rArray);
                    }
                    else break;
                }
            }
            if (r.Contains(',') && hasDot)
                r = r.Replace(@",", @".");
            await Context.Channel.SendMessageAsync($"`{convertAmt} {StartUnit} ≈ {r} {EndUnit}`");
        }

        [Command("convert cur"), Summary("Convert between currency units"), Priority(2)]
        public async Task ConvertCurrency(string convertAmt, string StartUnit, string EndUnit)
        {
            if (!GlobalVars.CurrencyList.Keys.Contains(StartUnit.ToUpper())) {
                var msg = await Context.Channel.SendMessageAsync($"The provided start unit ({StartUnit}) was not recognized.");
                GlobalVars.AddRandomTracker(msg);
                return;
            }
            if (!GlobalVars.CurrencyList.Keys.Contains(EndUnit.ToUpper()))
            {
                var msg = await Context.Channel.SendMessageAsync($"The provided end unit ({EndUnit}) was not recognized.");
                GlobalVars.AddRandomTracker(msg);
                return;
            }

            double amtToConvert = 0d;
            bool hasDot = false;

            #region Errorchecking
            try
            {
                if (convertAmt.Contains(','))
                {
                    amtToConvert = double.Parse(convertAmt.Replace(@",", @"."));
                }
                else
                {
                    amtToConvert = double.Parse(convertAmt);
                    hasDot = true;
                }
            }
            catch
            {
                var msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention} -> Something went wrong while reading your number, your entry: {convertAmt}");
                GlobalVars.AddRandomTracker(msg);
                return;
            }
            #endregion

            double valuesToSend = 0d;

            var toUSD = amtToConvert * GlobalVars.CurrencyList[StartUnit.ToUpper()].ValueInUSD;
            valuesToSend = GlobalVars.CurrencyList[EndUnit.ToUpper()].ValueInUSD / toUSD;

            NumberFormatInfo ni = new CultureInfo("sv-SE", false).NumberFormat;
            string r = valuesToSend.ToString("N6", ni);
            if (r.Contains(','))
            {
                for (int i = r.Length - 1; i > 0; i--)
                {
                    if (!r.Contains(',')) break;
                    if (r[i] == '0' || r[i] == ',')
                    {
                        var rArray = r.ToList();
                        rArray.RemoveAt(i);
                        r = string.Join("", rArray);
                    }
                    else break;
                }
            }
            if (r.Contains(',') && hasDot)
                r = r.Replace(@",", @".");
            await Context.Channel.SendMessageAsync($"`{convertAmt} {GlobalVars.CurrencyList[StartUnit.ToUpper()].FullName} ≈ {r} {GlobalVars.CurrencyList[EndUnit.ToUpper()].FullName}`");
        }
    }

    internal static class ConvertHelpers
    {
        #region Distance
        internal static double ToMeters(double startAmt, string sourceUnit)
        {
            double d = 0d;

            switch (sourceUnit)
            {
                case "mi":
                    d = startAmt * 1609.344d;
                    break;
                case "ft":
                    d = startAmt * 0.3048d;
                    break;
                case "inch":
                    d = startAmt * 0.0254d;
                    break;
                case "km":
                    d = startAmt * 1000d;
                    break;
                case "cm":
                    d = startAmt * 0.01d;
                    break;
                case "mm":
                    d = startAmt * 0.001d;
                    break;
                default:
                    d = startAmt;
                    break;
            }
            return d;
        }

        internal static double ToKm(double startAmt, string sourceUnit) { return ToMeters(startAmt * 0.001d, sourceUnit); }
        internal static double ToCm(double startAmt, string sourceUnit) { return ToMeters(startAmt * 100d, sourceUnit); }
        internal static double ToMm(double startAmt, string sourceUnit) { return ToMeters(startAmt * 1000d, sourceUnit); }

        internal static double ToMiles(double startAmt,string sourceUnit) { return ToKm(startAmt, sourceUnit) / 1.609344d; }
        internal static double ToFeet(double startAmt, string sourceUnit) { return ToMeters(startAmt, sourceUnit) / 0.3048d; }
        internal static double ToInches(double startAmt, string sourceUnit) { return ToMeters(startAmt,sourceUnit) / 0.0254d; }
        #endregion

        #region Temp
        internal static double ToCelcius(double startAmt, string sourceUnit)
        {
            double d = 0d;
            switch (sourceUnit)
            {
                case "F":
                    d = (startAmt - 32d) * (5d / 9d);
                    break;
                case "K":
                    d = startAmt - 273.15d;
                    break;
                default:
                    d = startAmt;
                    break;
            }
            return d;
        }

        internal static double ToFahrenheit(double startAmt, string sourceUnit) { return (ToCelcius(startAmt, sourceUnit) * (9d / 5d)) + 32d; }
        internal static double ToKelvin(double startAmt, string sourceUnit) { return ToCelcius(startAmt, sourceUnit) + 273.15d; }
        #endregion

        #region Liquid
        internal static double ToLiters(double startAmt, string sourceUnit)
        {
            double d = 0;
            switch (sourceUnit)
            {
                case "dl":
                    d = startAmt * 0.1d;
                    break;
                case "cl":
                    d = startAmt * 0.01d;
                    break;
                case "ml":
                    d = startAmt * 0.001d;
                    break;
                case "gal":
                    d = startAmt * 3.785412d;
                    break;
                case "oz":
                    d = startAmt * 0.029574d;
                    break;
                default:
                    d = startAmt;
                    break;
            }
            return d;
        }

        internal static double ToDl(double startAmt, string sourceUnit) { return ToLiters(startAmt, sourceUnit) * 10d; }
        internal static double ToCl(double startAmt, string sourceUnit) { return ToLiters(startAmt, sourceUnit) * 100d; }
        internal static double ToMl(double startAmt, string sourceUnit) { return ToLiters(startAmt, sourceUnit) * 1000d; }

        internal static double ToGallons(double startAmt, string sourceUnit) { return ToLiters(startAmt, sourceUnit) * 0.264172d; }
        internal static double ToFlOunces(double startAmt, string sourceUnit) { return ToLiters(startAmt, sourceUnit) * 33.814023d; }
        #endregion

        #region Weight
        internal static double ToKilos(double startAmt, string sourceUnit)
        {
            double d = 0d;
            switch (sourceUnit)
            {
                case "kg":
                    d = startAmt;
                    break;
                case "g":
                    d = startAmt / 1000d;
                    break;
                case "dg":
                    d = startAmt / 10000d;
                    break;
                case "cg":
                    d = startAmt / 100000d;
                    break;
                case "mg":
                    d = startAmt / 1000000d;
                    break;
                case "st":
                    d = startAmt / 0.15747;
                    break;
                case "lbs":
                    d = startAmt / 2.2046;
                    break;
                case "oz":
                    d = startAmt / 35.274;
                    break;
            }
            return d;
        }

        internal static double ToG(double startAmt, string sourceUnit) { return ToKilos(startAmt, sourceUnit) * 1000d; }
        internal static double ToDg(double startAmt, string sourceUnit) { return ToKilos(startAmt, sourceUnit) * 10000d; }
        internal static double ToCg(double startAmt, string sourceUnit) { return ToKilos(startAmt, sourceUnit) * 100000d; }
        internal static double ToMg(double startAmt, string sourceUnit) { return ToKilos(startAmt, sourceUnit) * 1000000d; }

        internal static double ToStone(double startAmt, string sourceUnit) { return ToKilos(startAmt, sourceUnit) * 0.15747d; }
        internal static double ToLbs(double startAmt, string sourceUnit) { return ToKilos(startAmt, sourceUnit) * 2.2046d; }
        internal static double ToOz(double startAmt, string sourceUnit) { return ToKilos(startAmt, sourceUnit) * 35.274d; }
        #endregion
    }
}