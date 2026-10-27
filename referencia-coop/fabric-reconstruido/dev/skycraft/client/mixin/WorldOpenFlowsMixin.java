package dev.skycraft.client.mixin;

import com.llamalad7.mixinextras.injector.wrapoperation.Operation;
import com.llamalad7.mixinextras.injector.wrapoperation.WrapOperation;
import net.minecraft.client.gui.screens.worldselection.WorldOpenFlows;
import net.minecraft.world.level.storage.LevelStorageSource.LevelStorageAccess;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;

@Mixin(WorldOpenFlows.class)
public abstract class WorldOpenFlowsMixin {
   @WrapOperation(
      method = "openWorldCheckWorldStemCompatibility",
      at = @At(
         value = "INVOKE",
         target = "Lnet/minecraft/client/gui/screens/worldselection/WorldOpenFlows;askForBackup(Lnet/minecraft/world/level/storage/LevelStorageSource$LevelStorageAccess;ZLjava/lang/Runnable;Ljava/lang/Runnable;)V"
      )
   )
   private void skycraft$skipBackupPrompt(WorldOpenFlows var1, LevelStorageAccess var2, boolean var3, Runnable var4, Runnable var5, Operation<Void> var6) {
      if ("SkyCraft".equals(var2.getLevelId())) {
         var4.run();
      } else {
         var6.call(new Object[]{var1, var2, var3, var4, var5});
      }
   }
}
