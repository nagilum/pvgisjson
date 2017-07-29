using System;
using System.IO;
using Float;
using Newtonsoft.Json;

namespace pvgisjsonapi {
    public class API {
        /// <summary>
        /// Get PV values for given location.
        /// </summary>
        public static PostResponse GetPVValue(FloatRouteContext ctx) {
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

            var key = JsonConvert.SerializeObject(pv);
            var data = Cacher.Get(key, () => {
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

        #region Helper Classes

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