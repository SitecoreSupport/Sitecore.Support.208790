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

    public class ContentTestDataSourceResolver : Sitecore.Support.ContentTesting.Pipelines.ContentTestDataSourceResolverBase<RenderLayoutArgs>
    {
        public ContentTestDataSourceResolver() : base(null, null)
        {
        }

        public ContentTestDataSourceResolver(IContentTestStore store, IContentTestingFactory factory) : base(store, factory)
        {
        }

        protected override Item GetRequestItem(RenderLayoutArgs args) =>
            Context.Item;

        protected override ID GetVariableType() =>
            ContentTestVariableItem.TemplateID;

        protected override void SetRequestItem(RenderLayoutArgs args, Item item)
        {
            Context.Item = item;
        }

        protected override bool ShouldRun(Item item) =>
            base.testStore.HasContentTest(item);
    }
}