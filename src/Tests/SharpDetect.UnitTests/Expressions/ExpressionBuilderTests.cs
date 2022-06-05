using SharpDetect.Common.Runtime.Arguments;
using SharpDetect.Common.Scripts.ExpressionBuilder;
using SharpDetect.Core.Runtime.Arguments;
using Xunit;

namespace SharpDetect.UnitTests.Expressions
{
    public class ExpressionBuilderTests
    {
        [Fact]
        public void ExpressionBuilder_ReturnAlwaysTrueFunc()
        {
            // Prepare & Act
            var expr = new CSharpExpressionBuilder().LoadConstant(true, "System.Boolean").Compile();

            // Assert
            Assert.True(expr.Invoke(null! /* unused */, null! /* unused */));
        }

        [Fact]
        public void ExpressionBuilder_ReturnAlwaysFalseFunc()
        {
            // Prepare & Act
            var expr = new CSharpExpressionBuilder().LoadConstant(false, "System.Boolean").Compile();

            // Assert
            Assert.False(expr.Invoke(null! /* unused */, null! /* unused */));
        }

        [Fact]
        public void ExpressionBuilder_ReturnFirstArgumentUnboxed()
        {
            // Prepare & Act
            var expr = new CSharpExpressionBuilder()
                .LoadArgument(0)
                .Member("Item2")
                .Member(nameof(IValueOrObject.BoxedValue))
                .Unbox("System.Boolean")
                .Compile();

            // Assert
            var firstArgument = (object)true;
            Assert.True(expr.Invoke(null! /* unused */, new[] { ((ushort)0, (IValueOrPointer)new ValueOrPointer(firstArgument)) }));
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(false, false, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        public void ExpressionBuilder_ReturnFirstTwoArgumentsEqual(bool first, bool second, bool result)
        {
            // Prepare & Act
            var expr = new CSharpExpressionBuilder()
                .LoadArgument(0)
                .Member("Item2")
                .Member(nameof(IValueOrObject.BoxedValue))
                .Unbox("System.Boolean")
                .LoadArgument(1)
                .Member("Item2")
                .Member(nameof(IValueOrObject.BoxedValue))
                .Unbox("System.Boolean")
                .BinaryOperation(BinaryOperationType.Equal)
                .Compile();

            // Assert
            Assert.Equal(result, expr.Invoke(null! /* unused */, new[] { ((ushort)0, (IValueOrPointer)new ValueOrPointer(first)), ((ushort)1, new ValueOrPointer(second)) }));
        }
    }
}
