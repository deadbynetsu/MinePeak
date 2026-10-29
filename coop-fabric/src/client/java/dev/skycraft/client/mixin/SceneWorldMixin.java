package dev.skycraft.client.mixin;

import dev.skycraft.client.MirrorWorld;
import dev.skycraft.client.SkyClient;
import dev.skycraft.client.scene.SceneWorlds;
import net.minecraft.client.Minecraft;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.ModifyArg;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(value = MirrorWorld.class, remap = false)
abstract class MirrorWorldScenePaths {
    @ModifyArg(method="openWhenReady", at=@At(value="INVOKE", target="Lnet/minecraft/world/level/storage/LevelStorageSource;levelExists(Ljava/lang/String;)Z"), index=0)
    private static String peakcraft$exists(String name) { return SceneWorlds.savePath(name); }
    @ModifyArg(method="openWhenReady", at=@At(value="INVOKE", target="Lnet/minecraft/client/gui/screens/worldselection/WorldOpenFlows;openWorld(Ljava/lang/String;Ljava/lang/Runnable;)V"), index=0)
    private static String peakcraft$open(String name) { return SceneWorlds.savePath(name); }
    @ModifyArg(method="openWhenReady", at=@At(value="INVOKE", target="Lnet/minecraft/client/gui/screens/worldselection/WorldOpenFlows;createFreshLevel(Ljava/lang/String;Lnet/minecraft/world/level/LevelSettings;Lnet/minecraft/world/level/levelgen/WorldOptions;Ljava/util/function/Function;Lnet/minecraft/client/gui/screens/Screen;)V"), index=0)
    private static String peakcraft$create(String name) { return SceneWorlds.savePath(name); }
}

@Mixin(value = SkyClient.class, remap = false)
abstract class SceneWorldMixin {
    @Inject(method="beginFrame", at=@At(value="INVOKE", target="Ldev/skycraft/client/MirrorWorld;openWhenReady(Lnet/minecraft/client/Minecraft;)V"), cancellable=true)
    private static void peakcraft$scene(CallbackInfo ci) {
        if (!SceneWorlds.beforeWorldFrame(Minecraft.getInstance())) {
            dev.skycraft.client.InputBridge.releaseAll();
            // Input entries carry no scene ID. Discard already queued old-scene
            // actions while C# retains new-scene remote edits behind its ready fence.
            dev.skycraft.link.SkyLink.drainInput((type, flags, a, b, c) -> {});
            ci.cancel();
        }
    }

    @Inject(method="afterRender", at=@At("HEAD"), cancellable=true)
    private static void peakcraft$renderScene(CallbackInfo ci) {
        if (!SceneWorlds.levelReady(Minecraft.getInstance())) ci.cancel();
    }

    @Inject(method="publishTick", at=@At("HEAD"), cancellable=true)
    private static void peakcraft$poseScene(Minecraft minecraft, CallbackInfo ci) {
        if (!SceneWorlds.levelReady(minecraft)) ci.cancel();
    }
}
