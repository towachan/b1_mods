
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using b1;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace EasyFengchuanhua
{
    public static class MyUtils
    {
        private static UWorld? world;

        public static UWorld? GetWorld()
        {
            if (world == null)
            {
                UObjectRef uobjectRef = GCHelper.FindRef(FGlobals.GWorld);
                world = uobjectRef?.Managed as UWorld;
            }
            return world;
        }

        public static APawn GetControlledPawn()
        {
            return UGSE_EngineFuncLib.GetFirstLocalPlayerController(GetWorld()).GetControlledPawn();
        }

        public static BGUPlayerCharacterCS GetBGUPlayerCharacterCS()
        {
            return (GetControlledPawn() as BGUPlayerCharacterCS)!;
        }

        public static BGP_PlayerControllerB1 GetPlayerController()
        {
            return (BGP_PlayerControllerB1)UGSE_EngineFuncLib.GetFirstLocalPlayerController(GetWorld());
        }

        public static BUS_GSEventCollection GetBUS_GSEventCollection()
        {
            return BUS_EventCollectionCS.Get(GetControlledPawn());
        }

        public static T LoadAsset<T>(string asset) where T : UObject
        {
            return b1.BGW.BGW_PreloadAssetMgr.Get(GetWorld()).TryGetCachedResourceObj<T>(asset, b1.BGW.ELoadResourceType.SyncLoadAndCache, b1.BGW.EAssetPriority.Default, null, -1, -1);
        }

        public static UClass LoadClass(string asset)
        {
            return LoadAsset<UClass>(asset);
        }

        public static AActor? SpawnActor(string classAsset)
        {
            var controlledPawn = GetControlledPawn();
            FVector actorLocation = controlledPawn.GetActorLocation();
            FVector b = controlledPawn.GetActorForwardVector() * 1000.0f;
            FVector start = actorLocation + b;
            FRotator frotator = UMathLibrary.FindLookAtRotation(start, actorLocation);
            UClass uClass = LoadClass($"PrefabricatorAsset'{classAsset}'");
            if (uClass == null)
            {
                return null;
            }
            return BGUFunctionLibraryCS.BGUSpawnActor(controlledPawn.World, uClass, start, frotator);
        }

        public static AActor GetActorOfClass(string classAsset)
        {
            return UGameplayStatics.GetActorOfClass(GetWorld(), LoadAsset<UClass>(classAsset));
        }


        public static void LogToFile(string message, string name = "EasyFengchuanhua")
        {
            string logDir = $"";
            string logfile = $"{logDir}\\{name}.log";
            if (!File.Exists(logfile))
            {
                Directory.CreateDirectory(logDir);
                File.Create(logfile);
            }
            TextWriter tw = new StreamWriter(logfile, true);
            tw.WriteLine(message);
            tw.Close();
            Console.WriteLine(message);
        }

        public static void Log(string message, string name = "EasyFengchuanhua")
        {
            string fullMessage = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}][{name}] {message}";

            Console.WriteLine(fullMessage);
            
            // write to file, for dev
            // LogToFile(message, name);
        }

        public static void logStackTrace(string name) {
            Log($"===========Stack trace for {name} start===========");
            StackTrace t = new StackTrace();
            StackFrame[] frames = t.GetFrames();
            for (int i = 0; i < frames.Length; i++)
            {
                Log($"{frames[i].GetMethod()} ({frames[i].GetFileLineNumber()})");
            }
            Log($"===========Stack trace for {name} end===========");
        }

        public static
        #nullable enable
        object? CallPrivateFunc(this
        #nullable disable
        object obj, string method_name, object[] paras)
        {
            MethodInfo method = obj.GetType().GetMethod(method_name, BindingFlags.Instance | BindingFlags.NonPublic);
            if ((object) method != null)
                return method.Invoke(obj, paras);
            Log("Fatal Error: Can't Find " + method_name);
            return (object) null;
        }
    }
}
