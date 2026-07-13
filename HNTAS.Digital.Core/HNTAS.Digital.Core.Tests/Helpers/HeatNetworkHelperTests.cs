using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;

namespace HNTAS.Digital.Core.Tests.Helpers
{
    public class HeatNetworkHelperTests
    {
        [Theory]
        [InlineData("Feasibility", "Concept design")]
        [InlineData("Design", "Developed design, technical design")]
        [InlineData("Construction", "Construction design, installation, commissioning")]
        [InlineData("Operational", "Operation, maintenance, ongoing monitoring")]
        [InlineData("Unknown", "NA")]
        public void GetStageFromPhase_ReturnsExpectedValue(string phase, string expected)
        {
            // Act
            var result = HeatNetworkHelper.GetStageFromPhase(phase);

            // Assert
            Assert.Equal(expected, result);
        }


        [Fact]
        public void GetStagesForPhase_WhenDesign_ReturnsStages2To7()
        {
            // Act
            var result = HeatNetworkHelper.GetStagesForPhase("Design");

            // Assert
            Assert.Equal(
                new[]
                {
                    SoaStage.Stage2.ToString(),
                    SoaStage.Stage3.ToString(),
                    SoaStage.Stage4.ToString(),
                    SoaStage.Stage5.ToString(),
                    SoaStage.Stage6.ToString(),
                    SoaStage.Stage7.ToString()
                },
                result);
        }

        [Fact]
        public void GetStagesForPhase_WhenConstruction_ReturnsStages3To7()
        {
            // Act
            var result = HeatNetworkHelper.GetStagesForPhase("Construction");

            // Assert
            Assert.DoesNotContain(SoaStage.Stage1.ToString(), result);
            Assert.Equal(5, result.Count);
        }

        [Fact]
        public void GetStagesForPhase_WhenOtherPhase_ReturnsAllStages()
        {
            // Act
            var result = HeatNetworkHelper.GetStagesForPhase("Other");

            // Assert
            Assert.Equal(7, result.Count);
            Assert.Contains(SoaStage.Stage1.ToString(), result);
        }


        [Theory]
        [InlineData(nameof(ElementTypeInShort.EC), "Energy centre")]
        [InlineData(nameof(ElementTypeInShort.SS), "Substation")]
        [InlineData(nameof(ElementTypeInShort.DDN), "District distribution network")]
        [InlineData(nameof(ElementTypeInShort.CC), "Consumer connections")]
        [InlineData(nameof(ElementTypeInShort.CDN), "Communal distribution network")]
        public void GetNetworkElementLabelByElementId_ReturnsExpectedLabel(
            string elementType,
            string expected)
        {
            // Act
            var result = HeatNetworkHelper.GetNetworkElementLabelByElementId(elementType);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetNetworkElementLabelByElementId_Throws_WhenInvalidValue()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                HeatNetworkHelper.GetNetworkElementLabelByElementId("INVALID"));

            Assert.Contains("Not expected heat network element type value", ex.Message);
        }
    }
}

