# BeepDM
A DataManagment Engine and Library for Managing you Connection to Different DataSources 
* This is a Engine That will people from regular clerk trying to make sense of his data from excel sheet or text file to a Data Analyst working to make decision.
* Its based on Modular Design and Most of the Engine is expandable.
* You can Add any DataSource using the Interface "IDataSource" and put the DLL in the "connectiondrivers" folder in your application.
* You can add any Type of Data Processing or Reporting using "IDM_Addin" Interface and put the DLL " in the "Addin" Folder in your Application.
* It support most of the major Databases's out of the box like oracle,sqlserver, mysql, .... etc.

Project mainly structured in 5 main Projects: 
1. Data Management Engine Models Project. 
2. Data Management Engine Project. 
3. Addin Project for Screens and User Controls. 
4. Tools Project has all the assembly loading and Reflection Code. 
5. WinForm Project has the Visualization Controls Like Tree structure for the Application.

![Image of Yaktocat](https://github.com/fahadTheTechIdea/gfx/raw/master/ProjectandSolutionStructure1.png) 

> Data Management Engine, Data management Engine Model, Tools Projects are written using Shared Project for .Net 4.7.2 and .NetCore

> Lets Go thourgh Some of the Features in the Engine:
- Create New Data Source:
  - Create New Class Implementing "IDataSource".
  - Deploy new Class Dll to ConnectionDrivers Directory in the Application.
  - Run "Connection Drivers" Module from Addin Tree in Application. Add new Record and Select Your Class From "Class Handler" Column.
  - Or Update File "ConnectionConfig.json" in the Config Folder in Application Folder.
  
- Create New Addin:
  - Create usercontrol of Form Implementing "IDM_Addin" , then Add Generated dll to Addin Folder and it will show in Addin Tree in Application.
    


