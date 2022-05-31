# Roslyn Ordered Source Code Generation Sample

This sample demonstrates how to order Roslyn Source Code Generators using an imperative approach.

Declarative ordering of generators is not supported at the time of writing, as by design, all generators execute on the context of the user's original source code alone, and therefore cannot see each other's output.

However, it is possible to order code generation in an explicit imperative way via the *driver* API, which is available for unit testing.

The sample scenario is the following:

* Project *Tests* declares an annotated user model;
* *Generator A* must run on the user model;
* *Generator B* must run on the user model;
* *Generator B* must also run on the output of *Generator A*.

The imperative approach is the following:

* Project *Tests* references both *Generator A* and *Generator B*;
* *Generator A* references *Generator B*;
* *Generator A*, upon its own execution:
    * generates its own source code;
    * creates a new compilation based on the user's but only containing the generated source code;
    * explicitly executes *Generator B* on the new compilation;
    * adds *Generator B*'s source code output to its own;

Additional care must be taken if:

* *Generator B* is prone to generate overlapping code, such as assembly-level attributes;
* *Generator B* is packaged as a development dependency, which:
*   requires *A* to use reflection to access to *B*.
*   requires library code to be moved to its own assembly.

For simplicity, the above edge cases are not part of this sample.
An example of how to handle these edge cases can be seen in [this project](https://github.com/JorgeCandeias/ColumnStore/tree/master/Outcompute.ColumnStore.CodeGenerator).

# Details

Project *Tests* declares an annotated user model:

```csharp
[GenerateA]
[GenerateB]
internal class UserModel
{
}
```

Project *Tests* also declares a unit test, based on the expectations that:

* *Generator A* will generate a class named `<UserModel>A` based on the user class marked with `[GenerateA]`;
  * This new class will be marked with `[GenerateB]`;
* *Generator B* will generate a class named `<UserModel>B` based on the user class marked with `[GenerateB]`;
* Given *B* must consume the output of *A*, *B* will also generate a class named `<UserModel>AB`;

```csharp
[Fact]
public void Test1()
{
    // asset model processed by first generator
    Assert.Equal(1, UserModelA.A);

    // assert model processed by second generator
    Assert.Equal(2, UserModelB.B);

    // assert model processed by first generator and then second generator
    Assert.Equal(2, UserModelAB.B);
}
```

Upon execution, *Generator A* generates its own source code as normal and adds its to the user context.
It also keeps track of that generated source code for further use.

```csharp
var sources = new List<SourceText>();

if (context.SyntaxContextReceiver is SyntaxReceiverA receiver)
{
    foreach (var candidate in receiver.Candidates)
    {
        var source = Generate(candidate);
        sources.Add(source);

        context.AddSource($"{candidate.Name}.a.g.cs", source);
    }
}
```

*Generator A* then creates a new compilation based on the user's but without any original syntax trees.
This prevents re-generating source code already generated in the step above.

```csharp
var compilation = context.Compilation.RemoveAllSyntaxTrees();
```

*Generator A* then adds its own output the new compilation.

```csharp
foreach (var source in sources)
{
    var tree = CSharpSyntaxTree.ParseText(source);
    compilation = compilation.AddSyntaxTrees(tree);
}
```

*Generator A* then uses the *driver* api to explicityly execute *Generator B* upon its own output.

```csharp
var result = CSharpGeneratorDriver
    .Create(new SourceGeneratorB())
    .RunGenerators(compilation)
    .GetRunResult();
```

Finally, *Generator A* adds *Generator B*'s source code to the user context.

```csharp
foreach (var item in result.Results)
{
    foreach (var source in item.GeneratedSources)
    {
        context.AddSource(source.HintName, source.SourceText);
    }
}
```

The above step may or may not still cause duplication or overlap, depending on what exact code is being generated.
This duplication must be resolved on a case-by-case basis by either:
* Ensuring *Generator B* is not prone to duplication, if you own the code.
* Manipulating the output of *Generator B* before adding it to the context, if you do not own the code.
