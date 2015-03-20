using Newtonsoft.Json.Linq;
using Sitecore.Analytics;
using Sitecore.Analytics.Data;
using Sitecore.Analytics.Data.DataAccess.DataSets;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using System;
using System.Web;
using System.Web.Caching;

namespace Sitecore.SharedSource.Conditions.Weather
{
    public class Common
    {
        public static void AddCache<T>(string cacheKey, T value, int minutes = 60) where T : class
        {
            HttpContext.Current.Cache.Insert(cacheKey, value, null, DateTime.Now.AddMinutes(minutes),
                    Cache.NoSlidingExpiration, CacheItemPriority.AboveNormal, null);
        }

        public static string GetCache(string cacheKey)
        {
            return GetCache<string>(cacheKey);
        }

        public static T GetCache<T>(string cacheKey) where T : class
        {
            return HttpContext.Current.Cache[cacheKey] != null
                ? HttpContext.Current.Cache[cacheKey] as T
                : null;
        }

        public static string GetTemperature(double? latitude = null, double? longitude = null)
        {
            return GetWeatherDetail(latitude, longitude, "temp");
        }

        public static string GetWeather(double? latitude = null, double? longitude = null)
        {
            return GetWeatherDetail(latitude, longitude, "weather");
        }

        private static string GetWeatherDetail(double? latitude, double? longitude, string detailField)
        {
            if (latitude == null) latitude = Tracker.CurrentVisit.Latitude;
            if (longitude == null) longitude = Tracker.CurrentVisit.Longitude;

            if (latitude == 0 || longitude == 0) return null;

            var cacheKey = detailField + "_" + latitude + "_" + longitude;
            
            var results = GetCache(cacheKey);

            if (!String.IsNullOrEmpty(results))
            {
                RegisterPageEvent("Weather Condition", "Weather Condition", "Lat:" + latitude + ",Lon:" + longitude, "Cached:" + results);
                return results;
            }

            var jobject = GetWeatherData(latitude, longitude);
            if (jobject == null) return null;

            results = detailField != "weather"
                ? jobject["main"][detailField].ToString()
                : jobject["weather"][0]["main"].ToString();

            AddCache(cacheKey, results);
            RegisterPageEvent("Weather Condition", "Weather Condition", "Lat:" + latitude + ",Lon:" + longitude, "Fresh:" + results);
            return results;
        }

        public static JObject GetWeatherData(double? latitude = null, double? longitude = null)
        {
            //Sitecore 7.5 route?
            //if(latitude == null) latitude = Tracker..Current.Session.Interaction.GeoData.Latitude;
            //if(longitude == null) longitude = Tracker.Current.Session.Interaction.GeoData.Longitude;

            if (latitude == null) latitude = Tracker.CurrentVisit.Latitude;
            if (longitude == null) longitude = Tracker.CurrentVisit.Longitude;

            if (latitude == 0 || longitude == 0) return null;

            var cacheKey = "weatherdata_" + latitude + "_" + longitude;

            var weatherdata = GetCache<JObject>(cacheKey);

            if (weatherdata != null) return weatherdata;

            using (var webClient = new System.Net.WebClient())
            {
                var weatherServiceUrl =
                    Settings.GetSetting("Sitecore.SharedSource.Conditions.Weather.WeatherServiceUrl",
                        "http://api.openweathermap.org/data/2.5/weather?lat={0}&lon={1}&units=imperial");
                var fullUrl = String.Format(weatherServiceUrl, latitude, longitude);

                try
                {
                    var json = webClient.DownloadString(fullUrl);
                    weatherdata = JObject.Parse(json);
                }
                catch (Exception ex)
                {
                    Log.Error("Sitecore.SharedSource.Conditions.Weather - Error retrieving weather:" + ex, ex, typeof(Common));
                }

                if (weatherdata == null) return null;

                AddCache(cacheKey, weatherdata);
            }

            return weatherdata;
        }
        
        public static bool RegisterPageEvent(string pageEventName, string pageEventText, string pageEventData,
            string pageEventDataKey)
        {
            try
            {
                if (!Settings.Analytics.Enabled)
                {
                    Log.Info("Analytics disabled.", new Common());
                    return false;
                }

                if (!Tracker.IsActive)
                {
                    Log.Info("StartTracking()", new Common());
                    Tracker.StartTracking();
                }

                if (Tracker.CurrentVisit == null)
                {
                    Log.Info("No Visit.", new Common());
                    return false;
                }

                Log.Info("RegisterPageEvent4.", new Common());

                if (Tracker.CurrentPage == null)
                {
                    Log.Info("No CurrentPage.", new Common());
                    return false;
                }

                //ResultsLabel.Text += "CurrentPage:" + Tracker.CurrentPage + "");

                if (Context.Site.EnableAnalytics)
                {
                    Log.Info("ready to get currentPage.", new Common());

                    //VisitorDataSet.PagesRow currentPage = Tracker.Visitor.CurrentVisit.CurrentPage;
                    var currentPage = Tracker.CurrentPage;

                    if (currentPage == null)
                    {
                        Log.Info("currentPage==null.", new Common());
                    }

                    return RegisterPageEvent(pageEventName, pageEventText, pageEventData, pageEventDataKey, currentPage);
                }
            }
            catch (Exception ex)
            {
                Log.Error("RegisterPageEvent", ex, new object());
            }
            return false;
        }

        public static bool RegisterPageEvent(string pageEventName, string pageEventText, string pageEventData,
            string pageEventDataKey, VisitorDataSet.PagesRow page)
        {
            try
            {
                Log.Info("RegisterPageEvent5", new Common());
                //&& Sitecore.Context.Site.EnableAnalytics
                if (Context.Site.EnableAnalytics)
                {
                    Log.Info("RegisterPageEvent5.", new Common());

                    if (!Tracker.IsActive)
                    {
                        Log.Info("StartTracking()", new Common());
                        Tracker.StartTracking();
                    }

                    Log.Info("RegisterPageEvent5.", new Common());
                    if (Context.Item == null)
                    {
                        Log.Info("Set Context.Item", new Common());
                        //Sitecore.Context.Item =  Sitecore.Context.Database.GetItem("/sitecore");
                    }
                    var pageData = new PageEventData(pageEventName)
                    {
                        Text = pageEventText,
                        Data = pageEventData,
                        DataKey = pageEventDataKey
                    };

                    if (Tracker.CurrentVisit != null)
                    {
                        page.Register(pageData);
                        Tracker.Submit();
                        Log.Info("RegisterPageEvent:", pageEventText + "-" + pageEventData + ":" + pageEventDataKey);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("RegisterPageEvent", ex, new object());
            }
            return false;
        }

        public static Data.Items.Item GetItem(string itempath)
        {
            var db = Context.Database ?? Factory.GetDatabase("web") ?? Factory.GetDatabase("master");

            Assert.ArgumentNotNull(db, "db");

            return db.GetItem(itempath);
        }
    }
}
