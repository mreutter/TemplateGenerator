UAD = Unaddressed
ACK = Acknoledged
LAY = Layed out
INC = Incomplete
FIN = Finished

# Overall
## V3 -> Fully Modular Template engine
-> Seperate the Code from Template
-> Build Engine and user can inject own logic (Only part that differs)
-> Conditional Use of certain Template Files / Sections

-> User can generate buildConfig or simply write his own -> Requires verbose error handling and maybe new syntax
-> Make TemplateEngine accept buildConfig which associates all parameters to all files

Reference Template Folder
Reference Build Folder
Reference Config
Build Depending on Config
## Reworked Syntax 
Allow Variants
Allow Easy Parameter Passing (Parameter structure inferred form template)
Allow Nesting multiple layers with local variables



# Backend
## Refactor & Consistency
SQL Parser Strength -> ACK
More Readable stitch replace pipeline (Read from special format) -> LAY in readme.md

General Naming defined by names.json pased to BackendGenerator which generates complete names and passes to individual generators -> INC seperated config & name doesnt intermediatly save (uses cs classes as types not dynamic)
UsePrefix refactored -> INC filetype can be specified more directly
Split CRUD Replacement into each function using new template format (at locations marked with an xxx) -> ACK
Convert Templates into new syntax and clean up code (maybe adjust syntax) -> ACK

## Ease of use (Small changes)
Make only executable be shipped -> UAD
Make a config readme -> LAY Either in seperate Readme or as comments in cs class or in json
Better Comments (Define extense (all / only methods / only properties ...)) -> INC templates still lack some comments and some require upgrades
Comments For better understanding (Title of sections) -> INC need better naming, comments, and structure (depends on refactor)

## Extended Functionality
Automate Authentication for any amount of identifers (also for claims and expiry date) -> UAD right now config includes auth for single identifîer
Automated ToDto & ToModel conversion specifed by table.json -> ACK implemented expose dto attribute on property class
Generate Multilayered Queries (when specified) -> UAD
Get UserId by token -> UAD

## Fixes
Implement Table alias when using DbSetSuffix -> ACK
Column aliases -> ACK in Properties
Utilize naming -> ACK use indexed table access



# Frontend
## What parts can be automated and templetized:
General Structure (Folder & Solution)
Shared
Components
Codebehind

CustomHttpHandler
CustomAuthStateProvider
Program.cs

Services
Login & Registration

Generic Overview page
- Card Grid
- Table

## Parameters
Page Names
Service Names
DTO Input (Parsed)
Token -> Claims: Roles etc.