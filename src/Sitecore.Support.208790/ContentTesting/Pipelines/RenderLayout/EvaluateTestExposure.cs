namespace Sitecore.Support.ContentTesting.Pipelines.RenderLayout
{
    using Sitecore;
    using Sitecore.ContentTesting;
    using Sitecore.ContentTesting.Data;
    using Sitecore.ContentTesting.Model.Data.Items;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Pipelines.RenderLayout;
    using System;
    using System.Collections.Generic;

    public class EvaluateTestExposure : Sitecore.Support.ContentTesting.Pipelines.EvaluateTestExposureBase<RenderLayoutArgs>
    {
        private readonly IContentTestStore contentTestStore;

        public EvaluateTestExposure() : base(null, null)
        {
        }

        public EvaluateTestExposure(IContentTestStore contentTestStore, IContentTestingFactory factory) : base(contentTestStore, factory)
        {
        }

        [Obsolete("Use FindTestForItem() method instead.")]
        protected virtual IEnumerable<TestDefinitionItem> FindTestsForItem(Item item, ID deviceId)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(deviceId, "deviceId");
            ITestConfiguration configuration = this.contentTestStore.LoadTestForItem(item, deviceId);
            List<TestDefinitionItem> list = new List<TestDefinitionItem>();
            if ((configuration != null) && (configuration.TestDefinitionItem != null))
            {
                list.Add(configuration.TestDefinitionItem);
            }
            return list;
        }

        protected override Item GetRequestItem(RenderLayoutArgs args) =>
            Context.Item;
    }
}