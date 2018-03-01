namespace Sitecore.Support.ContentTesting.Pipelines
{
    using Sitecore;
    using Sitecore.Analytics;
    using Sitecore.ContentTesting;
    using Sitecore.ContentTesting.Configuration;
    using Sitecore.ContentTesting.Data;
    using Sitecore.ContentTesting.Inspectors;
    using Sitecore.ContentTesting.Model.Data.Items;
    using Sitecore.ContentTesting.Models;
    using Sitecore.ContentTesting.Pipelines;
    using Sitecore.ContentTesting.Pipelines.DetermineTestExposure;
    using Sitecore.ContentTesting.Pipelines.GetCurrentTestCombination;
    using Sitecore.ContentTesting.Pipelines.SuspendTest;
    using Sitecore.ContentTesting.Web;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using System.Web;

    public abstract class EvaluateTestExposureBase<TPipelineArgs>
    {
        protected readonly IContentTestStore contentTestStore;
        protected readonly IContentTestingFactory factory;

        protected EvaluateTestExposureBase() : this(null, null)
        {
        }

        protected EvaluateTestExposureBase(IContentTestStore contentTestStore, IContentTestingFactory factory)
        {
            this.contentTestStore = contentTestStore ?? ContentTestingFactory.Instance.ContentTestStore;
            this.factory = factory ?? ContentTestingFactory.Instance;
        }

        protected virtual ITestConfiguration FindTestForItem(Item item, ID deviceId)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(deviceId, "deviceId");
            return this.contentTestStore.LoadTestForItem(item, deviceId);
        }

        protected abstract Item GetRequestItem(TPipelineArgs args);
        public void Process(TPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (Settings.IsAutomaticContentTestingEnabled && ((Context.Site == null) || (Context.Site.Name != "shell")))
            {
                Item requestItem = this.GetRequestItem(args);
                if (requestItem != null)
                {
                    ITestConfiguration testConfiguration = this.FindTestForItem(requestItem, Context.Device.ID);
                    if ((testConfiguration != null) && testConfiguration.TestDefinitionItem.IsRunning)
                    {
                        TestCombinationContextBase testCombinationContext = this.factory.GetTestCombinationContext(new HttpContextWrapper(HttpContext.Current));
                        TestSet testset = TestManager.GetTestSet(new TestDefinitionItem[] { testConfiguration.TestDefinitionItem }, requestItem, Context.Device.ID);
                        if (this.factory.EditModeContext.TestCombination != null)
                        {
                            TestCombination combination = new TestCombination(this.factory.EditModeContext.TestCombination, testset);
                            if (!this.ValidateCombinationDatasource(combination, testConfiguration))
                            {
                                this.factory.TestingTracker.ClearMvTest();
                                testCombinationContext.SaveToResponse(testset.Id, null);
                            }
                            else
                            {
                                this.factory.TestingTracker.SetTestCombination(combination, testConfiguration.TestDefinitionItem, false);
                            }
                        }
                        else if (!Context.PageMode.IsPageEditor && ((Tracker.Current != null) && Tracker.IsActive))
                        {
                            if (testCombinationContext.IsSetInRequest())
                            {
                                byte[] fromRequest = testCombinationContext.GetFromRequest(testset.Id);
                                if ((fromRequest != null) && (fromRequest.Length == testset.Variables.Count))
                                {
                                    bool flag = true;
                                    for (int i = 0; i < fromRequest.Length; i++)
                                    {
                                        flag = flag && (fromRequest[i] <= (testset.Variables[i].Values.Count - 1));
                                    }
                                    if (flag)
                                    {
                                        TestCombination combination2 = new TestCombination(fromRequest, testset);
                                        if (!this.ValidateCombinationDatasource(combination2, testConfiguration))
                                        {
                                            this.factory.TestingTracker.ClearMvTest();
                                            testCombinationContext.SaveToResponse(testset.Id, null);
                                            return;
                                        }
                                        this.factory.TestingTracker.SetTestCombination(combination2, testConfiguration.TestDefinitionItem, false);
                                        return;
                                    }
                                }
                            }
                            if (this.ShouldIncludeRequestByTrafficAllocation(requestItem, testConfiguration))
                            {
                                GetCurrentTestCombinationArgs args2 = new GetCurrentTestCombinationArgs(new TestDefinitionItem[] { testConfiguration.TestDefinitionItem })
                                {
                                    Item = requestItem,
                                    DeviceID = Context.Device.ID
                                };
                                SettingsDependantPipeline<GetCurrentTestCombinationPipeline, GetCurrentTestCombinationArgs>.Instance.Run(args2);
                                if (args2.Combination != null)
                                {
                                    if (!this.ValidateCombinationDatasource(args2.Combination, testConfiguration))
                                    {
                                        this.factory.TestingTracker.ClearMvTest();
                                        testCombinationContext.SaveToResponse(testset.Id, null);
                                    }
                                    else
                                    {
                                        this.factory.TestingTracker.SetTestCombination(args2.Combination, testConfiguration.TestDefinitionItem, true);
                                        testCombinationContext.SaveToResponse(args2.Combination.Testset.Id, args2.Combination.Combination);
                                    }
                                }
                            }
                            else
                            {
                                testCombinationContext.SaveToResponse(testset.Id, null);
                            }
                        }
                    }
                }
            }
        }

        protected virtual bool ShouldIncludeRequestByTrafficAllocation(Item item, ITestConfiguration testConfiguration)
        {
            DetermineTestExposureArgs args = new DetermineTestExposureArgs((item != null) ? Context.Item.ID.ToShortID().ToString() : string.Empty)
            {
                Item = (Item)testConfiguration.TestDefinitionItem
            };
            SettingsDependantPipeline<DetermineTestExposurePipeline, DetermineTestExposureArgs>.Instance.Run(args);
            return args.ShouldExpose;
        }

        private bool ValidateCombinationDatasource(TestCombination combination, ITestConfiguration testConfiguration)
        {
            TestValueInspector inspector = new TestValueInspector();
            for (int i = 0; i < combination.Combination.Length; i++)
            {
                TestValue testValue = combination.GetValue(i);

                if (!inspector.IsValidDataSource(testConfiguration.TestDefinitionItem, testValue))
                {
                    SuspendTestArgs args = new SuspendTestArgs(testConfiguration);
                    SettingsDependantPipeline<SuspendTestPipeline, SuspendTestArgs>.Instance.Run(args);
                    return false;
                }
            }
            return true;
        }
    }
}