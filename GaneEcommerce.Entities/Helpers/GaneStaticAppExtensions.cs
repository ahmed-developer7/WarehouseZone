﻿using System;
using System.Configuration;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web.Configuration;
using System.Web.WebPages;
using System.Data.Spatial;

namespace Ganedata.Core.Entities.Helpers
{
    public static class GaneStaticAppExtensions
    {
        /// <summary>
        ///     Specifies the default entry prefix value ("config").
        /// </summary>
        private const string Prefix = "config";

        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static string DefaultDateFormat { get; set; } = "dd/MM/yyyy";
        public static string DefaultTimeFormat { get; set; } = "HH:mm:ss";

        public static string AsDateString(this DateTime input)
        {
            return input.ToString(DefaultDateFormat);
        }
        public static string AsDateString(this DateTime? input)
        {
            return input?.ToString(DefaultDateFormat) ?? "";
        }
        public static string AsTimeString(this DateTime input)
        {
            return input.ToString(DefaultTimeFormat);
        }
        public static string AsTimeString(this DateTime? input)
        {
            return input?.ToString(DefaultTimeFormat) ?? "00:00:00";
        }

        public static string GetMd5(string input)
        {
            MD5 md5Hash = MD5.Create();
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        public static string DevexTheme = ConfigurationManager.AppSettings["DevexTheme"] ?? "MetropolisBlue";
        public static int DefaultSystemUserId = int.Parse(ConfigurationManager.AppSettings["DefaultSystemUserId"] ?? "0");
        public static bool MiniProfilerEnabled { get; set; } = ConfigurationManager.AppSettings["MiniProfilerEnabled"].ToLower() == "true";
        public static int WarehouseSyncIgnorePropertiesSiteId => string.IsNullOrEmpty(WebConfigurationManager.AppSettings["WarehouseSyncIgnorePropertiesSiteId"]) ? 0
            : int.Parse(WebConfigurationManager.AppSettings["WarehouseSyncIgnorePropertiesSiteId"]);

        public static int? ParseToNullableInt(string value)
        {
            if (String.IsNullOrEmpty(value)) return null;

            int number;
            bool result = int.TryParse(value, out number);
            if (result)
            {
                return number;
            }
            else
            {
                return null;
            }
        }


        public static DateTime ConvertDatetoUkFormat(DateTime passedDate)
        {
            DateTimeFormatInfo usDtfi = new CultureInfo("en-US", false).DateTimeFormat;
            DateTimeFormatInfo ukDtfi = new CultureInfo("en-GB", false).DateTimeFormat;

            string res = Convert.ToDateTime(passedDate, usDtfi).ToString(ukDtfi);

            DateTime convertedDate = DateTime.Parse(res);

            return convertedDate;
        }

        public static DbGeography CreatePoint(double lat, double lon, int srid = 4326)
        {
            string wkt = String.Format("POINT({0} {1})", lon, lat);

            return DbGeography.PointFromText(wkt, srid);
        }

        public static string GenerateDateRandomNo()
        {
            Random random = new Random();
            random.Next(0, 9999).ToString("D4");

            var dateRandom = DateTime.UtcNow.Date.ToString("ddMMyy");
            dateRandom += DateTime.UtcNow.ToString("mmHH");
            dateRandom += random.Next(0, 9999).ToString("D4");

            return dateRandom;
        }
    }
}