Add tests for local functions
Add tests for multiple SubTypes ()
Add tests where class has only commnets in methods but methods are not calling each other (what is the parent here? document node?)
Add tests with async
Add tests with partial classes where functions can be defined
Change how Nodes are referenced
 - add nodes to dictionary
 - 
Add node attributes template (Identifier, Level, etc)
 - Identifier (optional) identifies node if we want to relate to it later
 - Level - graph can geneareted with different level of details e.g. 1 - least detailed, 2 - detailed, 3 - all nodes
 - Path - defines which for which path node tree should be generated e.g. if we have two subtypes we can specify which subtype node tree to print
 - 

I have to go through whole code from start to end to create flow?
 - no
I can read only declared methods and if method invocation is detected I just store info about that invocation with reference to method declaration

