package dev.skycraft.client;

import dev.skycraft.world.SkyCollision;
import dev.skycraft.world.TriCollider;
import java.util.ArrayList;
import java.util.List;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.phys.AABB;
import net.minecraft.world.phys.Vec3;

public final class SkyCollider {
   private SkyCollider() {
   }

   public static Vec3 collide(LocalPlayer var0, Vec3 var1) {
      AABB var2 = var0.getBoundingBox();
      double var3 = var0.maxUpStep();
      ArrayList var5 = new ArrayList();
      SkyCollision.trianglesNear(var2.expandTowards(var1).inflate(1.0, 1.0 + var3, 1.0), var5);
      if (var5.isEmpty()) {
         return var1;
      }

      double[] var6 = TriCollider.resolve(
         var5,
         (var2.minX + var2.maxX) * 0.5,
         var2.minY,
         (var2.minZ + var2.maxZ) * 0.5,
         var2.getXsize() * 0.5,
         var2.getYsize(),
         var3,
         var0.onGround(),
         var1.x,
         var1.y,
         var1.z
      );
      return var6[0] == var1.x && var6[1] == var1.y && var6[2] == var1.z
         ? var1
         : Entity.collideBoundingBox(var0, new Vec3(var6[0], var6[1], var6[2]), var2, var0.level(), List.of());
   }

   public static double groundAt(double var0, double var2, double var4, double var6) {
      ArrayList var8 = new ArrayList();
      SkyCollision.trianglesNear(new AABB(var0 - 1.0, var2 - 4.0, var4 - 1.0, var0 + 1.0, var2 + var6 + 1.0, var4 + 1.0), var8);
      return TriCollider.groundAt(var8, var0, var2, var4, var6);
   }
}
