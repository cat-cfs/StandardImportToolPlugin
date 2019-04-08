# StandardImportToolPlugin
StandardImportToolPlugin is a wrapper for the Operational-Scale CBM-CFS3 standard import tool DLL. 



## Use cases
  * Import an existing SIT formatted excel spreadsheet or MS Access database to a CBM3 project database from the command line
  * Import SIT formatted text files to an CBM3 project database from the command line
  * Create a CBM3 project database via configuration
  * Build and use as a library for a new .NET application

## Buid instructions 

Tested most recently with Visual Studio 2017, but older versions should work as well.

  * Install the Operational-Scale CBM-CF3 Toolbox
  
  * Add references to the following toolbox dll files:
      * CBMSIT.dll
      * StringUtilities.dll
      * Interfaces.dll
      * WoodStockImportTool.dll
      * Global.dll
 
The DLL files can be found in your installation directory. For example
here:  
`C:\Program Files (x86)\Operational-Scale CBM-CFS3\CBMSIT.dll`

## Command line usage

If you want a binary copy instead of building please look [here](https://github.com/cat-cfs/StandardImportToolPlugin/releases)
A quick example for importing the included [tutorial6.json](https://github.com/cat-cfs/StandardImportToolPlugin/blob/master/Examples/tutorial6.json) configuration file.  The output path, as specified in the config file itself will be created as a valid CBM-CFS3 project

`StandardImportToolPlugin.exe -c "tutorial6.json"`

[Configuration documentation](https://github.com/cat-cfs/StandardImportToolPlugin/wiki/Configuration)

## Command line Examples

The directory [Examples](https://github.com/cat-cfs/StandardImportToolPlugin/tree/master/Examples) contains a few command line examples.
  * dataConfig.json - shows an SIT import using json objects to define CBM inventory, yield, events, and transition rules
  * excel_import_example.json/excel_import_example.xls - shows a mapped import of CBM data in SIT form in an excel spreadsheet.
  * tutorial1.json - replicates the CBM-CFS3 tutorial1 afforestation project by importing CBM3 data as json objects
  * tutorial2.json - maps and imports a text formatted SIT input dataset.  This replicates the CBM-CFS3 tutorial2 project
  * tutorial6.json - imports the SIT tutorial6 project 

## Other uses

The file [ExampleMethods.cs](https://github.com/cat-cfs/StandardImportToolPlugin/blob/master/ExampleMethods.cs) has 3 usage examples for importing SIT file directly in C# code

  * [Method 1](https://github.com/cat-cfs/StandardImportToolPlugin/blob/master/ExampleMethods.cs#L146) - Import an SIT project from an SIT formatted database or excel spreadsheet (for example tutorial6.xlsx) 
  * [Method 2](https://github.com/cat-cfs/StandardImportToolPlugin/blob/master/ExampleMethods.cs#L88) - Import an SIT project by building up a dataset manually with code.
  * [Method 3](https://github.com/cat-cfs/StandardImportToolPlugin/blob/master/ExampleMethods.cs#L19) - Import via json object
