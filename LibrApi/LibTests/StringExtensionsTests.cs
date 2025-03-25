using ApiLib.Extensions;
using System.Diagnostics;

namespace ApiLib.Tests
{
    public class StringExtensionsTests
    {
        public class TestModel
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public DateTime BirthDate { get; set; }
        }

        [Fact]
        public void BuildFieldAccessExpression_InvalidProperty_ThrowsArgumentException()
        {
            var invalidProperty = "InvalidProperty";

            var ex = Assert.Throws<ArgumentException>(() =>
                invalidProperty.BuildFieldAccessExpression<TestModel>());

            Assert.Contains("not found on type", ex.Message);
        }

        [Theory]
        [InlineData("42", typeof(int), 42)]
        [InlineData("true", typeof(bool), true)]
        [InlineData("3.14", typeof(double), 3.14)]
        public void TryParseValue_ValidInput_ReturnsTrueAndCorrectValue(string input, Type targetType, object expected)
        {
            var success = input.TryParseValue(targetType, out var result);

            Assert.True(success);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TryParseValue_DateTimeValidFormat_ReturnsTrue()
        {
            var dateStr = "15-01-2023";

            var success = dateStr.TryParseValue(typeof(DateTime), out var result);

            Assert.True(success);
            Assert.Equal(new DateTime(2023, 1, 15), result);
        }

        [Fact]
        public void TryParseValue_InvalidInput_ReturnsFalse()
        {
            var invalidNumber = "not-a-number";

            var success = invalidNumber.TryParseValue(typeof(int), out _);

            Assert.False(success);
        }

        [Theory]
        [InlineData("[10,20]", typeof(int), 10, 20)]
        public void TryParseRange_ValidRange_ReturnsTrueAndCorrectBounds(
            string range, Type type, object expectedLower, object expectedUpper)
        {
            var success = range.TryParseRange(type, out var lower, out var upper);

            Assert.True(success);

            if (expectedLower is string dateStrLower && type == typeof(DateTime))
            {
                var expectedDate = DateTime.ParseExact(dateStrLower, "dd-MM-yyyy", null);
                Assert.Equal(expectedDate, lower);
            }
            else
            {
                Assert.Equal(expectedLower, lower);
            }

            if (expectedUpper is string dateStrUpper && type == typeof(DateTime))
            {
                var expectedDate = DateTime.ParseExact(dateStrUpper, "dd-MM-yyyy", null);
                Assert.Equal(expectedDate, upper);
            }
            else
            {
                Assert.Equal(expectedUpper, upper);
            }
        }

        [Fact]
        public void TryParseRange_InvalidFormat_ReturnsFalse()
        {
            var invalidRange = "10-20"; // Mauvais format

            var success = invalidRange.TryParseRange(typeof(int), out _, out _);

            Assert.False(success);
        }

        [Fact]
        public void TryParseRange_PartialValidRange_ReturnsPartialResults()
        {
            var partialRange = "[10,not-a-number]";

            var success = partialRange.TryParseRange(typeof(int), out var lower, out var upper);

            Assert.True(success); // Un des bounds est valide
            Assert.Equal(10, lower);
            Assert.Null(upper);
        }
    }
}