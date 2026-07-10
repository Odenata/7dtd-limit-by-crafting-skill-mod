using System.IO;
using Xunit;

namespace LimitByCraftingSkillMod.Tests.Tools
{
    public class DeployToolTests
    {
        [Fact]
        public void DeployScript_Exists()
        {
            var scriptPath = TestDataPath.GetRepoFile(@"tools\deploy.ps1");
            Assert.True(File.Exists(scriptPath), "deploy.ps1 should exist");
        }

        [Fact]
        public void DeployScript_UsesOfficialTfpHarmony_NotBundledClassic()
        {
            var scriptPath = TestDataPath.GetRepoFile(@"tools\deploy.ps1");
            var content = File.ReadAllText(scriptPath);

            Assert.Contains("0_TFP_Harmony", content);
            Assert.Contains("LimitByCraftingSkillMod", content);
            Assert.DoesNotContain("7dtd-mod-dev-tools", content);
            Assert.DoesNotContain(@"third_party\harmony", content);
            Assert.DoesNotContain("Copied 0Harmony.dll", content);
        }
    }
}
