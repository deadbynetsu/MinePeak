using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using PeakCraft.Patches;
using UnityEngine;

static class Program
{
    private static int passed;
    private static readonly MethodInfo ForceMethod=AccessTools.Method(typeof(Character),"AddForce",new[] {typeof(Vector3),typeof(float),typeof(float)});
    static void Main()
    {
        Test("only collision and its generated bounce coroutine are selected", () =>
        {
            var targets=((IEnumerable<MethodBase>)Invoke("TargetMethods")).ToArray();
            Check(targets.Length==2 && targets.Any(m=>m.Name=="Collide") && targets.Any(m=>m.Name=="MoveNext"));
            Check(!targets.Any(m=>m.DeclaringType!.Name.Contains("Unrelated",StringComparison.Ordinal)));
        });
        Test("force calls are wrapped without changing unrelated calls or overloads", () =>
        {
            var other=AccessTools.Method(typeof(Character),"AddForce",new[] {typeof(object)});
            var native=new CodeInstruction(OpCodes.Call,ForceMethod); var unrelated=new CodeInstruction(OpCodes.Call,other);
            var result=Rewrite(native,unrelated);
            Check(result.Length==2 && result[0].operand is MethodInfo method && method.Name=="ApplyForce");
            Check(Equals(result[1].operand,other));
        });
        Test("native branch targets and exception boundaries remain attached to the call", () =>
        {
            var native=new CodeInstruction(OpCodes.Call,ForceMethod); native.labels.Add("bounce branch"); native.blocks.Add("protected region end");
            var result=Rewrite(native);
            Check(ReferenceEquals(result[0],native) && native.labels.Single()=="bounce branch" && native.blocks.Single()=="protected region end");
        });
        Test("a validated native impulse retains physics before forwarding exactly once", () =>
        {
            var c=new Character(); var force=new Vector3(2,7,-3);
            NativeFlightPatch.OnRetain=(movement,time)=> { Check(ReferenceEquals(movement,c.refs.movement) && time==0); c.Events.Add("retain"); };
            Invoke("ApplyForce",c,force,.25f,1.75f);
            Check(c.Calls==1 && c.Force==force && c.Min==.25f && c.Max==1.75f);
            Check(string.Join(',',c.Events)=="retain,native force");
        });
        Test("zero and invalid vectors cannot acquire physics but remain native calls", () =>
        {
            NativeFlightPatch.OnRetain=(_,_)=>throw new Exception("should not retain");
            int warnings=PeakCraft.Plugin.Log.Warnings;
            foreach(var force in new[] {default(Vector3),new Vector3(float.NaN,0,0),new Vector3(float.PositiveInfinity,0,0)})
            {
                var c=new Character(); Invoke("ApplyForce",c,force,1f,1f); Check(c.Calls==1);
            }
            Check(PeakCraft.Plugin.Log.Warnings==warnings);
        });
        Test("retention failure does not erase or duplicate the original force", () =>
        {
            var c=new Character(); int warnings=PeakCraft.Plugin.Log.Warnings;
            NativeFlightPatch.OnRetain=(_,_)=>throw new InvalidOperationException("test handoff failure");
            Invoke("ApplyForce",c,new Vector3(1,0,0),1f,1f);
            Check(c.Calls==1 && PeakCraft.Plugin.Log.Warnings==warnings+1);
        });
        Test("changed native code without a known force call is refused visibly", () =>
        {
            bool rejected=false;
            try { Rewrite(new CodeInstruction(OpCodes.Nop,new object())); }
            catch(InvalidOperationException) { rejected=true; }
            Check(rejected);
        });
        Test("both instance-call opcodes keep the same argument stack contract", () =>
        {
            var result=Rewrite(new CodeInstruction(OpCodes.Callvirt,ForceMethod));
            var bridge=(MethodInfo)result[0].operand;
            Check(result[0].opcode==OpCodes.Call && bridge.IsStatic && bridge.ReturnType==typeof(void));
            Check(bridge.GetParameters().Select(p=>p.ParameterType).SequenceEqual(new[] {typeof(Character),typeof(Vector3),typeof(float),typeof(float)}));
        });
        Console.WriteLine($"PASS: {passed} native hazard impulse bridge tests");
    }
    static CodeInstruction[] Rewrite(params CodeInstruction[] instructions)
        => ((IEnumerable<CodeInstruction>)Invoke("Transpiler",instructions,AccessTools.Method(typeof(CollisionModifier),"Collide"))).ToArray();
    static object Invoke(string method,params object[] arguments)
    {
        try { return typeof(NativeHazardImpulsePatch).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,arguments)!; }
        catch(TargetInvocationException error) when(error.InnerException!=null) { throw error.InnerException; }
    }
    static void Test(string name,Action run) { NativeFlightPatch.OnRetain=null; run(); passed++; Console.WriteLine("PASS: "+name); }
    static void Check(bool valid) { if(!valid)throw new Exception("Native hazard force bridge regression"); }
}
