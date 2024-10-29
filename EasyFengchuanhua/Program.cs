using b1;
using CSharpModBase;
using HarmonyLib;
using System.Reflection;
using CommB1;
using System.Collections.Generic;
using Google.Protobuf;
using System.Linq;
using System;
using BtlShare;
using System.Collections;

namespace EasyFengchuanhua
{
    [HarmonyPatch]
    public class EasyFengchuanhua : ICSharpMod
    {
        public string Name => nameof (EasyFengchuanhua);
        public string Version => "0.0.4";
        private readonly Harmony harmony;

        private static string FOCUS_LEVEL_0 = "棍势等级0";
        private static string FOCUS_LEVEL_1 = "棍势等级1";
        private static string FOCUS_LEVEL_2 = "棍势等级2";
        private static string FOCUS_LEVEL_3 = "棍势等级3";
        private static string FOCUS_LEVEL_4 = "棍势等级4";
        private static string FOCUS_LEVEL_5 = "棍势等级5";

        private static string CCG_PATH = "/Game/00Main/Design/Combo/CCG_Player_Wukong.CCG_Player_Wukong";
        private static string CCG_NAME = "CCG_Player_Wukong";
        private static string FENGCHUANHUA_WIN = "凤穿花窗口";

        private static bool ready = false;
        private static bool doubleCheck = false;

        private static Dictionary<Guid, FCalliopeNode> focusNodes { get; } = new Dictionary<Guid, FCalliopeNode>() ;
        private static FCalliopeEdge fengchuanhuaEdge = null;
        private static List<Guid> nodesContainFengchuanhua = new List<Guid>();

        private static Queue<EInputActionType> inputQueue = new Queue<EInputActionType>() ;

        public EasyFengchuanhua()
        {
            harmony = new Harmony(Name);
            Harmony.DEBUG = false;
        }

        void ICSharpMod.Init()
        {
            MyUtils.Log($"===================={Name} Init====================");
            
            // hook
            this.harmony.PatchAll(Assembly.GetExecutingAssembly());
            ready = cacheNodesAndEdge();
        }

        void ICSharpMod.DeInit()
        {           
            this.harmony.UnpatchAll();
            fengchuanhuaEdge = null;
            nodesContainFengchuanhua = new List<Guid>();
            focusNodes.Clear();
            ready = false;
            MyUtils.Log($"===================={Name} DeInit====================");
        }

        static bool cacheNodesAndEdge()
        {   
            FCalliopeGraph graph = BGW_CalliopeDataReader.Get().LoadGraphByAssetPath(CCG_PATH, CCG_NAME, false);

            graph.Nodes.ForEach(node =>
            {
                List<string> keys = new List<string>(node.OutputEdges.Keys);

                //printNodeInfo(node);
                if (keys.Contains(FOCUS_LEVEL_0) &&
                    keys.Contains(FOCUS_LEVEL_1) &&
                    keys.Contains(FOCUS_LEVEL_2) &&
                    keys.Contains(FOCUS_LEVEL_3) &&
                    keys.Contains(FOCUS_LEVEL_4) &&
                    (node.OutputEdges.Keys.Contains(FOCUS_LEVEL_5)
                        // special handling for Dasheng mode
                        || !node.InputEdges[node.InputEdges.Count - 1].From.OutputEdges.Keys.Contains("EAttackHeavyRelease"))
                    )
                {
                    if (node.OutputEdges[FOCUS_LEVEL_4].To.OutputEdges.Keys.Contains(FENGCHUANHUA_WIN))
                    {
                        nodesContainFengchuanhua.Add(node.NodeGuid);
                        if (fengchuanhuaEdge == null)
                        {
                            fengchuanhuaEdge = node.OutputEdges[FOCUS_LEVEL_4];
                            MyUtils.Log($"fengchuanhua edge in {node.NodeGuid} cached.");
                        }
                    }
                    
                    focusNodes[node.NodeGuid] = node;
                    MyUtils.Log($"Node [{node.NodeGuid}] with output keys: [{string.Join(",", node.OutputEdges.Keys)}] is cached.");
                };
            });

            if (focusNodes.Count > 0 && fengchuanhuaEdge != null)
            {
                MyUtils.Log($"[{focusNodes.Count}] focus nodes and fengchuanhua edge cached.");
                return true;
            }
            return false;
        }

        private static T GetNodeCustomData<T>(FCalliopeNode Node) where T : IMessage, new()
        {
            T message = new T();
            if (Node.NodeData != null)
                message.MergeFrom(Node.NodeData);
            return message;
        }

        private static bool compareEdge(FCalliopeEdge left, FCalliopeEdge right)
        {
            return left.To.NodeGuid.Equals(right.To.NodeGuid);
        }

        private static bool checkInputQueue()
        {
            bool inputQueueIsCorrect = false;

            if (inputQueue.Count >= 3)
            {
                MyUtils.Log($"input queue is [{string.Join(",", inputQueue)}]");
                if (string.Join(",", inputQueue).EndsWith($"Dodge,HeavyAttack,HeavyAttack"))
                {
                    inputQueueIsCorrect = true;
                    inputQueue.Clear();
                }
            }

            return inputQueueIsCorrect;
        }

        private static void printNodeInfo(FCalliopeNode node)
        {
            List<string> keys = new List<string>(node.OutputEdges.Keys);
            if(keys.Any(key => key.StartsWith("棍势"))) {
                MyUtils.Log($"Node is [{node.NodeGuid}] with edges [{string.Join(",", node.OutputEdges.Keys)}]");
            }
        }

        [HarmonyPatch(typeof(BUS_PlayerInputActionComp), "OnInputCastSkill")]
        [HarmonyPrefix]
        private static bool BeforeOnInputCastSkill(EInputActionType InputActionType)
        {
            inputQueue.Enqueue(InputActionType);

            if (inputQueue.Count > 5)
            {
                inputQueue.Dequeue();
            }

            return true;
        }

        [HarmonyPatch(typeof(BUS_PlayerInputActionComp), "OnExecuteEdge")]
        [HarmonyPrefix]
        private static bool BeforeOnExecuteEdge(ref BUS_PlayerInputActionComp __instance, FCalliopeEdge Edge)
        {
            if(ready)
            {
                FCalliopeNode nextNode = Edge.To;
                //printNodeInfo(nextNode);

                if (focusNodes.ContainsKey(nextNode.NodeGuid)) {

                    MyUtils.Log($"Next node [{nextNode.NodeGuid}] found in cache.");
                    
                    List<CalliopeCustom_ComboCondition> list = GetNodeCustomData<ComboCustom_Start>(nextNode).ComboConditions.ToList<CalliopeCustom_ComboCondition>();

                    string? matchedFocus = null;
                    CalliopeCustom_ComboCondition matchedComboCondition = null;
                    for (int index = 0; index < list.Count; ++index)
                    {
                        CalliopeCustom_ComboCondition ComboCondition = list[index];
                        bool result = result = (bool)__instance.CallPrivateFunc("IsConditionSuccess", [ComboCondition]);
                        if (result)
                        {
                            MyUtils.Log($"ComboCondition matched: [{ComboCondition.ConditionIdentity}]");
                            matchedFocus = ComboCondition.ConditionIdentity;
                            matchedComboCondition = ComboCondition;
                            break;
                        }
                    }
                        
                    if (matchedFocus != null)
                    {
                        if(matchedFocus == FOCUS_LEVEL_5 || (matchedFocus == FOCUS_LEVEL_4 && nodesContainFengchuanhua.Contains(nextNode.NodeGuid)))
                        {
                            MyUtils.Log($"Focus level is > 4 or is thrust node with focus level 4 - skip patch edge.");
                        } else
                        {
                            CalliopeCustom_ComboCondition condition = new CalliopeCustom_ComboCondition();

                            condition.UnitState = (int)EBGUUnitState.InDodgeWindow;
                            bool dodgeState = (bool)__instance.CallPrivateFunc("OnCheckUnitState", [condition]);
                            MyUtils.Log($"dodgeState is [{dodgeState}].");


                            if (!dodgeState || !checkInputQueue())
                            {
                                if (!compareEdge(
                                    nextNode.OutputEdges[matchedFocus],
                                    focusNodes[nextNode.NodeGuid].OutputEdges[matchedFocus]))
                                {
                                    MyUtils.Log($"Not in dodge window or input is not matched - fallback patch.");
                                    nextNode.OutputEdges[matchedFocus] = focusNodes[nextNode.NodeGuid].OutputEdges[matchedFocus];
                                }
                            }
                            else if (!compareEdge(nextNode.OutputEdges[matchedFocus], fengchuanhuaEdge))
                            {
                                MyUtils.Log($"Fengchuanhua patched !");
                                nextNode.OutputEdges[matchedFocus] = fengchuanhuaEdge;
                            }
                        }
                    }

                }
            }

            return true;
        }

    }
}
