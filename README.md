
# __kJson__

Simple and fast JSON parsing and serializing

#Nuget package

You can get nuget package **kJson** from http://www.nuget.org/packages/kJson/

or type 'Install-Package kJson' in  [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console)

#Usage example

```csharp
#using kJson;

...

var collection = JSON.Parse("{ \"a\": 1, \"b\": [1, 2, 3] }");

...

using(var stream = new FileStream("somefile", FileMode.Open))
{
  var another = JSON.Parse(stream);
}

...

var stringresult = JSON.Stringify(someobject);

...

using(var stream = new FileStream("anotherfile", FileMode.Open))
{
  JSON.Write(somecollection,  stream);
}

...
```
