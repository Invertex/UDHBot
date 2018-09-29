using System.Reflection.Metadata;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot.Modules;
using DiscordBot.Services;
using DiscordBot.Tests.TestExtensions;
using Moq;
using Xunit;

namespace DiscordBot.Tests.Modules
{
    public class ModerationModuleTests
    {
        [Fact]
        public async Task IsUserKickedAsync()
        {
            var loggingMock = new Mock<ILoggingService>();
            var moderatorMock = new Mock<IUser>();
            var userMock = new Mock<IGuildUser>();
            var contextMock = new Mock<ICommandContext>();

            contextMock.Setup(c => c.User).Returns(moderatorMock.Object);

            var modModule = new ModerationModule(loggingMock.Object, null, null, null, null, null, null);
            modModule.SetContext(contextMock.Object);

            await modModule.KickUser(userMock.Object);
            userMock.Verify(user => user.KickAsync(It.IsAny<string>(), It.IsAny<RequestOptions>()), Times.Once);
        }

        [Fact]
        public async Task IsModeratorAndUserLoggedOnKickAsync()
        {
            var loggingMock = new Mock<ILoggingService>();
            var moderatorMock = new Mock<IUser>();
            var contextMock = new Mock<ICommandContext>();
            var userMock = new Mock<IGuildUser>();
            var expectedUser = "UserName";
            var expectedModerator = "ModeratorName";

            userMock.Setup(u => u.Username).Returns(expectedUser);
            moderatorMock.Setup(mod => mod.Username).Returns(expectedModerator);
            contextMock.Setup(c => c.User).Returns(moderatorMock.Object);

            var modModule = new ModerationModule(loggingMock.Object, null, null, null, null, null, null);
            modModule.SetContext(contextMock.Object);

            await modModule.KickUser(userMock.Object);
            loggingMock.Verify(
                mock => mock.LogAction(It.Is<string>(message => message.Contains(expectedModerator) && message.Contains(expectedUser)),
                    It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<Embed>()));
        }
    }
}
