using Sitecore.Analytics;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;
using System;
using System.Linq;
using System.Net;

namespace Sitecore.SharedSource.Conditions.Weather
{
    /// <summary>
    /// can use firefox plugin - RefControl to test
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RegionGroupCondition<T> : WhenCondition<T> where T : RuleContext
    {
        private const string RegionGroupCacheKey = "CurrentVisit.RegionGroup.";
        private const string RegionGroupIdCacheKey = "CurrentVisit.RegionGroupId.";

        public Guid RegionGroupItemId { get; set; }

        protected override bool Execute(T ruleContext)
        {
            Assert.ArgumentNotNull(ruleContext, "ruleContext");
            Assert.ArgumentNotNull(RegionGroupItemId, "RegionGroupItemId");

            var key = RegionGroupIdCacheKey + new IPAddress(Tracker.CurrentVisit.Ip);

            var visitTag = Common.GetCache(key);

            ID regionGroup;

            if(!ID.TryParse(visitTag, out regionGroup))
            {
                // was coming back falsely negative!
                //if (!Tracker.CurrentVisit.HasGeoIpData) return false;

                if (String.IsNullOrEmpty(Tracker.CurrentVisit.Region)) return false;

                regionGroup = GetRegionGroup(Tracker.CurrentVisit.Region);

                Common.AddCache(key, regionGroup.ToString());
            }
            return String.Equals(RegionGroupItemId.ToString(), regionGroup.Guid.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        private ID GetRegionGroup(string regionCode)
        {
            var regionGroup = Common.GetCache(RegionGroupCacheKey + regionCode);

            if (!String.IsNullOrEmpty(regionGroup)) return ID.Parse(regionGroup);

            var regionGroupItemPath = Settings.GetSetting("Sitecore.SharedSource.Conditions.GeoIp.RegionGroups.Item",
                        "/sitecore/content/Global/Settings/Region Groups");

            var regionGroupItems = Common.GetItem(regionGroupItemPath);

            if (regionGroupItems == null) return null;

            foreach (Item regionGroupItem in regionGroupItems.Children)
            {
                //children
                //if (regionGroupItem.Children.Any(rg => String.Equals(rg.Name, regionCode, StringComparison.CurrentCultureIgnoreCase))) return regionGroupItem.Name;
                    
                Data.Fields.MultilistField regionsMultiListField = regionGroupItem.Fields["Regions"];

                if (regionsMultiListField == null) continue;

                if (!regionsMultiListField.GetItems()
                    .ToList()
                    .Any(i => String.Equals(i.Name, regionCode, StringComparison.InvariantCultureIgnoreCase))) continue;

                Common.AddCache(RegionGroupCacheKey + regionCode, regionGroupItem.ID);
                return regionGroupItem.ID;
            }

            return null;
        }
    }
}
