## Tech stack
- **Backend**: C# (.NET 8), SQL Server
- **Frontend**: React, TypeScript, Mantine

## Features
### **Backend**:
	- Define entities and their relationships
	- Generate SQL scripts for database creation and updates
	- Generate SQL Stored procedures for CRUD operations
	- Web API with conventions for CRUD operations for created entities
	- Email Service
	- Encrypon Service
	- File upload service
	- File serve service
	- Built-in Authentication Service
	- Code generation for frontend components using micromlib
	- Batch CRUD operations
	- Multitenancy support

### **Frontend**:
	- UI components for displaying data in tables
	- UI components for forms and validation
	- UI components for Google maps
	- Multimodal manager
	- API client
	- UI components for authentication
	- UI components for file upload

## Getting Started
### **Create a solution in visual studio**:
	- Create a new C# class library project targeting .Net 8 named `Entities`.
	- Create a new C# ASP.NET Core Web Api project named `WebAPI` in your solution.
	- Create a new Blank Javascript Project named `frontend` in your solution.
		
	- Install the NuGet package for the backend framework in `Entities` and `WebAPI`:
		```bash
		dotnet add package MicroM.Core --prerelease
		```
	- Install the NPM package for the frontend UI library in `frontend`:
		```bash
		npm install @mcurros2/microm@alpha
		```
	- Install and configure Parcel in `frontend`:
		```bash
		npm install --save-dev parcel@latest
		```

## Working with the Backend
### **Create your entities**:
- Create a new folder named `Entities` in the `Entities` project.
- Create a new folder named `Entities\Persons` in the `Entities` project.
- Create a new class in the `Entities\Persons` folder for each entity you want to create. For example, create a class named `Persons`:

	```csharp
	using MicroM.Core;
	using MicroM.Data;
	using MicroM.DataDictionary;
	using MicroM.Web.Services;
	
	namespace Entities;	

	// Definitiona and metadata for the entity
	public class PersonsDef : EntityDefinition
	{
		public PersonsDef() : base("pers", nameof(Persons)) { }

		// Define the primary key with the extension method PK()
		public readonly Column<string> c_person_id = Column<string>.PK(autonum: true);

		// Define the columns with the apropiate extension methods depending on the type of data you want to store.	
		public readonly Column<string> vc_person_name = Column<string>.Text();
		public readonly Column<string> vc_person_lastname = Column<string>.Text();
		public readonly Column<string> vc_person_email = Column<string>.Text(size: 2048);
		public readonly Column<string?> vc_person_phone = Column<string?>.Text(nullable: true);
		public readonly Column<string?> vc_person_mobilephone = Column<string?>.Text(nullable: true);
		public readonly Column<DateOnly?> dt_birthdate = new(nullable: true);

		// These two columna are for referencing the file store which contains a photo for the person
		public readonly Column<string?> c_photofileprocess_id = Column<string?>.FK(nullable: true);
		public readonly Column<string?> vc_photoguid = Column<string?>.Text(size: 255, nullable: true, fake: true);

		// Define the standard view for the entity. This is the view that will be used to display the data in the UI.
		public readonly ViewDefinition pers_brwStandard = new(nameof(c_person_id));

		// Define the relationship with the file store entity.
		public readonly EntityForeignKey<FileStoreProcess, Persons> FKFileStore = new(key_mappings: [new(parentColName: nameof(FileStoreDef.c_fileprocess_id), childColName: nameof(c_photofileprocess_id))]);

	}

	// Define the entity
	public class Persons : Entity<PersonsDef>
	{
		public Persons() : base() { }
		public Persons(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }
	}
	```

### Setup database and security initialization
- Create a new folder named `Init` in the `Entities` project.
- Add a new class named `HomeMenu` in the `Init` folder:
	```csharp
	using Entities;
	using MicroM.Configuration;
	using MicroM.DataDictionary;
	using MicroM.DataDictionary.Configuration;
	using MicroM.Extensions;

	namespace Init; 
 
	// This class defines the menu for the application. It is used to define the items that will be displayed in the menu for the user.
	// Security will be set over menu items the user is allowed to see. 
	// Note that for each menu iten you declare the allowed API routes to be used in the UI.
	public class HomeMenu : MenuDefinition
	{
		public HomeMenu() : base("Home menu") { }

		// Main menu
		public MenuItemDefinition home = new("Home", allowed_routes: [
			..typeof(Persons).GetRoutePaths(AllowedRouteFlags.Views | AllowedRouteFlags.Edit,
				views: [nameof(PersonsDef.pers_brwStandard)],
			]);

		public MenuItemDefinition logout = new("Log out");
	}
	```


- Add a new class named `AllUsersGroup` in the `Init` folder:
	```csharp
	using MicroM.DataDictionary.Configuration;

	namespace Init;

	// This security group represents all users that are logged in to the application.
	public class AllUsersGroup : UsersGroupDefinition
	{
		public AllUsersGroup() : base("Access for all loggedin users") 
		{
			var menu = new HomeMenu();
			// This method will add the menu items to the group and the resulting routes.
			AddMenuAllItems(menu);
		}

	}
	```

- OPTIONAL: Declare public endpoints implementing IPublicEndpoints. All API endpoints are required to be logged in. If you want to allow public access to some endpoints, you can declare them in the `PublicEndpoints` class.
	```csharp
	using Entities;
	using MicroM.Configuration;
	using MicroM.DataDictionary;
	using MicroM.Extensions;

	namespace Init;
	public class PublicEndpoints : IPublicEndpoints
	{
		// sync in siteConfig.ts
		public List<string>? AddAllowedPublicEndpointRoutes()
		{
			// This will allow toi call the API endpoint for the view without being authenticated.
			return [
			..typeof(Persons).GetRoutePaths(AllowedRouteFlags.Views, views: [nameof(PersonsDef.pers_brwStandard)]),
			];
		}
	}
	```
- Implement Database Initialization and Update code by implementing IDatabaseSchema.
	```csharp

	```
