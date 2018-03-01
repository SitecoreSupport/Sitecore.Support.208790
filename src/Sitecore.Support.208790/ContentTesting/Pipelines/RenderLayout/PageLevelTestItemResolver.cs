namespace Sitecore.Support.ContentTesting.Pipelines.RenderLayout
{
    using Sitecore;
    using Sitecore.ContentTesting;
    using Sitecore.ContentTesting.Data;
    using Sitecore.ContentTesting.Model.Data.Items;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Pipelines.RenderLayout;
    using System;

    public class PageLevelTestItemResolver : Sitecore.Support.ContentTesting.Pipelines.ContentTestDataSourceResolverBase<RenderLayoutArgs>
    {
        public PageLevelTestItemResolver() : this(null, null)
        {
        }

        public PageLevelTestItemResolver(IContentTestStore testStore, IContentTestingFactory factory) : base(testStore, factory)
        {
        }

        protected override Item GetRequestItem(RenderLayoutArgs args) =>
            Context.Item;

        protected override ID GetVariableType() =>
            PageLevelTestVariableItem.TemplateID;

        protected override void SetRequestItem(RenderLayoutArgs args, Item item)
        {
            Context.Item = item;
        }

        protected override bool ShouldRun(Item item) =>
            base.testStore.HasPageLevelTest(item);
    }
}