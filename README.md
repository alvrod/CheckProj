CheckProj
=========

CheckProj is a command line app to make your build fail if project files contain invalid references as defined by the user.

## What? ##

Your project files need to be squeaky clean if your builds are to be repeatable and your assemblies built correctly.

Have you ever...

- had to mulligan a deploy because there was an assembly referenced from bin\Debug?
- had your tests failed because your project was built against the Oracle DLL in the GAC instead of the one in your carefully built \lib\Oracle directory?
- found someone committed code directly accessing domain logic from a button?
- ... or found many others despicable acts of architectural heinousness?

Yes, code reviews can help. Documenting your architecture properly can help. None of that will defend you against Visual Studio's vicious contempt for reference paths, GAC hell or people simply putting an absolute path in your project file.

You can avoid these problems using CheckProj in your automated builds (you **do** have automated builds, right?)

CheckProj will:

- allow you to define validation rules
- check your project references against those rules
- print an error message for references rejected by your rules
- return an error code if any reference was rejected

Then you have but to add a call to CheckProj to your automated build to make it fail when your project has been naughty.

## Wait, really? ##

This is one of the purposes of automated builds: to fail when things are wrong. **Make it fail**.

## How? ##

You need to write a rules file, such as:

---

   # which project files to check  
   # can have as many as you need  
   check [*.*proj]  

   # some basic rules  
   reject any absolute # Do not reference files with absolute paths  
   reject any path [bin\\\*] # Do not reference assemblies from bin\\  
   allow project [\*.DataAccessLayer] under [..\\server] include [Oracle.DataAccess.dll]  
   reject any include [Oracle.DataAccess.dll] # Only DAL can reference Oracle  
   allow any path [..\\lib\\\*]  
   reject any under [..\\client] include [\*.Domain.dll] # Clients shouldn't reference business logic  

   # include another file with more rules
   include [..\\lib\\some\\morerules.prc]  

---

Then you run CheckProj like:  
   CheckProj -src ..\src -rules ..\doc\rules.prc

All relative paths will be relative to the working directory.
The -src parameter is optional. If absent, CheckProj will search for your project pattern files (the "check" in the example above) up to 6 directories above the working directory.