namespace Sitecore.Support.ContentTesting.Pipelines
{
    using Sitecore;
    using Sitecore.ContentTesting;
    using Sitecore.ContentTesting.Configuration;
    using Sitecore.ContentTesting.Data;
    using Sitecore.ContentTesting.Inspectors;
    using Sitecore.ContentTesting.Model.Data.Items;
    using Sitecore.ContentTesting.Models;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using System;
    using System.Linq;

    public abstract class ContentTestDataSourceResolverBase<TArgs>
    {
        #region Added code
        private const string IS_ERROR_KEY = "SitecoreSupport.IsError";
        #endregion

        protected readonly IContentTestingFactory factory;
        protected readonly IContentTestStore testStore;

        protected ContentTestDataSourceResolverBase() : this(null, null)
        {
        }

        protected ContentTestDataSourceResolverBase(IContentTestStore store, IContentTestingFactory factory)
        {
            this.factory = factory ?? ContentTestingFactory.Instance;
            this.testStore = store ?? this.factory.ContentTestStore;
        }

        protected virtual Item GetContentItem(TestCombination testCombination, ITestConfiguration test)
        {
            Assert.ArgumentNotNull(testCombination, "testCombination");
            Assert.ArgumentNotNull(test, "test");
            DataSourceResolver resolver = new DataSourceResolver();
            return resolver.GetContentTestDataSources(test, testCombination, this.GetVariableType()).FirstOrDefault<Item>();
        }

        [Obsolete("Use IDataSourceResolver from the ContentTesting factory instead.")]
        protected virtual Item GetContentItem(TestVariableItem variable, Guid valueId, ITestConfiguration test)
        {
            if (variable != null)
            {
                TestVariablesInspector inspector = new TestVariablesInspector();
                DataUri[] source = inspector.GetContentTestDataSources(variable, new ID(valueId)).ToArray<DataUri>();
                if (source.Any<DataUri>())
                {
                    return variable.InnerItem.Database.GetItem(source[0]);
                }
            }
            return null;
        }

        protected abstract Item GetRequestItem(TArgs args);
        protected abstract ID GetVariableType();
        public virtual void Process(TArgs args)
        {
            #region Added code
            if ((bool)Context.Items[IS_ERROR_KEY])
            {
                return;
            }
            #endregion

            Assert.ArgumentNotNull(args, "args");
            if (Settings.IsAutomaticContentTestingEnabled)
            {
                Item requestItem = this.GetRequestItem(args);
                if ((requestItem != null) && this.ShouldRun(requestItem))
                {
                    if (Context.PageMode.IsPageEditor)
                    {
                        requestItem = this.ProcessPageEditorRequest(requestItem);
                    }
                    else
                    {
                        requestItem = this.ProcessStandardRequest(requestItem);
                    }
                    if (requestItem != null)
                    {
                        this.SetRequestItem(args, requestItem);
                    }
                }
            }
        }

        protected virtual Item ProcessPageEditorRequest(Item hostItem)
        {
            Item contentItem = null;
            ITestConfiguration test = this.testStore.LoadTestForItem(hostItem);
            if (test == null)
            {
                return null;
            }
            byte[] testCombination = this.factory.EditModeContext.TestCombination;
            if ((testCombination != null) && (testCombination.Length > 0))
            {
                TestCombination combination = new TestCombination(testCombination, test.TestSet);
                contentItem = this.GetContentItem(combination, test);
            }
            return contentItem;
        }

        protected virtual Item ProcessStandardRequest(Item item)
        {
            Item contentItem;
            Profiler.StartOperation("Resolve content version for item.");
            ID testId = this.factory.TestingTracker.GetTestId();
            if (testId.IsNull)
            {
                return null;
            }
            ITestConfiguration test = this.testStore.LoadTest(testId, item, Context.Device.ID);
            try
            {
                TestCombination testCombination = this.factory.TestingTracker.GetTestCombination(test.TestDefinitionItem.ID.Guid);
                if (testCombination == null)
                {
                    return null;
                }
                contentItem = this.GetContentItem(testCombination, test);
            }
            finally
            {
                Profiler.EndOperation();
            }
            return contentItem;
        }

        protected abstract void SetRequestItem(TArgs args, Item item);
        protected abstract bool ShouldRun(Item item);
    }
}