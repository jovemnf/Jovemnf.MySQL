using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Jovemnf.MySQL.Builder;

internal static class WhereExpressionTranslator
{
    public static void Apply<T>(
        Expression<Func<T, bool>> predicate,
        Action<TranslatedWhereCondition> addCondition)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(addCondition);

        Translate(predicate.Body, isOr: false, addCondition);
    }

    public static TranslatedRawWhereClause BuildRaw<T>(
        Expression<Func<T, bool>> predicate,
        Func<string, string> resolveField)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(resolveField);

        var parameters = new List<object>();
        var translated = TranslateRaw(predicate.Body, resolveField, parameters);
        return new TranslatedRawWhereClause(translated.Sql, parameters.ToArray());
    }

    private static void Translate(
        Expression expression,
        bool isOr,
        Action<TranslatedWhereCondition> addCondition)
    {
        expression = StripConvert(expression);

        if (expression is BinaryExpression binaryExpression)
        {
            if (binaryExpression.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
            {
                ValidateLogicalGrouping(binaryExpression);
                Translate(binaryExpression.Left, isOr, addCondition);
                Translate(binaryExpression.Right, binaryExpression.NodeType == ExpressionType.OrElse, addCondition);
                return;
            }

            if (TryTranslateComparison(binaryExpression, out var field, out var value, out var op))
            {
                addCondition(new TranslatedWhereCondition(field, value, op, isOr));
                return;
            }
        }

        if (TryTranslateMethodCall(expression, isNegated: false, out var condition))
        {
            addCondition(condition with { IsOr = isOr });
            return;
        }

        if (TryTranslateBooleanMember(expression, out var boolField, out var boolValue))
        {
            addCondition(new TranslatedWhereCondition(boolField, boolValue, QueryOperator.Equals, isOr));
            return;
        }

        if (expression is UnaryExpression unaryExpression &&
            unaryExpression.NodeType == ExpressionType.Not &&
            TryTranslateMethodCall(unaryExpression.Operand, isNegated: true, out var negatedCondition))
        {
            addCondition(negatedCondition with { IsOr = isOr });
            return;
        }

        throw new NotSupportedException(
            "A expressão do WHERE não é suportada. Use comparações simples entre propriedades do modelo e valores, combinadas com && e ||.");
    }

    private static void ValidateLogicalGrouping(BinaryExpression expression)
    {
        if (expression.NodeType != ExpressionType.AndAlso)
            return;

        if (IsLogicalOr(expression.Left) || IsLogicalOr(expression.Right))
        {
            throw new NotSupportedException(
                "Agrupamentos misturando OR dentro de AND ainda não são suportados neste WHERE tipado. Reescreva a expressão ou use os métodos fluentes tradicionais.");
        }
    }

    private static bool IsLogicalOr(Expression expression)
    {
        expression = StripConvert(expression);
        return expression is BinaryExpression binaryExpression && binaryExpression.NodeType == ExpressionType.OrElse;
    }

    private static bool TryTranslateComparison(
        BinaryExpression expression,
        out string field,
        out object value,
        out QueryOperator op)
    {
        if (TryGetMemberField(expression.Left, out field) &&
            TryEvaluateValue(expression.Right, out value))
        {
            op = MapOperator(expression.NodeType, reverse: false, value);
            return true;
        }

        if (TryGetMemberField(expression.Right, out field) &&
            TryEvaluateValue(expression.Left, out value))
        {
            op = MapOperator(expression.NodeType, reverse: true, value);
            return true;
        }

        field = null;
        value = null;
        op = default;
        return false;
    }

    private static QueryOperator MapOperator(ExpressionType nodeType, bool reverse, object value)
    {
        if (value == null)
        {
            return nodeType switch
            {
                ExpressionType.Equal => QueryOperator.IsNull,
                ExpressionType.NotEqual => QueryOperator.IsNotNull,
                _ => throw new NotSupportedException("Somente comparações == null e != null são suportadas.")
            };
        }

        return (nodeType, reverse) switch
        {
            (ExpressionType.Equal, _) => QueryOperator.Equals,
            (ExpressionType.NotEqual, _) => QueryOperator.NotEquals,
            (ExpressionType.GreaterThan, false) => QueryOperator.GreaterThan,
            (ExpressionType.GreaterThan, true) => QueryOperator.LessThan,
            (ExpressionType.GreaterThanOrEqual, false) => QueryOperator.GreaterThanOrEqual,
            (ExpressionType.GreaterThanOrEqual, true) => QueryOperator.LessThanOrEqual,
            (ExpressionType.LessThan, false) => QueryOperator.LessThan,
            (ExpressionType.LessThan, true) => QueryOperator.GreaterThan,
            (ExpressionType.LessThanOrEqual, false) => QueryOperator.LessThanOrEqual,
            (ExpressionType.LessThanOrEqual, true) => QueryOperator.GreaterThanOrEqual,
            _ => throw new NotSupportedException(
                $"O operador '{nodeType}' não é suportado no WHERE tipado.")
        };
    }

    private static bool TryTranslateBooleanMember(Expression expression, out string field, out object value)
    {
        if (TryGetMemberField(expression, out field))
        {
            value = true;
            return true;
        }

        expression = StripConvert(expression);
        if (expression is UnaryExpression unaryExpression &&
            unaryExpression.NodeType == ExpressionType.Not &&
            TryGetMemberField(unaryExpression.Operand, out field))
        {
            value = false;
            return true;
        }

        field = null;
        value = null;
        return false;
    }

    private static bool TryTranslateMethodCall(Expression expression, bool isNegated, out TranslatedWhereCondition condition)
    {
        expression = StripConvert(expression);

        if (expression is not MethodCallExpression methodCallExpression)
        {
            condition = default;
            return false;
        }

        if (TryTranslateStringPattern(methodCallExpression, isNegated, out condition))
            return true;

        if (TryTranslateCollectionContains(methodCallExpression, isNegated, out condition))
            return true;

        if (TryTranslateEnumerableAny(methodCallExpression, isNegated, out condition))
            return true;

        condition = default;
        return false;
    }

    private static bool TryTranslateStringPattern(
        MethodCallExpression expression,
        bool isNegated,
        out TranslatedWhereCondition condition)
    {
        if (expression.Object == null ||
            expression.Object.Type != typeof(string) ||
            !TryGetMemberField(expression.Object, out var field) ||
            expression.Arguments.Count != 1 ||
            !TryEvaluateValue(expression.Arguments[0], out var rawValue))
        {
            condition = default;
            return false;
        }

        var stringValue = rawValue?.ToString() ?? string.Empty;
        var pattern = expression.Method.Name switch
        {
            nameof(string.Contains) => $"%{stringValue}%",
            nameof(string.StartsWith) => $"{stringValue}%",
            nameof(string.EndsWith) => $"%{stringValue}",
            _ => null
        };

        if (pattern == null)
        {
            condition = default;
            return false;
        }

        condition = new TranslatedWhereCondition(
            field,
            pattern,
            isNegated ? QueryOperator.NotLike : QueryOperator.Like,
            IsOr: false);
        return true;
    }

    private static bool TryTranslateCollectionContains(
        MethodCallExpression expression,
        bool isNegated,
        out TranslatedWhereCondition condition)
    {
        if (!string.Equals(expression.Method.Name, "Contains", StringComparison.Ordinal) ||
            expression.Arguments.Count == 0)
        {
            condition = default;
            return false;
        }

        Expression collectionExpression;
        Expression memberExpression;

        if (expression.Object != null && expression.Object.Type != typeof(string))
        {
            if (expression.Arguments.Count != 1)
            {
                condition = default;
                return false;
            }

            collectionExpression = expression.Object;
            memberExpression = expression.Arguments[0];
        }
        else
        {
            if (expression.Arguments.Count != 2)
            {
                condition = default;
                return false;
            }

            collectionExpression = expression.Arguments[0];
            memberExpression = expression.Arguments[1];
        }

        if (!TryGetMemberField(memberExpression, out var field) ||
            !TryEvaluateValue(collectionExpression, out var valuesObject) ||
            valuesObject is not IEnumerable enumerable ||
            valuesObject is string)
        {
            condition = default;
            return false;
        }

        condition = new TranslatedWhereCondition(
            field,
            enumerable,
            isNegated ? QueryOperator.NotIn : QueryOperator.In,
            IsOr: false);
        return true;
    }

    private static bool TryTranslateEnumerableAny(
        MethodCallExpression expression,
        bool isNegated,
        out TranslatedWhereCondition condition)
    {
        if (!string.Equals(expression.Method.Name, "Any", StringComparison.Ordinal) ||
            expression.Arguments.Count != 2)
        {
            condition = default;
            return false;
        }

        var lambdaExpression = StripQuote(expression.Arguments[1]) as LambdaExpression;
        if (lambdaExpression == null || lambdaExpression.Parameters.Count != 1)
        {
            condition = default;
            return false;
        }

        if (lambdaExpression.Body is not BinaryExpression predicateBody ||
            predicateBody.NodeType != ExpressionType.Equal)
        {
            condition = default;
            return false;
        }

        var parameter = lambdaExpression.Parameters[0];
        string field;

        if (IsLambdaParameterReference(predicateBody.Left, parameter) &&
            TryGetMemberField(predicateBody.Right, out field))
        {
            // collection item == model field
        }
        else if (IsLambdaParameterReference(predicateBody.Right, parameter) &&
                 TryGetMemberField(predicateBody.Left, out field))
        {
            // model field == collection item
        }
        else
        {
            condition = default;
            return false;
        }

        if (!TryEvaluateValue(expression.Arguments[0], out var valuesObject) ||
            valuesObject is not IEnumerable enumerable ||
            valuesObject is string)
        {
            condition = default;
            return false;
        }

        condition = new TranslatedWhereCondition(
            field,
            enumerable,
            isNegated ? QueryOperator.NotIn : QueryOperator.In,
            IsOr: false);
        return true;
    }

    private static bool TryGetMemberField(Expression expression, out string field)
    {
        expression = StripConvert(expression);

        if (expression is MemberExpression memberExpression &&
            memberExpression.Expression is ParameterExpression)
        {
            field = memberExpression.Member.Name;
            return true;
        }

        field = null;
        return false;
    }

    private static bool IsLambdaParameterReference(Expression expression, ParameterExpression parameter)
    {
        expression = StripConvert(expression);
        return expression == parameter;
    }

    private static bool TryEvaluateValue(Expression expression, out object value)
    {
        expression = StripConvert(expression);

        if (expression is ConstantExpression constantExpression)
        {
            value = constantExpression.Value;
            return true;
        }

        if (ContainsParameter(expression))
        {
            value = null;
            return false;
        }

        value = Expression.Lambda(expression).Compile().DynamicInvoke();
        return true;
    }

    private static bool ContainsParameter(Expression expression)
    {
        var visitor = new ParameterVisitor();
        visitor.Visit(expression);
        return visitor.Found;
    }

    private static Expression StripConvert(Expression expression)
    {
        while (expression is UnaryExpression unaryExpression &&
               (unaryExpression.NodeType == ExpressionType.Convert ||
                unaryExpression.NodeType == ExpressionType.ConvertChecked))
        {
            expression = unaryExpression.Operand;
        }

        return expression;
    }

    private static Expression StripQuote(Expression expression)
    {
        while (expression is UnaryExpression unaryExpression &&
               unaryExpression.NodeType == ExpressionType.Quote)
        {
            expression = unaryExpression.Operand;
        }

        return expression;
    }

    private sealed class ParameterVisitor : ExpressionVisitor
    {
        public bool Found { get; private set; }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Found = true;
            return base.VisitParameter(node);
        }
    }

    private static RawTranslationResult TranslateRaw(
        Expression expression,
        Func<string, string> resolveField,
        List<object> parameters)
    {
        expression = StripConvert(expression);

        if (expression is BinaryExpression binaryExpression)
        {
            if (binaryExpression.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
            {
                var precedence = GetPrecedence(binaryExpression.NodeType);
                var left = TranslateRaw(binaryExpression.Left, resolveField, parameters);
                var right = TranslateRaw(binaryExpression.Right, resolveField, parameters);
                var sql = $"{Parenthesize(left, precedence)} {(binaryExpression.NodeType == ExpressionType.AndAlso ? "AND" : "OR")} {Parenthesize(right, precedence)}";
                return new RawTranslationResult(sql, precedence);
            }

            if (TryTranslateRawComparison(binaryExpression, resolveField, parameters, out var comparisonSql))
            return new RawTranslationResult(comparisonSql, Precedence: 3);
        }

        if (TryTranslateRawBooleanMember(expression, resolveField, parameters, out var boolSql))
            return new RawTranslationResult(boolSql, Precedence: 3);

        if (TryTranslateRawMethodCall(expression, resolveField, parameters, isNegated: false, out var methodSql))
            return new RawTranslationResult(methodSql, Precedence: 3);

        if (expression is UnaryExpression unaryExpression &&
            unaryExpression.NodeType == ExpressionType.Not &&
            TryTranslateRawMethodCall(unaryExpression.Operand, resolveField, parameters, isNegated: true, out var negatedMethodSql))
        {
            return new RawTranslationResult(negatedMethodSql, Precedence: 3);
        }

        throw new NotSupportedException(
            "A expressão do WHERE não é suportada. Use comparações simples entre propriedades do modelo e valores, combinadas com && e ||.");
    }

    private static int GetPrecedence(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.OrElse => 1,
            ExpressionType.AndAlso => 2,
            _ => 3
        };
    }

    private static string Parenthesize(RawTranslationResult result, int parentPrecedence)
    {
        return result.Precedence < parentPrecedence ? $"({result.Sql})" : result.Sql;
    }

    private static bool TryTranslateRawComparison(
        BinaryExpression expression,
        Func<string, string> resolveField,
        List<object> parameters,
        out string sql)
    {
        if (TryGetMemberField(expression.Left, out var field) &&
            TryEvaluateValue(expression.Right, out var value))
        {
            sql = BuildRawComparison(resolveField(field), expression.NodeType, reverse: false, value, parameters);
            return true;
        }

        if (TryGetMemberField(expression.Right, out field) &&
            TryEvaluateValue(expression.Left, out value))
        {
            sql = BuildRawComparison(resolveField(field), expression.NodeType, reverse: true, value, parameters);
            return true;
        }

        sql = null;
        return false;
    }

    private static string BuildRawComparison(
        string field,
        ExpressionType nodeType,
        bool reverse,
        object value,
        List<object> parameters)
    {
        var escapedField = EscapeIdentifier(field);
        if (value == null)
        {
            return nodeType switch
            {
                ExpressionType.Equal => $"{escapedField} IS NULL",
                ExpressionType.NotEqual => $"{escapedField} IS NOT NULL",
                _ => throw new NotSupportedException("Somente comparações == null e != null são suportadas.")
            };
        }

        var index = AddParameter(parameters, value);
        var op = MapOperator(nodeType, reverse, value).ToSqlString();
        return $"{escapedField} {op} {{{index}}}";
    }

    private static bool TryTranslateRawBooleanMember(
        Expression expression,
        Func<string, string> resolveField,
        List<object> parameters,
        out string sql)
    {
        if (TryTranslateBooleanMember(expression, out var field, out var value))
        {
            var escapedField = EscapeIdentifier(resolveField(field));
            var index = AddParameter(parameters, value);
            sql = $"{escapedField} = {{{index}}}";
            return true;
        }

        sql = null;
        return false;
    }

    private static bool TryTranslateRawMethodCall(
        Expression expression,
        Func<string, string> resolveField,
        List<object> parameters,
        bool isNegated,
        out string sql)
    {
        expression = StripConvert(expression);

        if (expression is not MethodCallExpression methodCallExpression)
        {
            sql = null;
            return false;
        }

        if (TryTranslateRawStringPattern(methodCallExpression, resolveField, parameters, isNegated, out sql))
            return true;

        if (TryTranslateRawCollectionContains(methodCallExpression, resolveField, parameters, isNegated, out sql))
            return true;

        if (TryTranslateRawEnumerableAny(methodCallExpression, resolveField, parameters, isNegated, out sql))
            return true;

        sql = null;
        return false;
    }

    private static bool TryTranslateRawStringPattern(
        MethodCallExpression expression,
        Func<string, string> resolveField,
        List<object> parameters,
        bool isNegated,
        out string sql)
    {
        if (expression.Object == null ||
            expression.Object.Type != typeof(string) ||
            !TryGetMemberField(expression.Object, out var field) ||
            expression.Arguments.Count != 1 ||
            !TryEvaluateValue(expression.Arguments[0], out var rawValue))
        {
            sql = null;
            return false;
        }

        var stringValue = rawValue?.ToString() ?? string.Empty;
        var pattern = expression.Method.Name switch
        {
            nameof(string.Contains) => $"%{stringValue}%",
            nameof(string.StartsWith) => $"{stringValue}%",
            nameof(string.EndsWith) => $"%{stringValue}",
            _ => null
        };

        if (pattern == null)
        {
            sql = null;
            return false;
        }

        var index = AddParameter(parameters, pattern);
        sql = $"{EscapeIdentifier(resolveField(field))} {(isNegated ? "NOT LIKE" : "LIKE")} {{{index}}}";
        return true;
    }

    private static bool TryTranslateRawCollectionContains(
        MethodCallExpression expression,
        Func<string, string> resolveField,
        List<object> parameters,
        bool isNegated,
        out string sql)
    {
        if (!string.Equals(expression.Method.Name, nameof(string.Contains), StringComparison.Ordinal) ||
            expression.Arguments.Count == 0)
        {
            sql = null;
            return false;
        }

        Expression collectionExpression;
        Expression memberExpression;

        if (expression.Object != null && expression.Object.Type != typeof(string))
        {
            if (expression.Arguments.Count != 1)
            {
                sql = null;
                return false;
            }

            collectionExpression = expression.Object;
            memberExpression = expression.Arguments[0];
        }
        else
        {
            if (expression.Arguments.Count != 2)
            {
                sql = null;
                return false;
            }

            collectionExpression = expression.Arguments[0];
            memberExpression = expression.Arguments[1];
        }

        if (!TryGetMemberField(memberExpression, out var field) ||
            !TryEvaluateValue(collectionExpression, out var valuesObject) ||
            valuesObject is not IEnumerable enumerable ||
            valuesObject is string)
        {
            sql = null;
            return false;
        }

        var placeholderIndexes = new List<string>();
        foreach (var value in enumerable)
        {
            var index = AddParameter(parameters, value);
            placeholderIndexes.Add($"{{{index}}}");
        }

        sql = $"{EscapeIdentifier(resolveField(field))} {(isNegated ? "NOT IN" : "IN")} ({string.Join(", ", placeholderIndexes)})";
        return true;
    }

    private static bool TryTranslateRawEnumerableAny(
        MethodCallExpression expression,
        Func<string, string> resolveField,
        List<object> parameters,
        bool isNegated,
        out string sql)
    {
        if (!string.Equals(expression.Method.Name, "Any", StringComparison.Ordinal) ||
            expression.Arguments.Count != 2)
        {
            sql = null;
            return false;
        }

        var lambdaExpression = StripQuote(expression.Arguments[1]) as LambdaExpression;
        if (lambdaExpression == null || lambdaExpression.Parameters.Count != 1)
        {
            sql = null;
            return false;
        }

        if (!TryEvaluateValue(expression.Arguments[0], out var valuesObject) ||
            valuesObject is not IEnumerable enumerable ||
            valuesObject is string)
        {
            sql = null;
            return false;
        }

        // Optimization: ids.Any(x => x == v.Field) -> IN (...)
        if (TryTranslateEnumerableAny(expression, isNegated, out var condition))
        {
            var placeholderIndexes = new List<string>();
            foreach (var value in (IEnumerable)condition.Value)
            {
                var index = AddParameter(parameters, value);
                placeholderIndexes.Add($"{{{index}}}");
            }

            sql = $"{EscapeIdentifier(resolveField(condition.Field))} {(isNegated ? "NOT IN" : "IN")} ({string.Join(", ", placeholderIndexes)})";
            return true;
        }

        // General case: expand to OR of substituted predicates
        var parameter = lambdaExpression.Parameters[0];
        var clauses = new List<string>();

        foreach (var item in enumerable)
        {
            var constant = Expression.Constant(item, parameter.Type);
            var substituted = new ParameterReplaceVisitor(parameter, constant).Visit(lambdaExpression.Body);
            var translated = TranslateRaw(substituted, resolveField, parameters);
            clauses.Add(translated.Precedence < GetPrecedence(ExpressionType.OrElse)
                ? $"({translated.Sql})"
                : translated.Sql);
        }

        if (clauses.Count == 0)
        {
            sql = isNegated ? "1 = 1" : "1 = 0";
            return true;
        }

        var body = string.Join(" OR ", clauses);
        sql = isNegated ? $"NOT ({body})" : body;
        return true;
    }

    private sealed class ParameterReplaceVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;
        private readonly Expression _replacement;

        public ParameterReplaceVisitor(ParameterExpression parameter, Expression replacement)
        {
            _parameter = parameter;
            _replacement = replacement;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _parameter ? _replacement : base.VisitParameter(node);
        }
    }

    private static int AddParameter(List<object> parameters, object value)
    {
        parameters.Add(value);
        return parameters.Count - 1;
    }

    private static string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return identifier;

        if (identifier == "*")
            return "*";

        if (identifier.Contains("."))
        {
            var parts = identifier.Split('.');
            return string.Join(".", Array.ConvertAll(parts, part => part == "*" ? "*" : $"`{part.Replace("`", "``")}`"));
        }

        return $"`{identifier.Replace("`", "``")}`";
    }

    private readonly record struct RawTranslationResult(string Sql, int Precedence);
    internal readonly record struct TranslatedWhereCondition(
        string Field,
        object Value,
        QueryOperator Operator,
        bool IsOr);

    internal readonly record struct TranslatedRawWhereClause(string Sql, object[] Parameters);
}
