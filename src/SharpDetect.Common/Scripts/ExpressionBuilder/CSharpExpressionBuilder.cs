using SharpDetect.Common.Exceptions;
using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Common.Runtime.Arguments;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpDetect.Common.Scripts.ExpressionBuilder
{
    public enum BinaryOperationType
    {
        Add = ExpressionType.Add,
        Subtract = ExpressionType.Subtract,
        Multiply = ExpressionType.Multiply,
        Divide = ExpressionType.Divide,
        Modulo = ExpressionType.Modulo,

        Equal = ExpressionType.Equal,
        NotEqual = ExpressionType.NotEqual,
        GreaterThan = ExpressionType.GreaterThan,
        GreaterThanOrEqual = ExpressionType.GreaterThanOrEqual,
        LessThan = ExpressionType.LessThan,
        LessThanOrEqual = ExpressionType.LessThanOrEqual,

        And = ExpressionType.And,
        Or = ExpressionType.Or,
        ExclusiveOr = ExpressionType.ExclusiveOr,

        AndAlso = ExpressionType.AndAlso,
        OrElse = ExpressionType.OrElse
    }

    public class CSharpExpressionBuilder
    {
        private readonly Stack<Expression> stack;
        private ParameterExpression argumentsExpression;
        private ParameterExpression returnValueExpression;

        public CSharpExpressionBuilder()
        {
            stack = new Stack<Expression>();
            argumentsExpression = Expression.Parameter(typeof((ushort, IValueOrObject)[]));
            returnValueExpression = Expression.Parameter(typeof(IValueOrObject));
        }

        public ResultChecker Compile()
        {
            Guard.Equal<int, InvalidOperationException>(1, stack.Count);
            return new ResultChecker(
                (Func<IValueOrObject?, (ushort, IValueOrObject)[]?, bool>)Expression.Lambda(stack.Pop(), returnValueExpression, argumentsExpression).Compile());
        }

        public CSharpExpressionBuilder Convert(string typeFullName)
        {
            var type = Type.GetType(typeFullName);
            Guard.NotEmpty<InvalidOperationException>(stack);
            Guard.NotNull<Type, ArgumentException>(type);

            var argument = stack.Pop();
            stack.Push(Expression.Convert(argument, type));
            return this;
        }

        public CSharpExpressionBuilder LoadArgument(int index)
        {
            stack.Push(Expression.ArrayAccess(argumentsExpression, Expression.Constant(index)));
            return this;
        }

        public CSharpExpressionBuilder LoadReturnValue()
        {
            stack.Push(returnValueExpression);
            return this;
        }

        public CSharpExpressionBuilder LoadConstant(object value, string typeFullName)
        {
            var type = Type.GetType(typeFullName);
            Guard.NotNull<Type, ArgumentException>(type);

            stack.Push(Expression.Constant(value, type));
            return this;
        }

        public CSharpExpressionBuilder Member(string memberName)
        {
            Guard.NotEmpty<InvalidOperationException>(stack);
            var argument = stack.Pop();
            var candidateMembers = argument.Type.GetMember(memberName, MemberTypes.Field | MemberTypes.Property, BindingFlags.Public | BindingFlags.Instance);
            Guard.NotNull<MemberInfo[], ArgumentException>(candidateMembers);
            Guard.Single<ArgumentException>(candidateMembers);

            stack.Push(Expression.MakeMemberAccess(argument, candidateMembers.First()));
            return this;
        }

        public CSharpExpressionBuilder Unbox(string typeFullName)
        {
            var type = Type.GetType(typeFullName);
            Guard.NotEmpty<InvalidOperationException>(stack);
            Guard.NotNull<Type, ArgumentException>(type);

            var argument = stack.Pop();
            stack.Push(Expression.Unbox(argument, type));
            return this;
        }

        public CSharpExpressionBuilder BinaryOperation(BinaryOperationType type)
        {
            Guard.GreaterOrEqual<int, InvalidOperationException>(stack.Count).Than(2);

            var right = stack.Pop();
            var left = stack.Pop();
            stack.Push(Expression.MakeBinary((ExpressionType)type, left, right));
            return this;
        }
    }
}
