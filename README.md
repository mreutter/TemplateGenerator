# Log
INFO: Informs you about progrss.
WARNING: Warns about preemptive actions or ignored information.
ADMONITION: Requires your checking / correction.
ERROR: Fatal Crash. Program cannot continue operating.

# Parser
Gets the definitive part
Inserts all sections if they are true
Inserts parameters during section (iteration parameter in iterative sections)
Insert special characters

Instead of replacing blindly (actively looking for sections) go trough definitive string and replace conditionally. continue at the beginning of the inserted string to catch any further section locations

# Template Format
## Definitive Part (only one)
$!={

}#

## Location of section insertion
$?sectionName#
Pass a bool if it should be created or replaced with nothing ("")

## Definition (remove \n before and after)
$?sectionName={
	Hello my name is $parameterName#!
	Can also use other sections (Tree like structure)
}#

## Location of a section which will be repeated (determine glue elements) (determine iterations with different parameters)
$?*sectionName#
Pass array (iterations) of array (parameters & section bools) of string tuples (parameter, value)
If an empty array is passed the section will not 

## Parameter location (The replaced value is detmined purely from code)
$parameterName#

(After every other step to avoid unnecessary computation) 
!¬ -> remove '\r\n' (Use at line ends or beginnings)
¬ -> '\r\n' 

§ -> '\t'