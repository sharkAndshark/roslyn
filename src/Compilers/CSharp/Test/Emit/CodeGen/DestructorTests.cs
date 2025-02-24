﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class DestructorTests : EmitMetadataTestBase
    {
        [ConditionalFact(typeof(DesktopOnly))]
        public void ClassDestructor()
        {
            var text = @"
using System;

public class Base
{
    ~Base()
    {
        Console.WriteLine(""~Base"");
    }
}

public class Program
{
    public static void Main()
    {
        Base b = new Base();
        b = null;
        GC.Collect(GC.MaxGeneration);
        GC.WaitForPendingFinalizers();
    }
}
";
            var validator = GetDestructorValidator("Base");
            var compVerifier = CompileAndVerify(text,
                sourceSymbolValidator: validator,
                symbolValidator: validator,
                expectedOutput: @"~Base",
                expectedSignatures: new[]
                {
                    Signature("Base", "Finalize", ".method family hidebysig virtual instance System.Void Finalize() cil managed")
                });

            compVerifier.VerifyIL("Base.Finalize", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .try
  {
    IL_0000:  ldstr      ""~Base""
    IL_0005:  call       ""void System.Console.WriteLine(string)""
    IL_000a:  leave.s    IL_0013
  }
  finally
  {
    IL_000c:  ldarg.0
    IL_000d:  call       ""void object.Finalize()""
    IL_0012:  endfinally
  }
  IL_0013:  ret
}
");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [CompilerTrait(CompilerFeature.ExpressionBody)]
        public void ExpressionBodiedClassDestructor()
        {
            var text = @"
using System;

public class Base
{
    ~Base() => Console.WriteLine(""~Base"");
}

public class Program
{
    public static void Main()
    {
        Base b = new Base();
        b = null;
        GC.Collect(GC.MaxGeneration);
        GC.WaitForPendingFinalizers();
    }
}
";
            var validator = GetDestructorValidator("Base");
            var compVerifier = CompileAndVerify(text,
                sourceSymbolValidator: validator,
                symbolValidator: validator,
                expectedOutput: @"~Base",
                expectedSignatures: new[]
                {
                    Signature("Base", "Finalize", ".method family hidebysig virtual instance System.Void Finalize() cil managed")
                });

            compVerifier.VerifyIL("Base.Finalize", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .try
  {
    IL_0000:  ldstr      ""~Base""
    IL_0005:  call       ""void System.Console.WriteLine(string)""
    IL_000a:  leave.s    IL_0013
  }
  finally
  {
    IL_000c:  ldarg.0
    IL_000d:  call       ""void object.Finalize()""
    IL_0012:  endfinally
  }
  IL_0013:  ret
}
");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [CompilerTrait(CompilerFeature.ExpressionBody)]
        public void ExpressionBodiedSubClassDestructor()
        {
            var text = @"
using System;

public class Base
{
    ~Base() => Console.WriteLine(""~Base"");
}

public class Derived : Base
{
    ~Derived() => Console.WriteLine(""~Derived"");
}

public class Program
{
    public static void Main()
    {
        Derived d = new Derived();
        d = null;
        GC.Collect(GC.MaxGeneration);
        GC.WaitForPendingFinalizers();
    }
}
";
            var validator = GetDestructorValidator("Derived");
            var compVerifier = CompileAndVerify(text,
                sourceSymbolValidator: validator,
                symbolValidator: validator,
                expectedOutput: @"~Derived
~Base",
                expectedSignatures: new[]
                {
                    Signature("Base", "Finalize", ".method family hidebysig virtual instance System.Void Finalize() cil managed"),
                    Signature("Derived", "Finalize", ".method family hidebysig virtual instance System.Void Finalize() cil managed")
                });

            compVerifier.VerifyIL("Base.Finalize", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .try
  {
    IL_0000:  ldstr      ""~Base""
    IL_0005:  call       ""void System.Console.WriteLine(string)""
    IL_000a:  leave.s    IL_0013
  }
  finally
  {
    IL_000c:  ldarg.0
    IL_000d:  call       ""void object.Finalize()""
    IL_0012:  endfinally
  }
  IL_0013:  ret
}
");
            compVerifier.VerifyIL("Derived.Finalize", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .try
  {
    IL_0000:  ldstr      ""~Derived""
    IL_0005:  call       ""void System.Console.WriteLine(string)""
    IL_000a:  leave.s    IL_0013
  }
  finally
  {
    IL_000c:  ldarg.0
    IL_000d:  call       ""void Base.Finalize()""
    IL_0012:  endfinally
  }
  IL_0013:  ret
}
");
            compVerifier.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void SubclassDestructor()
        {
            var text = @"
using System;

public class Base
{
    ~Base()
    {
        Console.WriteLine(""~Base"");
    }
}

public class Derived : Base
{
    ~Derived()
    {
        Console.WriteLine(""~Derived"");
    }
}

public class Program
{
    public static void Main()
    {
        Base b = new Derived();
        b = null;
        GC.Collect(GC.MaxGeneration);
        GC.WaitForPendingFinalizers();
    }
}
";
            var validator = GetDestructorValidator("Derived");
            var compVerifier = CompileAndVerify(text,
                sourceSymbolValidator: validator,
                symbolValidator: validator,
                expectedOutput: @"~Derived
~Base",
                expectedSignatures: new[]
                {
                    Signature("Base", "Finalize", ".method family hidebysig virtual instance System.Void Finalize() cil managed"),
                    Signature("Derived", "Finalize", ".method family hidebysig virtual instance System.Void Finalize() cil managed")
                });

            compVerifier.VerifyIL("Base.Finalize", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .try
  {
    IL_0000:  ldstr      ""~Base""
    IL_0005:  call       ""void System.Console.WriteLine(string)""
    IL_000a:  leave.s    IL_0013
  }
  finally
  {
    IL_000c:  ldarg.0
    IL_000d:  call       ""void object.Finalize()""
    IL_0012:  endfinally
  }
  IL_0013:  ret
}
");
            compVerifier.VerifyIL("Derived.Finalize", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .try
  {
    IL_0000:  ldstr      ""~Derived""
    IL_0005:  call       ""void System.Console.WriteLine(string)""
    IL_000a:  leave.s    IL_0013
  }
  finally
  {
    IL_000c:  ldarg.0
    IL_000d:  call       ""void Base.Finalize()""
    IL_0012:  endfinally
  }
  IL_0013:  ret
}
");
            compVerifier.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void DestructorOverridesNonDestructor()
        {
            var text = @"
using System;

public class Base
{
    protected virtual void Finalize() //NB: does not override Object.Finalize
    {
        Console.WriteLine(""~Base"");
    }
}

public class Derived : Base
{
    ~Derived() //NB: in metadata, this will implicitly override Base.Finalize, but explicitly override Object.Finalize
    {
        Console.WriteLine(""~Derived"");
    }
}

public class Program
{
    public static void Main()
    {
        Base b = new Base();
        b = null;
        GC.Collect(GC.MaxGeneration);
        GC.WaitForPendingFinalizers();

        Derived d = new Derived();
        d = null;
        GC.Collect(GC.MaxGeneration);
        GC.WaitForPendingFinalizers();
    }
}
";
            var expectedOutput = @"
~Derived
~Base
";

            // Destructors generated by Roslyn should still be destructors when they are loaded back in - 
            // even in cases where the user creates their own Finalize methods (which is legal, but ill-advised).
            // This may not be the case for metadata from other C# compilers (which will likely not have
            // destructors explicitly override System.Object.Finalize).
            var validator = GetDestructorValidator("Derived");

            var compVerifier = CompileAndVerify(text,
                sourceSymbolValidator: validator,
                symbolValidator: validator,
                expectedOutput: expectedOutput,
                expectedSignatures: new[]
                {
                Signature("Base", "Finalize", ".method family hidebysig newslot virtual instance System.Void Finalize() cil managed"),
                    Signature("Derived", "Finalize", ".method family hidebysig virtual instance System.Void Finalize() cil managed")
                });

            compVerifier.VerifyDiagnostics(
                // (6,28): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"));
        }

        [WorkItem(542828, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542828")]
        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void BaseTypeHasNonVirtualFinalize()
        {
            var text = @"
using System;

public class Base
{
    protected void Finalize() //NB: does not override Object.Finalize
    {
        Console.WriteLine(""~Base"");
    }
}

public class Derived : Base
{
    ~Derived() //NB: in metadata, this will override Base.Finalize
    {
        Console.WriteLine(""~Derived"");
    }
}

public class Program
{
    public static void Main()
    {
        Base b = new Base();
        b = null;
        GC.Collect(GC.MaxGeneration);
        GC.WaitForPendingFinalizers();

        Derived d = new Derived();
        d = null;
        GC.Collect(GC.MaxGeneration);
        GC.WaitForPendingFinalizers();
    }
}
";
            var validator = GetDestructorValidator("Derived");
            var compVerifier = CompileAndVerify(text,
                sourceSymbolValidator: validator,
                symbolValidator: validator,
                expectedOutput: @"~Derived
~Base",
                expectedSignatures: new[]
                {
                Signature("Base", "Finalize", ".method family hidebysig instance System.Void Finalize() cil managed"),
                    Signature("Derived", "Finalize", ".method family hidebysig virtual instance System.Void Finalize() cil managed")
                });

            compVerifier.VerifyDiagnostics(
                // (6,20): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"));
        }

        [WorkItem(542828, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542828")]
        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void GenericBaseTypeHasNonVirtualFinalize()
        {
            var text = @"
using System;

public class Base<T>
{
    protected void Finalize() //NB: does not override Object.Finalize
    {
        Console.WriteLine(""~Base"");
    }
}

public class Derived : Base<int>
{
    ~Derived() //NB: in metadata, this will override Base.Finalize
    {
        Console.WriteLine(""~Derived"");
    }
}

public class Program
{
    public static void Main()
    {
        Base<char> b = new Base<char>();
        b = null;
        GC.Collect(GC.MaxGeneration);
        GC.WaitForPendingFinalizers();

        Derived d = new Derived();
        d = null;
        GC.Collect(GC.MaxGeneration);
        GC.WaitForPendingFinalizers();
    }
}
";

            var validator = GetDestructorValidator("Derived");
            var compVerifier = CompileAndVerify(text,
                sourceSymbolValidator: validator,
                symbolValidator: validator,
                expectedOutput: @"~Derived
~Base",
                expectedSignatures: new[]
                {
                Signature("Base`1", "Finalize", ".method family hidebysig instance System.Void Finalize() cil managed"),
                    Signature("Derived", "Finalize", ".method family hidebysig virtual instance System.Void Finalize() cil managed")
                });

            compVerifier.VerifyDiagnostics(
                // (6,20): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"));
        }

        [Fact]
        public void StructAndInterfaceHasNonVirtualFinalize()
        {
            var text = @"
public interface I
{
    void Finalize();
}

public struct S
{
    void Finalize() { }
}

class C
{
    private string Finalize;
}
";
            var compVerifier = CompileAndVerify(text);

            compVerifier.VerifyDiagnostics(
                // (4,10): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                //     void Finalize();
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"),
                // (9,10): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                //     void Finalize() { }
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"),
                // (14,20): warning CS0169: The field 'C.Finalize' is never used
                //     private string Finalize;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Finalize").WithArguments("C.Finalize")
                );
        }

        [Fact]
        public void IsRuntimeFinalizer1()
        {
            var text = @"
public class A
{
    ~A() { }
}

public class B
{
    public virtual void Finalize() { }
}

public class C : A
{
    public void Finalize() { }
}

public class D : B
{
    public override void Finalize() { }
}

public struct E
{
    public void Finalize() { }
}

public interface F
{
    void Finalize();
}

public class G
{
    protected virtual void Finalize() { }
}

public class H : G
{
    ~H() { }
}

public class I
{
    protected virtual void Finalize<T>() { }
}

public class J<T>
{
    protected virtual void Finalize() { }
}

public class K<T>
{
    ~K() { }
}

public class L<T>
{
	~L() { }
}

public class M<T> : L<T>
{
	~M() { }
}
";
            Action<ModuleSymbol> validator = module =>
            {
                var globalNamespace = module.GlobalNamespace;

                var mscorlib = module.ContainingAssembly.CorLibrary;
                var systemNamespace = mscorlib.GlobalNamespace.GetMember<NamespaceSymbol>("System");
                Assert.True(systemNamespace.GetMember<NamedTypeSymbol>("Object").GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());

                Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());
                Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());
                Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());
                Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("D").GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());
                Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("E").GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());
                Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("F").GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());
                Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("G").GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());
                Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("H").GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());
                Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("I").GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());

                var intType = systemNamespace.GetMember<TypeSymbol>("Int32");
                Assert.Equal(SpecialType.System_Int32, intType.SpecialType);

                var classJ = globalNamespace.GetMember<NamedTypeSymbol>("J");
                Assert.False(classJ.GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());
                var classJInt = classJ.Construct(intType);
                Assert.False(classJInt.GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());

                var classK = globalNamespace.GetMember<NamedTypeSymbol>("K");
                Assert.True(classK.GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());
                var classKInt = classK.Construct(intType);
                Assert.True(classKInt.GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());

                var classL = globalNamespace.GetMember<NamedTypeSymbol>("L");
                Assert.True(classL.GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());
                var classLInt = classL.Construct(intType);
                Assert.True(classLInt.GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());

                var classM = globalNamespace.GetMember<NamedTypeSymbol>("M");
                Assert.True(classM.GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());
                var classMInt = classM.Construct(intType);
                Assert.True(classMInt.GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer());
            };

            CompileAndVerify(text, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void IsRuntimeFinalizer2()
        {
            var text = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method family hidebysig virtual instance void 
          Finalize() cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class C

.class public auto ansi beforefieldinit D
       extends [mscorlib]System.Object
{
  .method family hidebysig newslot virtual instance void 
          Finalize() cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class D
";
            var compilation = CreateCompilationWithILAndMscorlib40("", text);

            var globalNamespace = compilation.GlobalNamespace;

            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer()); //override of object.Finalize
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("D").GetMember<MethodSymbol>("Finalize").IsRuntimeFinalizer()); //same but has "newslot"
        }

        [WorkItem(528903, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528903")] // Won't fix - test just captures behavior.
        [Fact]
        public void DestructorOverridesPublicFinalize()
        {
            var text = @"
public class A
{
    public virtual void Finalize() { }
}

public class B : A
{
    ~B() { }
}
";
            var compilation = CreateCompilation(text, options: TestOptions.ReleaseDll);

            // NOTE: has warnings, but not errors.
            compilation.VerifyDiagnostics(
                // (4,25): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                //     public virtual void Finalize() { }
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"));

            // We produce unverifiable code here as per bug resolution (compat concerns, not common case).
            CompileAndVerify(compilation, verify: Verification.FailsPEVerify).VerifyIL("B.Finalize",

                @"
{
  // Code size       10 (0xa)
  .maxstack  1
  .try
  {
    IL_0000:  leave.s    IL_0009
  }
  finally
  {
    IL_0002:  ldarg.0
    IL_0003:  call       ""void object.Finalize()""
    IL_0008:  endfinally
  }
  IL_0009:  ret
}
");
        }

        [WorkItem(528907, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528907")]
        [Fact]
        public void BaseTypeHasGenericFinalize()
        {
            var text = @"
public class A
{
    protected void Finalize<T>() { }
}

public class B : A
{
    ~B() { }
}
";
            // NOTE: calling object.Finalize, since A.Finalize has the wrong arity.
            // (Dev11 called A.Finalize and failed at runtime, since it wasn't providing
            // a type argument.)
            CompileAndVerify(text).VerifyIL("B.Finalize", @"
{
  // Code size       10 (0xa)
  .maxstack  1
  .try
  {
    IL_0000:  leave.s    IL_0009
  }
  finally
  {
    IL_0002:  ldarg.0
    IL_0003:  call       ""void object.Finalize()""
    IL_0008:  endfinally
  }
  IL_0009:  ret
}
");
        }

        [WorkItem(528903, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528903")]
        [Fact]
        public void MethodImplEntry()
        {
            var text = @"
public class A
{
    ~A() { }
}
";
            CompileAndVerify(text, assemblyValidator: (assembly) =>
            {
                var peFileReader = assembly.GetMetadataReader();

                // Find the handle and row for A.
                var pairA = peFileReader.TypeDefinitions.AsEnumerable().
                    Select(handle => new { handle = handle, row = peFileReader.GetTypeDefinition(handle) }).
                    Single(pair => peFileReader.GetString(pair.row.Name) == "A" &&
                        string.IsNullOrEmpty(peFileReader.GetString(pair.row.Namespace)));
                TypeDefinitionHandle handleA = pairA.handle;
                TypeDefinition typeA = pairA.row;

                // Find the handle for A's destructor.
                MethodDefinitionHandle handleDestructorA = typeA.GetMethods().AsEnumerable().
                    Single(handle => peFileReader.GetString(peFileReader.GetMethodDefinition(handle).Name) == WellKnownMemberNames.DestructorName);


                // Find the handle for System.Object.
                TypeReferenceHandle handleObject = peFileReader.TypeReferences.AsEnumerable().
                    Select(handle => new { handle = handle, row = peFileReader.GetTypeReference(handle) }).
                    Single(pair => peFileReader.GetString(pair.row.Name) == "Object" &&
                        peFileReader.GetString(pair.row.Namespace) == "System").handle;

                // Find the handle for System.Object's destructor.
                MemberReferenceHandle handleDestructorObject = peFileReader.MemberReferences.AsEnumerable().
                    Select(handle => new { handle = handle, row = peFileReader.GetMemberReference(handle) }).
                    Single(pair => pair.row.Parent == (EntityHandle)handleObject &&
                        peFileReader.GetString(pair.row.Name) == WellKnownMemberNames.DestructorName).handle;


                // Find the MethodImpl row for A.
                MethodImplementation methodImpl = typeA.GetMethodImplementations().AsEnumerable().
                    Select(handle => peFileReader.GetMethodImplementation(handle)).
                    Single();

                // The Class column should point to A.
                Assert.Equal(handleA, methodImpl.Type);

                // The MethodDeclaration column should point to System.Object.Finalize.
                Assert.Equal((EntityHandle)handleDestructorObject, methodImpl.MethodDeclaration);

                // The MethodDeclarationColumn should point to A's destructor.
                Assert.Equal((EntityHandle)handleDestructorA, methodImpl.MethodBody);
            });
        }

        private static Action<ModuleSymbol> GetDestructorValidator(string typeName)
        {
            return module => ValidateDestructor(module, typeName);
        }

        // NOTE: assumes there's a destructor.
        private static void ValidateDestructor(ModuleSymbol module, string typeName)
        {
            var @class = module.GlobalNamespace.GetMember<NamedTypeSymbol>(typeName);
            var destructor = @class.GetMember<MethodSymbol>(WellKnownMemberNames.DestructorName);

            Assert.Equal(MethodKind.Destructor, destructor.MethodKind);

            Assert.True(destructor.IsMetadataVirtual());
            Assert.False(destructor.IsVirtual);
            Assert.False(destructor.IsOverride);
            Assert.False(destructor.IsSealed);
            Assert.False(destructor.IsStatic);
            Assert.False(destructor.IsAbstract);
            Assert.Null(destructor.OverriddenMethod);

            Assert.Equal(SpecialType.System_Void, destructor.ReturnType.SpecialType);
            Assert.Equal(0, destructor.Parameters.Length);
            Assert.Equal(0, destructor.TypeParameters.Length);

            Assert.Equal(Accessibility.Protected, destructor.DeclaredAccessibility);
        }
    }
}
