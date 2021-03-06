﻿using System;
using DeepEqual.Syntax;
using EsiNet.Expressions;
using SharpTestsEx;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Expressions
{
    public class ExpressionParserTests
    {
        private readonly ITestOutputHelper _output;

        public ExpressionParserTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("$(HTTP_HOST)=='example.com'")]
        [InlineData("  $(HTTP_HOST)  ==  'example.com'  ")]
        [InlineData("$( HTTP_HOST )=='example.com'")]
        [InlineData("($(HTTP_HOST)=='example.com') ")]
        public void Compare_variable_to_constant(string input)
        {
            var expression = ExpressionParser.Parse(input);
            expression.ShouldDeepEqual(
                new ComparisonExpression(
                    new SimpleVariableExpression("HTTP_HOST"),
                    new ConstantExpression("example.com"),
                    ComparisonOperator.Equal,
                    BooleanOperator.And));
        }

        [Fact]
        public void Compare_variable_to_variable()
        {
            var input = "$(HTTP_HOST) == $(HTTP_REFERER)";
            var expression = ExpressionParser.Parse(input);
            expression.ShouldDeepEqual(
                new ComparisonExpression(
                    new SimpleVariableExpression("HTTP_HOST"),
                    new SimpleVariableExpression("HTTP_REFERER"),
                    ComparisonOperator.Equal,
                    BooleanOperator.And));
        }

        [Fact]
        public void Compare_constant_to_constant()
        {
            var input = "'a' == 'b'";
            var expression = ExpressionParser.Parse(input);
            expression.ShouldDeepEqual(
                new ComparisonExpression(
                    new ConstantExpression("a"),
                    new ConstantExpression("b"),
                    ComparisonOperator.Equal,
                    BooleanOperator.And));
        }

        [Theory]
        [InlineData(@"$(X) == 'a\'b'", "a'b")]
        [InlineData(@"$(X) == 'a\\b'", "a\\b")]
        [InlineData(@"$(X) == ' \b '", " \b ")]
        [InlineData(@"$(X) == ' \f '", " \f ")]
        [InlineData(@"$(X) == ' \n '", " \n ")]
        [InlineData(@"$(X) == ' \r '", " \r ")]
        [InlineData(@"$(X) == ' \u1120 '", " \u1120 ")]
        public void Compare_with_special_characters(string input, string expectedConstantValue)
        {
            var expression = (ComparisonExpression) ExpressionParser.Parse(input);
            ((ConstantExpression) expression.Right).Value.Should().Be.EqualTo(expectedConstantValue);
        }

        [Theory]
        [InlineData(@"$(X) == 'x'", ComparisonOperator.Equal)]
        [InlineData(@"$(X) != 'x'", ComparisonOperator.NotEqual)]
        [InlineData(@"$(X) >  'x'", ComparisonOperator.GreaterThan)]
        [InlineData(@"$(X) >= 'x'", ComparisonOperator.GreaterThanOrEqual)]
        [InlineData(@"$(X) <  'x'", ComparisonOperator.LessThan)]
        [InlineData(@"$(X) <= 'x'", ComparisonOperator.LessThanOrEqual)]
        [InlineData(@"$(X)>'x'", ComparisonOperator.GreaterThan)]
        [InlineData(@"$(X)>='x'", ComparisonOperator.GreaterThanOrEqual)]
        public void Compare_with_operators(string input, ComparisonOperator expectedComparisonOperator)
        {
            var expression = (ComparisonExpression) ExpressionParser.Parse(input);
            expression.ComparisonOperator.Should().Be.EqualTo(expectedComparisonOperator);
        }

        [Theory]
        [InlineData("$(HTTP_HOST) == example.com", 16)]
        [InlineData("$HTTP_HOST) == ''", 1)]
        [InlineData("$(HTTP_HOST == ''", 12)]
        [InlineData("$(HTTP_HOST) == '''", 18)]
        [InlineData("$(HTTP_HOST) <> ''", 14)]
        [InlineData("$(HTTP_HOST) : ''", 13)]
        [InlineData("€(HTTP_HOST) == ''", 0)]
        [InlineData("$(HTTP_HOST) == \"\"", 16)]
        [InlineData("$[HTTP_HOST) == ''", 1)]
        [InlineData("$(HTTP_HOST] == ''", 11)]
        [InlineData("$() == ''", 2)]
        [InlineData("$( ) == ''", 3)]
        [InlineData("$(HTTP_HOST) == '\"", 18)]
        [InlineData("$(HTTP_HOST) == '", 17)]
        [InlineData("$(HTTP_HOST) == '\\x'", 18)]
        [InlineData("$(HTTP_HOST) == '\\uXXXX'", 19)]
        [InlineData("", 0)]
        [InlineData("$(HTTP_HOST) == '')", 18)]
        public void Invalid_expression(string input, int position)
        {
            var exception = Record.Exception(() => ExpressionParser.Parse(input));

            var expected =
                $"Unexpected character at position {position}" + Environment.NewLine +
                input + Environment.NewLine +
                new string(' ', position) + '\u21D1';
            exception.Should().Be.InstanceOf<InvalidExpressionException>();
            Pad(exception.Message).Should().Be.EqualTo(Pad(expected));

            _output.WriteLine(Pad(exception.Message));

            string Pad(string s) =>
                Environment.NewLine + Environment.NewLine +
                s +
                Environment.NewLine + Environment.NewLine;
        }

        [Theory]
        [InlineData("$(HTTP_HOST)=='example.com' || $(HTTP_REFERER)=='http://example.com'", BooleanOperator.Or)]
        [InlineData("$(HTTP_HOST)=='example.com' && $(HTTP_REFERER)=='http://example.com'", BooleanOperator.And)]
        [InlineData("$(HTTP_HOST)=='example.com' | $(HTTP_REFERER)=='http://example.com'", BooleanOperator.Or)]
        [InlineData("$(HTTP_HOST)=='example.com' & $(HTTP_REFERER)=='http://example.com'", BooleanOperator.And)]
        public void Compare_multiple_boolean_expressions(string input, BooleanOperator expectedOperator)
        {
            var expression = ExpressionParser.Parse(input);

            expression.ShouldDeepEqual(
                new GroupExpression(new[]
                {
                    new ComparisonExpression(
                        new SimpleVariableExpression("HTTP_HOST"),
                        new ConstantExpression("example.com"),
                        ComparisonOperator.Equal,
                        BooleanOperator.And),
                    new ComparisonExpression(
                        new SimpleVariableExpression("HTTP_REFERER"),
                        new ConstantExpression("http://example.com"),
                        ComparisonOperator.Equal,
                        expectedOperator)
                }, BooleanOperator.And));
        }

        [Theory]
        [InlineData("$(a)=='1' && ($(b)=='2' || $(b)=='3')")]
        [InlineData("($(a)=='1') && ($(b)=='2' || $(b)=='3')")]
        [InlineData("($(a)=='1' && ($(b)=='2' || $(b)=='3'))")]
        [InlineData("$(a)=='1' && (($(b)=='2' || $(b)=='3'))")]
        [InlineData("$(a)=='1' && (($(b)=='2') || ($(b)=='3'))")]
        [InlineData("(($(a)=='1') && ((($(b)=='2') || ($(b)=='3'))))")]
        [InlineData(" ( ( $(a)=='1' ) && ( ( ( $(b)=='2' ) || ( $(b)=='3' ) ) ) ) ")]
        public void Compare_with_groups(string input)
        {
            var expression = ExpressionParser.Parse(input);

            expression.ShouldDeepEqual(
                new GroupExpression(new IBooleanExpression[]
                {
                    new ComparisonExpression(
                        new SimpleVariableExpression("a"),
                        new ConstantExpression("1"),
                        ComparisonOperator.Equal,
                        BooleanOperator.And),
                    new GroupExpression(new []
                    {
                        new ComparisonExpression(
                            new SimpleVariableExpression("b"),
                            new ConstantExpression("2"),
                            ComparisonOperator.Equal,
                            BooleanOperator.And),
                        new ComparisonExpression(
                            new SimpleVariableExpression("b"),
                            new ConstantExpression("3"),
                            ComparisonOperator.Equal,
                            BooleanOperator.Or),
                    }, BooleanOperator.And), 
                }, BooleanOperator.And));
        }

        [Theory]
        [InlineData("$(HTTP_COOKIE{showPricesWithVat})=='true'")]
        [InlineData("$(HTTP_COOKIE{showPricesWithVat}) == 'true'")]
        [InlineData("$( HTTP_COOKIE{showPricesWithVat} )=='true'")]
        [InlineData("$(HTTP_COOKIE {showPricesWithVat})=='true'")]
        public void Compare_dictionary_variable_to_constant(string input)
        {
            var expression = ExpressionParser.Parse(input);
            expression.ShouldDeepEqual(
                new ComparisonExpression(
                    new DictionaryVariableExpression("HTTP_COOKIE", "showPricesWithVat"),
                    new ConstantExpression("true"),
                    ComparisonOperator.Equal,
                    BooleanOperator.And));
        }
    }
}