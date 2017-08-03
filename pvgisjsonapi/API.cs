using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Float;
using Newtonsoft.Json;

namespace pvgisjsonapi {
    public class API {
        /// <summary>
        /// Get PV values for given location.
        /// </summary>
        public static PostResponse GetPVValueByGet(FloatRouteContext ctx) {
            var culture = new CultureInfo("en-US");

            double lat;
            double lng;

            if (!ctx.Request.Query.ContainsKey("lat") ||
                !ctx.Request.Query.ContainsKey("lng")) {
                throw new FloatRouteException(
                    400,
                    new PostResponse {
                        message = "'lat' and 'lng' are required."
                    });
            }

            if (!double.TryParse(ctx.Request.Query["lat"].ToString(), NumberStyles.Any, culture, out lat) ||
                !double.TryParse(ctx.Request.Query["lng"].ToString(), NumberStyles.Any, culture, out lng)) {
                throw new FloatRouteException(
                    400,
                    new PostResponse {
                        message = "'lat' and/or 'lng' are invalid."
                    });
            }

            double? peakpower = null;
            double? losses = null;
            double? slope = null;
            double? azimuth = null;

            if (ctx.Request.Query.ContainsKey("peakpower")) {
                if (double.TryParse(ctx.Request.Query["peakpower"].ToString(), NumberStyles.Any, culture, out double temp)) {
                    peakpower = temp;
                }
            }

            if (ctx.Request.Query.ContainsKey("losses")) {
                if (double.TryParse(ctx.Request.Query["losses"].ToString(), NumberStyles.Any, culture, out double temp)) {
                    losses = temp;
                }
            }

            if (ctx.Request.Query.ContainsKey("slope")) {
                if (double.TryParse(ctx.Request.Query["slope"].ToString(), NumberStyles.Any, culture, out double temp)) {
                    slope = temp;
                }
            }

            if (ctx.Request.Query.ContainsKey("azimuth")) {
                if (double.TryParse(ctx.Request.Query["azimuth"].ToString(), NumberStyles.Any, culture, out double temp)) {
                    azimuth = temp;
                }
            }

            // Compile the values and get from cache or website.
            var data = compileRequest(
                lat,
                lng,
                ctx.Request.Query.ContainsKey("pvtech") ? ctx.Request.Query["pvtech"].ToString() : null,
                peakpower,
                losses,
                ctx.Request.Query.ContainsKey("mounting") ? ctx.Request.Query["mounting"].ToString() : null,
                slope,
                azimuth);

            // Post the API request to GA.
            postTrackingToGA(ctx);

            if (data != null) {
                return new PostResponse {
                    message = "Ok",
                    data = data
                };
            }

            throw new FloatRouteException(
                500,
                new PostResponse {
                    message = "Unable to query PVGIS for info. Try again later."
                });
        }

        /// <summary>
        /// Get PV values for given location.
        /// </summary>
        public static PostResponse GetPVValueByPost(FloatRouteContext ctx) {
            PostValues pv;

            try {
                pv = ctx.BodyTo<PostValues>();
            }
            catch (Exception ex) {
                throw new FloatRouteException(
                    400,
                    new PostResponse {
                        message = ex.Message
                    });
            }

            // Compile the values and get from cache or website.
            var data = compileRequest(
                pv.lat,
                pv.lng,
                pv.pvtech,
                pv.peakpower,
                pv.losses,
                pv.mounting,
                pv.slope,
                pv.azimuth);

            // Post the API request to GA.
            postTrackingToGA(ctx);

            if (data != null) {
                return new PostResponse {
                    message = "Ok",
                    data = data
                };
            }

            throw new FloatRouteException(
                500,
                new PostResponse {
                    message = "Unable to query PVGIS for info. Try again later."
                });
        }

        /// <summary>
        /// Serve the front page.
        /// </summary>
        public static FloatRouteResponse ServeFrontpage(FloatRouteContext ctx) {
            var html = Cacher.Get("Frontpage", () => {
                var file = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "index.html");

                return File.Exists(file)
                    ? File.ReadAllText(file)
                    : null;
            });

            if (string.IsNullOrWhiteSpace(html)) {
                throw new FloatRouteException(404);
            }

            return new FloatRouteHtmlResponse(html);
        }

        #region Helper functions

        /// <summary>
        /// Compile the values and get from cache or website.
        /// </summary>
        private static PVGIS.Response compileRequest(double lat, double lng, string pvtech, double? peakpower, double? losses, string mounting, double? slope, double? azimuth) {
            var pv = new PostValues {
                lat = lat,
                lng = lng,
                pvtech = pvtech,
                peakpower = peakpower,
                losses = losses,
                mounting = mounting,
                slope = slope,
                azimuth = azimuth
            };

            var key = JsonConvert.SerializeObject(pv);

            return Cacher.Get(key, () => {
                var options = new PVGIS.Options();

                // PVTechnology
                if (string.Equals(pv.pvtech, "crystSi", StringComparison.CurrentCultureIgnoreCase)) {
                    options.PVTechnology = PVGIS.PVTechnology.CrystallineSilicon;
                }
                else if (string.Equals(pv.pvtech, "CIS", StringComparison.CurrentCultureIgnoreCase)) {
                    options.PVTechnology = PVGIS.PVTechnology.CrystallineSilicon;
                }
                else if (string.Equals(pv.pvtech, "CdTe", StringComparison.CurrentCultureIgnoreCase)) {
                    options.PVTechnology = PVGIS.PVTechnology.CrystallineSilicon;
                }

                // InstalledPeakPVPower
                if (pv.peakpower.HasValue) {
                    options.InstalledPeakPVPower = pv.peakpower.Value;
                }

                // EstimatedSystemLosses
                if (pv.losses.HasValue) {
                    options.EstimatedSystemLosses = pv.losses.Value;
                }

                // MountingPosition
                if (string.Equals(pv.mounting, "free", StringComparison.CurrentCultureIgnoreCase)) {
                    options.MountingPosition = PVGIS.MountingPosition.FreeStanding;
                }
                else if (string.Equals(pv.mounting, "building", StringComparison.CurrentCultureIgnoreCase)) {
                    options.MountingPosition = PVGIS.MountingPosition.BuildingIntegrated;
                }

                // Slope
                if (pv.slope.HasValue) {
                    options.Slope = pv.slope.Value;
                }

                // Azimuth
                if (pv.azimuth.HasValue) {
                    options.Azimuth = pv.azimuth.Value;
                }

                // Query PVGIS.
                try {
                    var task = PVGIS.GetAsync(
                        pv.lat,
                        pv.lng,
                        options);

                    task.Wait(10 * 1000);

                    return task.Result;
                }
                catch {
                    return null;
                }
            });
        }

        /// <summary>
        /// Post the API request to GA.
        /// </summary>
        private static void postTrackingToGA(FloatRouteContext ctx) {
            Task.Run(() => {
                GAMP.PostSingle(
                    ctx.Request,
                    new GAMP.Options {
                        TrackingID = "UA-103855040-1"
                    });
            });
        }

        #endregion

        #region Helper classes

        public class PostResponse {
            public string message { get; set; }
            public PVGIS.Response data { get; set; }
        }

        public class PostValues {
            public double lat { get; set; }
            public double lng { get; set; }
            public string pvtech { get; set; }
            public double? peakpower { get; set; }
            public double? losses { get; set; }
            public string mounting { get; set; }
            public double? slope { get; set; }
            public double? azimuth { get; set; }
        }

        #endregion
    }
}