package dev.skycraft.mixin;

import dev.skycraft.link.SkyLink;
import dev.skycraft.world.SkyCollision;
import dev.skycraft.world.TriCollider;
import java.util.ArrayList;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.phys.AABB;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(Player.class)
public abstract class PlayerEdgeMixin {
   @Inject(method = "canFallAtLeast", at = @At("RETURN"), cancellable = true)
   private void skycraft$hostGroundHoldsToo(double var1, double var3, double var5, CallbackInfoReturnable<Boolean> var7) {
      Player var8 = (Player)this;
      if ((Boolean)var7.getReturnValue() && SkyLink.active() && SkyCollision.usesSmoothCollider(var8)) {
         AABB var9 = var8.getBoundingBox();
         double var10 = (var9.minX + var9.maxX) * 0.5 + var1;
         double var12 = (var9.minZ + var9.maxZ) * 0.5 + var3;
         double var14 = var9.minY;
         ArrayList var16 = new ArrayList();
         SkyCollision.trianglesNear(new AABB(var10 - 1.0, var14 - var5 - 1.0, var12 - 1.0, var10 + 1.0, var14 + 1.0, var12 + 1.0), var16);
         if (!var16.isEmpty()) {
            double var17 = TriCollider.groundAt(var16, var10, var14, var12, 0.05);
            if (!Double.isNaN(var17) && var17 >= var14 - var5 - 1.0E-4) {
               var7.setReturnValue(false);
            }
         }
      }
   }
}
