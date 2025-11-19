UAD = Unaddressed
ACK = Acknoledged
LAY = Layed out
INC = Incomplete
FIN = Finished

# Refactor & Consistency
SQL Parser Strength -> ACK
More Readable stitch replace pipeline (Read from special format) -> LAY in readme.md

General Naming defined by names.json pased to BackendGenerator which generates complete names and passes to individual generators -> INC seperated config & name doesnt intermediatly save (uses cs classes as types not dynamic)
UsePrefix refactored -> INC filetype can be specified more directly
Split CRUD Replacement into each function using new template format (at locations marked with an xxx) -> ACK
Convert Templates into new syntax and clean up code (maybe adjust syntax) -> ACK

# Ease of use (Small changes)
Make only executable be shipped -> UAD
Make a config readme -> LAY Either in seperate Readme or as comments in cs class or in json
Better Comments (Define extense (all / only methods / only properties ...)) -> INC templates still lack some comments and some require upgrades
Comments For better understanding (Title of sections) -> INC need better naming, comments, and structure (depends on refactor)

# Extended Functionality
Automate Authentication for any amount of identifers (also for claims and expiry date) -> UAD right now config includes auth for single identifîer
Automated ToDto & ToModel conversion specifed by table.json -> ACK implemented expose dto attribute on property class
Generate Multilayered Queries (when specified) -> UAD
Get UserId by token -> UAD

# Fixes
Implement Table alias when using DbSetSuffix -> ACK
Column aliases -> ACK in Properties
Utilize naming -> ACK use indexed table access
