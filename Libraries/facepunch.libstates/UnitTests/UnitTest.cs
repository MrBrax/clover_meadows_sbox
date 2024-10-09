global using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sandbox.States.Test;

[TestClass]
public class TestInit
{
	[AssemblyInitialize]
	public static void ClassInitialize( TestContext context )
	{
		Sandbox.Application.InitUnitTest();
	}
}
