using DiscordBot.Extensions;
using Xunit;

namespace DiscordBot.Tests.Extensions
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("Foo", "Foo\n")]
        [InlineData("", "\n")]
        [InlineData("Line1\nLine2\nLine3", "Line1\nLine2\nLine3\n")]
        public void MessageSplitSingleMessage(string input, params string[] expected)
        {
            var actual = input.MessageSplit().ToArray();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("Line1\nLine2\nLine3", "Line1\n", "Line2\n","Line3\n")]
        public void MessageSplitMultipleMessages(string input, params string[] expected)
        {
            var actual = input.MessageSplit(6).ToArray();
            Assert.Equal(expected, actual);
        }
    }
}
