using HNTAS.Core.Api.Enums;

namespace HNTAS.Core.Api.Helpers
{
    public static class HeatNetworkHelper
    {
        public static string GetStageFromPhase(string phase)
        {
            return phase switch
            {
                "Feasibility" => "Concept design",
                "Design" => "Developed design, technical design",
                "Construction" => "Construction design, installation, commissioning",
                "Operational" => "Operation, maintenance, ongoing monitoring",
                _ => "NA"
            };
        }

        public static List<string> GetStagesForPhase(string phase)
        {
            if (phase == "Design")
            {
                return [SoaStage.Stage2.ToString(), SoaStage.Stage3.ToString(), SoaStage.Stage4.ToString(), SoaStage.Stage5.ToString(), SoaStage.Stage6.ToString(), SoaStage.Stage7.ToString()];
            }
            else if (phase == "Construction")
            {
                return [SoaStage.Stage3.ToString(), SoaStage.Stage4.ToString(), SoaStage.Stage5.ToString(), SoaStage.Stage6.ToString(), SoaStage.Stage7.ToString()];
            }
            else
            {
                return [SoaStage.Stage1.ToString(), SoaStage.Stage2.ToString(), SoaStage.Stage3.ToString(), SoaStage.Stage4.ToString(), SoaStage.Stage5.ToString(), SoaStage.Stage6.ToString(), SoaStage.Stage7.ToString()];
            }                
        }
    }
}
