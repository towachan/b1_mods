using b1;
using CSharpModBase;
using HarmonyLib;
using System.Reflection;
using CommB1;
using System.Collections.Generic;
using Google.Protobuf;
using System.Linq;
using SharpDX;


namespace EasyFengchuanhua
{
    [HarmonyPatch]
    public class EasyFengchuanhua : ICSharpMod
    {
        public string Name => nameof (EasyFengchuanhua);
        public string Version => "0.0.2";
        private readonly Harmony harmony;

        private static string FOCUS_LEVEL_0 = "棍势等级0";
        private static string FOCUS_LEVEL_1 = "棍势等级1";
        private static string FOCUS_LEVEL_2 = "棍势等级2";
        private static string FOCUS_LEVEL_3 = "棍势等级3";
        private static string FOCUS_LEVEL_4 = "棍势等级4";
        private static string FOCUS_LEVEL_5 = "棍势等级5";

        private static bool edgesCached;
        private static FCalliopeEdge Focus0Edge;
        private static FCalliopeEdge Focus1Edge;
        private static FCalliopeEdge Focus2Edge;
        private static FCalliopeEdge Focus3Edge;


        public EasyFengchuanhua()
        {
            harmony = new Harmony(Name);
            Harmony.DEBUG = false;
        }

        void resetFocusEdges()
        {
            Focus0Edge = null;
            Focus1Edge = null;
            Focus2Edge = null;
            Focus3Edge = null;
            edgesCached = false;
            MyUtils.Log("Edges reset.");
        }

        static void cacheFocusEdges(
            FCalliopeEdge value0,
            FCalliopeEdge value1,
            FCalliopeEdge value2,
            FCalliopeEdge value3
            )
        {
            if(!edgesCached)
            {
                Focus0Edge = value0;
                Focus1Edge = value1;
                Focus2Edge = value2;
                Focus3Edge = value3;
                edgesCached = true;
                MyUtils.Log("Edges cached.");
            }
        }

        void ICSharpMod.Init()
        {
            MyUtils.Log($"===================={Name} Init====================");
            
            // hook
            this.harmony.PatchAll(Assembly.GetExecutingAssembly());
            resetFocusEdges();
        }

        void ICSharpMod.DeInit()
        {           
            this.harmony.UnpatchAll();
            resetFocusEdges();
            MyUtils.Log($"===================={Name} DeInit====================");
        }

        private static T GetNodeCustomData<T>(FCalliopeNode Node) where T : IMessage, new()
        {
            T message = new T();
            if (Node.NodeData != null)
                message.MergeFrom(Node.NodeData);
            return message;
        }

        [HarmonyPatch(typeof(BUS_PlayerInputActionComp), "OnExecuteEdge")]
        [HarmonyPrefix]
        private static bool PrintOnExecuteEdge(ref BUS_PlayerInputActionComp __instance, FCalliopeEdge Edge)
        {
 
            MyUtils.Log($"OnExecuteEdge: {Edge.From.NodeGuid} --> {Edge.To.NodeGuid}");
            List<string> toKeys = new List<string>(Edge.To.OutputEdges.Keys);
                

            if (toKeys.Contains(FOCUS_LEVEL_0)
                && toKeys.Contains(FOCUS_LEVEL_1)
                && toKeys.Contains(FOCUS_LEVEL_2)
                && toKeys.Contains(FOCUS_LEVEL_3)
                && toKeys.Contains(FOCUS_LEVEL_4)
                && toKeys.Contains(FOCUS_LEVEL_5))
            {
                toKeys.ForEach(key =>
                {
                    MyUtils.Log($"ToNode outputEdge's to: {key} / {Edge.To.OutputEdges[key].To.NodeGuid.ToString()}");
                });

                FCalliopeEdge focus4Edge = Edge.To.OutputEdges[FOCUS_LEVEL_4];

                List<string> nodeKeys = new List<string>(focus4Edge.To.OutputEdges.Keys);


                if (nodeKeys.Contains("凤穿花窗口"))
                {
                    MyUtils.Log("Found Fengchuanhua !");

                    cacheFocusEdges(
                        Edge.To.OutputEdges[FOCUS_LEVEL_0],
                        Edge.To.OutputEdges[FOCUS_LEVEL_1],
                        Edge.To.OutputEdges[FOCUS_LEVEL_2],
                        Edge.To.OutputEdges[FOCUS_LEVEL_3]
                        );

                    List<CalliopeCustom_ComboCondition> list = GetNodeCustomData<ComboCustom_Start>(Edge.To).ComboConditions.ToList<CalliopeCustom_ComboCondition>();

                    CalliopeCustom_ComboCondition? matched = null;
                    for (int index = 0; index < list.Count; ++index) {
                        CalliopeCustom_ComboCondition ComboCondition = list[index];
                        bool result = false;
                        if (__instance != null)
                        {
                            #pragma warning disable CS8605
                            result = (bool) __instance.CallPrivateFunc("IsConditionSuccess", [ComboCondition]);
                            #pragma warning restore CS8605 
                        }

                        if(result)
                        {
                            MyUtils.Log($"ComboCondition matched: {ComboCondition.ConditionIdentity}");
                            matched = ComboCondition;
                            break;
                        }
                    }
                    if(matched != null)
                    {
                        CalliopeCustom_ComboCondition dodgeWin = new CalliopeCustom_ComboCondition();
                        dodgeWin.UnitState = (int) EBGUUnitState.InDodgeWindow;

                        #pragma warning disable CS8605
                        bool dodgeState = (bool)__instance.CallPrivateFunc("OnCheckUnitState", [dodgeWin]);
                        #pragma warning restore CS8605

                        MyUtils.Log($"dodgeState: {dodgeState}");

                        if(!dodgeState || matched.ConditionIdentity == FOCUS_LEVEL_4 || matched.ConditionIdentity == FOCUS_LEVEL_5)
                        {
                            MyUtils.Log($"Focus level is greater than 4 or not in dodge - skip patch edge.");
                            Edge.To.OutputEdges[FOCUS_LEVEL_0] = Focus0Edge;
                            Edge.To.OutputEdges[FOCUS_LEVEL_1] = Focus1Edge;
                            Edge.To.OutputEdges[FOCUS_LEVEL_2] = Focus2Edge;
                            Edge.To.OutputEdges[FOCUS_LEVEL_3] = Focus3Edge;
                        } else
                        {
                            Edge.To.OutputEdges[FOCUS_LEVEL_0] = Edge.To.OutputEdges[FOCUS_LEVEL_4];
                            Edge.To.OutputEdges[FOCUS_LEVEL_1] = Edge.To.OutputEdges[FOCUS_LEVEL_4];
                            Edge.To.OutputEdges[FOCUS_LEVEL_2] = Edge.To.OutputEdges[FOCUS_LEVEL_4];
                            Edge.To.OutputEdges[FOCUS_LEVEL_3] = Edge.To.OutputEdges[FOCUS_LEVEL_4];
                        }

                    }

                }
            }


            return true;
        }

    }
}
