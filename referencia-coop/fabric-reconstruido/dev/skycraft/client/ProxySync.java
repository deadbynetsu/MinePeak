package dev.skycraft.client;

import dev.skycraft.combat.SkyrimActorEntity;
import dev.skycraft.link.SkyLink;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import net.minecraft.client.Minecraft;
import net.minecraft.world.entity.Entity;

final class ProxySync {
   private static final List<SkyLink.Actor> ACTORS = new ArrayList<>();
   private static final Map<Integer, SkyLink.Actor> BY_ID = new HashMap<>();

   private ProxySync() {
   }

   static void frame(Minecraft var0) {
      if (var0.level != null && SkyLink.readActors(ACTORS)) {
         BY_ID.clear();

         for (SkyLink.Actor var2 : ACTORS) {
            BY_ID.put(var2.formId(), var2);
         }

         for (Entity var6 : var0.level.entitiesForRendering()) {
            if (var6 instanceof SkyrimActorEntity var3) {
               SkyLink.Actor var4 = BY_ID.get(var3.formId());
               if (var4 != null) {
                  var3.setSize(var4.width(), var4.height());
                  var3.setPos(var4.x(), var4.y(), var4.z());
                  var3.xo = var4.x();
                  var3.yo = var4.y();
                  var3.zo = var4.z();
                  var3.setYRot(var4.yaw());
                  var3.yRotO = var4.yaw();
               }
            }
         }
      }
   }
}
