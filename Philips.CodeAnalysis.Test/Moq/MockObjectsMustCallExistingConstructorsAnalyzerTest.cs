﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MoqAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Moq
{
	[TestClass]
	public class MockObjectsMustCallExistingConstructorsAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new MockObjectsMustCallExistingConstructorsAnalyzer();
		}

		protected override ImmutableArray<MetadataReference> GetMetadataReferences()
		{
			string mockReference = typeof(Mock<>).Assembly.Location;
			MetadataReference reference = MetadataReference.CreateFromFile(mockReference);
			return base.GetMetadataReferences().Add(reference);
		}

		#endregion

		#region Public Interface

		[DataRow("", false)]
		[DataRow("MockBehavior.Default", false)]
		[DataRow("1, 2", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ConstructorsMustExistViaNewMock(string arguments, bool isError)
		{
			const string template = @"
using Moq;
public class Mockable
{{
	public Mockable() {{ }}
}}

public static class Bar
{{
	public static void Method()
	{{
		Mock<Mockable> m = new Mock<Mockable>({0});
	}}
}}
";

			var code = string.Format(template, arguments);
			if (isError)
			{
				VerifyDiagnostic(code, DiagnosticId.MockArgumentsMustMatchConstructor);
			}
			else
			{
				VerifySuccessfulCompilation(code);
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ConstructorsMustExistViaNewMock2()
		{
			const string template = @"
using Moq;
public class Mockable
{{
	public Mockable(int a, int b) {{ }}
}}

public static class Bar
{{
	public static void Method()
	{{
		Mock<Mockable> m = new Mock<Mockable> {{ }};
	}}
}}
";
			VerifyDiagnostic(template, DiagnosticId.MockArgumentsMustMatchConstructor);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ConstructorsMustExistViaNewMock3()
		{
			const string template = @"
using Moq;

public delegate void Foo();

public static class Bar
{{
	public static void Method()
	{{
		Mock<Foo> m = new Mock<Foo> {{ }};
	}}
}}
";
			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void MockBehaviorCanBeTakenFromMethodParameter()
		{
			const string template = @"
using Moq;
public class Mockable
{{
	public Mockable() {{ }}
}}

public static class Bar
{{
	public static Mock<Mockable> MakeMock(MockBehavior behavior)
	{{
		return new Mock<Mockable>(behavior);
	}}

	public static void Method()
	{{
		Mock<Mockable> m1 = MakeMock(MockBehavior.Loose);
		Mock<Mockable> m2 = MakeMock(MockBehavior.Strict);
		Mock<Mockable> m3 = MakeMock(MockBehavior.Default);
	}}
}}
";

			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void MockBehaviorCanBeTakenFromLocal()
		{
			const string template = @"
using Moq;
public class Mockable
{{
	public Mockable() {{ }}
}}

public static class Bar
{{
	public static Mock<Mockable> MakeMock()
	{{
		MockBehavior behavior = MockBehavior.Loose;
		return new Mock<Mockable>(behavior);
	}}

	public static void Method()
	{{
		Mock<Mockable> m1 = MakeMock();
	}}
}}
";

			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void MockBehaviorCanBeTakenFromField()
		{
			const string template = @"
using Moq;
public class Mockable
{{
	public Mockable() {{ }}
}}

public static class Bar
{{
	private static MockBehavior _behavior = MockBehavior.Loose;
	public static Mock<Mockable> MakeMock()
	{{
		return new Mock<Mockable>(_behavior);
	}}

	public static void Method()
	{{
		Mock<Mockable> m1 = MakeMock();
	}}
}}
";

			VerifySuccessfulCompilation(template);
		}

		[DataRow("", false)]
		[DataRow("MockBehavior.Default", false)]
		[DataRow("1, 2", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DelegateConstructorsMustExistViaNewMock(string arguments, bool isError)
		{
			const string template = @"
using Moq

public delegate void Foo();

public static class Bar
{{
	public static void Method()
	{{
		Mock<Foo> m = new Mock<Foo>({0});
	}}
}}
";
			var code = string.Format(template, arguments);
			if (isError)
			{
				VerifyDiagnostic(code, DiagnosticId.MockArgumentsMustMatchConstructor);
			}
			else
			{
				VerifySuccessfulCompilation(code);
			}
		}

		[DataRow("{ TestProperty = string.Empty }", false)]
		[DataRow("(MockBehavior.Default) { TestProperty = string.Empty }", false)]
		[DataRow("(1, 2) { TestProperty = string.Empty }", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DelegateConstructorsMustHandleNoArgumentList(string arguments, bool isError)
		{
			const string template = @"
using Moq;
public class Mockable
{{
	public Mockable() {{ }}
	public string TestProperty {{ get; set; }}
}}

public static class Bar
{{
	public static void Method()
	{{
		Mock<Mockable> m = new Mock<Mockable>{0};
	}}
}}
";
			var code = string.Format(template, arguments);
			if (isError)
			{
				VerifyDiagnostic(code, DiagnosticId.MockArgumentsMustMatchConstructor);
			}
			else
			{
				VerifySuccessfulCompilation(code);
			}
		}

		[DataRow("-1, false", false)]
		[DataRow("string.Empty, false", false)]
		[DataRow("It.IsAny<string>(), It.IsAny<bool>()", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ConstructorHandlesTypesCorrectly(string arguments, bool isError)
		{
			const string template = @"
using Moq;
public class Mockable
{{
	public Mockable(int i, bool b = true) {{ }}
	public Mockable(string foo, bool b = true) {{ }}
}}

public static class Bar
{{
	public static void Method()
	{{
		Mock<Mockable> m = new Mock<Mockable>({0});
	}}
}}
";
			var code = string.Format(template, arguments);
			if (isError)
			{
				VerifyDiagnostic(code, DiagnosticId.MockArgumentsMustMatchConstructor);
			}
			else
			{
				VerifySuccessfulCompilation(code);
			}
		}

		[DataRow("-1, false", false)]
		[DataRow("string.Empty, false", false)]
		[DataRow("It.IsAny<string>(), It.IsAny<bool>()", false)]
		[DataRow("DateTime.Now, false", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ConstructorHandlesGenericsTypesCorrectly(string arguments, bool isError)
		{
			const string template = @"using System;
using Moq;
public class Mockable<T>
{{
	public Mockable(T i, bool b = true) {{ }}
	public Mockable(string foo, bool b = true) {{ }}
	public Mockable(DateTime foo, bool b = true) {{ }}
}}

public static class Bar
{{
	public static void Method()
	{{
		Mock<Mockable> m = new Mock<Mockable<int>>({0});
	}}
}}
";
			var code = string.Format(template, arguments);
			if (isError)
			{
				VerifyDiagnostic(code, DiagnosticId.MockArgumentsMustMatchConstructor);
			}
			else
			{
				VerifySuccessfulCompilation(code);
			}
		}

		[DataRow("", false)]
		[DataRow("MockBehavior.Default", false)]
		[DataRow("1, 2", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ConstructorsMustExistViaCreate(string arguments, bool isError)
		{
			const string template = @"
using Moq;
public class Mockable
{{
	public Mockable() {{ }}
}}

public static class Bar
{{
	public static void Method()
	{{
		MockRepository repository = new MockRepository(MockBehavior.Default);
		Mock<Mockable> m = repository.Create<Mockable>({0});
	}}
}}
";
			var code = string.Format(template, arguments);
			if (isError)
			{
				VerifyDiagnostic(code, DiagnosticId.MockArgumentsMustMatchConstructor);
			}
			else
			{
				VerifySuccessfulCompilation(code);
			}
		}

		[DataRow("", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ConstructorsMustExistViaMockOf(string arguments, bool isError)
		{
			const string template = @"
using Moq;
public class Mockable
{{
	public Mockable() {{ }}
}}

public static class Bar
{{
	public static void Method()
	{{
		Mockable m = Mock.Of<Mockable>({0});
	}}
}}
";
			var code = string.Format(template, arguments);
			if (isError)
			{
				VerifyDiagnostic(code, DiagnosticId.MockArgumentsMustMatchConstructor);
			}
			else
			{
				VerifySuccessfulCompilation(code);
			}
		}

		[DataRow("Mockable m = Mock.Of<")]
		[DataRow("Mockable m = Mock.Of<Mockable")]
		[DataRow("Mockable m = Mock.Of<Mockable>")]
		[DataRow("Mockable m = new Mock<")]
		[DataRow("Mockable m = new Mock<Mockable")]
		[DataRow("Mockable m = new Mock<Mockable>")]
		[DataRow("Mockable m = repo.Create<")]
		[DataRow("Mockable m = repo.Create<Mockable")]
		[DataRow("Mockable m = repo.Create<Mockable>")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ConstructorsMustNotCauseCrash(string statement)
		{
			const string template = @"
using Moq;
public class Mockable
{{
	public Mockable() {{ }}
}}

public static class Bar
{{
	public static void Method()
	{{
		MockRepository repo = new MockRepository(MockBehavior.Default);
		Mockable m = {0}
	}}
}}
";
			VerifySuccessfulCompilation(string.Format(template, statement));
		}

		[DataRow("", false)]
		[DataRow("MockBehavior.Default", false)]
		[DataRow("1, 2", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CanMockInterfaces(string arguments, bool isError)
		{
			const string template = @"
using Moq;

public interface IMockable
{{
}}

public static class Bar
{{
	public static void Method()
	{{
		Mock<IMockable> m = new Mock<IMockable>({0});
	}}
}}
";
			var code = string.Format(template, arguments);
			if (isError)
			{
				VerifyDiagnostic(code, DiagnosticId.MockArgumentsMustMatchConstructor);
			}
			else
			{
				VerifySuccessfulCompilation(code);
			}
		}

		[DataRow("string.Empty", false)]
		[DataRow("null", false)]
		[DataRow("int.MaxValue", true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ConstructorsMustHaveCorrectTypeParameters(string arguments, bool isError)
		{
			const string template = @"
using System;
using Moq;
public class Mockable
{{
	public Mockable(string str) {{ }}
}}

public static class Bar
{{
	public static void Method()
	{{
		Mock<Mockable> mock = new Mock<Mockable>({0});
	}}
}}
";
			var code = string.Format(template, arguments);
			if (isError)
			{
				VerifyDiagnostic(code, DiagnosticId.MockArgumentsMustMatchConstructor);
			}
			else
			{
				VerifySuccessfulCompilation(code);
			}
		}

		#endregion
	}
}
