
using MicroM.DataDictionary.Configuration;
using MicroM.DataDictionary;

namespace LibraryTest
{
        public class MainMenuDefinition : MenuDefinition
        {
            public MenuItemDefinition dashboard = new("Dashboard");

            public MenuItemDefinition stores = new("Stores");
            public MenuItemDefinition channels = new("Channels", nameof(stores));
            public MenuItemDefinition businessgroups = new("Business Groups", nameof(stores));
            public MenuItemDefinition chains = new("Chains", nameof(stores));
            public MenuItemDefinition business = new("Business", nameof(stores));

            public MainMenuDefinition() : base("Main Menu") { }
        }

}
