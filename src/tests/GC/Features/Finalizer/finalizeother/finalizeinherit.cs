// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests Finalize() with Inheritance

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace One
{
    abstract class A
    {

    }

    class B: A
    {
        ~B()
        {
            Console.WriteLine("In Finalize of B");
        }
    }

    class C: B
    {
        public static int count=0;
        ~C()
        {
            Console.WriteLine("In Finalize of C");
            count++;
        }
    }
}

namespace Two
{
    using One;
    class D: C
    {
    }
}

namespace Three {
    using One;
    using Two;

    class CreateObj
    {

// disabling unused variable warning
#pragma warning disable 0414
        B b;
        D d;
#pragma warning restore 0414
        C c;

        // No inline to ensure no stray refs to the B, C, D objects.
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public CreateObj()
        {
            b = new B();
            c = new C();
            d = new D();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public bool RunTest()
        {
            A a = c;

            d=null;
            b=null;
            a=null;
            c=null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return (C.count == 2);
        }
    }

    public class Test
    {
        [Fact]
        public static int TestEntryPoint()
        {
            CreateObj temp = new CreateObj();

            temp.RunTest();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (C.count == 2)
            {
                Console.WriteLine("Test Passed");
                return 100;
            }
            Console.WriteLine("Test Failed");
            return 1;

        }
    }

}
