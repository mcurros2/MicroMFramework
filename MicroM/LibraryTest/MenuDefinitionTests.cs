using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LibraryTest
{
    [TestClass]
    public class MainMenuTests
    {
        private MainMenuDefinition menu;

        [TestInitialize]
        public void Setup()
        {
            menu = new MainMenuDefinition();
        }

        [TestMethod]
        public void Test_Dashboard_Exists_In_MainMenu()
        {
            Assert.IsTrue(menu.MenuItems.Contains("dashboard"), "Menu item 'dashboard' does not exist in menu.");
        }

        [TestMethod]
        public void Test_Stores_Has_Correct_Children()
        {
            var stores = menu.MenuItems["stores"];
            Assert.AreEqual(4, stores.Children.Count, "Item 'stores' has an incorrect number of child items.");

            var expectedChildren = new[] { "channels", "businessgroups", "chains", "business" };
            var actualChildren = stores.Children.Select(c => c.MenuItemID).ToArray();

            CollectionAssert.AreEquivalent(expectedChildren, actualChildren, "Child items for store are not the ones expected.");
        }

        [TestMethod]
        public void Test_Tiendas_ItemPath_Is_Correct()
        {
            var stores = menu.MenuItems["stores"];
            Assert.AreEqual("/stores", stores.ItemPath, "The route for itme 'stores' is not correct.");
        }

        [TestMethod]
        public void Test_Child_Channels_Has_Correct_ItemPath()
        {
            var channels = menu.MenuItems["channels"];
            Assert.AreEqual("/stores/channels", channels.ItemPath, "The route for item 'channels' is not correct.");
        }

        [TestMethod]
        public void Test_MenuItems_Are_Correctly_Added_To_Dictionary()
        {
            var expectedMenuItems = new[]
            {
                "dashboard", "stores", "channels", "businessgroups", "chains", "business"
            };

            var actualMenuItems = menu.MenuItems.Keys.ToArray();

            CollectionAssert.AreEquivalent(expectedMenuItems, actualMenuItems, "Menu items are not as expected.");
        }

        [TestMethod]
        public void Test_Business_Has_Correct_Parent()
        {
            var business = menu.MenuItems["business"];
            Assert.IsNotNull(business.Parent, "Item 'business' has not parent.");
            Assert.AreEqual("stores", business.Parent.MenuItemID, "Parent of item 'business' is not 'stores'.");
        }

        [TestMethod]
        public void Test_Item_Has_No_Children_When_Expected()
        {
            var dashboard = menu.MenuItems["dashboard"];
            Assert.AreEqual(0, dashboard.Children.Count, "Item 'dashboard' has chidlren when it should not.");
        }
    }
}
