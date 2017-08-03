/**
 *  PHOTOVOLTAIC GEOGRAPHICAL INFORMATION SYSTEM
 *  
 *  HTML scraper for the http://re.jrc.ec.europa.eu/pvgis/apps4/pvest.php website.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using pvgisjsonapi;

public class PVGIS {
    /// <summary>
    /// Get PV values from the PVGIS database.
    /// </summary>
    /// <param name="pv">Input parameters from the client.</param>
    /// <returns>Full collection of position, options, and parsed values.</returns>
    public static async Task<Response> GetAsync(API.PostValues pv) {
        var culture = new CultureInfo("en-US");

        // Set default values if none are given.
        if (pv == null) {
            throw new MissingMemberException("pv is missing.");
        }

        // pvtech
        if (string.IsNullOrWhiteSpace(pv.pvtech)) {
            pv.pvtech = "crystSi";
        }

        // peakpower
        if (!pv.peakpower.HasValue) {
            pv.peakpower = 1;
        }

        // losses
        if (!pv.losses.HasValue) {
            pv.losses = 14;
        }

        // mounting
        if (string.IsNullOrWhiteSpace(pv.mounting)) {
            pv.mounting = "free";
        }

        // slope
        if (!pv.slope.HasValue) {
            pv.slope = 35;
        }

        // azimuth
        if (!pv.azimuth.HasValue) {
            pv.azimuth = 0;
        }

        // Compile postback dictionary for webcall.
        var dict = new Dictionary<string, string> {
            {"MAX_FILE_SIZE", "10000"},
            {"pv_database", "PVGIS-classic"},
            {"pvtechchoice", pv.pvtech},
            {"peakpower", pv.peakpower.Value.ToString(culture)},
            {"efficiency", pv.losses.Value.ToString(culture)},
            {"mountingplace", pv.mounting},
            {"angle", pv.slope.Value.ToString(culture)},
            {"aspectangle", pv.azimuth.Value.ToString(culture)},
            {"horizonfile", ""},
            {"outputchoicebuttons", "window"},
            {"sbutton", "Calculate"},
            {"outputformatchoice", "window"},
            {"optimalchoice", ""},
            {"latitude", pv.lat.ToString(culture)},
            {"longitude", pv.lng.ToString(culture)},
            {"regionname", "europe"},
            {"language", "en_en"}
        };

        // Get HTML from postback.
        var html = await GetHtmlFromWebsiteAsync(dict);

        if (string.IsNullOrWhiteSpace(html) ||
            html.IndexOf("no valid daily radiation data", StringComparison.CurrentCultureIgnoreCase) > -1) {
            return null;
        }

        // Parse the HTML into a readable format.
        var values = ScrapeHTMLAndParseValues(html);

        if (values == null) {
            return null;
        }

        // Output to user.
        return new Response(
            pv,
            values);
    }

    /// <summary>
    /// Contact the PVGIS database and get HTML from the postback calculator.
    /// </summary>
    /// <param name="dict">Postback data to transmit.</param>
    /// <returns>HTML from the website.</returns>
    private static async Task<string> GetHtmlFromWebsiteAsync(Dictionary<string, string> dict) {
        const string url = "http://re.jrc.ec.europa.eu/pvgis/apps4/PVcalc.php";

        var client = new HttpClient();
        var content = new StringContent(
            dict.Aggregate("", (c, p) => c + "&" + p.Key + "=" + WebUtility.UrlEncode(p.Value)).Substring(1),
            Encoding.UTF8,
            "application/x-www-form-urlencoded");

        var response = await client.PostAsync(
            url,
            content);

        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Get values from a monthly row.
    /// </summary>
    /// <param name="html">The source HTML to parse.</param>
    /// <param name="searchKey">Key to search for.</param>
    /// <returns>Parsed values.</returns>
    private static ParsedValues.Monthly GetMonthlyRowFromHTML(ref string html, string searchKey) {
        var key = "<td> " + searchKey + " </td>";
        var sp = html.IndexOf(key, StringComparison.CurrentCultureIgnoreCase);

        if (sp == -1) {
            key = "<td><b> " + searchKey + " </b></td>";
            sp = html.IndexOf(key, StringComparison.CurrentCultureIgnoreCase);
        }

        if (sp == -1) {
            return null;
        }

        var temp = html.Substring(sp + key.Length);

        sp = temp.IndexOf("</td></tr>", StringComparison.CurrentCultureIgnoreCase);

        if (sp == -1) {
            return null;
        }

        temp = temp.Substring(0, sp)
            .Replace("</td>", "")
            .Replace("<b>", "")
            .Replace("</b>", "")
            .Replace("<td align=\"right\">", ",");

        var values = temp.Split(',');

        if (values.Length != 5) {
            return null;
        }

        return new ParsedValues.Monthly {
            Ed = ParseDecimal(values[1]),
            Em = ParseDecimal(values[2]),
            Hd = ParseDecimal(values[3]),
            Hm = ParseDecimal(values[4])
        };
    }

    /// <summary>
    /// Get values from a yearly row.
    /// </summary>
    /// <param name="html">The source HTML to parse.</param>
    /// <param name="searchKey">Key to search for.</param>
    /// <returns>Parsed values.</returns>
    private static ParsedValues.Yearly GetYearlyRowFromHTML(ref string html, string searchKey) {
        var key = "<td><b>" + searchKey + "</b></td>";
        var sp = html.IndexOf(key, StringComparison.CurrentCultureIgnoreCase);

        if (sp == -1) {
            return null;
        }

        var temp = html.Substring(sp + key.Length);

        sp = temp.IndexOf("</td> </tr>", StringComparison.CurrentCultureIgnoreCase);

        if (sp == -1) {
            return null;
        }

        temp = temp.Substring(0, sp)
            .Replace("<td align=\"right\" colspan=2 >", ",")
            .Replace("<b>", "")
            .Replace("</b>", "")
            .Replace("</td>", "");

        var values = temp.Split(',');

        if (values.Length != 3) {
            return null;
        }

        return new ParsedValues.Yearly {
            E = ParseDecimal(values[1]),
            H = ParseDecimal(values[2])
        };
    }

    /// <summary>
    /// Parse valid decimal from given string.
    /// </summary>
    /// <param name="value">String to parse.</param>
    /// <returns>Decimal</returns>
    public static decimal ParseDecimal(string value) {
        if (value == null) {
            return 0;
        }

        decimal d;

        return decimal.TryParse(value.Trim(), NumberStyles.Any, new CultureInfo("en-US"), out d)
            ? d
            : 0;
    }

    /// <summary>
    /// Scrape the given HTML and parse the values.
    /// </summary>
    /// <param name="html">HTML from the website.</param>
    /// <returns>Parsed values.</returns>
    private static ParsedValues ScrapeHTMLAndParseValues(string html) {
        return new ParsedValues {
            monthlyAverage = new Dictionary<string, ParsedValues.Monthly> {
                {"Jan", GetMonthlyRowFromHTML(ref html, "Jan")},
                {"Feb", GetMonthlyRowFromHTML(ref html, "Feb")},
                {"Mar", GetMonthlyRowFromHTML(ref html, "Mar")},
                {"Apr", GetMonthlyRowFromHTML(ref html, "Apr")},
                {"May", GetMonthlyRowFromHTML(ref html, "May")},
                {"Jun", GetMonthlyRowFromHTML(ref html, "Jun")},
                {"Jul", GetMonthlyRowFromHTML(ref html, "Jul")},
                {"Aug", GetMonthlyRowFromHTML(ref html, "Aug")},
                {"Sep", GetMonthlyRowFromHTML(ref html, "Sep")},
                {"Oct", GetMonthlyRowFromHTML(ref html, "Oct")},
                {"Nov", GetMonthlyRowFromHTML(ref html, "Nov")},
                {"Dec", GetMonthlyRowFromHTML(ref html, "Dec")},
            },
            yearlyAverage = GetMonthlyRowFromHTML(ref html, "Yearly average"),
            yearlyTotal = GetYearlyRowFromHTML(ref html, "Total for year")
        };
    }

    #region Enums

    /// <summary>
    /// PV Technology.
    /// </summary>
    public enum PVTechnology {
        /// <summary>
        /// Crystalline silicon cells.
        /// </summary>
        CrystallineSilicon,

        /// <summary>
        /// Thin film modules made from CIS or CIGS.
        /// </summary>
        CIS,

        /// <summary>
        /// Thin film modules made from Cadmium Telluride (CdTe).
        /// </summary>
        CdTe
    }

    /// <summary>
    /// Mounting position.
    /// </summary>
    public enum MountingPosition {
        /// <summary>
        /// Free standing mounting on the outside of the structure.
        /// </summary>
        FreeStanding,

        /// <summary>
        /// Integrated into the building structure.
        /// </summary>
        BuildingIntegrated
    }

    #endregion

    #region Classes

    /// <summary>
    /// Set options for the webcall.
    /// </summary>
    public class Options {
        /// <summary>
        /// The performance of PV modules depends on the temperature and on the
        /// solar irradiance, but the exact dependence varies between different
        /// types of PV modules.
        /// </summary>
        public PVTechnology PVTechnology = PVTechnology.CrystallineSilicon;

        /// <summary>
        /// This is the power that the manufacturer declares that the PV array
        /// can produce under standard test conditions, which are a constant
        /// 1000W of solar irradiation per square meter in the plane of the
        /// array, at an array temperature of 25°C.
        /// </summary>
        public double InstalledPeakPVPower = 1;

        /// <summary>
        /// The estimated system losses are all the losses in the system, which
        /// cause the power actually delivered to the electricity grid to be
        /// lower than the power produced by the PV modules.
        /// </summary>
        public double EstimatedSystemLosses = 14;

        /// <summary>
        /// For fixed (non-tracking) systems, the way the modules are mounted
        /// will have an influence on the temperature of the module, which in
        /// turn affects the efficiency.
        /// </summary>
        public MountingPosition MountingPosition = MountingPosition.FreeStanding;

        /// <summary>
        /// This is the angle of the PV modules from the horizontal plane,
        /// for a fixed (non-tracking) mounting.
        /// </summary>
        public double Slope = 35;

        /// <summary>
        /// The azimuth, or orientation, is the angle of the PV modules
        /// relative to the direction due South. -90° is East, 0° is South and
        /// 90° is West.
        /// </summary>
        public double Azimuth = 0;

        /// <summary>
        /// Get the postback value for the selected PV Technology.
        /// </summary>
        /// <returns>String</returns>
        public string GetPVTechnology() {
            if (this.PVTechnology == PVTechnology.CrystallineSilicon) {
                return "crystSi";
            }

            if (this.PVTechnology == PVTechnology.CIS) {
                return "CIS";
            }

            if (this.PVTechnology == PVTechnology.CdTe) {
                return "CdTe";
            }

            return "Unknown";
        }

        /// <summary>
        /// Get the postback value for the selected mounting position.
        /// </summary>
        /// <returns>String</returns>
        public string GetMountingPosition() {
            return this.MountingPosition == MountingPosition.FreeStanding
                ? "free"
                : "building";
        }
    }

    /// <summary>
    /// Response collection.
    /// </summary>
    public class Response {
        /// <summary>
        /// Create a new response collection.
        /// </summary>
        /// <param name="input">The input parameters from the client.</param>
        /// <param name="output">Scraped and parsed values from the website.</param>
        public Response(API.PostValues input, ParsedValues output) {
            this.input = input;
            this.output = output;
        }

        /// <summary>
        /// The input parameters from the client.
        /// </summary>
        public API.PostValues input { get; set; }

        /// <summary>
        /// Scraped and parsed values from the website.
        /// </summary>
        public ParsedValues output { get; }
    }

    /// <summary>
    /// Scraped and parsed values from the website.
    /// </summary>
    public class ParsedValues {
        /// <summary>
        /// Average pr month.
        /// </summary>
        public Dictionary<string, Monthly> monthlyAverage { get;set; }

        /// <summary>
        /// Average for entire year.
        /// </summary>
        public Monthly yearlyAverage { get; set; }

        /// <summary>
        /// Total for entire year.
        /// </summary>
        public Yearly yearlyTotal { get; set; }

        public class Monthly {
            public decimal Ed { get; set; }
            public decimal Em { get; set; }
            public decimal Hd { get; set; }
            public decimal Hm { get; set; }
        }

        public class Yearly {
            public decimal E { get; set; }
            public decimal H { get; set; }
        }
    }

    #endregion
}