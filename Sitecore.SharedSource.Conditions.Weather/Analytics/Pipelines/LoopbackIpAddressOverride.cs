  using System;
  using System.Net;
  using System.Web;
  using System.Web.Caching;
  using Sitecore.Analytics;
  using Sitecore.Analytics.Pipelines.StartTracking;
  using Sitecore.ApplicationCenter.Applications;
  using Sitecore.Shell.Applications.ContentEditor;
  using Sitecore.Web.Authentication;

namespace Sitecore.SharedSource.Conditions.Weather.Analytics.Pipelines
{
    public class LoopbackIpAddressOverride
    {
        public string LookupProviderUrl { get; set; }

        public string DefaultIp { get; set; }

        public void Process(StartTrackingArgs args)
        {
            var currentVisit = Tracker.CurrentVisit;

            if (currentVisit == null || currentVisit.Ip == null) return;

            var visitIp = new IPAddress(currentVisit.Ip).ToString();

            var ip = visitIp;

            if (currentVisit.RDNS == "0.0.0.0" || currentVisit.RDNS == "127.0.0.1")
            {
                if (String.IsNullOrEmpty(DefaultIp))
                {
                    if (!String.IsNullOrEmpty(LookupProviderUrl))
                    {
                        DefaultIp = Common.GetCache("IP-Lookup-" + currentVisit.VisitorId);
                        if (String.IsNullOrEmpty(DefaultIp))
                        {
                            var html = Web.WebUtil.ExecuteWebPage(LookupProviderUrl);
                            DefaultIp = html.ToLowerInvariant().Replace("\n", "").Replace("\r", "");
                            Common.AddCache("IP-Lookup-" + currentVisit.VisitorId, DefaultIp);
                        }
                    }
                }
                ip = DefaultIp;
            }

            if (ip == visitIp && currentVisit.HasGeoIpData) return;
            
            var address = IPAddress.Parse(ip);
            
            currentVisit.GeoIp = Tracker.Visitor.DataContext.GetGeoIp(address.GetAddressBytes());

            Tracker.Visitor.CurrentVisit.UpdateGeoIpData();
        }
    }
}