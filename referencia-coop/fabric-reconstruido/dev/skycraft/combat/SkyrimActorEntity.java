package dev.skycraft.combat;

import net.minecraft.network.syncher.EntityDataAccessor;
import net.minecraft.network.syncher.EntityDataSerializers;
import net.minecraft.network.syncher.SynchedEntityData;
import net.minecraft.network.syncher.SynchedEntityData.Builder;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.sounds.SoundEvent;
import net.minecraft.tags.DamageTypeTags;
import net.minecraft.tags.ItemTags;
import net.minecraft.world.damagesource.DamageSource;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.EntityDimensions;
import net.minecraft.world.entity.EntityType;
import net.minecraft.world.entity.HumanoidArm;
import net.minecraft.world.entity.LivingEntity;
import net.minecraft.world.entity.Pose;
import net.minecraft.world.entity.projectile.Projectile;
import net.minecraft.world.entity.projectile.arrow.ThrownTrident;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.Level;
import org.jspecify.annotations.Nullable;

public class SkyrimActorEntity extends LivingEntity {
   private static final EntityDataAccessor<Integer> FORM_ID = SynchedEntityData.defineId(SkyrimActorEntity.class, EntityDataSerializers.INT);
   private static final EntityDataAccessor<Float> WIDTH = SynchedEntityData.defineId(SkyrimActorEntity.class, EntityDataSerializers.FLOAT);
   private static final EntityDataAccessor<Float> HEIGHT = SynchedEntityData.defineId(SkyrimActorEntity.class, EntityDataSerializers.FLOAT);
   private float pendingDamage;
   private int pendingFlags;
   private int pendingWeapon;
   private double pushX;
   private double pushZ;
   private float pushStrength;
   private boolean hitThisTick;

   public SkyrimActorEntity(EntityType<? extends SkyrimActorEntity> var1, Level var2) {
      super(var1, var2);
      this.setNoGravity(true);
      this.noPhysics = true;
      this.setInvisible(true);
      this.setSilent(true);
   }

   public int formId() {
      return this.entityData.get(FORM_ID);
   }

   public void setFormId(int var1) {
      this.entityData.set(FORM_ID, var1);
   }

   @Override
   protected void defineSynchedData(Builder var1) {
      super.defineSynchedData(var1);
      var1.define(FORM_ID, 0);
      var1.define(WIDTH, 0.6F);
      var1.define(HEIGHT, 1.8F);
   }

   public void setSize(float var1, float var2) {
      if (Math.abs(this.entityData.get(WIDTH) - var1) > 0.01F || Math.abs(this.entityData.get(HEIGHT) - var2) > 0.01F) {
         this.entityData.set(WIDTH, var1);
         this.entityData.set(HEIGHT, var2);
         this.refreshDimensions();
      }
   }

   @Override
   public void onSyncedDataUpdated(EntityDataAccessor<?> var1) {
      super.onSyncedDataUpdated(var1);
      if (WIDTH.equals(var1) || HEIGHT.equals(var1)) {
         this.refreshDimensions();
      }
   }

   @Override
   protected EntityDimensions getDefaultDimensions(Pose var1) {
      return EntityDimensions.scalable(this.entityData.get(WIDTH), this.entityData.get(HEIGHT));
   }

   @Override
   protected void actuallyHurt(ServerLevel var1, DamageSource var2, float var3) {
      if (!this.isInvulnerableTo(var1, var2) && !(var3 <= 0.0F)) {
         this.pendingDamage += var3;
         if (var2.getDirectEntity() instanceof Projectile) {
            this.pendingFlags |= 2;
         }

         this.pendingWeapon = weaponClass(var2);
         if (var2.is(DamageTypeTags.IS_FIRE)) {
            this.pendingFlags |= 8;
         }

         this.hitThisTick = true;
         this.getCombatTracker().recordDamage(var2, var3);
      }
   }

   @Override
   public void knockback(double var1, double var3, double var5, DamageSource var7, float var8, boolean var9) {
      double var10 = Math.sqrt(var3 * var3 + var5 * var5);
      if (var10 > 1.0E-6 && var1 > this.pushStrength) {
         this.pushStrength = (float)var1;
         this.pushX = -var3 / var10;
         this.pushZ = -var5 / var10;
      }

      this.hitThisTick = true;
   }

   public void markCritical() {
      this.pendingFlags |= 1;
   }

   private static int weaponClass(DamageSource var0) {
      if (var0.getDirectEntity() instanceof ThrownTrident) {
         return 4;
      }

      if (var0.getDirectEntity() instanceof Projectile) {
         return 5;
      }

      ItemStack var1 = var0.getWeaponItem();
      if (var1 == null && var0.getEntity() instanceof LivingEntity var2) {
         var1 = var2.getMainHandItem();
      }

      if (var1 == null || var1.isEmpty()) {
         return 0;
      } else if (var1.is(ItemTags.SWORDS)) {
         return 1;
      } else if (var1.is(ItemTags.AXES)) {
         return 2;
      } else {
         return var1.is(Items.TRIDENT) ? 4 : 3;
      }
   }

   public float[] takeHit() {
      if (!this.hitThisTick) {
         return null;
      }

      float[] var1 = new float[]{
         this.pendingDamage,
         (float)this.pushX,
         (float)this.pushZ,
         this.pushStrength,
         Float.intBitsToFloat(this.pendingFlags),
         Float.intBitsToFloat(this.pendingWeapon)
      };
      this.pendingDamage = 0.0F;
      this.pendingFlags = 0;
      this.pushX = this.pushZ = 0.0;
      this.pushStrength = 0.0F;
      this.hitThisTick = false;
      return var1;
   }

   @Override
   public void tick() {
      this.baseTick();
      this.setHealth(this.getMaxHealth());
   }

   @Override
   public boolean isPushable() {
      return false;
   }

   @Override
   protected void doPush(Entity var1) {
   }

   @Override
   public boolean canBeCollidedWith(@Nullable Entity var1) {
      return false;
   }

   @Override
   public boolean shouldShowName() {
      return false;
   }

   @Override
   public boolean shouldBeSaved() {
      return false;
   }

   @Override
   protected @Nullable SoundEvent getHurtSound(DamageSource var1) {
      return null;
   }

   @Override
   protected @Nullable SoundEvent getDeathSound() {
      return null;
   }

   @Override
   public HumanoidArm getMainArm() {
      return HumanoidArm.RIGHT;
   }
}
