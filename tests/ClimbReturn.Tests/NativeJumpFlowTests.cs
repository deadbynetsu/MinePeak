using System;
using PeakCraft;
using PeakCraft.Link;
using PeakCraft.Player;
using UnityEngine;
using UnityEngine.InputSystem;

internal static class NativeJumpFlowTests
{
    private const uint Ready = (uint)(Proto.GuestFlags.InWorld | Proto.GuestFlags.MovementReady);

    public static void Run(Action<string, Action> test)
    {
        test("an ordinary ground jump stays in Minecraft and arms only the real passive allowance", () =>
        {
            var (jump,c,g) = Setup();
            Keyboard.current[Key.Space].isPressed = true;
            jump.UpdateRequest(true,false,c,g,Owner.MinecraftOwns);
            Check(!jump.WantsNative && c.data.extraJumps == 1 && c.data.sinceJump == 0);
        });
        test("holding Space across a Minecraft ground jump cannot trigger a bonus jump", () =>
        {
            var (jump,c,g) = Setup();
            Keyboard.current[Key.Space].isPressed = true;
            jump.UpdateRequest(true,false,c,g,Owner.MinecraftOwns);
            g.flags = Ready; c.data.sinceJump = 1;
            jump.UpdateRequest(true,false,c,g,Owner.MinecraftOwns);
            Check(!jump.WantsNative);
        });
        test("an airborne second press hands off, restores velocity and calls native jump once", () =>
        {
            var (jump,c,g) = Setup();
            g.flags = Ready; g.tickMs = 50; g.curX=0.2; g.curY=0.1; g.curZ=0.15;
            Press(jump,c,g);
            Check(jump.WantsNative);
            jump.TickNative(c);
            Check(c.refs.movement.Attempts==1 && c.data.extraJumps==0 && c.data.jumpsRemaining==0);
            var v=c.refs.ragdoll.partList[0].Rig.linearVelocity;
            Check(Math.Abs(v.x-4)<0.0001 && Math.Abs(v.y-2)<0.0001 && Math.Abs(v.z+3)<0.0001);
            jump.TickNative(c); Check(c.refs.movement.Attempts==1);
        });
        test("native extra jump retains ownership through charge and flight until settled landing", () =>
        {
            var (jump,c,g)=Setup(); g.flags=Ready; Press(jump,c,g); jump.TickNative(c);
            Time.fixedTime=1; jump.TickNative(c); Check(jump.WantsNative);
            c.data.chargingJump=false; c.data.isGrounded=false;
            Time.fixedTime=2; jump.TickNative(c); Check(jump.WantsNative);
            c.data.isGrounded=true; c.data.avarageVelocity=default;
            Time.fixedTime=3; jump.TickNative(c); Check(jump.WantsNative);
            Time.fixedTime=3.24f; jump.TickNative(c); Check(!jump.WantsNative);
        });
        test("a native refusal returns without spending or retrying the bonus allowance", () =>
        {
            var (jump,c,g)=Setup(); g.flags=Ready; c.refs.movement.AcceptJump=false;
            Press(jump,c,g); jump.TickNative(c); jump.TickNative(c);
            Check(!jump.WantsNative && c.data.extraJumps==1 && c.refs.movement.Attempts==1);
        });
        test("creative, flight, swimming, screens, death and old profiles never request native jump", () =>
        {
            foreach (uint flag in new uint[]{0x100,0x80,0x40,2,0x20,0x200})
            {
                var (jump,c,g)=Setup();
                g.flags=flag==0x200 ? 1u : Ready|flag;
                Press(jump,c,g); Check(!jump.WantsNative);
            }
        });
        test("no passive, no remaining allowance and native cooldown each block the request", () =>
        {
            foreach (int condition in new[]{0,1,2})
            {
                var(jump,c,g)=Setup(); g.flags=Ready;
                if(condition==0)c.refs.afflictions.totalExtraJumps=0;
                if(condition==1)c.data.extraJumps=0;
                if(condition==2)c.data.sinceJump=0.05f;
                Press(jump,c,g); Check(!jump.WantsNative);
            }
        });
        test("screen, mode, death, passout, warp and loading cancel a native jump request", () =>
        {
            foreach(int condition in new[]{0,1,2,3,4,5})
            {
                var(jump,c,g)=Setup(); g.flags=Ready; Press(jump,c,g); Check(jump.WantsNative);
                if(condition==2)c.data.dead=true;
                if(condition==3)c.data.passedOut=true;
                if(condition==4)LoadingScreenHandler.loading=true;
                if(condition==5)c.warping=true;
                jump.UpdateRequest(condition!=1,condition==0,c,g,Owner.NativeControl);
                Check(!jump.WantsNative);
            }
        });
        test("physical Space held while unfocused does not become a press on focus recovery", () =>
        {
            var(jump,c,g)=Setup(); g.flags=Ready;
            Application.isFocused=false; Keyboard.current[Key.Space].isPressed=true;
            jump.UpdateRequest(true,false,c,g,Owner.MinecraftOwns);
            Application.isFocused=true;
            jump.UpdateRequest(true,false,c,g,Owner.MinecraftOwns);
            Check(!jump.WantsNative);
        });
        test("dev jump follows the same native allowance and validation", () =>
        {
            var(jump,c,g)=Setup(); g.flags=Ready; Application.isFocused=false;
            jump.DevJump(); jump.UpdateRequest(true,false,c,g,Owner.MinecraftOwns);
            Check(jump.WantsNative); jump.TickNative(c); Check(c.data.extraJumps==0);
        });
        test("invalid or discontinuous guest motion cannot inject unbounded native velocity", () =>
        {
            foreach(double x in new[]{double.NaN,double.PositiveInfinity,1000000.0})
            {
                var(jump,c,g)=Setup(); g.flags=Ready; g.tickMs=50; g.curX=x;
                Press(jump,c,g); jump.TickNative(c);
                float velocity=c.refs.ragdoll.partList[0].Rig.linearVelocity.x;
                Check(!float.IsNaN(velocity) && !float.IsInfinity(velocity) && velocity<=30);
            }
        });
        test("an original native super-jump keeps the body through its force interval and landing", () =>
        {
            var(jump,c,g)=Setup();
            jump.RetainNativeFlight(c,3);
            Check(jump.WantsNative && jump.RetainingFlight);
            c.data.isGrounded=true;
            Time.fixedTime=1; jump.TickNative(c); Check(jump.WantsNative);
            Time.fixedTime=2.99f; jump.TickNative(c); Check(jump.WantsNative);
            Time.fixedTime=3.01f; jump.TickNative(c); Check(jump.WantsNative);
            Time.fixedTime=3.34f; jump.TickNative(c);
            Check(!jump.WantsNative && !jump.RetainingFlight && c.refs.movement.Attempts==0);
            Check(c.data.extraJumps==1);
        });
        test("overlapping original super-jumps extend force retention without replaying an action", () =>
        {
            var(jump,c,g)=Setup();
            jump.RetainNativeFlight(c,3);
            Time.fixedTime=1; jump.RetainNativeFlight(c,0);
            Time.fixedTime=2; jump.RetainNativeFlight(c,3);
            c.data.isGrounded=true;
            Time.fixedTime=4; jump.TickNative(c); Check(jump.WantsNative);
            Time.fixedTime=5.01f; jump.TickNative(c); Check(jump.WantsNative);
            Time.fixedTime=5.35f; jump.TickNative(c);
            Check(!jump.WantsNative && c.refs.movement.Attempts==0);
        });
        test("native super-jump retention obeys mode, screen, death and loading cancellation", () =>
        {
            foreach(int condition in new[]{0,1,2,3})
            {
                var(jump,c,g)=Setup(); jump.RetainNativeFlight(c,10);
                if(condition==2)c.data.dead=true;
                if(condition==3)LoadingScreenHandler.loading=true;
                jump.UpdateRequest(condition!=0,condition==1,c,g,Owner.NativeControl);
                Check(!jump.WantsNative && !jump.RetainingFlight);
            }
        });
        test("invalid native force durations cannot leave retained ownership stuck", () =>
        {
            foreach(float seconds in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            {
                var(jump,c,g)=Setup(); jump.RetainNativeFlight(c,seconds);
                Check(!jump.WantsNative);
            }
        });
    }
    private static (NativeJump jump,Character c,Proto.GuestState g) Setup()
    {
        Time.unscaledTime=Time.fixedTime=0; Application.isFocused=true; LoadingScreenHandler.loading=false;
        Keyboard.current=new(); Plugin.Instance=new();
        var c=new Character(); c.refs.afflictions.totalExtraJumps=1;
        var g=new Proto.GuestState{flags=Ready|(uint)Proto.GuestFlags.OnGround};
        var jump=new NativeJump(); jump.UpdateRequest(true,false,c,g,Owner.MinecraftOwns);
        return(jump,c,g);
    }
    private static void Press(NativeJump jump,Character c,Proto.GuestState g)
    {
        Keyboard.current[Key.Space].isPressed=true;
        jump.UpdateRequest(true,false,c,g,Owner.MinecraftOwns);
    }
    private static void Check(bool valid){if(!valid)throw new Exception("native extra jump regression");}
}
