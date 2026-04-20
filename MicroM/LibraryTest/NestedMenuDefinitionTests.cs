using MicroM.DataDictionary.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LibraryTest
{
    public class FinanceMainMenuDefinition : MenuDefinition
    {
        public MenuItemDefinition home = new("Home");
        public MenuItemDefinition finance = new FinanceSectionMenu();
        public MenuItemDefinition reports = new("Reports");

        public FinanceMainMenuDefinition() : base("Main Menu") { }
    }

    public class FinanceSectionMenu() : MenuItemDefinition("Finance")
    {
        public MenuItemDefinition externalCollections = new ExternalCollectionsMenu();
    }

    public class ExternalCollectionsMenu() : MenuItemDefinition("External Collections")
    {
        public MenuItemDefinition batchPresentation = new("Batch Presentation");
        public MenuItemDefinition addBatch = new("Add Batch", nameof(batchPresentation));
        public MenuItemDefinition removeBatch = new("Remove Batch", nameof(batchPresentation));
        public MenuItemDefinition viewBatches = new("View Batches", nameof(batchPresentation));

        public MenuItemDefinition downloads = new("Downloads");
        public MenuItemDefinition reconcilePayments = new("Reconcile Payments", nameof(downloads));
        public MenuItemDefinition downloadExternalFile = new("Download External File", nameof(downloads));
    }

    [TestClass]
    public class NestedMenuDefinitionTests
    {
        private FinanceMainMenuDefinition menu = null!;

        [TestInitialize]
        public void Setup()
        {
            menu = new FinanceMainMenuDefinition();
        }

        [TestMethod]
        public void Test_Nested_MenuItems_Keep_Root_MenuID()
        {
            foreach (var item in menu.MenuItems.Values)
            {
                Assert.AreEqual("FinanceMainMenuDefinition", item.MenuID);
            }
        }

        [TestMethod]
        public void Test_Nested_Container_Assigns_Parent_Automatically()
        {
            var finance = menu.MenuItems["finance"]!;
            var externalCollections = menu.MenuItems["externalCollections"]!;

            Assert.AreEqual("finance", externalCollections.ParentMenuItemID);
            Assert.IsNotNull(externalCollections.Parent);
            Assert.AreEqual("finance", externalCollections.Parent!.MenuItemID);
            Assert.IsTrue(finance.Children.Any(c => c.MenuItemID == "externalCollections"));
        }

        [TestMethod]
        public void Test_Explicit_Parent_Assignment_Works_For_Grandchildren()
        {
            var batchPresentation = menu.MenuItems["batchPresentation"]!;
            var addBatch = menu.MenuItems["addBatch"]!;

            Assert.AreEqual("batchPresentation", addBatch.ParentMenuItemID);
            Assert.IsNotNull(addBatch.Parent);
            Assert.AreEqual("batchPresentation", addBatch.Parent!.MenuItemID);
            Assert.IsTrue(batchPresentation.Children.Any(c => c.MenuItemID == "addBatch"));
        }

        [TestMethod]
        public void Test_ItemPath_Is_Correct_For_Nested_Items()
        {
            Assert.AreEqual("/finance", menu.MenuItems["finance"]!.ItemPath);
            Assert.AreEqual("/finance/externalCollections", menu.MenuItems["externalCollections"]!.ItemPath);
            Assert.AreEqual("/finance/externalCollections/batchPresentation", menu.MenuItems["batchPresentation"]!.ItemPath);
            Assert.AreEqual("/finance/externalCollections/batchPresentation/addBatch", menu.MenuItems["addBatch"]!.ItemPath);
            Assert.AreEqual("/finance/externalCollections/downloads", menu.MenuItems["downloads"]!.ItemPath);
            Assert.AreEqual("/finance/externalCollections/downloads/reconcilePayments", menu.MenuItems["reconcilePayments"]!.ItemPath);
        }

        [TestMethod]
        public void Test_MenuItems_Preserve_Declaration_Order_DepthFirst()
        {
            var expectedOrder = new[]
            {
                "home",
                "finance",
                "externalCollections",
                "batchPresentation",
                "addBatch",
                "removeBatch",
                "viewBatches",
                "downloads",
                "reconcilePayments",
                "downloadExternalFile",
                "reports"
            };

            var actualOrder = menu.MenuItems.Keys.ToArray();

            CollectionAssert.AreEqual(expectedOrder, actualOrder);
        }
    }
}