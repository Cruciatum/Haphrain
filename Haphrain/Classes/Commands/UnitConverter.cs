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
        private string[] SupportesUnitsLiq = new string[] { "l", "dl","cl","ml", "gal","oz" };
        [Command("convert"), Summary("Convert from 1 unit measure to another"), Priority(1)]
        public async Task MainConvert()
        {
            EmbedBuilder builder = new EmbedBuilder() { Title = "Available unit conversions:" };
            builder.AddField("Distance Conversion", $"{Context.Message.Content.Substring(0, 1)}convert dist # <start unit> <end unit> [Supported units: km/m/cm/mm; mi/yd/ft/inch]");
            builder.AddField("Temperature Conversion", $"{Context.Message.Content.Substring(0, 1)}convert temp # <start unit> <end unit> [Supported units: C/F/K]");
            builder.AddField("Liquid Measure Conversion", $"{Context.Message.Content.Substring(0, 1)}convert liq # <start unit> <end unit> [Supported units: l/dl/cl/ml; gal/oz]");

            await Context.Channel.SendMessageAsync(null, false, builder.Build());
        }

        [Command("convert dist"), Summary("Convert from Metric to Imperial and back"), Priority(2)]
        public async Task ConvertMetricImp(string convertAmt, string StartUnit, string EndUnit)
        {
            StartUnit = StartUnit.ToLower();
            EndUnit = EndUnit.ToLower();
            double amtToConvert = 0d;

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
                amtToConvert = double.Parse(convertAmt);
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
                }
            }
            await Context.Channel.SendMessageAsync($"`{convertAmt} {StartUnit} = {r} {EndUnit}`");
        }

        [Command("convert temp"), Summary("Convert from Celcius to Fahrenheit and back"), Priority(2)]
        public async Task ConvertCF(string convertAmt, string StartUnit, string EndUnit)
        {
            StartUnit = StartUnit.ToUpper();
            EndUnit = EndUnit.ToUpper();
            double amtToConvert = 0d;

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
                amtToConvert = double.Parse(convertAmt);
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
                }
            }
            await Context.Channel.SendMessageAsync($"`{convertAmt} °{StartUnit} = {r} °{EndUnit}`");
        }

        [Command("convert liq"), Summary("Convert from Liters to Ounces/Gallons and back"), Priority(2)]
        public async Task ConvertLiq(string convertAmt, string StartUnit, string EndUnit)
        {
            StartUnit = StartUnit.ToLower();
            EndUnit = EndUnit.ToLower();
            double amtToConvert = 0d;

            #region Errorchecking
            if (!SupportesUnitsLiq.Contains(StartUnit))
            {
                var msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention} -> Your start unit is incorrect, supported units: {string.Join(" - ", SupportedUnitsDist)}");
                GlobalVars.AddRandomTracker(msg);
                return;
            }
            if (!SupportesUnitsLiq.Contains(EndUnit))
            {
                var msg = await Context.Channel.SendMessageAsync($"{Context.User.Mention} -> Your end unit is incorrect, supported units: {string.Join(" - ", SupportedUnitsDist)}");
                GlobalVars.AddRandomTracker(msg);
                return;
            }

            try
            {
                amtToConvert = double.Parse(convertAmt);
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
                }
            }
            await Context.Channel.SendMessageAsync($"`{convertAmt} {StartUnit} = {r} {EndUnit}`");
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

        internal static double ToKm(double startAmt, string sourceUnit) { return ConvertHelpers.ToMeters(startAmt * 0.001d, sourceUnit); }
        internal static double ToCm(double startAmt, string sourceUnit) { return ConvertHelpers.ToMeters(startAmt * 100d, sourceUnit); }
        internal static double ToMm(double startAmt, string sourceUnit) { return ConvertHelpers.ToMeters(startAmt * 1000d, sourceUnit); }

        internal static double ToMiles(double startAmt,string sourceUnit) { return ConvertHelpers.ToKm(startAmt, sourceUnit) / 1.609344d; }
        internal static double ToFeet(double startAmt, string sourceUnit) { return ConvertHelpers.ToMeters(startAmt, sourceUnit) / 0.3048d; }
        internal static double ToInches(double startAmt, string sourceUnit) { return ConvertHelpers.ToMeters(startAmt,sourceUnit) / 0.0254d; }
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

        internal static double ToFahrenheit(double startAmt, string sourceUnit) { return (ConvertHelpers.ToCelcius(startAmt, sourceUnit) * (9d / 5d)) + 32d; }
        internal static double ToKelvin(double startAmt, string sourceUnit) { return ConvertHelpers.ToCelcius(startAmt, sourceUnit) + 273.15d; }
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

        internal static double ToDl(double startAmt, string sourceUnit) { return ConvertHelpers.ToLiters(startAmt, sourceUnit) * 10d; }
        internal static double ToCl(double startAmt, string sourceUnit) { return ConvertHelpers.ToLiters(startAmt, sourceUnit) * 100d; }
        internal static double ToMl(double startAmt, string sourceUnit) { return ConvertHelpers.ToLiters(startAmt, sourceUnit) * 1000d; }

        internal static double ToGallons(double startAmt, string sourceUnit) { return ConvertHelpers.ToLiters(startAmt, sourceUnit) * 0.264172d; }
        internal static double ToFlOunces(double startAmt, string sourceUnit) { return ConvertHelpers.ToLiters(startAmt, sourceUnit) * 33.814023d; }
        #endregion
    }
}