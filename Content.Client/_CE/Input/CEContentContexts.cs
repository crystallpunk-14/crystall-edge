using Content.Shared._CE.Input;
using Robust.Shared.Input;

namespace Content.Client._CE.Input
{
    public static class CEContentContexts
    {
        public static void SetupContexts(IInputContextContainer contexts)
        {
            var human = contexts.GetContext("human");
            human.AddFunction(CEContentKeyFunctions.OpenBelt2);
            human.AddFunction(CEContentKeyFunctions.SmartEquipBelt2);
            human.AddFunction(CEContentKeyFunctions.OpenSkillMenu);
        }
    }
}
