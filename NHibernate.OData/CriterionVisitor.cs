﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Criterion;

namespace NHibernate.OData
{
    internal class CriterionVisitor : QueryVisitorBase<ICriterion>
    {
        private static readonly CriterionVisitor _instance = new CriterionVisitor();

        private CriterionVisitor()
        {
        }

        public static ICriterion CreateCriterion(Expression expression)
        {
            return expression.Visit(_instance);
        }

        public override ICriterion ComparisonExpression(ComparisonExpression expression)
        {
            IProjection left;
            IProjection right;
            var leftExpression = expression.Left;
            var rightExpression = expression.Right;

            // Swap the expressions if the left operand is null.

            if (IsNull(leftExpression))
            {
                var tmp = leftExpression;
                leftExpression = rightExpression;
                rightExpression = tmp;
            }

            if (IsNull(rightExpression))
            {
                if (IsNull(leftExpression))
                    throw new NotSupportedException();

                left = ProjectionVisitor.CreateProjection(leftExpression);

                switch (expression.Operator)
                {
                    case Operator.Eq: return Restrictions.IsNull(left);
                    case Operator.Ne: return Restrictions.IsNotNull(left);
                    default: throw new NotSupportedException();
                }
            }

            left = ProjectionVisitor.CreateProjection(expression.Left);
            right = ProjectionVisitor.CreateProjection(expression.Right);

            switch (expression.Operator)
            {
                case Operator.Eq: return Restrictions.EqProperty(left, right);
                case Operator.Ne: return Restrictions.NotEqProperty(left, right);
                case Operator.Gt: return Restrictions.GtProperty(left, right);
                case Operator.Ge: return Restrictions.GeProperty(left, right);
                case Operator.Lt: return Restrictions.LtProperty(left, right);
                case Operator.Le: return Restrictions.LeProperty(left, right);
                default: throw new NotSupportedException();
            }
        }

        private bool IsNull(Expression expression)
        {
            var literalExpression = expression as LiteralExpression;

            return literalExpression != null && literalExpression.LiteralType == LiteralType.Null;
        }

        public override ICriterion LogicalExpression(LogicalExpression expression)
        {
            var left = CreateCriterion(expression.Left);
            var right = CreateCriterion(expression.Right);

            switch (expression.Operator)
            {
                case Operator.And: return Restrictions.And(left, right);
                case Operator.Or: return Restrictions.Or(left, right);
                default: throw new NotSupportedException();
            }
        }

        public override ICriterion BoolUnaryExpression(BoolUnaryExpression expression)
        {
            var criterion = CreateCriterion(expression.Expression);

            switch (expression.Operator)
            {
                case Operator.Not: return Restrictions.Not(criterion);
                default: throw new NotSupportedException();
            }
        }

        public override ICriterion MethodCallExpression(MethodCallExpression expression)
        {
            return CriterionMethodVisitor.CreateCriterion(expression.Method, expression.Arguments);
        }
    }
}
