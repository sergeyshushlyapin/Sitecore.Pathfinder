Version next
============
* Fix: Cleaned up schemas (2016-04-27)
* Fix: Cleaned up VSCode build tasks (2016-04-27)
* Fix: Warning if no layout file compiler was found (2016-04-27)
* Add: Refactored help system to use Markdown files (2016-04-27)
* Add: Heavy NuGet refactoring (2016-05-23) - Dmitry Kostenko
* Add: check-website task (2016-07-15) - Anders Laub

Version 0.7.0
============
* Add: Added add-project task for creating a Pathfinder project in an existing directory (2015-12-20)
* Add: Support for running as NPM module (2016-01-05)
* Fix: Template field IDs now has the specified ID (2016-01-06) - Dave Morrison
* Removed: validate-website task (Sitecore Rocks SitecoreCop) (2016-01-12)
* Removed: list-addins, install-addin and update-addins tasks (2016-01-12)
* Removed: Experimental website UI (2016-01-12)
* Removed: rename task (2016-01-12)
* Add: clean-website task for removing Pathfinder files from the website (2016-01-12)
* Add: Type/Assembly reference in .config files checker (2016-01-12)
* Add: Number field checker (2016-01-22)
* Add: DateTime and Date field compilers (2016-01-22)
* Add: Reference field compiler (2016-01-22)
* Add: Run Sitecore validations on saving an item (2016-01-22)
* Fix: Roslyn is no longer copied to the website, since it breaks Sitecore (2016-01-26) - Dmitry Kostenko
* Add: Xslt Transforms of xml files (2016-04-08) - Dmitry Kostenko
* Add: Renaming and deletions now works by stamping the Project Unique ID into the __PathfinderUniqueIds fields on every item on the server (2016-04-16)
* Add: Merged 'Layout.HtmlFile' and 'Layout.Page' into single feature 'Layout.File' (2016-04-16)
* Add: show-config task (2016-04-19)
* Add: show-website task (2016-04-20)
* Add: restore-packages task for downloading dependency Nuget packages (2016-04-20) - Dmitry Kostenko

Version 0.6.0
=============
* Add: Project changed event for invalidating caches (2015-12-01)
* Add: Project roles - enables/disabled checkers and conventions (2015-12-01)
* Add: Rules Engine (2015-12-01)
* Add: Convention checker (2015-12-01)
* Add: XPath expressions (2015-12-03)
* Add: PathMapper API (2015-12-07)
* Add: Serializing Data Provider (2015-12-09)
* Add: Stricter checking using build-project:schema setting (2015-12-12)
* Add: Renamed /content directory to /items to avoid conflict with ASP.NET 5 (2015-12-12)
* Add: Merged Markdown compiler pull request (2015-12-18) - Alistair Deneys

Version 0.5.0
=============
* Add: Support for Include files (2015-10-21)
* Add: Pack and install multiple NuGet packages (2015-10-29) - Todd Mitchell
* Add: Support for user config files (scconfig.json.user) (2015-10-29) - Alistair Denneys
* Add: Colored Console output (2015-11-01) - Max Reimer-Nielsen
* Add: Allows sitecore.tools to be added to the PATH environment variable (2015-11-01) - Martin Svarrer Christensen
* Add: Troubleshooting task; republish, rebuild search indexes, rebuild link database (2015-11-01) - SPEAK team
* Add: Revamped the entire process for creating a new project (2015-11-04) - Martin Svarrer Christensen
* Add: reset-website tak for deleting items and files from the website (2015-11-06) - SPEAK team
* Add: install-project task for installing a project directly from the project directory (2015-11-06)
* Add: watch-project task for watching a project for changes and installing the project (2015-11-06)
* Add: build-project:force-update setting which indicates if media and files are always overwritten (2015-11-06)
* Add: build-project:file-search-pattern sets the file search pattern for project directory visitor (2015-11-06)
* Add: Replaced externals with projects. Exports are now defined in a standard NuGet package (2015-11-09) - Dmitry Kostenko
* Add: Repository directory for installable files. list-repository and install-repository tasks (2015-11-09)
* Add: Support for Unicorn files (2015-11-17) - Emil Okkels Klein
* Changed: Renamed list-repository to list-addins and install-repository to install-addin (2015-11-17)
* Add: Added update-addins task to update installed add-ins (2015-11-17)
* Add: Added /disable-extensions=true switch to prevent extensions from loading (2015-11-17)
* Add: Added support for T4 templates (2015-11-18) - Emil Okkels Klein
* Removed: run-unittests and generate-unittests tasks. These have been replaced with generate-code and T4 templates (2015-11-18)
* Add: scc can now run scripts files (PowerShell, .cmd and -bat) (2015-11-19)
* Add: Script files for installing FakeDb and NUnit-Runners (2015-11-19)
* Add: import-website task for importing a website into a Pathfinder project (2015-11-20)
